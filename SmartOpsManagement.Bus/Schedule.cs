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
            var adLogin    = group.Key;
            var firstShift = group.OrderByDescending(e => e.ShiftStart).First();

            // Build the canonical pattern from the most recent week's data
            var extracted = ExtractPattern(firstShift);

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
        var now    = DateTime.UtcNow;
        var day    = startDate;

        while (day <= endDate)
        {
            var dow     = (byte)day.DayOfWeek;
            var pattern = template.ScheduleShiftPatterns.FirstOrDefault(p => p.DayOfWeek == dow);

            if (pattern != null)
            {
                var isException = exceptions.Any(e => e.StartDate <= day && e.EndDate >= day);

                if (!isException)
                {
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
            }

            day = day.AddDays(1);
        }

        return await Task.FromResult(shifts);
    }

    /// <summary>
    /// Returns the next scheduled shift for the employee, looking up to 90 days ahead.
    /// Respects ScheduleExceptions (e.g. approved time off).
    /// </summary>
    public async Task<ScheduledShift?> GetNextScheduledShiftForUserAsync(string adLogin)
    {
        var today      = DateOnly.FromDateTime(DateTime.UtcNow);
        var lookAhead  = today.AddDays(90);
        var shifts     = await GetScheduledShiftsForUserAsync(adLogin, today, lookAhead);
        var now        = DateTime.UtcNow;
        return shifts.Where(s => s.StartTime > now).OrderBy(s => s.StartTime).FirstOrDefault();
    }

    /// <summary>
    /// Calculates total scheduled hours (net of breaks) for the employee's week.
    /// </summary>
    public async Task<double> GetWeeklyHoursForUserAsync(string adLogin, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        var shifts  = await GetScheduledShiftsForUserAsync(adLogin, weekStart, weekEnd);
        return shifts.Sum(s =>
            (s.EndTime - s.StartTime).TotalHours
            - s.Breaks.Sum(b => (b.EndTime - b.StartTime).TotalMinutes) / 60.0);
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
    /// Extracts the day-of-week pattern from one EtimeShift week row.
    /// Working days = every weekday between ShiftStart.Date and ShiftEnd.Date.
    /// All working days share the same start/end time from the row.
    /// Break minutes are divided evenly across working days.
    /// </summary>
    private static List<ShiftPatternEntry> ExtractPattern(EtimeShift shift)
    {
        var startTime = TimeOnly.FromDateTime(shift.ShiftStart);
        var endTime   = TimeOnly.FromDateTime(shift.ShiftEnd);

        var workDays = new List<byte>();
        var day = shift.ShiftStart.Date;
        while (day <= shift.ShiftEnd.Date)
        {
            var dow = (byte)day.DayOfWeek;
            if (dow != 0 && dow != 6)   // exclude Sunday=0, Saturday=6
                workDays.Add(dow);
            day = day.AddDays(1);
        }

        var breakPerDay = workDays.Count > 0 ? shift.BreakMin / workDays.Count : 0;

        return workDays
            .Distinct()
            .OrderBy(d => d)
            .Select(d => new ShiftPatternEntry(d, startTime, endTime, breakPerDay, shift.PayCode))
            .ToList();
    }

    private static bool PatternsMatch(
        List<ScheduleShiftPattern> existing,
        List<ShiftPatternEntry> extracted)
    {
        if (existing.Count != extracted.Count) return false;

        foreach (var e in extracted)
        {
            var match = existing.FirstOrDefault(p => p.DayOfWeek == e.DayOfWeek);
            if (match == null)                          return false;
            if (match.ShiftStartTime != e.StartTime)   return false;
            if (match.ShiftEndTime   != e.EndTime)     return false;
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
        int BreakMinutes, string? PayCode);
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
