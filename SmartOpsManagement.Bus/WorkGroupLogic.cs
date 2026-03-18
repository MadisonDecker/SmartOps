using Microsoft.EntityFrameworkCore;
using SmartManagement.Repo.Models;
using SmartOps.Models;

namespace SmartOpsManagement.Bus;

public partial class SmartOpsBusinessLogic
{
    /// <summary>
    /// Returns all active WorkGroups with their current members.
    /// </summary>
    public async Task<List<Workgroup>> GetWorkGroupsAsync()
    {
        var groups = await _context.WorkGroups
            .Where(g => g.IsActive)
            .Include(g => g.WorkGroupMembers.Where(m => m.RemovedDateUtc == null))
            .OrderBy(g => g.Name)
            .ToListAsync();

        return groups.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single WorkGroup with its current members, or null if not found.
    /// </summary>
    public async Task<Workgroup?> GetWorkGroupByIdAsync(int workGroupId)
    {
        var group = await _context.WorkGroups
            .Include(g => g.WorkGroupMembers.Where(m => m.RemovedDateUtc == null))
            .FirstOrDefaultAsync(g => g.WorkGroupId == workGroupId);

        return group == null ? null : MapToDto(group);
    }

    /// <summary>
    /// Adds an employee to a WorkGroup. Returns the new member record or an error.
    /// </summary>
    public async Task<(WorkGroupMemberDto? Result, string? Error)> AddWorkGroupMemberAsync(
        int workGroupId, string adloginName, string addedBy)
    {
        var group = await _context.WorkGroups.FindAsync(workGroupId);
        if (group == null)
            return (null, "WorkGroup not found.");

        var alreadyMember = _context.WorkGroupMembers.Any(m =>
            m.WorkGroupId == workGroupId &&
            m.AdloginName == adloginName &&
            m.RemovedDateUtc == null);

        if (alreadyMember)
            return (null, "Employee is already an active member of this workgroup.");

        var now = DateTime.UtcNow;
        var member = new WorkGroupMember
        {
            WorkGroupId  = workGroupId,
            AdloginName  = adloginName,
            AddedBy      = addedBy,
            AddedDateUtc = now
        };

        _context.WorkGroupMembers.Add(member);
        await _context.SaveChangesAsync();

        return (MapMemberToDto(member), null);
    }

    /// <summary>
    /// Soft-removes an employee from a WorkGroup by setting RemovedDateUtc.
    /// </summary>
    public async Task<(bool Success, string? Error)> RemoveWorkGroupMemberAsync(
        int workGroupId, string adloginName, string removedBy)
    {
        var member = _context.WorkGroupMembers.FirstOrDefault(m =>
            m.WorkGroupId == workGroupId &&
            m.AdloginName == adloginName &&
            m.RemovedDateUtc == null);

        if (member == null)
            return (false, "Active membership not found.");

        member.RemovedDateUtc = DateTime.UtcNow;
        member.RemovedBy      = removedBy;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    private static Workgroup MapToDto(WorkGroup g) => new()
    {
        Id          = g.WorkGroupId,
        Name        = g.Name,
        Description = g.Description,
        IsActive    = g.IsActive,
        Members     = g.WorkGroupMembers.Select(MapMemberToDto).ToList()
    };

    private static WorkGroupMemberDto MapMemberToDto(WorkGroupMember m) => new()
    {
        WorkGroupMemberId = m.WorkGroupMemberId,
        WorkGroupId       = m.WorkGroupId,
        AdloginName       = m.AdloginName,
        AddedDateUtc      = m.AddedDateUtc,
        AddedBy           = m.AddedBy
    };
}
