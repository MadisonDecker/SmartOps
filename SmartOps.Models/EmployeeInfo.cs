namespace SmartOps.Models;

/// <summary>
/// Extended employee information linked to ApplicationUser.
/// Stores workforce-specific data like division, supervisor, etc.
/// </summary>
public class EmployeeInfo
{
    public int Id { get; set; }
    
    /// <summary>
    /// Employee ID (foreign key to ApplicationUser)
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Employee's assigned division
    /// </summary>
    public string Division { get; set; } = string.Empty;
    
    /// <summary>
    /// Supervisor's user ID (ApplicationUser.Id)
    /// </summary>
    public string? SupervisorId { get; set; }
    
    /// <summary>
    /// Supervisor's display name (cached for convenience)
    /// </summary>
    public string? SupervisorName { get; set; }
    
    /// <summary>
    /// Is this employee active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Employment start date
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    /// <summary>
    /// Employment end date (if terminated)
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Preferred contact phone
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Maximum hours per week allowed
    /// </summary>
    public decimal? MaxHoursPerWeek { get; set; }
    
    /// <summary>
    /// Notes about employee
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Skills this employee has
    /// </summary>
    public List<EmployeeSkill> Skills { get; set; } = new();
    
    /// <summary>
    /// Scheduled shifts for this employee
    /// </summary>
    public List<ScheduledShift> ScheduledShifts { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
