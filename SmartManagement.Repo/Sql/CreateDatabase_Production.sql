-- ============================================================
-- SmartOps: Production Database Build Script
-- Run against an empty SmartOps database.
-- Creates all tables, constraints, indexes, and seed data
-- in dependency order (no FK violations).
--
-- Table creation order:
--   1.  EtimeShifts               (no inbound FKs)
--   2.  LineAdherence             (no inbound FKs)
--   3.  TimeOffRequestStatus      (lookup, no inbound FKs)
--   4.  ScheduleExceptionType     (lookup, no inbound FKs)
--   5.  ScheduleTemplate          (no inbound FKs)
--   6.  ScheduleShiftPattern      (FK → ScheduleTemplate)
--   7.  TimeOffRequest            (FK → TimeOffRequestStatus)
--   8.  ScheduleException         (FK → ScheduleTemplate,
--                                       ScheduleExceptionType,
--                                       TimeOffRequest)
--   9.  FK: TimeOffRequest → ScheduleException  (added last
--          to resolve the circular reference)
--  10.  WorkGroup                 (no inbound FKs)
--  11.  WorkGroupMember           (FK → WorkGroup; AD login as employee key)
-- ============================================================

USE [SmartOps]
GO

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
GO

-- ============================================================
-- 1. EtimeShifts
--    Raw import staging table populated from the Etime/UKG
--    workforce system.  All other schedule logic is derived
--    from this source data.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'EtimeShifts' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[EtimeShifts] (
        [EtimeShiftId]  INT            NOT NULL IDENTITY(1,1),
        [ShiftCodeId]   INT            NOT NULL,
        [ADLoginName]   NVARCHAR(200)  NOT NULL,
        [PersonNum]     INT            NOT NULL,
        [EmplId]        INT            NOT NULL,
        [FileNumber]    INT            NOT NULL,
        [PayGroup]      NVARCHAR(50)   NULL,
        [PayCodeId]     INT            NULL,
        [PayCode]       NVARCHAR(50)   NULL,
        [ShiftStart]    DATETIME       NOT NULL,
        [ShiftEnd]      DATETIME       NOT NULL,
        [BreakMin]      INT            NOT NULL CONSTRAINT [DF_EtimeShifts_BreakMin] DEFAULT 0,

        CONSTRAINT [PK_EtimeShifts] PRIMARY KEY ([EtimeShiftId])
    );

    CREATE INDEX [IX_EtimeShifts_ADLoginName_ShiftStart]
        ON [dbo].[EtimeShifts] ([ADLoginName], [ShiftStart])
        INCLUDE ([ShiftEnd], [PayGroup], [PayCode], [FileNumber]);

    CREATE INDEX [IX_EtimeShifts_ShiftStart]
        ON [dbo].[EtimeShifts] ([ShiftStart], [ShiftEnd]);

    PRINT 'Created table: EtimeShifts';
END
ELSE
    PRINT 'Skipped: EtimeShifts already exists';
GO

-- ============================================================
-- 2. LineAdherence
--    Stores required staffing levels per client per time slot.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'LineAdherence' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[LineAdherence] (
        [LineAdherenceId] INT           NOT NULL IDENTITY(1,1),
        [ClientAbbr]      NVARCHAR(10)  NOT NULL,
        [RequiredDate]    DATE          NOT NULL,
        [RequiredTime]    TIME(0)       NOT NULL,
        [RequiredHours]   INT           NOT NULL,
        [InsertedUtcDate] DATETIME      NOT NULL CONSTRAINT [DF_LineAdherence_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedDate] DATETIME      NOT NULL CONSTRAINT [DF_LineAdherence_Updated]  DEFAULT GETUTCDATE(),
        [LastTimestamp]   ROWVERSION    NOT NULL,

        CONSTRAINT [PK_LineAdherence] PRIMARY KEY ([LineAdherenceId])
    );

    CREATE INDEX [IX_LineAdherence_ClientAbbr_RequiredDate]
        ON [dbo].[LineAdherence] ([ClientAbbr], [RequiredDate], [RequiredTime]);

    PRINT 'Created table: LineAdherence';
END
ELSE
    PRINT 'Skipped: LineAdherence already exists';
GO

-- ============================================================
-- 3. TimeOffRequestStatus  (lookup)
--    1=Pending  2=Approved  3=Denied  4=Cancelled
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'TimeOffRequestStatus' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[TimeOffRequestStatus] (
        [StatusId]   TINYINT      NOT NULL,
        [StatusName] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK_TimeOffRequestStatus] PRIMARY KEY ([StatusId])
    );

    INSERT INTO [dbo].[TimeOffRequestStatus] ([StatusId], [StatusName]) VALUES
        (1, 'Pending'),
        (2, 'Approved'),
        (3, 'Denied'),
        (4, 'Cancelled');

    PRINT 'Created table: TimeOffRequestStatus';
