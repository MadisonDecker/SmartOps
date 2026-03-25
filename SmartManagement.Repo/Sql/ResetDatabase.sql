-- ============================================================
-- SmartOps: Full Database Reset Script
--
-- WARNING: This script drops ALL SmartOps objects and
--          recreates them from scratch.  ALL DATA WILL BE
--          LOST.  Run only against a dev/test database or
--          when intentionally rebuilding a clean environment.
--
-- Object creation order:
--   1.  EtimeShifts               (no inbound FKs)
--   2.  LATDetails                (no inbound FKs)
--   3.  LineAdherence             (no inbound FKs)
--   4.  AlertContactMethod        (lookup, no inbound FKs)
--   5.  TimeOffRequestStatus      (lookup, no inbound FKs)
--   6.  ScheduleExceptionType     (lookup, no inbound FKs)
--   7.  ScheduleTemplate          (no inbound FKs)
--   8.  ScheduleShiftPattern      (FK → ScheduleTemplate)
--   9.  TimeOffRequest            (FK → TimeOffRequestStatus;
--                                       no FK to ScheduleException yet)
--  10.  ScheduleException         (FK → ScheduleTemplate,
--                                       ScheduleExceptionType,
--                                       TimeOffRequest)
--  11.  FK: TimeOffRequest → ScheduleException  (circular; added last)
--  12.  EmployeeAvailability      (FK → AlertContactMethod)
--  13.  EmployeeAvailabilityDay   (FK → EmployeeAvailability)
--  14.  WorkGroup                 (no inbound FKs)
--  15.  WorkGroupMember           (FK → WorkGroup)
--  16.  vw_CurrentEmployeeAvailability  (view)
-- ============================================================

USE [SmartOps]
GO

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
GO

PRINT '=== SmartOps Database Reset: BEGIN ===';
PRINT '';
PRINT 'WARNING: Dropping all objects. All data will be lost.';
PRINT '';
GO

-- ============================================================
-- PHASE 1 — DROP (reverse dependency order)
-- ============================================================

-- View
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_CurrentEmployeeAvailability' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP VIEW [dbo].[vw_CurrentEmployeeAvailability];
    PRINT 'Dropped view: vw_CurrentEmployeeAvailability';
END
GO

-- WorkGroupMember (FK → WorkGroup)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkGroupMember' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[WorkGroupMember];
    PRINT 'Dropped table: WorkGroupMember';
END
GO

-- WorkGroup
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkGroup' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[WorkGroup];
    PRINT 'Dropped table: WorkGroup';
END
GO

-- EmployeeAvailabilityDay (FK → EmployeeAvailability)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeAvailabilityDay' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[EmployeeAvailabilityDay];
    PRINT 'Dropped table: EmployeeAvailabilityDay';
END
GO

-- EmployeeAvailability (FK → AlertContactMethod)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeAvailability' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[EmployeeAvailability];
    PRINT 'Dropped table: EmployeeAvailability';
END
GO

-- AlertContactMethod
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AlertContactMethod' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[AlertContactMethod];
    PRINT 'Dropped table: AlertContactMethod';
END
GO

-- Circular FK: TimeOffRequest → ScheduleException (must go before either table drops)
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TimeOffRequest_ScheduleException')
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest] DROP CONSTRAINT [FK_TimeOffRequest_ScheduleException];
    PRINT 'Dropped FK: FK_TimeOffRequest_ScheduleException';
END
GO

-- ScheduleException (FK → ScheduleTemplate, ScheduleExceptionType, TimeOffRequest)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleException' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[ScheduleException];
    PRINT 'Dropped table: ScheduleException';
END
GO

-- TimeOffRequest (FK → TimeOffRequestStatus)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TimeOffRequest' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[TimeOffRequest];
    PRINT 'Dropped table: TimeOffRequest';
END
GO

-- TimeOffRequestStatus (lookup)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TimeOffRequestStatus' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[TimeOffRequestStatus];
    PRINT 'Dropped table: TimeOffRequestStatus';
END
GO

