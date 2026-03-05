namespace SmartOps.Models;

/// <summary>
/// Represents a client that the organization provides services to.
/// Clients can be associated with one or more workgroups.
/// </summary>
public class Client
{
    /// <summary>
    /// Unique identifier for the client.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique client code/identifier (e.g., "ACME", "CONTOSO").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the client.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes about the client.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the client is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to the associated workgroup.
    /// </summary>
    public int? WorkgroupId { get; set; }

    /// <summary>
    /// Navigation property for the associated workgroup.
    /// </summary>
    public Workgroup? Workgroup { get; set; }
}
