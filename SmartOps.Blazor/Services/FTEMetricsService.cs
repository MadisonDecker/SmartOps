using System.Net.Http.Json;
using System.Web;
using SmartOps.Models;

namespace SmartOps.Blazor.Services;

/// <summary>
/// Service for retrieving FTE metrics from the SmartOps Management Web API.
/// </summary>
public class FTEMetricsService : IFTEMetricsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FTEMetricsService> _logger;

    public FTEMetricsService(HttpClient httpClient, ILogger<FTEMetricsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WeeklyFTEMetrics> GetWeeklyMetricsAsync(
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

            var url = $"api/fte-metrics/weekly?{queryParams}";
            
            var metrics = await _httpClient.GetFromJsonAsync<WeeklyFTEMetrics>(url);
            
            return metrics ?? new WeeklyFTEMetrics { WeekStart = weekStart };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weekly FTE metrics for week starting {WeekStart}", weekStart);
            
            // Return empty metrics on error
            return new WeeklyFTEMetrics { WeekStart = weekStart };
        }
    }
}