-- ScheduleShiftPattern (FK → ScheduleTemplate)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleShiftPattern' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[ScheduleShiftPattern];
    PRINT 'Dropped table: ScheduleShiftPattern';
END
GO

-- ScheduleTemplate
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleTemplate' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[ScheduleTemplate];
    PRINT 'Dropped table: ScheduleTemplate';
END
GO

-- ScheduleExceptionType (lookup)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleExceptionType' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[ScheduleExceptionType];
    PRINT 'Dropped table: ScheduleExceptionType';
END
GO

-- LineAdherence
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineAdherence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[LineAdherence];
    PRINT 'Dropped table: LineAdherence';
END
GO

-- LATDetails
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LATDetails' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[LATDetails];
    PRINT 'Dropped table: LATDetails';
END
GO

-- EtimeShifts
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EtimeShifts' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE [dbo].[EtimeShifts];
    PRINT 'Dropped table: EtimeShifts';
END
GO

PRINT '';
PRINT '--- All objects dropped. Recreating... ---';
PRINT '';
GO

-- ============================================================
-- PHASE 2 — CREATE
-- ============================================================

-- ============================================================
-- 1. EtimeShifts
--    Raw schedule import from the Etime/UKG workforce system.
-- ============================================================

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
GO

-- ============================================================
-- 2. LATDetails
--    Staffing level detail by client, camp, work group, and
--    time slot.  Used for line adherence reporting.
-- ============================================================

CREATE TABLE [dbo].[LATDetails] (
    [LATDetailId]     INT           NOT NULL IDENTITY(1,1),
    [ClientAbbr]      NVARCHAR(10)  NOT NULL,
    [CampAbbr]        NVARCHAR(10)  NULL,
    [WorkGroup]       NVARCHAR(50)  NULL,
    [RequiredDate]    DATE          NOT NULL,
    [RequiredTime]    TIME(0)       NOT NULL,
    [RequiredHours]   INT           NOT NULL,
    [InsertedUtcDate] DATETIME      NOT NULL CONSTRAINT [DF_LATDetails_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedDate] DATETIME      NOT NULL CONSTRAINT [DF_LATDetails_Updated]  DEFAULT GETUTCDATE(),
    [LastTimestamp]   ROWVERSION    NOT NULL,

    CONSTRAINT [PK_LATDetails] PRIMARY KEY ([LATDetailId])
);

CREATE INDEX [IX_LATDetails_ClientAbbr_RequiredDate]
    ON [dbo].[LATDetails] ([ClientAbbr], [RequiredDate], [RequiredTime]);

PRINT 'Created table: LATDetails';
GO

-- ============================================================
-- 3. LineAdherence
--    Required staffing levels per client per time slot.
-- ============================================================

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
GO

-- ============================================================
-- 4. AlertContactMethod  (lookup)
--    How an employee wants to be contacted for OT/VTO alerts.
-- ============================================================

CREATE TABLE [dbo].[AlertContactMethod] (
    [ContactMethodId] TINYINT      NOT NULL,
    [MethodName]      NVARCHAR(50) NOT NULL,

    CONSTRAINT [PK_AlertContactMethod] PRIMARY KEY ([ContactMethodId])
);

INSERT INTO [dbo].[AlertContactMethod] ([ContactMethodId], [MethodName]) VALUES
    (1, 'Email'),
    (2, 'SMS / Text'),
    (3, 'Phone Call'),
    (4, 'Microsoft Teams'),
    (5, 'No Alerts');

PRINT 'Created table: AlertContactMethod';
GO

-- ============================================================
-- 5. TimeOffRequestStatus  (lookup)
--    1=Pending  2=Approved  3=Denied  4=Cancelled
-- ============================================================

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
GO

-- ============================================================
-- 6. ScheduleExceptionType  (lookup)
-- ============================================================

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
GO

-- ============================================================
-- 7. ScheduleTemplate
--    One row per schedule version per employee.
--    EndDate NULL = currently active.
-- ============================================================

