-- ============================================================
-- SmartOps: Employee Availability Schema
-- Creates tables that capture each employee's scheduling
-- preferences: available hours per day-of-week, weekly hour
-- targets, overtime/VTO willingness, and alert preferences.
--
-- Table creation order:
--   1.  AlertContactMethod        (lookup, no inbound FKs)
--   2.  EmployeeAvailability      (FK → AlertContactMethod)
--   3.  EmployeeAvailabilityDay   (FK → EmployeeAvailability)
--
-- DayOfWeek encoding (matches .NET DayOfWeek enum and
-- ScheduleShiftPattern convention already in this database):
--   0 = Sunday  1 = Monday  2 = Tuesday  3 = Wednesday
--   4 = Thursday  5 = Friday  6 = Saturday
--
-- A missing EmployeeAvailabilityDay row for a given day means
-- the employee is NOT available on that day.
--
-- EffectiveDate / EndDate pattern:
--   The active profile is the one where EffectiveDate <= TODAY
--   and EndDate IS NULL (or EndDate > TODAY).  When preferences
--   change, set EndDate on the old row and insert a new one.
-- ============================================================

USE [SmartOps]
GO

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
GO

-- ============================================================
-- 1. AlertContactMethod
--    Lookup table for how an employee wants to be contacted
--    when an OT or VTO opportunity becomes available.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'AlertContactMethod' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
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
END
ELSE
    PRINT 'Skipped: AlertContactMethod already exists';
GO

-- ============================================================
-- 2. EmployeeAvailability
--    One active profile per employee.  Stores weekly hour
--    bounds, overtime / VTO openness, and alert preference.
--    Supports history via EffectiveDate / EndDate.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'EmployeeAvailability' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[EmployeeAvailability] (
        [EmployeeAvailabilityId]  INT           NOT NULL IDENTITY(1,1),
        [AdloginName]             NVARCHAR(100) NOT NULL,

        -- Weekly hour targets
        [MinWeeklyHours]          DECIMAL(5,2)  NOT NULL CONSTRAINT [DF_EmployeeAvailability_MinHours]  DEFAULT 0,
        [MaxWeeklyHours]          DECIMAL(5,2)  NOT NULL,

        -- Scheduling preferences
        [IsOpenToOvertime]        BIT           NOT NULL CONSTRAINT [DF_EmployeeAvailability_OT]         DEFAULT 0,
        [IsOpenToVto]             BIT           NOT NULL CONSTRAINT [DF_EmployeeAvailability_VTO]        DEFAULT 0,

        -- How to alert this employee for OT / VTO opportunities
        -- FK → AlertContactMethod
        [PreferredAlertContactMethodId] TINYINT  NOT NULL CONSTRAINT [DF_EmployeeAvailability_AlertContact] DEFAULT 1,  -- Email

        -- Effective date range for this profile
        [EffectiveDate]           DATE          NOT NULL CONSTRAINT [DF_EmployeeAvailability_EffDate]    DEFAULT CAST(GETUTCDATE() AS DATE),
        [EndDate]                 DATE          NULL,

        [Notes]                   NVARCHAR(500) NULL,

        -- Audit
        [InsertedDateUtc]         DATETIME      NOT NULL CONSTRAINT [DF_EmployeeAvailability_Inserted]   DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]          DATETIME      NOT NULL CONSTRAINT [DF_EmployeeAvailability_Updated]    DEFAULT GETUTCDATE(),
        [Timestamp]               ROWVERSION    NOT NULL,

        CONSTRAINT [PK_EmployeeAvailability]
            PRIMARY KEY ([EmployeeAvailabilityId]),

        CONSTRAINT [FK_EmployeeAvailability_AlertContact]
            FOREIGN KEY ([PreferredAlertContactMethodId])
            REFERENCES [dbo].[AlertContactMethod] ([ContactMethodId]),

        -- No two profiles for the same employee may start on the same date
        CONSTRAINT [UX_EmployeeAvailability_LoginEffDate]
            UNIQUE ([AdloginName], [EffectiveDate]),

        -- MinWeeklyHours must be <= MaxWeeklyHours; both within 0–168 (hrs/week)
        CONSTRAINT [CK_EmployeeAvailability_Hours]
            CHECK (
                [MinWeeklyHours] >= 0
                AND [MaxWeeklyHours] >= [MinWeeklyHours]
                AND [MaxWeeklyHours] <= 168
            ),

        CONSTRAINT [CK_EmployeeAvailability_Dates]
            CHECK ([EndDate] IS NULL OR [EndDate] > [EffectiveDate])
    );

    -- Supports "get current profile for employee" and workgroup-level queries
    CREATE INDEX [IX_EmployeeAvailability_AdloginName_Dates]
        ON [dbo].[EmployeeAvailability] ([AdloginName], [EffectiveDate], [EndDate])
        INCLUDE ([MinWeeklyHours], [MaxWeeklyHours], [IsOpenToOvertime], [IsOpenToVto],
                 [PreferredAlertContactMethodId]);

    -- Supports scheduler queries: "who is open to OT and not at max hours?"
    CREATE INDEX [IX_EmployeeAvailability_OT_Dates]
        ON [dbo].[EmployeeAvailability] ([IsOpenToOvertime], [EffectiveDate], [EndDate])
        INCLUDE ([AdloginName], [MaxWeeklyHours], [PreferredAlertContactMethodId]);

    CREATE INDEX [IX_EmployeeAvailability_VTO_Dates]
        ON [dbo].[EmployeeAvailability] ([IsOpenToVto], [EffectiveDate], [EndDate])
        INCLUDE ([AdloginName], [MinWeeklyHours], [PreferredAlertContactMethodId]);

    PRINT 'Created table: EmployeeAvailability';
