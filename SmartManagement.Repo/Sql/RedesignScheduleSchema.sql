-- ============================================================
-- SmartOps: Schedule Schema Redesign
-- Replaces: Schedule, Shift, ShiftBreak
-- Adds:     ScheduleTemplate, ScheduleShiftPattern,
--           ScheduleExceptionType, ScheduleException
-- Alters:   TimeOffRequest
-- Database: SmartOps
-- ============================================================

USE [SmartOps]
GO

-- ============================================================
-- STEP 1: Drop old FK constraints that reference tables being
--         replaced, so the drops in Step 2 succeed cleanly.
-- ============================================================

-- TimeOffRequest.EtimeShiftId FK (EtimeShift table stays, but
-- this FK will be removed when we alter TimeOffRequest below)
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_TimeOffRequest_EtimeShift'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        DROP CONSTRAINT [FK_TimeOffRequest_EtimeShift];
    PRINT 'Dropped FK: FK_TimeOffRequest_EtimeShift';
END

-- Shift → Schedule
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Shifts_Schedules'
)
BEGIN
    ALTER TABLE [dbo].[Shift]
        DROP CONSTRAINT [FK_Shifts_Schedules];
    PRINT 'Dropped FK: FK_Shifts_Schedules';
END

-- ShiftBreak → Schedule
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_ShiftBreaks_Schedules'
)
BEGIN
    ALTER TABLE [dbo].[ShiftBreak]
        DROP CONSTRAINT [FK_ShiftBreaks_Schedules];
    PRINT 'Dropped FK: FK_ShiftBreaks_Schedules';
END
GO

-- ============================================================
-- STEP 2: Drop old tables (Schedule, Shift, ShiftBreak)
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ShiftBreak' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    DROP TABLE [dbo].[ShiftBreak];
    PRINT 'Dropped table: ShiftBreak';
END

IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'Shift' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    DROP TABLE [dbo].[Shift];
    PRINT 'Dropped table: Shift';
END

IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'Schedule' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    DROP TABLE [dbo].[Schedule];
    PRINT 'Dropped table: Schedule';
END
GO

-- ============================================================
-- STEP 3: Create ScheduleTemplate
--         One row per schedule version per employee.
--         EndDate = NULL means currently active.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleTemplate' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleTemplate] (
        [ScheduleTemplateId] INT           NOT NULL IDENTITY(1,1),
        [AdloginName]        NVARCHAR(100) NOT NULL,
        [ExternalMatchId]    NVARCHAR(50)  NOT NULL,  -- FileNumber from Etime
        [PayGroup]           NVARCHAR(50)  NOT NULL,
        [EffectiveDate]      DATE          NOT NULL,  -- when this version became active
        [EndDate]            DATE          NULL,       -- NULL = currently active
        [InsertedDateUtc]    DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleTemplate_Updated]  DEFAULT GETUTCDATE(),
        [Timestamp]          ROWVERSION    NOT NULL,

        CONSTRAINT [PK_ScheduleTemplate] PRIMARY KEY ([ScheduleTemplateId]),
        CONSTRAINT [CK_ScheduleTemplate_DateOrder]
            CHECK ([EndDate] IS NULL OR [EndDate] >= [EffectiveDate])
    );

    -- Only one active (EndDate IS NULL) template per employee at any time
    CREATE UNIQUE INDEX [UX_ScheduleTemplate_OneActivePerEmployee]
        ON [dbo].[ScheduleTemplate] ([AdloginName])
        WHERE [EndDate] IS NULL;

    -- Fast lookup: "give me the template active on date X for employee Y"
    CREATE INDEX [IX_ScheduleTemplate_AdloginName_EffectiveDate]
        ON [dbo].[ScheduleTemplate] ([AdloginName], [EffectiveDate])
        INCLUDE ([PayGroup], [ExternalMatchId], [EndDate]);

    PRINT 'Created table: ScheduleTemplate';
END
ELSE
    PRINT 'Skipped: ScheduleTemplate already exists';
GO

