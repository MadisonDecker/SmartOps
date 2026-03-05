using SmartOps.Models;

namespace SmartOps.Blazor.Services;

/// <summary>
/// Service interface for retrieving FTE metrics from the Web API.
/// </summary>
public interface IFTEMetricsService
{
    /// <summary>
    /// Gets the weekly FTE metrics for the specified week and optional filters.
    /// </summary>
    /// <param name="weekStart">The start date of the week.</param>
    /// <param name="workgroupIds">Optional list of workgroup IDs to filter by.</param>
    /// <param name="clientId">Optional client ID to filter by.</param>
    /// <returns>The weekly FTE metrics.</returns>
    Task<WeeklyFTEMetrics> GetWeeklyMetricsAsync(
        DateTime weekStart,
        IEnumerable<int>? workgroupIds = null,
        int? clientId = null);
}