CREATE TABLE [dbo].[ScheduleTemplate] (
    [ScheduleTemplateId] INT           NOT NULL IDENTITY(1,1),
    [AdloginName]        NVARCHAR(100) NOT NULL,
    [ExternalMatchId]    NVARCHAR(50)  NOT NULL,   -- FileNumber from Etime
    [PayGroup]           NVARCHAR(50)  NOT NULL,
    [EffectiveDate]      DATE          NOT NULL,
    [EndDate]            DATE          NULL,
    [InsertedDateUtc]    DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Updated]  DEFAULT GETUTCDATE(),
    [Timestamp]          ROWVERSION    NOT NULL,

    CONSTRAINT [PK_ScheduleTemplate]        PRIMARY KEY ([ScheduleTemplateId]),
    CONSTRAINT [CK_ScheduleTemplate_DateOrder]
        CHECK ([EndDate] IS NULL OR [EndDate] >= [EffectiveDate])
);

CREATE UNIQUE INDEX [UX_ScheduleTemplate_OneActivePerEmployee]
    ON [dbo].[ScheduleTemplate] ([AdloginName])
    WHERE [EndDate] IS NULL;

CREATE INDEX [IX_ScheduleTemplate_AdloginName_EffectiveDate]
    ON [dbo].[ScheduleTemplate] ([AdloginName], [EffectiveDate])
    INCLUDE ([PayGroup], [ExternalMatchId], [EndDate]);

PRINT 'Created table: ScheduleTemplate';
GO

-- ============================================================
-- 8. ScheduleShiftPattern
--    Day-of-week shift times belonging to a ScheduleTemplate.
--    DayOfWeek: 0=Sunday 1=Monday ... 6=Saturday
--    Overnight shifts are split at midnight; each portion is
--    a separate row (ShiftSequence 1 = start, 2 = continuation).
-- ============================================================

CREATE TABLE [dbo].[ScheduleShiftPattern] (
    [ShiftPatternId]     INT          NOT NULL IDENTITY(1,1),
    [ScheduleTemplateId] INT          NOT NULL,
    [DayOfWeek]          TINYINT      NOT NULL,
    [ShiftSequence]      TINYINT      NOT NULL CONSTRAINT [DF_ShiftPattern_Sequence] DEFAULT 1,
    [ShiftStartTime]     TIME(0)      NOT NULL,
    [ShiftEndTime]       TIME(0)      NOT NULL,
    [BreakMinutes]       INT          NOT NULL CONSTRAINT [DF_ShiftPattern_BreakMin] DEFAULT 0,
    [PayCode]            NVARCHAR(50) NULL,
    [InsertedDateUtc]    DATETIME     NOT NULL CONSTRAINT [DF_ShiftPattern_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]     DATETIME     NOT NULL CONSTRAINT [DF_ShiftPattern_Updated]  DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_ScheduleShiftPattern]        PRIMARY KEY ([ShiftPatternId]),
    CONSTRAINT [FK_ShiftPattern_Template]       FOREIGN KEY ([ScheduleTemplateId])
        REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId])
        ON DELETE CASCADE,
    CONSTRAINT [CK_ShiftPattern_DayOfWeek]      CHECK ([DayOfWeek] BETWEEN 0 AND 6),
    CONSTRAINT [CK_ShiftPattern_BreakMinutes]   CHECK ([BreakMinutes] >= 0),
    CONSTRAINT [CK_ShiftPattern_Times]          CHECK ([ShiftEndTime] > [ShiftStartTime]),
    CONSTRAINT [UX_ShiftPattern_DaySeqPerTemplate]
        UNIQUE ([ScheduleTemplateId], [DayOfWeek], [ShiftSequence])
);

PRINT 'Created table: ScheduleShiftPattern';
GO

-- ============================================================
-- 9. TimeOffRequest
--    Submitted by employees; reviewed by supervisors.
--    IsPartialShift / PartialStart / PartialEnd allow the
--    employee to request only part of a shift off.
--    PlanToMakeUpTime / MakeUpStart / MakeUpEnd capture the
--    employee's intended make-up window.
--    ScheduleExceptionId FK is added in step 11.
-- ============================================================