END
ELSE
    PRINT 'Skipped: TimeOffRequestStatus already exists';
GO

-- ============================================================
-- 4. ScheduleExceptionType  (lookup)
--    Describes what kind of override a ScheduleException is.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleExceptionType' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleExceptionType] (
        [ExceptionTypeId] TINYINT      NOT NULL,
        [TypeName]        NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK_ScheduleExceptionType] PRIMARY KEY ([ExceptionTypeId])
    );

    INSERT INTO [dbo].[ScheduleExceptionType] ([ExceptionTypeId], [TypeName]) VALUES
        (1, 'TimeOff'),
        (2, 'ModifiedHours'),
        (3, 'Holiday'),
        (4, 'LeaveOfAbsence'),
        (5, 'Training');

    PRINT 'Created table: ScheduleExceptionType';
END
ELSE
    PRINT 'Skipped: ScheduleExceptionType already exists';
GO

-- ============================================================
-- 5. ScheduleTemplate
--    One row per schedule version per employee.
--    EffectiveDate = when this version became active.
--    EndDate NULL  = currently active (no end date yet).
--    When a schedule permanently changes, close the current
--    row (set EndDate) and insert a new one.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleTemplate' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleTemplate] (
        [ScheduleTemplateId] INT           NOT NULL IDENTITY(1,1),
        [AdloginName]        NVARCHAR(100) NOT NULL,
        [ExternalMatchId]    NVARCHAR(50)  NOT NULL,   -- FileNumber from Etime
        [PayGroup]           NVARCHAR(50)  NOT NULL,
        [EffectiveDate]      DATE          NOT NULL,   -- first day this template applies
        [EndDate]            DATE          NULL,        -- last day; NULL = still active
        [InsertedDateUtc]    DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Updated]  DEFAULT GETUTCDATE(),
        [Timestamp]          ROWVERSION    NOT NULL,

        CONSTRAINT [PK_ScheduleTemplate] PRIMARY KEY ([ScheduleTemplateId]),
        CONSTRAINT [CK_ScheduleTemplate_DateOrder]
            CHECK ([EndDate] IS NULL OR [EndDate] >= [EffectiveDate])
    );

    -- Enforce: only one active template per employee at any time
    CREATE UNIQUE INDEX [UX_ScheduleTemplate_OneActivePerEmployee]
        ON [dbo].[ScheduleTemplate] ([AdloginName])
        WHERE [EndDate] IS NULL;

    -- Fast lookup for "get template active on date X for employee Y"
    CREATE INDEX [IX_ScheduleTemplate_AdloginName_EffectiveDate]
        ON [dbo].[ScheduleTemplate] ([AdloginName], [EffectiveDate])
        INCLUDE ([PayGroup], [ExternalMatchId], [EndDate]);

    PRINT 'Created table: ScheduleTemplate';
END
ELSE
    PRINT 'Skipped: ScheduleTemplate already exists';
GO

-- ============================================================
-- 6. ScheduleShiftPattern
--    Day-of-week shift times that belong to a template.
--    Uses TIME(0) — no specific calendar date is stored.
--    DayOfWeek: 0=Sunday 1=Monday 2=Tuesday 3=Wednesday
--               4=Thursday 5=Friday 6=Saturday
--    BreakMinutes replaces the old ShiftBreak table.
--    Cascade delete: removing a template removes its patterns.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleShiftPattern' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleShiftPattern] (
        [ShiftPatternId]     INT          NOT NULL IDENTITY(1,1),
        [ScheduleTemplateId] INT          NOT NULL,
        [DayOfWeek]          TINYINT      NOT NULL,   -- 0–6
        [ShiftSequence]      TINYINT      NOT NULL CONSTRAINT [DF_ShiftPattern_Sequence] DEFAULT 1,
                                                      -- 1 = normal/start portion, 2 = post-midnight continuation
        [ShiftStartTime]     TIME(0)      NOT NULL,   -- e.g. 07:00:00
        [ShiftEndTime]       TIME(0)      NOT NULL,   -- e.g. 15:00:00
        [BreakMinutes]       INT          NOT NULL CONSTRAINT [DF_ShiftPattern_BreakMin] DEFAULT 0,
        [PayCode]            NVARCHAR(50) NULL,
        [InsertedDateUtc]    DATETIME     NOT NULL CONSTRAINT [DF_ShiftPattern_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]     DATETIME     NOT NULL CONSTRAINT [DF_ShiftPattern_Updated]  DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_ScheduleShiftPattern]   PRIMARY KEY ([ShiftPatternId]),
        CONSTRAINT [FK_ShiftPattern_Template]  FOREIGN KEY ([ScheduleTemplateId])
            REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId])
            ON DELETE CASCADE,
        CONSTRAINT [CK_ShiftPattern_DayOfWeek]    CHECK ([DayOfWeek] BETWEEN 0 AND 6),
        CONSTRAINT [CK_ShiftPattern_BreakMinutes] CHECK ([BreakMinutes] >= 0),
        CONSTRAINT [CK_ShiftPattern_Times]
            CHECK ([ShiftEndTime] > [ShiftStartTime]),  -- overnight shifts are split at midnight so each portion is valid

        -- One shift pattern row per (day, sequence) per template
        -- Sequence 1 = normal/start portion, Sequence 2 = post-midnight continuation of an overnight shift
        CONSTRAINT [UX_ShiftPattern_DaySeqPerTemplate]
            UNIQUE ([ScheduleTemplateId], [DayOfWeek], [ShiftSequence])
    );

    PRINT 'Created table: ScheduleShiftPattern';
