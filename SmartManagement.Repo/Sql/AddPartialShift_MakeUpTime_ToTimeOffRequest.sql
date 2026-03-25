-- ============================================================
-- SmartOps: Add partial-shift and make-up-time columns
--           to TimeOffRequest
-- ============================================================

USE [SmartOps]
GO

-- ------------------------------------------------------------
-- 1. Add IsPartialShift / PartialStart / PartialEnd
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'IsPartialShift'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD [IsPartialShift] BIT  NOT NULL DEFAULT 0,
            [PartialStart]   TIME NULL,
            [PartialEnd]     TIME NULL;
    PRINT 'Added columns: IsPartialShift, PartialStart, PartialEnd';
END
ELSE
    PRINT 'Skipped: IsPartialShift already exists';
GO

-- ------------------------------------------------------------
-- 2. Add PlanToMakeUpTime
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'PlanToMakeUpTime'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD [PlanToMakeUpTime] BIT NOT NULL DEFAULT 0;
    PRINT 'Added column: PlanToMakeUpTime';
END
ELSE
    PRINT 'Skipped: PlanToMakeUpTime already exists';
GO

-- ------------------------------------------------------------
-- 3. Drop old MakeUpDateTime if it exists from a prior run
-- ------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'MakeUpDateTime'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest] DROP COLUMN [MakeUpDateTime];
    PRINT 'Dropped column: MakeUpDateTime';
END
GO

-- ------------------------------------------------------------
-- 4. Add MakeUpStart / MakeUpEnd
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TimeOffRequest') AND name = 'MakeUpStart'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD [MakeUpStart] DATETIME NULL,
            [MakeUpEnd]   DATETIME NULL;
    PRINT 'Added columns: MakeUpStart, MakeUpEnd';
END
ELSE
    PRINT 'Skipped: MakeUpStart already exists';
GO

-- ------------------------------------------------------------
-- 5. Constraints
-- ------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TimeOffRequest_PartialShiftTimes'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD CONSTRAINT [CK_TimeOffRequest_PartialShiftTimes]
        CHECK (
            [IsPartialShift] = 0
            OR ([PartialStart] IS NOT NULL AND [PartialEnd] IS NOT NULL AND [PartialEnd] > [PartialStart])
        );
    PRINT 'Added constraint: CK_TimeOffRequest_PartialShiftTimes';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TimeOffRequest_MakeUpRange'
)
BEGIN
    ALTER TABLE [dbo].[TimeOffRequest]
        ADD CONSTRAINT [CK_TimeOffRequest_MakeUpRange]
        CHECK (
            [PlanToMakeUpTime] = 0
            OR ([MakeUpStart] IS NOT NULL AND [MakeUpEnd] IS NOT NULL AND [MakeUpEnd] > [MakeUpStart])
        );
    PRINT 'Added constraint: CK_TimeOffRequest_MakeUpRange';
END
GO

PRINT 'Migration complete.';
GO