END
ELSE
    PRINT 'Skipped: EmployeeAvailability already exists';
GO

-- ============================================================
-- 3. EmployeeAvailabilityDay
--    One row per day-of-week per availability profile.
--    Defines the earliest the employee can start and the
--    latest they can work on that day.  No row = not available.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'EmployeeAvailabilityDay' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[EmployeeAvailabilityDay] (
        [EmployeeAvailabilityDayId] INT      NOT NULL IDENTITY(1,1),
        [EmployeeAvailabilityId]    INT      NOT NULL,

        -- 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        [DayOfWeek]                 TINYINT  NOT NULL,

        -- TIME(0) = second precision, no fractional seconds
        [EarliestStart]             TIME(0)  NOT NULL,
        [LatestStop]                TIME(0)  NOT NULL,

        -- Audit
        [InsertedDateUtc]           DATETIME NOT NULL CONSTRAINT [DF_EmployeeAvailabilityDay_Inserted] DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]            DATETIME NOT NULL CONSTRAINT [DF_EmployeeAvailabilityDay_Updated]  DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_EmployeeAvailabilityDay]
            PRIMARY KEY ([EmployeeAvailabilityDayId]),

        CONSTRAINT [FK_EmployeeAvailabilityDay_Availability]
            FOREIGN KEY ([EmployeeAvailabilityId])
            REFERENCES [dbo].[EmployeeAvailability] ([EmployeeAvailabilityId])
            ON DELETE CASCADE,

        -- Only one row per day per profile
        CONSTRAINT [UX_EmployeeAvailabilityDay_DayPerProfile]
            UNIQUE ([EmployeeAvailabilityId], [DayOfWeek]),

        CONSTRAINT [CK_EmployeeAvailabilityDay_DayOfWeek]
            CHECK ([DayOfWeek] BETWEEN 0 AND 6)

        -- NOTE: No EarliestStart < LatestStop check here to support
        -- employees whose window crosses midnight (e.g. 22:00 – 06:00).
        -- Enforce that constraint in application logic instead.
    );

    CREATE INDEX [IX_EmployeeAvailabilityDay_AvailabilityId]
        ON [dbo].[EmployeeAvailabilityDay] ([EmployeeAvailabilityId], [DayOfWeek])
        INCLUDE ([EarliestStart], [LatestStop]);

    PRINT 'Created table: EmployeeAvailabilityDay';
END
ELSE
    PRINT 'Skipped: EmployeeAvailabilityDay already exists';
GO