END
ELSE
    PRINT 'Skipped: ScheduleShiftPattern already exists';
GO

-- ============================================================
-- 7. TimeOffRequest
--    Submitted by employees; reviewed by supervisors.
--    StartDate/EndDate = the requested off period (can span
--    multiple days for vacation).
--    IsPartialShift / PartialStart / PartialEnd allow the
--    employee to request only part of a shift off (on-the-hour
--    or half-hour boundaries only).
--    PlanToMakeUpTime / MakeUpStart / MakeUpEnd let the
--    employee indicate when they plan to make up the missed time.
--    ScheduleExceptionId is populated when the request is
--    approved and a ScheduleException row is created.
--    (The FK to ScheduleException is added in Step 9 after
--    ScheduleException is created.)
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'TimeOffRequest' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[TimeOffRequest] (
        [TimeOffRequestId]    INT           NOT NULL IDENTITY(1,1),
        [AdloginName]         NVARCHAR(100) NOT NULL,
        [StartDate]           DATE          NOT NULL,   -- first day of requested time off
        [EndDate]             DATE          NOT NULL,   -- last day of requested time off
        [Reason]              NVARCHAR(500) NOT NULL,
        [StatusId]            TINYINT       NOT NULL CONSTRAINT [DF_TimeOffRequest_Status] DEFAULT 1,
        [RequestedOn]         DATETIME      NOT NULL CONSTRAINT [DF_TimeOffRequest_RequestedOn] DEFAULT GETUTCDATE(),

        -- Supervisor review
        [ReviewedBy]          NVARCHAR(100) NULL,
        [ReviewedOn]          DATETIME      NULL,
        [ReviewNotes]         NVARCHAR(500) NULL,

        -- Populated when approved and ScheduleException is created
        [ScheduleExceptionId] INT           NULL,

        -- Partial-shift request (hour/half-hour boundaries only)
        [IsPartialShift]      BIT           NOT NULL CONSTRAINT [DF_TimeOffRequest_IsPartialShift] DEFAULT 0,
        [PartialStart]        TIME          NULL,       -- start of the partial window
        [PartialEnd]          TIME          NULL,       -- end of the partial window

        -- Make-up time
        [PlanToMakeUpTime]    BIT           NOT NULL CONSTRAINT [DF_TimeOffRequest_PlanToMakeUpTime] DEFAULT 0,
        [MakeUpStart]         DATETIME      NULL,       -- start of the make-up block
        [MakeUpEnd]           DATETIME      NULL,       -- end of the make-up block

        -- Audit
        [InsertedDateUtc]     DATETIME      NOT NULL CONSTRAINT [DF_TimeOffRequest_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]      DATETIME      NOT NULL CONSTRAINT [DF_TimeOffRequest_Updated]  DEFAULT GETUTCDATE(),
        [Timestamp]           ROWVERSION    NOT NULL,

        CONSTRAINT [PK_TimeOffRequest]           PRIMARY KEY ([TimeOffRequestId]),
        CONSTRAINT [FK_TimeOffRequest_Status]    FOREIGN KEY ([StatusId])
            REFERENCES [dbo].[TimeOffRequestStatus] ([StatusId]),
        CONSTRAINT [CK_TimeOffRequest_DateOrder] CHECK ([EndDate] >= [StartDate]),
        CONSTRAINT [CK_TimeOffRequest_ReviewFields]
            CHECK (
                ([ReviewedBy] IS NULL AND [ReviewedOn] IS NULL)
                OR ([ReviewedBy] IS NOT NULL AND [ReviewedOn] IS NOT NULL)
            ),
        CONSTRAINT [CK_TimeOffRequest_PartialShiftTimes]
            CHECK (
                [IsPartialShift] = 0
                OR ([PartialStart] IS NOT NULL AND [PartialEnd] IS NOT NULL AND [PartialEnd] > [PartialStart])
            ),
        CONSTRAINT [CK_TimeOffRequest_MakeUpRange]
            CHECK (
                [PlanToMakeUpTime] = 0
                OR ([MakeUpStart] IS NOT NULL AND [MakeUpEnd] IS NOT NULL AND [MakeUpEnd] > [MakeUpStart])
            )
    );

    -- Employee view: their own requests by status
    CREATE INDEX [IX_TimeOffRequest_AdloginName_StatusId]
        ON [dbo].[TimeOffRequest] ([AdloginName], [StatusId])
        INCLUDE ([StartDate], [EndDate], [RequestedOn]);

    -- Supervisor queue: pending requests ordered by requested start date
    CREATE INDEX [IX_TimeOffRequest_StatusId_StartDate]
        ON [dbo].[TimeOffRequest] ([StatusId], [StartDate])
        INCLUDE ([AdloginName], [Reason], [RequestedOn]);

    -- Date range lookup for a specific employee
    CREATE INDEX [IX_TimeOffRequest_AdloginName_StartDate]
        ON [dbo].[TimeOffRequest] ([AdloginName], [StartDate], [EndDate]);

    PRINT 'Created table: TimeOffRequest';
