-- ============================================================
-- SmartOps: Time-Off Request Tables
-- Database : SmartOps
-- ============================================================

USE [SmartOps]
GO

-- ------------------------------------------------------------
-- 1. Status lookup table
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'TimeOffRequestStatus' AND schema_id = SCHEMA_ID('dbo')
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

-- ------------------------------------------------------------
-- 2. Main request table
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'TimeOffRequest' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[TimeOffRequest] (
        [TimeOffRequestId]  INT            NOT NULL IDENTITY(1,1),
        [AdloginName]       NVARCHAR(100)  NOT NULL,           -- employee (matches EtimeShifts.AdloginName)
        [EtimeShiftId]      INT            NOT NULL,           -- shift being requested off
        [ShiftStart]        DATETIME       NOT NULL,           -- denormalized for display without joins
        [ShiftEnd]          DATETIME       NOT NULL,           -- denormalized for display without joins
        [Reason]            NVARCHAR(500)  NOT NULL,
        [StatusId]          TINYINT        NOT NULL DEFAULT 1, -- 1=Pending, 2=Approved, 3=Denied, 4=Cancelled
        [RequestedOn]       DATETIME       NOT NULL DEFAULT GETUTCDATE(),

        -- Supervisor review
        [ReviewedBy]        NVARCHAR(100)  NULL,               -- supervisor AdloginName
        [ReviewedOn]        DATETIME       NULL,
        [ReviewNotes]       NVARCHAR(500)  NULL,

        -- Schedule update tracking
        [ScheduleUpdated]   BIT            NOT NULL DEFAULT 0, -- was the shift removed/changed in Etime?
        [ScheduleUpdatedOn] DATETIME       NULL,
        [ScheduleUpdatedBy] NVARCHAR(100)  NULL,

        -- Audit
        [InsertedDateUtc]   DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        [LastUpdatedUtc]    DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        [Timestamp]         ROWVERSION     NOT NULL,

        CONSTRAINT [PK_TimeOffRequest]
            PRIMARY KEY ([TimeOffRequestId]),

        CONSTRAINT [FK_TimeOffRequest_EtimeShift]
            FOREIGN KEY ([EtimeShiftId]) REFERENCES [dbo].[EtimeShifts] ([EtimeShiftId]),

        CONSTRAINT [FK_TimeOffRequest_Status]
            FOREIGN KEY ([StatusId]) REFERENCES [dbo].[TimeOffRequestStatus] ([StatusId]),

        -- ScheduleUpdated can only be set to 1 on an approved request
        CONSTRAINT [CK_TimeOffRequest_ScheduleUpdated]
            CHECK ([ScheduleUpdated] = 0 OR [StatusId] = 2),

        -- ReviewedBy/ReviewedOn must both be present or both absent
        CONSTRAINT [CK_TimeOffRequest_ReviewFields]
            CHECK (
                ([ReviewedBy] IS NULL AND [ReviewedOn] IS NULL)
                OR ([ReviewedBy] IS NOT NULL AND [ReviewedOn] IS NOT NULL)
            )
    );

    PRINT 'Created table: TimeOffRequest';
END
ELSE
    PRINT 'Skipped: TimeOffRequest already exists';
GO

-- ------------------------------------------------------------
-- 3. Indexes
-- ------------------------------------------------------------

-- Employee view: their own requests filtered by status
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeOffRequest_AdloginName_StatusId')
BEGIN
    CREATE INDEX [IX_TimeOffRequest_AdloginName_StatusId]
        ON [dbo].[TimeOffRequest] ([AdloginName], [StatusId])
        INCLUDE ([ShiftStart], [ShiftEnd], [RequestedOn]);

    PRINT 'Created index: IX_TimeOffRequest_AdloginName_StatusId';
END

-- Supervisor review queue: pending requests by shift date
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeOffRequest_StatusId_ShiftStart')
BEGIN
    CREATE INDEX [IX_TimeOffRequest_StatusId_ShiftStart]
        ON [dbo].[TimeOffRequest] ([StatusId], [ShiftStart])
        INCLUDE ([AdloginName], [Reason], [RequestedOn]);

    PRINT 'Created index: IX_TimeOffRequest_StatusId_ShiftStart';
END

-- Prevent duplicate active requests for the same employee + shift
-- (re-requests are allowed after Denied=3 or Cancelled=4)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TimeOffRequest_ShiftId_AdloginName_Active')
BEGIN
    CREATE UNIQUE INDEX [UX_TimeOffRequest_ShiftId_AdloginName_Active]
        ON [dbo].[TimeOffRequest] ([EtimeShiftId], [AdloginName])
        WHERE [StatusId] <> 3 AND [StatusId] <> 4;

    PRINT 'Created index: UX_TimeOffRequest_ShiftId_AdloginName_Active';
END
GO

PRINT 'TimeOffRequest setup complete.';
GO
