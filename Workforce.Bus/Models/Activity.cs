namespace Workforce.Bus.Models;

public class Activity
{
    public string ActivityId { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string? Task { get; set; }
    public string? TaskCode { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? LocationCode { get; set; }
    public int? DurationInSeconds { get; set; }
    public ActivitySource? Source { get; set; }
}

public class ActivitySource
{
    public string Method { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
