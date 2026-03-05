namespace SmartOps.Models;

/// <summary>
/// Defines mandatory break rules for a division.
/// Ensures employees get required breaks based on shift duration.
/// </summary>
public class BreakTemplate
{
    public int Id { get; set; }
    
    /// <summary>
    /// Division/Department these break rules apply to
    /// </summary>
    public string Division { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum shift duration (in minutes) that requires a break
    /// </summary>
    public int MinShiftDurationMinutes { get; set; }
    
    /// <summary>
    /// Break rules for this template
    /// </summary>
    public List<BreakRule> BreakRules { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual break rule within a template
/// </summary>
public class BreakRule
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to BreakTemplate
    /// </summary>
    public int BreakTemplateId { get; set; }
    
    /// <summary>
    /// Minutes into shift when break is due (0-based)
    /// </summary>
    public int MinutesIntoBreach { get; set; }
    
    /// <summary>
    /// Duration of break in minutes
    /// </summary>
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// Is this break paid or unpaid
    /// </summary>
    public bool IsPaid { get; set; } = false;
    
    /// <summary>
    /// Break type (e.g., "Lunch", "15 Minute Break", "Rest Break")
    /// </summary>
    public string BreakType { get; set; } = string.Empty;
    
    /// <summary>
    /// Order in which breaks should be taken
    /// </summary>
    public int SequenceOrder { get; set; }
    
    public BreakTemplate? BreakTemplate { get; set; }
}
