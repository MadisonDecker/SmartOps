using SmartOps.Models;

namespace SmartOps.Blazor.Services;

/// <summary>
/// Service interface for retrieving SmartOps data from the Web API.
/// All UI data requests should go through this service.
/// </summary>
public interface ISmartOpsDataService
{
    /// <summary>
    /// Gets the weekly staffing metrics for the specified week and optional filters.
    /// </summary>
    /// <param name="weekStart">The start date of the week.</param>
    /// <param name="workgroupIds">Optional list of workgroup IDs to filter by.</param>
    /// <param name="clientId">Optional client ID to filter by.</param>
    /// <returns>The weekly staffing metrics.</returns>
    Task<WeeklyStaffingMetrics> GetWeeklyMetricsAsync(
        DateTime weekStart,
        IEnumerable<int>? workgroupIds = null,
        int? clientId = null);

    Task<List<Workgroup>> GetWorkGroupsAsync();

    Task<WorkGroupMemberDto?> AddWorkGroupMemberAsync(int workGroupId, string adloginName, string addedBy);

    Task<bool> RemoveWorkGroupMemberAsync(int workGroupId, string adloginName, string removedBy);
}
