namespace SmartOps.Models;

/// <summary>
/// Represents a scheduled shift for an employee.
/// This is the actual assignment of an employee to a time slot.
/// </summary>
public class ScheduledShift
{
    public int Id { get; set; }
    
    /// <summary>
    /// Employee ID (foreign key to ApplicationUser)
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Division for this shift
    /// </summary>
    public string Division { get; set; } = string.Empty;
    
    /// <summary>
    /// Client ID (optional)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client name for display
    /// </summary>
    public string? ClientName { get; set; }
    
    /// <summary>
    /// Start date and time of the shift
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End date and time of the shift
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Shift status (Scheduled, InProgress, Completed, Cancelled)
    /// </summary>
    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;
    
    /// <summary>
    /// Assigned breaks for this shift
    /// </summary>
    public List<ScheduledBreak> Breaks { get; set; } = new();
    
    /// <summary>
    /// Notes about this shift
    /// </summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User who created/modified this shift
    /// </summary>
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Break scheduled within a shift
/// </summary>
public class ScheduledBreak
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to ScheduledShift
    /// </summary>
    public int ScheduledShiftId { get; set; }
    
    /// <summary>
    /// Start time of the break
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time of the break
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Break type (e.g., "Lunch", "Break")
    /// </summary>
    public string BreakType { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this break paid
    /// </summary>
    public bool IsPaid { get; set; }
    
    /// <summary>
    /// Was this break actually taken
    /// </summary>
    public bool IsTaken { get; set; } = false;
    
    public ScheduledShift? ScheduledShift { get; set; }
}

public enum ShiftStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}
