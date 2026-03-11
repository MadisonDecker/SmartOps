CREATE OR ALTER PROCEDURE dbo.GetSchedules
    @ShiftStartDate DATE,
    @ShiftEndDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        sc.SHIFTCODEID, 
        e.NT_Login_name,
        p.PERSONNUM,
        el.[EMPLID],
        el.[FILE_NUMBER],
        vp.HOMELABORLEVELNAME1 AS 'PayGroup', 
        sc.PAYCODEID, 
        pc.NAME AS PayCode, 
        MIN(SHIFTSTARTDATE) AS STARTDTM, 
        MAX(SHIFTendDATE) AS ENDDTM, 
        SUM(CASE WHEN ss.SHIFTSEGMNTTYPID = 4 THEN DATEDIFF(minute, ss.STARTDTM, ss.ENDDTM) ELSE 0 END) AS BreakMin
    FROM adpeet.dbo.ShiftAssignmnt sa
    JOIN adpeet.dbo.ShiftCode sc ON sa.SHIFTCODEID = sc.SHIFTCODEID
    JOIN adpeet.dbo.ShiftSegment ss ON sa.SHIFTCODEID = ss.SHIFTCODEID
    JOIN adpeet.dbo.WtkEmployee wtk ON sa.employeeid = wtk.employeeid  
    JOIN adpeet.dbo.person p ON wtk.personid = p.personid 
    JOIN adpeet.dbo.VP_PERSON vp ON p.PERSONNUM = vp.PERSONNUM
            JOIN Cmsprd01.admin.dbo.employee e ON p.PERSONNUM = e.[ETIME_EMPLOYEENUMBER]
            JOIN CmsPrd01.admin.dbo.EMPLOYEE_EMPLID_LINK EL ON p.PERSONNUM = el.person_id
    LEFT JOIN adpeet.dbo.PAYCODE pc ON sc.PAYCODEID = pc.PAYCODEID
    WHERE SHIFTSTARTDATE >= @ShiftStartDate
      AND SHIFTSTARTDATE < @ShiftEndDate
      AND sa.DELETEDSW = 0
      AND sa.SHIFTTYPEID IN (1, 2, 4, 8) --exclude hidden types
    GROUP BY sc.SHIFTCODEID,p.PERSONNUM, vp.HOMELABORLEVELNAME1, sc.PAYCODEID, pc.NAME, SHIFTSTARTDATE, e.NT_Login_name,el.[EMPLID],el.[FILE_NUMBER]
    ORDER BY p.PERSONNUM, STARTDTM;
END