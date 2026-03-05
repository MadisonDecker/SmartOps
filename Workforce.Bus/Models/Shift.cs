namespace Workforce.Bus.Models;

public class Shift
{
    public string ShiftId { get; set; } = string.Empty;
    public string? ShiftName { get; set; }
    public string? JobId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? DurationInSeconds { get; set; }
    public string? ServiceSpecificId { get; set; }
    public string? Status { get; set; }
    public List<Activity> Activities { get; set; } = new();
    public List<ShiftBreak> Breaks { get; set; } = new();
    public string? Signature { get; set; }
    public string? EmployeeJob { get; set; }
}

public class ShiftBreak
{
    public string BreakId { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool? IsPaid { get; set; }
    public int? DurationInSeconds { get; set; }
}

public class ShiftDay
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public List<Shift> Shifts { get; set; } = new();
}
