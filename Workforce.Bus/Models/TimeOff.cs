namespace Workforce.Bus.Models;

public class TimeOff
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string TimeOffId { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public string? ExternalJobId { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Comments { get; set; }
    public DateTime RequestMadeAt { get; set; }
    public DateOnly StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TimeOffType { get; set; } = string.Empty;
    public List<TimeOffDetail> Details { get; set; } = new();
}

public class TimeOffDetail
{
    public string CodeId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime? EndDateTime { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime? StartDateTime { get; set; }
    public string? Unit { get; set; }
}
