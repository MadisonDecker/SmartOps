using Microsoft.EntityFrameworkCore;
using SmartManagement.Repo.Models;
using SmartOps.Models;
using System.Text.Json;

namespace SmartOpsManagement.Bus;

/// <summary>
/// Schedule-related business logic.
/// EtimeShifts has one row per employee per week (ShiftStart = first day start,
/// ShiftEnd = last day end). This class extracts the recurring day-of-week pattern
/// from those rows and maintains ScheduleTemplate + ScheduleShiftPattern records.
/// </summary>
public partial class SmartOpsBusinessLogic
{
    // -------------------------------------------------------------------------
    // Etime import
    // -------------------------------------------------------------------------

    /// <summary>
    /// Imports Etime schedule records from a JSON file into the EtimeShifts
    /// staging table. Does not create or modify ScheduleTemplate records —
    /// call SyncEtimeShiftsToTemplatesAsync after this to reconcile.
    /// </summary>
    public async Task<int> ImportEtimeSchedulesAsync(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"File not found: {jsonFilePath}");
            return -1;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var schedules = JsonSerializer.Deserialize<List<ScheduleImportRecord>>(json);

            if (schedules == null || schedules.Count == 0)
            {
                Console.WriteLine("No schedules found in the JSON file.");
                return 0;
            }

            var etimeShifts = schedules.Select(s => new EtimeShift
            {
                ShiftCodeId = s.ShiftCodeId,
                AdloginName = s.NtLoginName,
                PersonNum   = int.TryParse(s.PersonNum, out var pn) ? pn : 0,
                EmplId      = s.EmplId      is not null && int.TryParse(s.EmplId.ToString(),      out var emp) ? emp : 0,
                FileNumber  = s.FileNumber  is not null && int.TryParse(s.FileNumber.ToString(),  out var fl)  ? fl  : 0,
                PayGroup    = s.PayGroup  ?? string.Empty,
                PayCodeId   = s.PayCodeId,
                PayCode     = string.IsNullOrWhiteSpace(s.PayCode) ? null : s.PayCode,
                ShiftStart  = s.StartDtm,
                ShiftEnd    = s.EndDtm,
                BreakMin    = s.BreakMin
            }).ToList();

