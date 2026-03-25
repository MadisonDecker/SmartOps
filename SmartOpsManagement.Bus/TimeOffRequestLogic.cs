using SmartManagement.Repo.Models;
using SmartOps.Models;

namespace SmartOpsManagement.Bus;

public partial class SmartOpsBusinessLogic
{
    /// <summary>
    /// Returns all time-off requests for the given list of employees, newest first.
    /// </summary>
    public async Task<List<TimeOffRequestDto>> GetTimeOffRequestsForTeamAsync(IEnumerable<string> adLoginNames)
    {
        var logins = adLoginNames.ToList();
        return await Task.FromResult(
            _context.TimeOffRequests
                .Where(r => logins.Contains(r.AdloginName))
                .OrderByDescending(r => r.RequestedOn)
                .Select(r => MapToDto(r))
                .ToList());
    }

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
    /// Persists a new time-off request.
    /// Enforces that the first requested day is more than 7 days from today
    /// and that no overlapping active request already exists.
    /// </summary>
    public async Task<(TimeOffRequestDto? Result, string? Error)> SubmitTimeOffRequestAsync(TimeOffRequestDto dto)
    {
        if (dto.StartDate.ToDateTime(TimeOnly.MinValue) <= DateTime.Today.AddDays(7))
            return (null, "Time-off requests can only be submitted for dates more than 7 days in the future.");

        // Guard against overlapping active requests (Pending or Approved)
        var duplicate = _context.TimeOffRequests.Any(r =>
            r.AdloginName == dto.AdloginName  &&
            r.StartDate   <= dto.EndDate       &&
            r.EndDate     >= dto.StartDate     &&
            r.StatusId != (byte)TimeOffStatus.Denied    &&
            r.StatusId != (byte)TimeOffStatus.Cancelled);

        if (duplicate)
            return (null, "An active time-off request already exists for this date range.");

        var now = DateTime.UtcNow;
        var entity = new TimeOffRequest
        {
            AdloginName      = dto.AdloginName,
            StartDate        = dto.StartDate,
            EndDate          = dto.EndDate,
            Reason           = dto.Reason,
            StatusId         = (byte)TimeOffStatus.Pending,
            RequestedOn      = now,
            InsertedDateUtc  = now,
            LastUpdatedUtc   = now,
            IsPartialShift   = dto.IsPartialShift,
            PartialStart     = dto.PartialStart,
            PartialEnd       = dto.PartialEnd,
            PlanToMakeUpTime = dto.PlanToMakeUpTime,
            MakeUpStart      = dto.MakeUpStart,
            MakeUpEnd        = dto.MakeUpEnd
        };

        _context.TimeOffRequests.Add(entity);
        await _context.SaveChangesAsync();

        dto.TimeOffRequestId = entity.TimeOffRequestId;
        dto.Status           = TimeOffStatus.Pending;
        dto.RequestedOn      = now;
        return (dto, null);
    }

    /// <summary>
    /// Approves or denies a pending time-off request.
    /// On approval, creates a ScheduleException that will suppress the
    /// employee's shifts for the requested date range.
    /// </summary>
    public async Task<(TimeOffRequestDto? Result, string? Error)> ReviewTimeOffRequestAsync(
        int timeOffRequestId, bool approved, string reviewedBy, string? notes)
    {
        var entity = _context.TimeOffRequests.FirstOrDefault(r => r.TimeOffRequestId == timeOffRequestId);
        if (entity == null)
            return (null, "Time-off request not found.");

        if (entity.StatusId != (byte)TimeOffStatus.Pending)
            return (null, "Only pending requests can be reviewed.");

        var now = DateTime.UtcNow;
        entity.StatusId       = approved ? (byte)TimeOffStatus.Approved : (byte)TimeOffStatus.Denied;
        entity.ReviewedBy     = reviewedBy;
        entity.ReviewedOn     = now;
        entity.ReviewNotes    = notes;
        entity.LastUpdatedUtc = now;

        if (approved)
        {
            // Attach a ScheduleException to suppress the employee's shifts
            var template = _context.ScheduleTemplates
                .FirstOrDefault(t =>
                    t.AdloginName == entity.AdloginName &&
                    t.EndDate     == null);

            if (template != null)
            {
                var exception = new ScheduleException
                {
                    AdloginName        = entity.AdloginName,
                    ScheduleTemplateId = template.ScheduleTemplateId,
                    ExceptionTypeId    = 1,  // TimeOff (seed value)
                    StartDate          = entity.StartDate,
                    EndDate            = entity.EndDate,
                    TimeOffRequestId   = entity.TimeOffRequestId,
                    Notes              = entity.Reason,
                    CreatedBy          = reviewedBy,
                    InsertedDateUtc    = now,
                    LastUpdatedUtc     = now
                };

                _context.ScheduleExceptions.Add(exception);
                await _context.SaveChangesAsync();

                entity.ScheduleExceptionId = exception.ScheduleExceptionId;
                entity.LastUpdatedUtc      = now;
            }
        }

        await _context.SaveChangesAsync();
        return (MapToDto(entity), null);
    }

    private static TimeOffRequestDto MapToDto(TimeOffRequest r) => new()
    {
        TimeOffRequestId    = r.TimeOffRequestId,
        AdloginName         = r.AdloginName,
        StartDate           = r.StartDate,
        EndDate             = r.EndDate,
        Reason              = r.Reason,
        Status              = (TimeOffStatus)r.StatusId,
        RequestedOn         = r.RequestedOn,
        ReviewedBy          = r.ReviewedBy,
        ReviewedOn          = r.ReviewedOn,
        ReviewNotes         = r.ReviewNotes,
        ScheduleExceptionId = r.ScheduleExceptionId,
        IsPartialShift      = r.IsPartialShift,
        PartialStart        = r.PartialStart,
        PartialEnd          = r.PartialEnd,
        PlanToMakeUpTime    = r.PlanToMakeUpTime,
        MakeUpStart         = r.MakeUpStart,
        MakeUpEnd           = r.MakeUpEnd
    };
}
