using SmartOps.Models;

namespace SmartOpsManagement.Bus;

/// <summary>
/// Business logic for WeeklyFTEMetrics operations.
/// </summary>
public partial class SmartOpsBusinessLogic
{
    /// <summary>
    /// Gets the weekly FTE metrics for the specified week and optional filters.
    /// </summary>
    /// <param name="weekStart">The start date of the week.</param>
    /// <param name="workgroupIds">Optional list of workgroup IDs to filter by.</param>
    /// <param name="clientId">Optional client ID to filter by.</param>
    /// <returns>The weekly FTE metrics.</returns>
    public async Task<WeeklyFTEMetrics> GetWeeklyFTEMetricsAsync(
        DateTime weekStart,
        IEnumerable<int>? workgroupIds = null,
        int? clientId = null)
    {
        // TODO: Replace with actual repository call when available
        // For now, generate test data
        await Task.Delay(50); // Simulate async operation

        var random = new Random(weekStart.GetHashCode());
        var baseRequired = 25.0m + (decimal)(random.NextDouble() * 15);
        var baseScheduled = baseRequired * (0.75m + (decimal)(random.NextDouble() * 0.35));

        // Adjust based on filters
        if (workgroupIds?.Any() == true)
        {
            var workgroupCount = workgroupIds.Count();
            baseRequired *= workgroupCount * 0.4m;
            baseScheduled *= workgroupCount * 0.4m;
        }

        if (clientId.HasValue)
        {
            baseRequired *= 0.3m;
            baseScheduled *= 0.3m;
        }

        return new WeeklyFTEMetrics
        {
            WeekStart = weekStart,
            Required = Math.Round(baseRequired, 1),
            Scheduled = Math.Round(baseScheduled, 1)
        };
    }
}