CREATE TABLE [dbo].[TimeOffRequest] (
    [TimeOffRequestId]    INT           NOT NULL IDENTITY(1,1),
    [AdloginName]         NVARCHAR(100) NOT NULL,
    [StartDate]           DATE          NOT NULL,
    [EndDate]             DATE          NOT NULL,
    [Reason]              NVARCHAR(500) NOT NULL,
    [StatusId]            TINYINT       NOT NULL CONSTRAINT [DF_TimeOffRequest_Status]      DEFAULT 1,
    [RequestedOn]         DATETIME      NOT NULL CONSTRAINT [DF_TimeOffRequest_RequestedOn] DEFAULT GETUTCDATE(),

    -- Supervisor review
    [ReviewedBy]          NVARCHAR(100) NULL,
    [ReviewedOn]          DATETIME      NULL,
    [ReviewNotes]         NVARCHAR(500) NULL,

    -- Populated when approved and a ScheduleException is created (FK added in step 11)
    [ScheduleExceptionId] INT           NULL,

    -- Partial-shift request (hour / half-hour boundaries only)
    [IsPartialShift]      BIT           NOT NULL CONSTRAINT [DF_TimeOffRequest_IsPartialShift]   DEFAULT 0,
    [PartialStart]        TIME          NULL,
    [PartialEnd]          TIME          NULL,

    -- Make-up time
    [PlanToMakeUpTime]    BIT           NOT NULL CONSTRAINT [DF_TimeOffRequest_PlanToMakeUpTime] DEFAULT 0,
    [MakeUpStart]         DATETIME      NULL,
    [MakeUpEnd]           DATETIME      NULL,

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

CREATE INDEX [IX_TimeOffRequest_AdloginName_StatusId]
    ON [dbo].[TimeOffRequest] ([AdloginName], [StatusId])
    INCLUDE ([StartDate], [EndDate], [RequestedOn]);

CREATE INDEX [IX_TimeOffRequest_StatusId_StartDate]
    ON [dbo].[TimeOffRequest] ([StatusId], [StartDate])
    INCLUDE ([AdloginName], [Reason], [RequestedOn]);

CREATE INDEX [IX_TimeOffRequest_AdloginName_StartDate]
    ON [dbo].[TimeOffRequest] ([AdloginName], [StartDate], [EndDate]);

PRINT 'Created table: TimeOffRequest';
GO

-- ============================================================
-- 10. ScheduleException
--     Date-range overrides that suppress or replace an
--     employee's template shifts for specific days.
--     Created automatically when a TimeOffRequest is approved.
--     TimeOffRequestId links back to the originating request.
-- ============================================================

CREATE TABLE [dbo].[ScheduleException] (
    [ScheduleExceptionId] INT           NOT NULL IDENTITY(1,1),
    [AdloginName]         NVARCHAR(100) NOT NULL,
    [ScheduleTemplateId]  INT           NOT NULL,
    [ExceptionTypeId]     TINYINT       NOT NULL,
    [StartDate]           DATE          NOT NULL,
    [EndDate]             DATE          NOT NULL,
    [TimeOffRequestId]    INT           NULL,
    [Notes]               NVARCHAR(500) NULL,
    [CreatedBy]           NVARCHAR(100) NOT NULL,
    [InsertedDateUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]      DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Updated]  DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_ScheduleException]        PRIMARY KEY ([ScheduleExceptionId]),
    CONSTRAINT [FK_Exception_Template]       FOREIGN KEY ([ScheduleTemplateId])
        REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId]),
    CONSTRAINT [FK_Exception_Type]           FOREIGN KEY ([ExceptionTypeId])
        REFERENCES [dbo].[ScheduleExceptionType] ([ExceptionTypeId]),
    CONSTRAINT [FK_Exception_TimeOffRequest] FOREIGN KEY ([TimeOffRequestId])
        REFERENCES [dbo].[TimeOffRequest] ([TimeOffRequestId]),
    CONSTRAINT [CK_Exception_DateOrder]      CHECK ([EndDate] >= [StartDate])
);

