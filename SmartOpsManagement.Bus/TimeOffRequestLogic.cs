using SmartManagement.Repo.Models;
using SmartOps.Models;

namespace SmartOpsManagement.Bus;

public partial class SmartOpsBusinessLogic
{
    /// <summary>
    /// Returns all time-off requests for the given employee, newest first.
    /// </summary>
    public async Task<List<TimeOffRequestDto>> GetTimeOffRequestsForUserAsync(string adLoginName)
    {
        return await Task.FromResult(
            _context.TimeOffRequests
                .Where(r => r.AdloginName == adLoginName)
                .OrderByDescending(r => r.RequestedOn)
                .Select(r => MapToDto(r))
                .ToList());
    }

    /// <summary>
    /// Persists a new time-off request and returns it with the generated ID.
    /// Enforces that the shift starts more than 7 days from today.
    /// </summary>
    public async Task<(TimeOffRequestDto? Result, string? Error)> SubmitTimeOffRequestAsync(TimeOffRequestDto dto)
    {
        if (dto.ShiftStart.Date <= DateTime.Today.AddDays(7))
            return (null, "Time-off requests can only be submitted for shifts more than 7 days in the future.");

        // Guard against duplicate active requests (Pending or Approved) for the same shift
        var duplicate = _context.TimeOffRequests.Any(r =>
            r.EtimeShiftId == dto.EtimeShiftId &&
            r.AdloginName  == dto.AdloginName  &&
            r.StatusId != (byte)TimeOffStatus.Denied    &&
            r.StatusId != (byte)TimeOffStatus.Cancelled);

        if (duplicate)
            return (null, "An active time-off request already exists for this shift.");

        var now = DateTime.UtcNow;
        var entity = new TimeOffRequest
        {
            AdloginName      = dto.AdloginName,
            EtimeShiftId     = dto.EtimeShiftId,
            ShiftStart       = dto.ShiftStart,
            ShiftEnd         = dto.ShiftEnd,
            Reason           = dto.Reason,
            StatusId         = (byte)TimeOffStatus.Pending,
            RequestedOn      = now,
            InsertedDateUtc  = now,
            LastUpdatedUtc   = now
        };

        _context.TimeOffRequests.Add(entity);
        await _context.SaveChangesAsync();

        dto.TimeOffRequestId = entity.TimeOffRequestId;
        dto.Status           = TimeOffStatus.Pending;
        dto.RequestedOn      = now;
        return (dto, null);
    }

    private static TimeOffRequestDto MapToDto(TimeOffRequest r) => new()
    {
        TimeOffRequestId = r.TimeOffRequestId,
        AdloginName      = r.AdloginName,
        EtimeShiftId     = r.EtimeShiftId,
        ShiftStart       = r.ShiftStart,
        ShiftEnd         = r.ShiftEnd,
        Reason           = r.Reason,
        Status           = (TimeOffStatus)r.StatusId,
        RequestedOn      = r.RequestedOn,
        ReviewedBy       = r.ReviewedBy,
        ReviewedOn       = r.ReviewedOn,
        ReviewNotes      = r.ReviewNotes,
        ScheduleUpdated  = r.ScheduleUpdated
    };
}
