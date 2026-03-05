namespace Workforce.Bus.Models;

public class BankEvent
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public List<BankDetail> Banks { get; set; } = new();
}

public class BankDetail
{
    public string BankId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public List<EventDetail> Events { get; set; } = new();
}

public class EventDetail
{
    public string? EmployeeJob { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public string PayCodeId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string PreviousBalance { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
