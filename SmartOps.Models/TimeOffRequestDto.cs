namespace SmartOps.Models;

/// <summary>
/// Data transfer object for time-off requests.
/// Shared between the Blazor UI and the Web API.
/// </summary>
public class TimeOffRequestDto
{
    public int TimeOffRequestId { get; set; }
    public string AdloginName { get; set; } = string.Empty;
    public int EtimeShiftId { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeOffStatus Status { get; set; } = TimeOffStatus.Pending;
    public DateTime RequestedOn { get; set; }

    // Supervisor review fields
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewNotes { get; set; }

    // Schedule update tracking
    public bool ScheduleUpdated { get; set; }
}

public enum TimeOffStatus : byte
{
    Pending   = 1,
    Approved  = 2,
    Denied    = 3,
    Cancelled = 4
}
