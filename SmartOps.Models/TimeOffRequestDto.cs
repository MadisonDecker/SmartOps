namespace SmartOps.Models;

/// <summary>
/// Data transfer object for time-off requests.
/// Shared between the Blazor UI and the Web API.
/// A request covers a date range (single day or multi-day vacation).
/// </summary>
public class TimeOffRequestDto
{
    public int TimeOffRequestId { get; set; }
    public string AdloginName { get; set; } = string.Empty;

    /// <summary>First day of the requested time off.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Last day of the requested time off (same as StartDate for a single shift).</summary>
    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;
    public TimeOffStatus Status { get; set; } = TimeOffStatus.Pending;
    public DateTime RequestedOn { get; set; }

    // Supervisor review
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewNotes { get; set; }

    // Populated when the request is approved and a ScheduleException is created
    public int? ScheduleExceptionId { get; set; }
}

public enum TimeOffStatus : byte
{
    Pending   = 1,
    Approved  = 2,
    Denied    = 3,
    Cancelled = 4
}
