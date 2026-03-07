using System.Net.Http.Json;
using System.Web;
using SmartOps.Models;

namespace SmartOps.Blazor.Services;

/// <summary>
/// Service for retrieving staffing metrics from the SmartOps Management Web API.
/// </summary>
public class StaffingMetricsService : IStaffingMetricsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StaffingMetricsService> _logger;

    public StaffingMetricsService(HttpClient httpClient, ILogger<StaffingMetricsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WeeklyStaffingMetrics> GetWeeklyMetricsAsync(
        DateTime weekStart,
        IEnumerable<int>? workgroupIds = null,
        int? clientId = null)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["weekStart"] = weekStart.ToString("yyyy-MM-dd");

            if (workgroupIds?.Any() == true)
            {
                foreach (var id in workgroupIds)
                {
                    queryParams.Add("workgroupIds", id.ToString());
                }
            }

            if (clientId.HasValue)
            {
                queryParams["clientId"] = clientId.Value.ToString();
            }

            var url = $"api/staffing-metrics/weekly?{queryParams}";
            
            var metrics = await _httpClient.GetFromJsonAsync<WeeklyStaffingMetrics>(url);
            
            return metrics ?? new WeeklyStaffingMetrics { WeekStart = weekStart };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weekly staffing metrics for week starting {WeekStart}", weekStart);
            
            // Return empty metrics on error
            return new WeeklyStaffingMetrics { WeekStart = weekStart };
        }
    }
}
