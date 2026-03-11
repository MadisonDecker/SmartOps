CREATE OR ALTER PROCEDURE dbo.GetSchedules
    @ShiftStartDate DATE,
    @ShiftEndDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Pre-filter shift assignments to reduce row count before expensive joins
    ;WITH FilteredShifts AS (
        SELECT 
            sa.SHIFTCODEID,
            sa.employeeid,
            sa.SHIFTSTARTDATE,
            sa.SHIFTendDATE
        FROM adpeet.dbo.ShiftAssignmnt sa WITH (NOLOCK)
        WHERE sa.SHIFTSTARTDATE >= @ShiftStartDate
          AND sa.SHIFTSTARTDATE < @ShiftEndDate
          AND sa.DELETEDSW = 0
          AND sa.SHIFTTYPEID IN (1, 2, 4, 8)
    ),
    -- Pre-aggregate break minutes to avoid repeated calculations in main query
    BreakMinutes AS (
        SELECT 
            ss.SHIFTCODEID,
            SUM(DATEDIFF(minute, ss.STARTDTM, ss.ENDDTM)) AS BreakMin
        FROM adpeet.dbo.ShiftSegment ss WITH (NOLOCK)
        WHERE ss.SHIFTSEGMNTTYPID = 4
          AND EXISTS (SELECT 1 FROM FilteredShifts fs WHERE fs.SHIFTCODEID = ss.SHIFTCODEID)
        GROUP BY ss.SHIFTCODEID
    )
    SELECT 
        sc.SHIFTCODEID, 
        e.NT_Login_name,
        p.PERSONNUM,
        el.[EMPLID],
        el.[FILE_NUMBER],
        vp.HOMELABORLEVELNAME1 AS PayGroup, 
        sc.PAYCODEID, 
        pc.NAME AS PayCode, 
        MIN(fs.SHIFTSTARTDATE) AS STARTDTM, 
        MAX(fs.SHIFTendDATE) AS ENDDTM, 
        ISNULL(bm.BreakMin, 0) AS BreakMin
    FROM FilteredShifts fs
    JOIN adpeet.dbo.ShiftCode sc WITH (NOLOCK) ON fs.SHIFTCODEID = sc.SHIFTCODEID
    JOIN adpeet.dbo.WtkEmployee wtk WITH (NOLOCK) ON fs.employeeid = wtk.employeeid  
    JOIN adpeet.dbo.person p WITH (NOLOCK) ON wtk.personid = p.personid 
    JOIN adpeet.dbo.VP_PERSON vp WITH (NOLOCK) ON p.PERSONNUM = vp.PERSONNUM
    JOIN Cmsprd01.admin.dbo.employee e WITH (NOLOCK) ON p.PERSONNUM = e.[ETIME_EMPLOYEENUMBER]
    JOIN CmsPrd01.admin.dbo.EMPLOYEE_EMPLID_LINK el WITH (NOLOCK) ON p.PERSONNUM = el.person_id
    LEFT JOIN adpeet.dbo.PAYCODE pc WITH (NOLOCK) ON sc.PAYCODEID = pc.PAYCODEID
    LEFT JOIN BreakMinutes bm ON sc.SHIFTCODEID = bm.SHIFTCODEID
    GROUP BY 
        sc.SHIFTCODEID,
        p.PERSONNUM, 
        vp.HOMELABORLEVELNAME1, 
        sc.PAYCODEID, 
        pc.NAME, 
        fs.SHIFTSTARTDATE, 
        e.NT_Login_name,
        el.[EMPLID],
        el.[FILE_NUMBER],
        bm.BreakMin
    ORDER BY p.PERSONNUM, STARTDTM
    OPTION (RECOMPILE);
END
