-- ============================================================
-- Fix: overnight shift support in ScheduleShiftPattern
--
-- Business rule: overnight shifts are split at midnight so each
-- stored portion has ShiftEndTime > ShiftStartTime.
-- Example: 22:00 → 06:00 becomes:
--   Sequence 1 (start day)  : 22:00 → 23:59:59
--   Sequence 2 (next day)   : 00:00:00 → 06:00
--
-- To accommodate two rows per (template, day), the unique
-- constraint is extended to include ShiftSequence.
--
-- NOTE: If you previously ran Fix_ShiftPatternTimes_Constraint.sql
-- (which changed the check to <>), this script re-restores the
-- original > check because the split approach makes it valid again.
-- ============================================================

-- 1. Restore CK_ShiftPattern_Times to require end > start
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_ShiftPattern_Times'
      AND parent_object_id = OBJECT_ID('dbo.ScheduleShiftPattern')
)
BEGIN
    ALTER TABLE [dbo].[ScheduleShiftPattern]
        DROP CONSTRAINT [CK_ShiftPattern_Times];
END
GO

ALTER TABLE [dbo].[ScheduleShiftPattern]
    ADD CONSTRAINT [CK_ShiftPattern_Times]
        CHECK ([ShiftEndTime] > [ShiftStartTime]);

PRINT 'Restored constraint: CK_ShiftPattern_Times (ShiftEndTime > ShiftStartTime)';
GO

-- 2. Add ShiftSequence column (1 = normal/start portion, 2 = post-midnight continuation)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ScheduleShiftPattern')
      AND name = 'ShiftSequence'
)
BEGIN
    ALTER TABLE [dbo].[ScheduleShiftPattern]
        ADD [ShiftSequence] TINYINT NOT NULL
            CONSTRAINT [DF_ShiftPattern_Sequence] DEFAULT 1;

    PRINT 'Added column: ScheduleShiftPattern.ShiftSequence';
END
ELSE
    PRINT 'Skipped: ShiftSequence already exists';
GO

-- 3. Drop the old single-pattern-per-day unique constraint
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_ShiftPattern_DayPerTemplate'
      AND object_id = OBJECT_ID('dbo.ScheduleShiftPattern')
)
BEGIN
    ALTER TABLE [dbo].[ScheduleShiftPattern]
        DROP CONSTRAINT [UX_ShiftPattern_DayPerTemplate];

    PRINT 'Dropped index: UX_ShiftPattern_DayPerTemplate';
END
GO

-- 4. Create new unique constraint that allows start + continuation per day
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_ShiftPattern_DaySeqPerTemplate'
      AND object_id = OBJECT_ID('dbo.ScheduleShiftPattern')
)
BEGIN
    ALTER TABLE [dbo].[ScheduleShiftPattern]
        ADD CONSTRAINT [UX_ShiftPattern_DaySeqPerTemplate]
            UNIQUE ([ScheduleTemplateId], [DayOfWeek], [ShiftSequence]);

    PRINT 'Created constraint: UX_ShiftPattern_DaySeqPerTemplate';
END
GO