            _context.EtimeShifts.AddRange(etimeShifts);
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing schedules: {ex.Message}");
            return -1;
        }
    }

    // -------------------------------------------------------------------------
    // Template sync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reconciles EtimeShift staging data with ScheduleTemplate / ScheduleShiftPattern.
    ///
    /// Each EtimeShift row represents one week for one employee:
    ///   ShiftStart = first shift start  (e.g. Mon 07:00)
    ///   ShiftEnd   = last shift end     (e.g. Fri 15:00)
    ///
    /// Working days are every weekday (Mon–Fri) between ShiftStart.Date and
    /// ShiftEnd.Date.  All working days share the same start/end time.
    ///
    /// Logic per employee:
    ///   - No active template → create one
    ///   - Active template pattern matches → no change
    ///   - Active template pattern differs → close old, create new
    /// </summary>
    public async Task<(int Created, int Updated, int Unchanged)> SyncEtimeShiftsToTemplatesAsync(
        DateTime startDate, DateTime endDate)
    {
        var etimeShifts = await GetEtimeShiftsByDateRangeAsync(startDate, endDate);
        if (!etimeShifts.Any()) return (0, 0, 0);

        int created = 0, updated = 0, unchanged = 0;
        var now = DateTime.UtcNow;
        var effectiveDate = DateOnly.FromDateTime(startDate);

        var employeeGroups = etimeShifts.GroupBy(e => e.AdloginName);

        foreach (var group in employeeGroups)
        {
            var adLogin = group.Key;

            // Group this employee's shifts by ISO week (Monday = week start),
            // then take the most recent week.  This handles both per-day Etime
            // exports (5 rows/week) and per-week exports (1 row/week) correctly.
            var mostRecentWeek = group
                .GroupBy(e => MondayOf(e.ShiftStart))
                .OrderByDescending(w => w.Key)
                .First()
                .OrderBy(e => e.ShiftStart)
                .ToList();

            var firstShift = mostRecentWeek.First();   // representative for FileNumber / PayGroup

            // Build the canonical pattern from every record in that week
            var extracted = ExtractPatternFromWeek(mostRecentWeek);

            // Load existing active template with its patterns
            var existing = _context.ScheduleTemplates
                .Include(t => t.ScheduleShiftPatterns)
                .FirstOrDefault(t => t.AdloginName == adLogin && t.EndDate == null);

            if (existing == null)
            {
                CreateTemplate(adLogin, firstShift, extracted, effectiveDate, now);
                created++;
            }
            else if (!PatternsMatch(existing.ScheduleShiftPatterns.ToList(), extracted))
            {
                // Close the current template the day before the new data starts
                existing.EndDate       = effectiveDate.AddDays(-1);
                existing.LastUpdatedUtc = now;
                CreateTemplate(adLogin, firstShift, extracted, effectiveDate, now);
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Template sync: {created} created, {updated} updated, {unchanged} unchanged.");
        return (created, updated, unchanged);
    }

    // -------------------------------------------------------------------------
    // Schedule expansion — called by endpoints for display
    // -------------------------------------------------------------------------

    /// <summary>
    /// Expands the employee's active ScheduleTemplate into concrete ScheduledShift
    /// instances for every day in [startDate, endDate], then removes any days
    /// covered by a ScheduleException (approved time off, holiday, etc.).
    /// </summary>
    public async Task<List<ScheduledShift>> GetScheduledShiftsForUserAsync(
        string adLogin, DateOnly startDate, DateOnly endDate)
    {
        var template = _context.ScheduleTemplates
            .Include(t => t.ScheduleShiftPatterns)
            .FirstOrDefault(t =>
                t.AdloginName   == adLogin         &&
                t.EffectiveDate <= endDate          &&
                (t.EndDate == null || t.EndDate >= startDate));

        if (template == null) return new List<ScheduledShift>();

        var exceptions = _context.ScheduleExceptions
            .Where(e =>
                e.AdloginName == adLogin &&
                e.StartDate   <= endDate &&
                e.EndDate     >= startDate)
            .ToList();

        var shifts = new List<ScheduledShift>();
        var now    = DateTime.Now;
        var day    = startDate;

        while (day <= endDate)
        {
            var dow         = (byte)day.DayOfWeek;
            var isException = exceptions.Any(e => e.StartDate <= day && e.EndDate >= day);
            var patterns    = template.ScheduleShiftPatterns
                                      .Where(p => p.DayOfWeek == dow)
                                      .OrderBy(p => p.ShiftSequence);

            foreach (var pattern in patterns)
            {
                if (isException) break;

                // Overnight continuation (sequence 2): the "day" it belongs to is
                // the previous calendar day's night, so advance the end to next day
                // only for display purposes — the stored times are already correct.
                var shiftStart = day.ToDateTime(pattern.ShiftStartTime);
                var shiftEnd   = day.ToDateTime(pattern.ShiftEndTime);

                var status = shiftEnd < now
                    ? ShiftStatus.Completed
                    : shiftStart <= now && shiftEnd >= now
                        ? ShiftStatus.InProgress
                        : ShiftStatus.Scheduled;

                var shift = new ScheduledShift
                {
                    EmployeeId = adLogin,
                    Division   = template.PayGroup,
                    StartTime  = shiftStart,
                    EndTime    = shiftEnd,
                    Status     = status
                };

                if (pattern.BreakMinutes > 0)
                {
                    var breakStart = shiftStart.AddHours(4);
                    shift.Breaks.Add(new ScheduledBreak
                    {
                        StartTime = breakStart,
                        EndTime   = breakStart.AddMinutes(pattern.BreakMinutes),
                        BreakType = "Break",
                        IsPaid    = false
                    });
                }

                shifts.Add(shift);
            }

            day = day.AddDays(1);
        }

        return await Task.FromResult(shifts);
    }

    /// <summary>
    /// Returns the next scheduled shift for the employee, looking up to 90 days ahead.
    /// Excludes days covered by approved time-off and includes approved make-up time.
    /// </summary>
    public async Task<ScheduledShift?> GetNextScheduledShiftForUserAsync(string adLogin)
    {
        var today     = DateOnly.FromDateTime(DateTime.Now);
        var lookAhead = today.AddDays(90);
        var now       = DateTime.Now;

        var shifts          = await GetScheduledShiftsForUserAsync(adLogin, today, lookAhead);
        var timeOffRequests = await GetTimeOffRequestsForUserAsync(adLogin);

        var approvedTimeOff = timeOffRequests
            .Where(r => r.Status == TimeOffStatus.Approved)
            .ToList();

        // Exclude any regular shifts that fall on an approved time-off date
        var regularNext = shifts
            .Where(s => s.StartTime > now &&
                        !approvedTimeOff.Any(r =>
                            r.StartDate <= DateOnly.FromDateTime(s.StartTime) &&
                            r.EndDate   >= DateOnly.FromDateTime(s.StartTime)))
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        var makeUpNext = approvedTimeOff
            .Where(r => r.PlanToMakeUpTime      &&
                        r.MakeUpStart.HasValue  &&
                        r.MakeUpEnd.HasValue    &&
                        r.MakeUpStart.Value > now)
            .Select(r => new ScheduledShift
            {
                EmployeeId = adLogin,
                StartTime  = r.MakeUpStart!.Value,
                EndTime    = r.MakeUpEnd!.Value,
                Status     = ShiftStatus.Scheduled
            })
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        if (regularNext == null) return makeUpNext;
        if (makeUpNext  == null) return regularNext;
        return makeUpNext.StartTime < regularNext.StartTime ? makeUpNext : regularNext;
    }

    /// <summary>
    /// Calculates total scheduled hours (net of breaks) for the employee's week,
    /// including any approved make-up time that falls within the week.
    /// </summary>
    public async Task<double> GetWeeklyHoursForUserAsync(string adLogin, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        var shifts  = await GetScheduledShiftsForUserAsync(adLogin, weekStart, weekEnd);
        var shiftHours = shifts.Sum(s =>
            (s.EndTime - s.StartTime).TotalHours
            - s.Breaks.Sum(b => (b.EndTime - b.StartTime).TotalMinutes) / 60.0);

        var timeOffRequests = await GetTimeOffRequestsForUserAsync(adLogin);
        var weekStartDt = weekStart.ToDateTime(TimeOnly.MinValue);
        var weekEndDt   = weekEnd.ToDateTime(TimeOnly.MaxValue);
        var makeUpHours = timeOffRequests
            .Where(r => r.Status          == TimeOffStatus.Approved &&
                        r.PlanToMakeUpTime                          &&
                        r.MakeUpStart.HasValue                      &&
                        r.MakeUpEnd.HasValue                        &&
                        r.MakeUpStart.Value >= weekStartDt          &&
                        r.MakeUpStart.Value <= weekEndDt)
            .Sum(r => (r.MakeUpEnd!.Value - r.MakeUpStart!.Value).TotalHours);

        return shiftHours + makeUpHours;
    }

    // -------------------------------------------------------------------------
    // Raw EtimeShift queries (staging / admin use)
    // -------------------------------------------------------------------------

    public async Task<List<EtimeShift>> GetAllEtimeShiftsAsync() =>
        await Task.FromResult(_context.EtimeShifts.ToList());

    public async Task<List<EtimeShift>> GetEtimeShiftsByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        await Task.FromResult(_context.EtimeShifts
            .Where(e => e.ShiftStart >= startDate && e.ShiftStart < endDate)
            .ToList());

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts the day-of-week pattern from one EtimeShift row.
    ///
    /// Three cases based on ShiftStart/ShiftEnd:
    ///
    ///  1. Day shift (endTime &gt; startTime, or endTime == 00:00 meaning "ends at midnight"):
    ///     One pattern row per working weekday in [ShiftStart.Date, ShiftEnd.Date), sequence 1.
    ///     A midnight-ending shift (ShiftEnd stored as next-day 00:00) is treated as ending at
    ///     23:59:59 on the start day — all work hours belong to the start day.
    ///
    ///  2. True overnight (ShiftEnd.Date &gt; ShiftStart.Date and endTime != 00:00):
    ///     Split at midnight.
    ///     ShiftStart.Date → ShiftEnd.Date-1  = "start nights"  (startTime → 23:59:59, sequence 1)
    ///     ShiftStart.Date+1 → ShiftEnd.Date  = "continuations" (00:00:00 → endTime,   sequence 2)
    /// </summary>
    private static List<ShiftPatternEntry> ExtractPattern(EtimeShift shift)
    {
        var startTime = TimeOnly.FromDateTime(shift.ShiftStart);
        var endTime   = TimeOnly.FromDateTime(shift.ShiftEnd);
        var endOfDay  = new TimeOnly(23, 59, 59);

        // ── Midnight-ending shift ─────────────────────────────────────────────
        // ShiftEnd is stored as the next calendar day at 00:00 (e.g. Mon 07:00 → Tue 00:00).
        // All hours belong to the start day; no continuation on the next day.
        // Normalise endTime to 23:59:59 so it satisfies ShiftEndTime > ShiftStartTime.
        if (endTime == TimeOnly.MinValue && shift.ShiftEnd.Date > shift.ShiftStart.Date)
        {
            endTime = endOfDay;
            var workDaysMid  = WeekdaysBetween(shift.ShiftStart.Date, shift.ShiftStart.Date);
            var breakMid     = workDaysMid.Count > 0 ? shift.BreakMin / workDaysMid.Count : 0;
            return workDaysMid
                .Distinct().OrderBy(d => d)
                .Select(d => new ShiftPatternEntry(d, startTime, endTime, breakMid, shift.PayCode, 1))
                .ToList();
        }

        // ── Zero-duration: bad Etime data — skip ─────────────────────────────
        if (startTime == endTime)
        {
            Console.WriteLine(
                $"[ExtractPattern] Skipping shift {shift.EtimeShiftId} for {shift.AdloginName}: " +
                $"start and end time are identical ({startTime}).");
            return new List<ShiftPatternEntry>();
        }

        // ── Day shift ─────────────────────────────────────────────────────────
        if (endTime > startTime)
        {
            var workDays    = WeekdaysBetween(shift.ShiftStart.Date, shift.ShiftEnd.Date);
            var breakPerDay = workDays.Count > 0 ? shift.BreakMin / workDays.Count : 0;
            return workDays
                .Distinct().OrderBy(d => d)
                .Select(d => new ShiftPatternEntry(d, startTime, endTime, breakPerDay, shift.PayCode, 1))
                .ToList();
        }

        // ── True overnight shift — split at midnight ──────────────────────────
        // endTime < startTime and ShiftEnd is past the start day (e.g. Mon 22:00 → Tue 06:00).
        // startNights : days the shift STARTS  (ShiftStart.Date up to but not including ShiftEnd.Date)
        // contDays    : days the continuation ENDS (day after ShiftStart through ShiftEnd.Date)
        var startOfDay  = TimeOnly.MinValue;  // 00:00:00

        var startNights  = WeekdaysBetween(shift.ShiftStart.Date, shift.ShiftEnd.Date.AddDays(-1));
        var contDays     = WeekdaysBetween(shift.ShiftStart.Date.AddDays(1), shift.ShiftEnd.Date);
        var breakPerNight = startNights.Count > 0 ? shift.BreakMin / startNights.Count : 0;

        var entries = new List<ShiftPatternEntry>();
        foreach (var dow in startNights.Distinct().OrderBy(d => d))
            entries.Add(new ShiftPatternEntry(dow, startTime, endOfDay, breakPerNight, shift.PayCode, 1));
        foreach (var dow in contDays.Distinct().OrderBy(d => d))
            entries.Add(new ShiftPatternEntry(dow, startOfDay, endTime, 0, shift.PayCode, 2));

        return entries;
    }

    /// <summary>
    /// Builds a shift pattern from all EtimeShift records in a single week.
    /// Works correctly whether Etime exports one row per week or one row per day.
    /// Each record contributes its own working days using ExtractPattern logic.
    /// </summary>
    private static List<ShiftPatternEntry> ExtractPatternFromWeek(List<EtimeShift> weekShifts)
    {
        var allEntries = weekShifts.SelectMany(ExtractPattern).ToList();

        // Deduplicate: if two records produce the same (DayOfWeek, ShiftSequence) — which
        // can happen if per-week and per-day records overlap — keep the first occurrence.
        return allEntries
            .GroupBy(e => (e.DayOfWeek, e.ShiftSequence))
            .Select(g => g.First())
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.ShiftSequence)
            .ToList();
    }

    /// <summary>Returns the Monday of the ISO week that contains <paramref name="d"/>.</summary>
    private static DateTime MondayOf(DateTime d) =>
        d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    /// <summary>Returns the DayOfWeek byte for every weekday (Mon–Fri) in [from, to].</summary>
    private static List<byte> WeekdaysBetween(DateTime from, DateTime to)
    {
        var result = new List<byte>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            var dow = (byte)d.DayOfWeek;
            if (dow != 0 && dow != 6)   // exclude Sunday=0, Saturday=6
                result.Add(dow);
        }
        return result;
    }

    private static bool PatternsMatch(
        List<ScheduleShiftPattern> existing,
        List<ShiftPatternEntry> extracted)
    {
        if (existing.Count != extracted.Count) return false;

        foreach (var e in extracted)
        {
            var match = existing.FirstOrDefault(p =>
                p.DayOfWeek     == e.DayOfWeek  &&
                p.ShiftSequence == e.ShiftSequence);
            if (match == null)                        return false;
            if (match.ShiftStartTime != e.StartTime) return false;
            if (match.ShiftEndTime   != e.EndTime)   return false;
        }
        return true;
    }

    private void CreateTemplate(
        string adLogin,
        EtimeShift sourceShift,
        List<ShiftPatternEntry> pattern,
        DateOnly effectiveDate,
        DateTime now)
    {
        var template = new ScheduleTemplate
        {
            AdloginName     = adLogin,
            ExternalMatchId = sourceShift.FileNumber.ToString().PadLeft(8, '0'),
            PayGroup        = sourceShift.PayGroup ?? string.Empty,
            EffectiveDate   = effectiveDate,
            EndDate         = null,
            InsertedDateUtc = now,
            LastUpdatedUtc  = now
        };

        _context.ScheduleTemplates.Add(template);

        foreach (var entry in pattern)
        {
            _context.ScheduleShiftPatterns.Add(new ScheduleShiftPattern
            {
                ScheduleTemplate = template,
                DayOfWeek        = entry.DayOfWeek,
                ShiftSequence    = entry.ShiftSequence,
                ShiftStartTime   = entry.StartTime,
                ShiftEndTime     = entry.EndTime,
                BreakMinutes     = entry.BreakMinutes,
                PayCode          = entry.PayCode,
                InsertedDateUtc  = now,
                LastUpdatedUtc   = now
            });
        }
    }

    private record ShiftPatternEntry(
        byte DayOfWeek, TimeOnly StartTime, TimeOnly EndTime,
        int BreakMinutes, string? PayCode, byte ShiftSequence);
}

/// <summary>
/// Record structure for importing schedules from JSON (Etime export format).
/// </summary>
public class ScheduleImportRecord
{
    public int ShiftCodeId { get; set; }
    public string NtLoginName { get; set; } = string.Empty;
    public string PersonNum { get; set; } = string.Empty;
    public int? EmplId { get; set; }
    public int? FileNumber { get; set; }
    public string? PayGroup { get; set; }
    public int? PayCodeId { get; set; }
    public string? PayCode { get; set; }
    public DateTime StartDtm { get; set; }
    public DateTime EndDtm { get; set; }
    public int BreakMin { get; set; }
}
