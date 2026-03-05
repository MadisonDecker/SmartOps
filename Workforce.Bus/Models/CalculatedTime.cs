namespace Workforce.Bus.Models;

public class CalculatedTime
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public List<TimeRecord> TimeRecords { get; set; } = new();
}

public class TimeRecord
{
    public string? EmployeeJob { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime? StartTimestamp { get; set; }
    public DateTime? EndTimestamp { get; set; }
    public string PayCode { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string? Hours { get; set; }
    public string? GrossPay { get; set; }
    public string? EffectiveRate { get; set; }
    public string? Amount { get; set; }
    public string? PayCurrencyCode { get; set; }
    public int Index { get; set; }
    public Dictionary<string, string>? AdditionalFields { get; set; }
}