-- ============================================================
-- 3b. Migration: collapse OtAlertContactMethodId +
--     VtoAlertContactMethodId → PreferredAlertContactMethodId
--     Runs only when the table already exists with the old layout.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EmployeeAvailability')
      AND name = 'OtAlertContactMethodId'
)
BEGIN
    -- Drop old FK constraints
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeAvailability_OtContact')
        ALTER TABLE [dbo].[EmployeeAvailability] DROP CONSTRAINT [FK_EmployeeAvailability_OtContact];

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeAvailability_VtoContact')
        ALTER TABLE [dbo].[EmployeeAvailability] DROP CONSTRAINT [FK_EmployeeAvailability_VtoContact];

    -- Drop old default constraints
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_EmployeeAvailability_OTContact')
        ALTER TABLE [dbo].[EmployeeAvailability] DROP CONSTRAINT [DF_EmployeeAvailability_OTContact];

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_EmployeeAvailability_VTOContact')
        ALTER TABLE [dbo].[EmployeeAvailability] DROP CONSTRAINT [DF_EmployeeAvailability_VTOContact];

    -- Rename the OT column; its existing values become the preferred method
    EXEC sp_rename 'dbo.EmployeeAvailability.OtAlertContactMethodId',
                   'PreferredAlertContactMethodId', 'COLUMN';

    -- Remove the now-redundant VTO column
    ALTER TABLE [dbo].[EmployeeAvailability]
        DROP COLUMN [VtoAlertContactMethodId];

    -- Restore default and FK under the new names
    ALTER TABLE [dbo].[EmployeeAvailability]
        ADD CONSTRAINT [DF_EmployeeAvailability_AlertContact]
            DEFAULT 1 FOR [PreferredAlertContactMethodId];

    ALTER TABLE [dbo].[EmployeeAvailability]
        ADD CONSTRAINT [FK_EmployeeAvailability_AlertContact]
            FOREIGN KEY ([PreferredAlertContactMethodId])
            REFERENCES [dbo].[AlertContactMethod] ([ContactMethodId]);

    PRINT 'Migrated: collapsed OT/VTO alert columns to PreferredAlertContactMethodId';
END
GO

-- ============================================================
-- 4. View: vw_CurrentEmployeeAvailability
--    Returns the active availability profile for each employee
--    (EffectiveDate <= today AND EndDate IS NULL or future).
--    Join to EmployeeAvailabilityDay on EmployeeAvailabilityId
--    to get the per-day windows.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.views
    WHERE name = 'vw_CurrentEmployeeAvailability' AND schema_id = SCHEMA_ID('dbo')
)
    DROP VIEW [dbo].[vw_CurrentEmployeeAvailability];
GO

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
FROM  [dbo].[EmployeeAvailability]  a
JOIN  [dbo].[AlertContactMethod]    cm  ON cm.[ContactMethodId] = a.[PreferredAlertContactMethodId]
WHERE a.[EffectiveDate] <= CAST(GETUTCDATE() AS DATE)
  AND (a.[EndDate] IS NULL OR a.[EndDate] > CAST(GETUTCDATE() AS DATE));
GO

PRINT 'Created view: vw_CurrentEmployeeAvailability';
GO

-- ============================================================
-- Sample data  (comment out before running in production)
-- ============================================================
/*
-- Create a profile for jdoe: Mon–Fri 07:00–18:00,
-- min 32 hrs, max 40 hrs, open to OT, preferred alert method SMS.

DECLARE @Id INT;

INSERT INTO [dbo].[EmployeeAvailability]
    ([AdloginName], [MinWeeklyHours], [MaxWeeklyHours],
     [IsOpenToOvertime], [IsOpenToVto],
     [PreferredAlertContactMethodId])
VALUES
    ('jdoe', 32, 40, 1, 0, 2);   -- PreferredAlert=SMS

SET @Id = SCOPE_IDENTITY();

INSERT INTO [dbo].[EmployeeAvailabilityDay]
    ([EmployeeAvailabilityId], [DayOfWeek], [EarliestStart], [LatestStop])
VALUES
    (@Id, 1, '07:00', '18:00'),  -- Monday
    (@Id, 2, '07:00', '18:00'),  -- Tuesday
    (@Id, 3, '07:00', '18:00'),  -- Wednesday
    (@Id, 4, '07:00', '18:00'),  -- Thursday
    (@Id, 5, '07:00', '18:00');  -- Friday
*/
