namespace Workforce.Bus.Models;

public class Swipe
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string SwipeId { get; set; } = string.Empty;
    public DateTime SwipeTime { get; set; }
    public DateTime? SwipeProcessedTime { get; set; }
    public string PayCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public SwipeLocation? Location { get; set; }
    public string ExternalMatchId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string? ClockId { get; set; }
    public string? ClockType { get; set; }
}

public class SwipeLocation
{
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}