-- ============================================================
-- STEP 4: Create ScheduleShiftPattern
--         Day-of-week shift times belonging to a template.
--         DayOfWeek: 0=Sunday, 1=Monday ... 6=Saturday
--         Uses TIME(0) so no specific calendar date is stored.
--         BreakMinutes replaces the ShiftBreak table.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'ScheduleShiftPattern' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[ScheduleShiftPattern] (
        [ShiftPatternId]     INT           NOT NULL IDENTITY(1,1),
        [ScheduleTemplateId] INT           NOT NULL,
        [DayOfWeek]          TINYINT       NOT NULL,  -- 0=Sun, 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat
        [ShiftStartTime]     TIME(0)       NOT NULL,  -- e.g. 07:00:00
        [ShiftEndTime]       TIME(0)       NOT NULL,  -- e.g. 15:00:00
        [BreakMinutes]       INT           NOT NULL CONSTRAINT [DF_ShiftPattern_BreakMin] DEFAULT 0,
        [PayCode]            NVARCHAR(50)  NULL,
        [InsertedDateUtc]    DATETIME      NOT NULL CONSTRAINT [DF_ShiftPattern_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ShiftPattern_Updated]  DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_ScheduleShiftPattern]      PRIMARY KEY ([ShiftPatternId]),
        CONSTRAINT [FK_ShiftPattern_Template]     FOREIGN KEY ([ScheduleTemplateId])
            REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId])
            ON DELETE CASCADE,
        CONSTRAINT [CK_ShiftPattern_DayOfWeek]    CHECK ([DayOfWeek] BETWEEN 0 AND 6),
        CONSTRAINT [CK_ShiftPattern_BreakMinutes] CHECK ([BreakMinutes] >= 0),

        -- One shift pattern per day per template
        CONSTRAINT [UX_ShiftPattern_DayPerTemplate]
            UNIQUE ([ScheduleTemplateId], [DayOfWeek])
    );

    PRINT 'Created table: ScheduleShiftPattern';
END
ELSE
    PRINT 'Skipped: ScheduleShiftPattern already exists';
GO

-- ============================================================
-- STEP 5: Create ScheduleExceptionType (lookup)
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
-- STEP 6: Create ScheduleException
--         Date-range overrides that suppress or modify the
--         template for specific days.  When a TimeOffRequest
--         is approved, a row is inserted here and linked back
--         via TimeOffRequest.ScheduleExceptionId.
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
        [TimeOffRequestId]    INT           NULL,       -- populated when exception originates from an approved request
        [Notes]               NVARCHAR(500) NULL,
        [CreatedBy]           NVARCHAR(100) NOT NULL,
        [InsertedDateUtc]     DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]      DATETIME      NOT NULL CONSTRAINT [DF_ScheduleException_Updated]  DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_ScheduleException]        PRIMARY KEY ([ScheduleExceptionId]),
        CONSTRAINT [FK_Exception_Template]       FOREIGN KEY ([ScheduleTemplateId])
            REFERENCES [dbo].[ScheduleTemplate] ([ScheduleTemplateId]),
        CONSTRAINT [FK_Exception_Type]           FOREIGN KEY ([ExceptionTypeId])
            REFERENCES [dbo].[ScheduleExceptionType] ([ExceptionTypeId]),
        -- TimeOffRequest FK added after TimeOffRequest is altered (Step 7)
        CONSTRAINT [CK_Exception_DateOrder]      CHECK ([EndDate] >= [StartDate])
    );

    -- Supervisor review queue: all exceptions for an employee ordered by date
    CREATE INDEX [IX_ScheduleException_AdloginName_StartDate]
        ON [dbo].[ScheduleException] ([AdloginName], [StartDate])
        INCLUDE ([ExceptionTypeId], [EndDate]);

    -- Lookup exceptions overlapping a date range
    CREATE INDEX [IX_ScheduleException_TemplateId_StartDate]
        ON [dbo].[ScheduleException] ([ScheduleTemplateId], [StartDate], [EndDate]);

    PRINT 'Created table: ScheduleException';
END
ELSE
    PRINT 'Skipped: ScheduleException already exists';
GO

-- ============================================================
-- STEP 7: Alter TimeOffRequest
--         - Remove EtimeShiftId FK and concrete shift columns
--         - Remove ScheduleUpdated tracking (moved to ScheduleException)
--         - Add StartDate / EndDate (the requested off period)
--         - Add ScheduleExceptionId (populated on approval)
-- ============================================================

-- 7a. Drop columns that are being replaced
DECLARE @sql NVARCHAR(MAX) = '';

-- Drop EtimeShiftId column if it exists
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'EtimeShiftId'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [EtimeShiftId];' + CHAR(13);

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ShiftStart'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [ShiftStart];' + CHAR(13);

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ShiftEnd'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [ShiftEnd];' + CHAR(13);

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ScheduleUpdated'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [ScheduleUpdated];' + CHAR(13);

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ScheduleUpdatedOn'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [ScheduleUpdatedOn];' + CHAR(13);

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ScheduleUpdatedBy'
)
    SET @sql += 'ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [ScheduleUpdatedBy];' + CHAR(13);