CREATE INDEX [IX_ScheduleException_AdloginName_StartDate]
    ON [dbo].[ScheduleException] ([AdloginName], [StartDate], [EndDate])
    INCLUDE ([ExceptionTypeId], [ScheduleTemplateId]);

CREATE INDEX [IX_ScheduleException_TemplateId]
    ON [dbo].[ScheduleException] ([ScheduleTemplateId], [StartDate]);

-- Lookup by originating time-off request
CREATE INDEX [IX_ScheduleException_TimeOffRequestId]
    ON [dbo].[ScheduleException] ([TimeOffRequestId])
    WHERE [TimeOffRequestId] IS NOT NULL;

PRINT 'Created table: ScheduleException';
GO

-- ============================================================
-- 11. Close the circular reference:
--     TimeOffRequest.ScheduleExceptionId → ScheduleException
-- ============================================================

ALTER TABLE [dbo].[TimeOffRequest]
    ADD CONSTRAINT [FK_TimeOffRequest_ScheduleException]
        FOREIGN KEY ([ScheduleExceptionId])
        REFERENCES [dbo].[ScheduleException] ([ScheduleExceptionId]);

PRINT 'Added FK: FK_TimeOffRequest_ScheduleException';
GO

-- ============================================================
-- 12. EmployeeAvailability
--     One active profile per employee.  Stores weekly hour
--     bounds, overtime / VTO openness, and alert preference.
-- ============================================================

CREATE TABLE [dbo].[EmployeeAvailability] (
    [EmployeeAvailabilityId]      INT           NOT NULL IDENTITY(1,1),
    [AdloginName]                 NVARCHAR(100) NOT NULL,
    [MinWeeklyHours]              DECIMAL(5,2)  NOT NULL CONSTRAINT [DF_EmployeeAvailability_MinHours]    DEFAULT 0,
    [MaxWeeklyHours]              DECIMAL(5,2)  NOT NULL,
    [IsOpenToOvertime]            BIT           NOT NULL CONSTRAINT [DF_EmployeeAvailability_OT]          DEFAULT 0,
    [IsOpenToVto]                 BIT           NOT NULL CONSTRAINT [DF_EmployeeAvailability_VTO]         DEFAULT 0,
    [PreferredAlertContactMethodId] TINYINT     NOT NULL CONSTRAINT [DF_EmployeeAvailability_AlertContact] DEFAULT 1,
    [EffectiveDate]               DATE          NOT NULL CONSTRAINT [DF_EmployeeAvailability_EffDate]     DEFAULT CAST(GETUTCDATE() AS DATE),
    [EndDate]                     DATE          NULL,
    [Notes]                       NVARCHAR(500) NULL,
    [InsertedDateUtc]             DATETIME      NOT NULL CONSTRAINT [DF_EmployeeAvailability_Inserted]    DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]              DATETIME      NOT NULL CONSTRAINT [DF_EmployeeAvailability_Updated]     DEFAULT GETUTCDATE(),
    [Timestamp]                   ROWVERSION    NOT NULL,

    CONSTRAINT [PK_EmployeeAvailability]            PRIMARY KEY ([EmployeeAvailabilityId]),
    CONSTRAINT [FK_EmployeeAvailability_AlertContact] FOREIGN KEY ([PreferredAlertContactMethodId])
        REFERENCES [dbo].[AlertContactMethod] ([ContactMethodId]),
    CONSTRAINT [UX_EmployeeAvailability_LoginEffDate]
        UNIQUE ([AdloginName], [EffectiveDate]),
    CONSTRAINT [CK_EmployeeAvailability_Hours]
        CHECK (
            [MinWeeklyHours] >= 0
            AND [MaxWeeklyHours] >= [MinWeeklyHours]
            AND [MaxWeeklyHours] <= 168
        ),
    CONSTRAINT [CK_EmployeeAvailability_Dates]
        CHECK ([EndDate] IS NULL OR [EndDate] > [EffectiveDate])
);

