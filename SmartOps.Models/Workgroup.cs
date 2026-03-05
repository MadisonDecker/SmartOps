namespace SmartOps.Models;

/// <summary>
/// Represents a workgroup/division within the organization.
/// Workgroups are used to organize employees and staffing requirements.
/// </summary>
public class Workgroup
{
    /// <summary>
    /// Unique identifier for the workgroup.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the workgroup (e.g., "SALES", "SUPPORT").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the workgroup (e.g., "Sales", "Customer Support").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the workgroup's responsibilities.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the workgroup is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property for clients associated with this workgroup.
    /// </summary>
    public List<Client> Clients { get; set; } = [];
}
