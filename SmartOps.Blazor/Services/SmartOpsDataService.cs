using System.Net.Http.Json;
using System.Web;
using SmartOps.Models;

namespace SmartOps.Blazor.Services;

/// <summary>
/// Service for retrieving SmartOps data from the SmartOps Management Web API.
/// All UI data requests should go through this service.
/// </summary>
public class SmartOpsDataService : ISmartOpsDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmartOpsDataService> _logger;

    public SmartOpsDataService(HttpClient httpClient, ILogger<SmartOpsDataService> logger)
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

    /// <inheritdoc />
    public async Task<List<Workgroup>> GetWorkGroupsAsync()
    {
        try
        {
            var groups = await _httpClient.GetFromJsonAsync<List<Workgroup>>("api/workgroups");
            return groups ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workgroups");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<WorkGroupMemberDto?> AddWorkGroupMemberAsync(int workGroupId, string adloginName, string addedBy)
    {
        try
        {
            var request = new AddWorkGroupMemberRequest { AdloginName = adloginName, AddedBy = addedBy };
            var response = await _httpClient.PostAsJsonAsync($"api/workgroups/{workGroupId}/members", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<WorkGroupMemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {AdloginName} to workgroup {WorkGroupId}", adloginName, workGroupId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveWorkGroupMemberAsync(int workGroupId, string adloginName, string removedBy)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/workgroups/{workGroupId}/members/{Uri.EscapeDataString(adloginName)}?removedBy={Uri.EscapeDataString(removedBy)}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {AdloginName} from workgroup {WorkGroupId}", adloginName, workGroupId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<TimeOffRequestDto>> GetTeamTimeOffRequestsAsync(IEnumerable<string> adLoginNames)
    {
        try
        {
            var logins = adLoginNames.ToList();
            if (logins.Count == 0) return [];

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            foreach (var login in logins)
                queryParams.Add("logins", login);

            var result = await _httpClient.GetFromJsonAsync<List<TimeOffRequestDto>>($"api/timeoff/team?{queryParams}");
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching team time-off requests");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<TimeOffRequestDto?> ApproveTimeOffRequestAsync(int requestId, string reviewedBy, string? notes)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/timeoff/{requestId}/approve",
                new { ReviewedBy = reviewedBy, Notes = notes });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TimeOffRequestDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving time-off request {RequestId}", requestId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TimeOffRequestDto?> DenyTimeOffRequestAsync(int requestId, string reviewedBy, string? notes)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/timeoff/{requestId}/deny",
                new { ReviewedBy = reviewedBy, Notes = notes });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TimeOffRequestDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error denying time-off request {RequestId}", requestId);
            return null;
        }
    }
}