CREATE INDEX [IX_EmployeeAvailability_AdloginName_Dates]
    ON [dbo].[EmployeeAvailability] ([AdloginName], [EffectiveDate], [EndDate])
    INCLUDE ([MinWeeklyHours], [MaxWeeklyHours], [IsOpenToOvertime], [IsOpenToVto],
             [PreferredAlertContactMethodId]);

CREATE INDEX [IX_EmployeeAvailability_OT_Dates]
    ON [dbo].[EmployeeAvailability] ([IsOpenToOvertime], [EffectiveDate], [EndDate])
    INCLUDE ([AdloginName], [MaxWeeklyHours], [PreferredAlertContactMethodId]);

CREATE INDEX [IX_EmployeeAvailability_VTO_Dates]
    ON [dbo].[EmployeeAvailability] ([IsOpenToVto], [EffectiveDate], [EndDate])
    INCLUDE ([AdloginName], [MinWeeklyHours], [PreferredAlertContactMethodId]);

PRINT 'Created table: EmployeeAvailability';
GO

-- ============================================================
-- 13. EmployeeAvailabilityDay
--     One row per day-of-week per availability profile.
--     No row = employee not available on that day.
-- ============================================================

CREATE TABLE [dbo].[EmployeeAvailabilityDay] (
    [EmployeeAvailabilityDayId] INT      NOT NULL IDENTITY(1,1),
    [EmployeeAvailabilityId]    INT      NOT NULL,
    [DayOfWeek]                 TINYINT  NOT NULL,   -- 0=Sun 1=Mon ... 6=Sat
    [EarliestStart]             TIME(0)  NOT NULL,
    [LatestStop]                TIME(0)  NOT NULL,
    [InsertedDateUtc]           DATETIME NOT NULL CONSTRAINT [DF_EmployeeAvailabilityDay_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]            DATETIME NOT NULL CONSTRAINT [DF_EmployeeAvailabilityDay_Updated]  DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_EmployeeAvailabilityDay]          PRIMARY KEY ([EmployeeAvailabilityDayId]),
    CONSTRAINT [FK_EmployeeAvailabilityDay_Availability] FOREIGN KEY ([EmployeeAvailabilityId])
        REFERENCES [dbo].[EmployeeAvailability] ([EmployeeAvailabilityId])
        ON DELETE CASCADE,
    CONSTRAINT [UX_EmployeeAvailabilityDay_DayPerProfile]
        UNIQUE ([EmployeeAvailabilityId], [DayOfWeek]),
    CONSTRAINT [CK_EmployeeAvailabilityDay_DayOfWeek]
        CHECK ([DayOfWeek] BETWEEN 0 AND 6)
);

CREATE INDEX [IX_EmployeeAvailabilityDay_AvailabilityId]
    ON [dbo].[EmployeeAvailabilityDay] ([EmployeeAvailabilityId], [DayOfWeek])
    INCLUDE ([EarliestStart], [LatestStop]);

PRINT 'Created table: EmployeeAvailabilityDay';
GO

-- ============================================================
-- 14. WorkGroup
-- ============================================================

CREATE TABLE [dbo].[WorkGroup] (
    [WorkGroupId]     INT            NOT NULL IDENTITY(1,1),
    [Name]            NVARCHAR(100)  NOT NULL,
    [Description]     NVARCHAR(500)  NULL,
    [IsActive]        BIT            NOT NULL CONSTRAINT [DF_WorkGroup_IsActive] DEFAULT 1,
    [InsertedDateUtc] DATETIME       NOT NULL CONSTRAINT [DF_WorkGroup_Inserted] DEFAULT GETUTCDATE(),
    [LastUpdatedUtc]  DATETIME       NOT NULL CONSTRAINT [DF_WorkGroup_Updated]  DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_WorkGroup]      PRIMARY KEY ([WorkGroupId]),
    CONSTRAINT [UX_WorkGroup_Name] UNIQUE ([Name])
);

PRINT 'Created table: WorkGroup';
GO

-- ============================================================
-- 15. WorkGroupMember
-- ============================================================

