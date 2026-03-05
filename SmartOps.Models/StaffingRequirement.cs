namespace SmartOps.Models;

/// <summary>
/// Represents staffing requirements for a specific division, client, time slot, and day of week.
/// Used to determine how many employees are needed at specific times.
/// </summary>
public class StaffingRequirement
{
    public int Id { get; set; }
    
    /// <summary>
    /// Division/Department (e.g., "Sales", "Support", "Billing")
    /// </summary>
    public string Division { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional client identifier (null if applies to all clients in division)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client name for display purposes
    /// </summary>
    public string? ClientName { get; set; }
    
    /// <summary>
    /// Day of week (0 = Sunday, 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }
    
    /// <summary>
    /// Hour of day (0-23)
    /// </summary>
    public int HourOfDay { get; set; }
    
    /// <summary>
    /// Required number of staff for this time slot
    /// </summary>
    public int RequiredHeadCount { get; set; }
    
    /// <summary>
    /// Expected call volume for forecasting purposes
    /// </summary>
    public int? ExpectedCallVolume { get; set; }
    
    /// <summary>
    /// List of required skill codes (comma-separated)
    /// </summary>
    public string? RequiredSkills { get; set; }
    
    /// <summary>
    /// When this requirement was created/last updated
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User who last modified this requirement
    /// </summary>
    public string? ModifiedBy { get; set; }
}
