namespace Workforce.Bus.Models;

public class TimeRequest
{
    public string ExternalMatchId { get; set; } = string.Empty;
    public string? SourceSystem { get; set; }
    public List<TimeRequestJob> Jobs { get; set; } = new();
}

public class TimeRequestJob
{
    public string ExternalJobId { get; set; } = string.Empty;
    public List<TimeRequestRecord> TimeData { get; set; } = new();
}

public class TimeRequestRecord
{
    public string PayCode { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime? StartTimestamp { get; set; }
    public DateTime? EndTimestamp { get; set; }
    public string? Hours { get; set; }
    public string? Amount { get; set; }
    public Dictionary<string, string>? AdditionalFields { get; set; }
}
