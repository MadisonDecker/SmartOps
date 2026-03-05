namespace SmartOps.Models;

/// <summary>
/// Represents a skill or certification that an employee possesses.
/// Used to track employee qualifications for assignment to specific shifts/clients.
/// </summary>
public class EmployeeSkill
{
    public int Id { get; set; }
    
    /// <summary>
    /// Employee ID (foreign key to ApplicationUser)
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Skill/Certification code (e.g., "BILINGUAL_ES", "TIER2", "VOICE_QUALITY")
    /// </summary>
    public string SkillCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Skill name for display
    /// </summary>
    public string SkillName { get; set; } = string.Empty;
    
    /// <summary>
    /// Proficiency level (1-5 or "Basic", "Intermediate", "Advanced")
    /// </summary>
    public string? ProficiencyLevel { get; set; }
    
    /// <summary>
    /// Date when this skill became effective
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
    
    /// <summary>
    /// Date when this skill expires (null if no expiration)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    
    /// <summary>
    /// Is this skill currently active/valid
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Notes or additional certification details
    /// </summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
