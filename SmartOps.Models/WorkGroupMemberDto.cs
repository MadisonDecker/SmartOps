namespace SmartOps.Models;

/// <summary>
/// Represents an active member of a WorkGroup.
/// </summary>
public class WorkGroupMemberDto
{
    public int WorkGroupMemberId { get; set; }
    public int WorkGroupId { get; set; }
    public string AdloginName { get; set; } = string.Empty;
    public DateTime AddedDateUtc { get; set; }
    public string AddedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request body for adding a member to a WorkGroup.
/// </summary>
public class AddWorkGroupMemberRequest
{
    public string AdloginName { get; set; } = string.Empty;
    public string AddedBy { get; set; } = string.Empty;
}