END
ELSE
    PRINT 'Skipped: TimeOffRequest already exists';
GO

-- ============================================================
-- 8. ScheduleException
--    Date-range overrides that suppress or replace the
--    employee's template shifts for specific days.
--    TimeOffRequestId is set when the exception originated
--    from an approved time-off request.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleException' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleException] (
        [ScheduleExceptionId] INT           NOT NULL IDENTITY(1,1),
        [AdloginName]         NVARCHAR(100) NOT NULL,
        [ScheduleTemplateId]  INT           NOT NULL,
        [ExceptionTypeId]     TINYINT       NOT NULL,
        [StartDate]           DATE          NOT NULL,
        [EndDate]             DATE          NOT NULL,
        [TimeOffRequestId]    INT           NULL,       -- set when exception comes from an approved request
        [Notes]               NVARCHAR(500) NULL,
        [CreatedBy]           NVARCHAR(100) NOT NULL,
        [InsertedDateUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]      DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Updated]  DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_ScheduleException]         PRIMARY KEY ([ScheduleExceptionId]),
        CONSTRAINT [FK_Exception_Template]        FOREIGN KEY ([ScheduleTemplateId])
            REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId]),
        CONSTRAINT [FK_Exception_Type]            FOREIGN KEY ([ExceptionTypeId])
            REFERENCES [dbo].[ScheduleExceptionType] ([ExceptionTypeId]),
        CONSTRAINT [FK_Exception_TimeOffRequest]  FOREIGN KEY ([TimeOffRequestId])
            REFERENCES [dbo].[TimeOffRequest] ([TimeOffRequestId]),
        CONSTRAINT [CK_Exception_DateOrder]       CHECK ([EndDate] >= [StartDate])
    );

    -- Fast date-range overlap query:
    -- "are there any exceptions for employee X between date A and date B?"
    CREATE INDEX [IX_ScheduleException_AdloginName_StartDate]
        ON [dbo].[ScheduleException] ([AdloginName], [StartDate], [EndDate])
        INCLUDE ([ExceptionTypeId], [ScheduleTemplateId]);

    -- Lookup all exceptions attached to a template
    CREATE INDEX [IX_ScheduleException_TemplateId]
        ON [dbo].[ScheduleException] ([ScheduleTemplateId], [StartDate]);

    PRINT 'Created table: ScheduleException';
END
ELSE
    PRINT 'Skipped: ScheduleException already exists';
GO

-- ============================================================
-- 9. Close the circular reference:
--    TimeOffRequest.ScheduleExceptionId → ScheduleException
--    Added last because both tables must exist first.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_TimeOffRequest_ScheduleException'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD CONSTRAINT [FK_TimeOffRequest_ScheduleException]
            FOREIGN KEY ([ScheduleExceptionId])
            REFERENCES [dbo].[ScheduleException] ([ScheduleExceptionId]);

    PRINT 'Added FK: FK_TimeOffRequest_ScheduleException';
END
ELSE
    PRINT 'Skipped: FK_TimeOffRequest_ScheduleException already exists';