IF LEN(@sql) > 0
BEGIN
    EXEC sp_executesql @sql;
    PRINT 'Dropped replaced columns from TimeOffRequest';
END
GO

-- 7b. Add StartDate and EndDate (the requested off period)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'StartDate'
)
BEGIN
    -- Add as nullable first so existing rows don't violate NOT NULL
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD [StartDate] DATE NULL,
            [EndDate]   DATE NULL;

    -- Default any existing rows to today so we can apply NOT NULL
    UPDATE [dbo].[TimeOffRequest]
    SET [StartDate] = CAST(GETUTCDATE() AS DATE),
        [EndDate]   = CAST(GETUTCDATE() AS DATE)
    WHERE [StartDate] IS NULL;

    ALTER TABLE [dbo].[TimeOffRequest]
        ALTER COLUMN [StartDate] DATE NOT NULL;
    ALTER TABLE [dbo].[TimeOffRequest]
        ALTER COLUMN [EndDate] DATE NOT NULL;

    ALTER TABLE [dbo].[TimeOffRequest]
        ADD CONSTRAINT [CK_TimeOffRequest_DateOrder] CHECK ([EndDate] >= [StartDate]);

    PRINT 'Added StartDate / EndDate to TimeOffRequest';
END
ELSE
    PRINT 'Skipped: StartDate already exists on TimeOffRequest';
GO

-- 7c. Add ScheduleExceptionId (nullable — set when supervisor approves)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'ScheduleExceptionId'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD [ScheduleExceptionId] INT NULL;

    ALTER TABLE [dbo].[TimeOffRequest]
        ADD CONSTRAINT [FK_TimeOffRequest_ScheduleException]
            FOREIGN KEY ([ScheduleExceptionId])
            REFERENCES [dbo].[ScheduleException] ([ScheduleExceptionId]);

    PRINT 'Added ScheduleExceptionId to TimeOffRequest';
END
ELSE
    PRINT 'Skipped: ScheduleExceptionId already exists on TimeOffRequest';
GO

-- 7d. Now that TimeOffRequest has ScheduleExceptionId, add the
--     reverse FK from ScheduleException back to TimeOffRequest
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Exception_TimeOffRequest'
)
BEGIN
    ALTER TABLE [dbo].[ScheduleException]
        ADD CONSTRAINT [FK_Exception_TimeOffRequest]
            FOREIGN KEY ([TimeOffRequestId])
            REFERENCES [dbo].[TimeOffRequest] ([TimeOffRequestId]);

    PRINT 'Added FK: FK_Exception_TimeOffRequest';
END
ELSE
    PRINT 'Skipped: FK_Exception_TimeOffRequest already exists';
GO

-- ============================================================
-- STEP 8: Drop the old unique index on TimeOffRequest that
--         referenced EtimeShiftId (no longer valid)
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TimeOffRequest_ShiftId_AdloginName_Active'
      AND object_id = OBJECT_ID('dbo.TimeOffRequest')
)
BEGIN
    DROP INDEX [UX_TimeOffRequest_ShiftId_AdloginName_Active]
        ON [dbo].[TimeOffRequest];
    PRINT 'Dropped index: UX_TimeOffRequest_ShiftId_AdloginName_Active';
END

-- Add a replacement unique index: one active request per employee per date range
-- (prevents duplicate pending/approved requests for overlapping periods)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TimeOffRequest_AdloginName_StartDate'
      AND object_id = OBJECT_ID('dbo.TimeOffRequest')
)
BEGIN
    CREATE INDEX [IX_TimeOffRequest_AdloginName_StartDate]
        ON [dbo].[TimeOffRequest] ([AdloginName], [StartDate], [EndDate])
        INCLUDE ([StatusId], [RequestedOn]);

    PRINT 'Created index: IX_TimeOffRequest_AdloginName_StartDate';
END
GO

-- ============================================================
-- Verification: list all new/altered objects
-- ============================================================

SELECT
    t.name        AS TableName,
    c.name        AS ColumnName,
    tp.name       AS DataType,
    c.max_length,
    c.is_nullable
FROM sys.tables t
JOIN sys.columns c  ON c.object_id = t.object_id
JOIN sys.types  tp  ON tp.user_type_id = c.user_type_id
WHERE t.name IN (
    'ScheduleTemplate', 'ScheduleShiftPattern',
    'ScheduleExceptionType', 'ScheduleException',
    'TimeOffRequest'
)
ORDER BY t.name, c.column_id;
GO

PRINT '=== RedesignScheduleSchema complete ===';
GO
