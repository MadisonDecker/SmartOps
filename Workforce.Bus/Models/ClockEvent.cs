namespace Workforce.Bus.Models;

public class ClockEvent
{
    public string? ExternalMatchId { get; set; }
    public string? JobId { get; set; }
    public string? UserId { get; set; }
    public string? DisplayEmployee { get; set; }
    public ClockBadge? Badge { get; set; }
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? PayCode { get; set; }
    public string? ClockId { get; set; }
    public string? EventType { get; set; }
    public GeoLocation? GeoLocation { get; set; }
    public string? TransferOutPayCode { get; set; }
}

public class ClockBadge
{
    public string Group { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class GeoLocation
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Accuracy { get; set; }
}
