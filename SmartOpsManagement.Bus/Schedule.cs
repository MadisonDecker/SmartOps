using SmartManagement.Repo.Models;
using System.ComponentModel;
using System.Text.Json;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace SmartOpsManagement.Bus;

/// <summary>
/// Schedule related business logic. Getting schedules, processing schedules, etc.
/// </summary>
public partial class SmartOpsBusinessLogic
{
    /// <summary>
    /// Imports Etime schedules from a JSON file into the database.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file containing schedule records.</param>
    /// <returns>Number of records imported, or -1 if failed.</returns>
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

            // Map import records to EtimeShift entities
            var etimeShifts = schedules.Select(s => new EtimeShift
            {
                ShiftCodeId = s.ShiftCodeId,
                AdloginName = s.NtLoginName,
                PersonNum = int.TryParse(s.PersonNum, out var pn) ? pn : 0,
                EmplId = s.EmplId != null && int.TryParse(s.EmplId.ToString(), out var emp) ? emp : 0,
                FileNumber = s.FileNumber != null && int.TryParse(s.FileNumber.ToString(), out var fl) ? fl : 0,
                PayGroup = s.PayGroup ?? string.Empty,
                PayCodeId = s.PayCodeId != null ? s.PayCodeId : null,
                PayCode = string.IsNullOrWhiteSpace(s.PayCode) ? null : s.PayCode,
                ShiftStart = s.StartDtm,
                ShiftEnd = s.EndDtm,
                BreakMin = s.BreakMin
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

    /// <summary>
    /// Convert Etime shifts to new schedule format.
    /// Groups EtimeShifts by ADLoginName and ShiftCodeId to create Schedule records,
    /// then creates Shift records from ShiftStart/ShiftEnd times.
    /// </summary>
    public async Task<bool> ConvertEtimeShiftsToSmartOpsSchedulesAsync(DateTime startDate, DateTime endDate)
    {
        var etimeShifts = await GetEtimeShiftsByDateRangeAsync(startDate, endDate);

        if (etimeShifts.Count == 0)
        {
            Console.WriteLine("No Etime shifts found for the specified date range.");
            return false;
        }

        var now = DateTime.UtcNow;

        // Group by ADLoginName to create Schedule records
        var scheduleGroups = etimeShifts
            .GroupBy(e => e.AdloginName)
            .ToList();

        foreach (var group in scheduleGroups)
        {
            var firstShift = group.First();
            var minStart = group.Min(e => e.ShiftStart);
            var maxEnd = group.Max(e => e.ShiftEnd);

            // Create the Schedule record
            var schedule = new Schedule
            {
                Name = group.Key,
                Adlogin = group.Key,
                ExternalMatchId = firstShift.FileNumber.ToString().PadLeft(8, '0'),
                PayGroup = firstShift.PayGroup ?? string.Empty,
                StartDate = minStart,
                EndDate = maxEnd,
                IsOngoing = true,
                EffectiveDate = DateOnly.FromDateTime(minStart),
                InsertedDateUtc = now,
                LastUpdatedUtc = now
            };

            _context.Schedules.Add(schedule);

            // Create Shift records for each EtimeShift in the group
            foreach (var etimeShift in group)
            {
                var shift = new Shift
                {
                    Schedule = schedule,
                    PayCode = etimeShift.PayCode ?? string.Empty,
                    StartTime = etimeShift.ShiftStart,
                    EndTime = etimeShift.ShiftEnd,
                    InsertedDateUtc = now,
                    LastUpdatedUtc = now
                };

                _context.Shifts.Add(shift);

                // Create ShiftBreak if there are break minutes
                if (etimeShift.BreakMin > 0)
                {
                    // Calculate break time - default to middle of shift
                    var breakStart = etimeShift.ShiftStart.AddHours(4);
                    var shiftBreak = new ShiftBreak
                    {
                        Schedule = schedule,
                        StartTime = breakStart,
                        EndTime = breakStart.AddMinutes(etimeShift.BreakMin),
                        InsertedDateUtc = now,
                        LastUpdatedUtc = now
                    };

                    _context.ShiftBreaks.Add(shiftBreak);
                }
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Created {scheduleGroups.Count} schedules with shifts from {etimeShifts.Count} Etime records.");
        return true;
    }

    /// <summary>
    /// Gets all Etime shifts from the database.
    /// </summary>
    public async Task<List<EtimeShift>> GetAllEtimeShiftsAsync()
    {
        return await Task.FromResult(_context.EtimeShifts.ToList());
    }

    /// <summary>
    /// Gets Etime shifts for a specific date range.
    /// </summary>
    public async Task<List<EtimeShift>> GetEtimeShiftsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(_context.EtimeShifts
            .Where(e => e.ShiftStart >= startDate && e.ShiftStart < endDate)
            .ToList());
    }
}

/// <summary>
/// Record structure for importing schedules from JSON.
/// </summary>
public class ScheduleImportRecord
{
    public int ShiftCodeId { get; set; }
    public string NtLoginName { get; set; } = string.Empty;
    public string PersonNum { get; set; } 
    public int? EmplId { get; set; }
    public int? FileNumber { get; set; }
    public string? PayGroup { get; set; }
    public int? PayCodeId { get; set; }
    public string? PayCode { get; set; }
    public DateTime StartDtm { get; set; }
    public DateTime EndDtm { get; set; }
    public int BreakMin { get; set; }
}
