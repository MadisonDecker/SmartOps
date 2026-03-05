namespace SmartOps.Models;

/// <summary>
/// Represents weekly Full-Time Equivalent (FTE) staffing metrics.
/// </summary>
public class WeeklyFTEMetrics
{
    /// <summary>
    /// The start date of the week these metrics apply to.
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Total FTE required for the week based on staffing requirements.
    /// </summary>
    public decimal Required { get; set; }

    /// <summary>
    /// Total FTE scheduled for the week based on employee shifts.
    /// </summary>
    public decimal Scheduled { get; set; }

    /// <summary>
    /// The gap between required and scheduled FTE (positive means understaffed).
    /// </summary>
    public decimal Gap => Required - Scheduled;

    /// <summary>
    /// Coverage efficiency as a percentage (Scheduled / Required * 100).
    /// Returns 0 if no FTE is required.
    /// </summary>
    public decimal EfficiencyPercent => Required > 0 ? Scheduled / Required * 100 : 0;
}
