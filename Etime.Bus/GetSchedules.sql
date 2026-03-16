USE [SSO]
GO

/****** Object:  StoredProcedure [dbo].[GetSchedules]    Script Date: 3/16/2026 10:19:30 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetSchedules]
    @ShiftStartDate DATE,
    @ShiftEndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- CTE 1: Filter assignments to the requested date window first.
    -- This keeps all subsequent joins small before hitting the cross-db tables.
    WITH FilteredAssignments AS (
        SELECT
            sa.SHIFTCODEID,
            sa.EMPLOYEEID,
            sa.SHIFTSTARTDATE,
            sa.SHIFTENDDATE
        FROM adpeet.dbo.ShiftAssignmnt sa WITH (NOLOCK)
        WHERE sa.SHIFTSTARTDATE >= @ShiftStartDate
          AND sa.SHIFTSTARTDATE <  @ShiftEndDate
          AND sa.DELETEDSW    = 0
          AND sa.SHIFTTYPEID IN (1, 2, 4, 8) -- exclude hidden types
    ),
    -- CTE 2: Pre-aggregate ShiftSegment once per ShiftCode instead of once per
    -- assignment row.  The original join on SHIFTCODEID alone multiplied every
    -- assignment by N segments before GROUP BY collapsed them; this avoids that.
    ShiftCodeAgg AS (
        SELECT
            ss.SHIFTCODEID,
            SUM(CASE WHEN ss.SHIFTSEGMNTTYPID = 4
                     THEN DATEDIFF(minute, ss.STARTDTM, ss.ENDDTM)
                     ELSE 0 END) AS BreakMin
        FROM adpeet.dbo.ShiftSegment ss WITH (NOLOCK)
        WHERE ss.SHIFTCODEID IN (SELECT SHIFTCODEID FROM FilteredAssignments)
        GROUP BY ss.SHIFTCODEID
    )
    SELECT
        sc.SHIFTCODEID,
        e.NT_Login_name,
        p.PERSONNUM,
        el.[EMPLID],
        el.[FILE_NUMBER],
        vp.HOMELABORLEVELNAME1   AS PayGroup,
        sc.PAYCODEID,
        pc.NAME                  AS PayCode,
        fa.SHIFTSTARTDATE        AS STARTDTM,
        fa.SHIFTENDDATE          AS ENDDTM,
        ISNULL(sca.BreakMin, 0)  AS BreakMin
    FROM FilteredAssignments fa
    JOIN adpeet.dbo.ShiftCode   sc  WITH (NOLOCK) ON sc.SHIFTCODEID  = fa.SHIFTCODEID
    JOIN ShiftCodeAgg           sca              ON sca.SHIFTCODEID = fa.SHIFTCODEID
    JOIN adpeet.dbo.WtkEmployee wtk WITH (NOLOCK) ON wtk.EMPLOYEEID  = fa.EMPLOYEEID
    JOIN adpeet.dbo.person      p   WITH (NOLOCK) ON p.PERSONID      = wtk.PERSONID
    JOIN adpeet.dbo.VP_PERSON   vp               ON vp.PERSONNUM    = p.PERSONNUM
    JOIN Cmsprd01.admin.dbo.employee             e  WITH (NOLOCK) ON e.[ETIME_EMPLOYEENUMBER] = p.PERSONNUM
    JOIN CmsPrd01.admin.dbo.EMPLOYEE_EMPLID_LINK el WITH (NOLOCK) ON el.FILE_NUMBER           = p.PERSONNUM
    LEFT JOIN adpeet.dbo.PAYCODE pc WITH (NOLOCK) ON pc.PAYCODEID = sc.PAYCODEID
    ORDER BY p.PERSONNUM, fa.SHIFTSTARTDATE
    OPTION (RECOMPILE); -- prevent a cached plan built on a different date range from being reused
END
GO