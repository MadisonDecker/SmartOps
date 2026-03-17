-- ============================================================
-- Fix: CK_ShiftPattern_Times — allow overnight shifts
--
-- The original constraint  CHECK ([ShiftEndTime] > [ShiftStartTime])
-- rejects overnight shifts where the time component of ShiftEnd is
-- numerically less than ShiftStart (e.g. 22:00 → 06:00 next day).
--
-- The corrected constraint only prohibits zero-duration shifts
-- (start == end), which are always invalid.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_ShiftPattern_Times'
      AND parent_object_id = OBJECT_ID('dbo.ScheduleShiftPattern')
)
BEGIN
    ALTER TABLE [dbo].[ScheduleShiftPattern]
        DROP CONSTRAINT [CK_ShiftPattern_Times];
    PRINT 'Dropped constraint: CK_ShiftPattern_Times';
END
GO

ALTER TABLE [dbo].[ScheduleShiftPattern]
    ADD CONSTRAINT [CK_ShiftPattern_Times]
        CHECK ([ShiftEndTime] <> [ShiftStartTime]);

PRINT 'Recreated constraint: CK_ShiftPattern_Times (now allows overnight shifts)';
GO