GO

-- ============================================================
-- 10. WorkGroup
--     A named group used to organize employees (e.g. a team,
--     queue, or unit).  IsActive allows soft-deletion.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'WorkGroup' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[WorkGroup] (
        [WorkGroupId]     INT            NOT NULL IDENTITY(1,1),
        [Name]            NVARCHAR(100)  NOT NULL,
        [Description]     NVARCHAR(500)  NULL,
        [IsActive]        BIT            NOT NULL CONSTRAINT [DF_WorkGroup_IsActive] DEFAULT 1,
        [InsertedDateUtc] DATETIME       NOT NULL CONSTRAINT [DF_WorkGroup_Inserted]  DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]  DATETIME       NOT NULL CONSTRAINT [DF_WorkGroup_Updated]   DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_WorkGroup]      PRIMARY KEY ([WorkGroupId]),
        CONSTRAINT [UX_WorkGroup_Name] UNIQUE ([Name])
    );

    PRINT 'Created table: WorkGroup';
END
ELSE
    PRINT 'Skipped: WorkGroup already exists';
GO

-- ============================================================
-- 11. WorkGroupMember
--     Lookup table: which employees belong to which WorkGroup.
--     AdloginName is the employee's AD account (same key used
--     throughout EtimeShifts, ScheduleTemplate, etc.).
--     A member row can be soft-removed via RemovedDateUtc
--     without losing history.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'WorkGroupMember' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[WorkGroupMember] (
        [WorkGroupMemberId] INT            NOT NULL IDENTITY(1,1),
        [WorkGroupId]       INT            NOT NULL,
        [AdloginName]       NVARCHAR(100)  NOT NULL,
        [AddedDateUtc]      DATETIME       NOT NULL CONSTRAINT [DF_WorkGroupMember_Added]   DEFAULT GETUTCDATE(),
        [RemovedDateUtc]    DATETIME       NULL,       -- NULL = currently active member
        [AddedBy]           NVARCHAR(100)  NOT NULL,
        [RemovedBy]         NVARCHAR(100)  NULL,

        CONSTRAINT [PK_WorkGroupMember]        PRIMARY KEY ([WorkGroupMemberId]),
        CONSTRAINT [FK_WorkGroupMember_Group]  FOREIGN KEY ([WorkGroupId])
            REFERENCES [dbo].[WorkGroup] ([WorkGroupId])
            ON DELETE CASCADE
    );

    -- An employee may only have one active membership per group at a time
    -- (filtered index; cannot be expressed as an inline CONSTRAINT in T-SQL)
    CREATE UNIQUE INDEX [UX_WorkGroupMember_ActivePerGroup]
        ON [dbo].[WorkGroupMember] ([WorkGroupId], [AdloginName])
        WHERE [RemovedDateUtc] IS NULL;

    -- Lookup: all active members of a group
    CREATE INDEX [IX_WorkGroupMember_WorkGroupId_Active]
        ON [dbo].[WorkGroupMember] ([WorkGroupId])
        INCLUDE ([AdloginName], [AddedDateUtc])
        WHERE [RemovedDateUtc] IS NULL;

    -- Lookup: all groups an employee belongs to
    CREATE INDEX [IX_WorkGroupMember_AdloginName_Active]
        ON [dbo].[WorkGroupMember] ([AdloginName])
        INCLUDE ([WorkGroupId], [AddedDateUtc])
        WHERE [RemovedDateUtc] IS NULL;

    PRINT 'Created table: WorkGroupMember';
END
ELSE
    PRINT 'Skipped: WorkGroupMember already exists';
GO

-- ============================================================
-- Verification: show all tables and column counts
-- ============================================================

SELECT
    t.name                                    AS TableName,
    COUNT(c.column_id)                        AS ColumnCount,
    (
        SELECT COUNT(*)
        FROM sys.indexes i
        WHERE i.object_id = t.object_id
          AND i.type > 0
    )                                         AS IndexCount,
    (
        SELECT COUNT(*)
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = t.object_id
    )                                         AS OutboundFKCount
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
  AND t.name IN (
      'EtimeShifts', 'LineAdherence',
      'TimeOffRequestStatus', 'ScheduleExceptionType',
      'ScheduleTemplate', 'ScheduleShiftPattern',
      'TimeOffRequest', 'ScheduleException',
      'WorkGroup', 'WorkGroupMember',
      'EmployeeAvailability', 'EmployeeAvailabilityDay'
  )
GROUP BY t.name, t.object_id
ORDER BY t.name;
GO

PRINT '=== CreateDatabase_Production complete ===';
GO
