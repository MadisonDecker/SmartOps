using System.Text.Json;
using SmartManagement.Repo.Models;

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
                PersonNum = s.PersonNum,
                PayGroup = s.PayGroup ?? string.Empty,
                PayCodeId = s.PayCodeId,
                PayCode = s.PayCode != null && int.TryParse(s.PayCode, out var pc) ? pc : null,
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
    public string PersonNum { get; set; } = string.Empty;
    public string? PayGroup { get; set; }
    public int? PayCodeId { get; set; }
    public string? PayCode { get; set; }
    public DateTime StartDtm { get; set; }
    public DateTime EndDtm { get; set; }
    public int BreakMin { get; set; }
}