CREATE TABLE [dbo].[WorkGroupMember] (
    [WorkGroupMemberId] INT            NOT NULL IDENTITY(1,1),
    [WorkGroupId]       INT            NOT NULL,
    [AdloginName]       NVARCHAR(100)  NOT NULL,
    [AddedDateUtc]      DATETIME       NOT NULL CONSTRAINT [DF_WorkGroupMember_Added]   DEFAULT GETUTCDATE(),
    [RemovedDateUtc]    DATETIME       NULL,
    [AddedBy]           NVARCHAR(100)  NOT NULL,
    [RemovedBy]         NVARCHAR(100)  NULL,

    CONSTRAINT [PK_WorkGroupMember]       PRIMARY KEY ([WorkGroupMemberId]),
    CONSTRAINT [FK_WorkGroupMember_Group] FOREIGN KEY ([WorkGroupId])
        REFERENCES [dbo].[WorkGroup] ([WorkGroupId])
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX [UX_WorkGroupMember_ActivePerGroup]
    ON [dbo].[WorkGroupMember] ([WorkGroupId], [AdloginName])
    WHERE [RemovedDateUtc] IS NULL;

CREATE INDEX [IX_WorkGroupMember_WorkGroupId_Active]
    ON [dbo].[WorkGroupMember] ([WorkGroupId])
    INCLUDE ([AdloginName], [AddedDateUtc])
    WHERE [RemovedDateUtc] IS NULL;

CREATE INDEX [IX_WorkGroupMember_AdloginName_Active]
    ON [dbo].[WorkGroupMember] ([AdloginName])
    INCLUDE ([WorkGroupId], [AddedDateUtc])
    WHERE [RemovedDateUtc] IS NULL;

PRINT 'Created table: WorkGroupMember';
GO

-- ============================================================
-- 16. View: vw_CurrentEmployeeAvailability
-- ============================================================

CREATE VIEW [dbo].[vw_CurrentEmployeeAvailability] AS
SELECT
    a.[EmployeeAvailabilityId],
    a.[AdloginName],
    a.[MinWeeklyHours],
    a.[MaxWeeklyHours],
    a.[IsOpenToOvertime],
    a.[IsOpenToVto],
    cm.[MethodName] AS [PreferredAlertMethod],
    a.[EffectiveDate],
    a.[EndDate],
    a.[Notes]
FROM  [dbo].[EmployeeAvailability] a
JOIN  [dbo].[AlertContactMethod]   cm ON cm.[ContactMethodId] = a.[PreferredAlertContactMethodId]
WHERE a.[EffectiveDate] <= CAST(GETUTCDATE() AS DATE)
  AND (a.[EndDate] IS NULL OR a.[EndDate] > CAST(GETUTCDATE() AS DATE));
GO

PRINT 'Created view: vw_CurrentEmployeeAvailability';
GO

-- ============================================================
-- Verification: confirm all expected objects exist
-- ============================================================

PRINT '';
PRINT '--- Verification ---';

SELECT
    t.name                                    AS TableName,
    COUNT(c.column_id)                        AS ColumnCount,
    (SELECT COUNT(*) FROM sys.indexes   i  WHERE i.object_id  = t.object_id AND i.type > 0) AS IndexCount,
    (SELECT COUNT(*) FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id)      AS OutboundFKCount
FROM sys.tables  t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
  AND t.name IN (
      'EtimeShifts', 'LATDetails', 'LineAdherence',
      'AlertContactMethod',
      'TimeOffRequestStatus', 'ScheduleExceptionType',
      'ScheduleTemplate', 'ScheduleShiftPattern',
      'TimeOffRequest', 'ScheduleException',
      'EmployeeAvailability', 'EmployeeAvailabilityDay',
      'WorkGroup', 'WorkGroupMember'
  )
GROUP BY t.name, t.object_id
ORDER BY t.name;

SELECT name AS ViewName, create_date, modify_date
FROM sys.views
WHERE schema_id = SCHEMA_ID('dbo') AND name = 'vw_CurrentEmployeeAvailability';
GO

PRINT '';
PRINT '=== SmartOps Database Reset: COMPLETE ===';
GO
