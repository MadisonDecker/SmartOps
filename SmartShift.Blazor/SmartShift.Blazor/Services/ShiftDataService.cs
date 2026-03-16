using System.Net.Http.Json;
using System.Text.Json;
using SmartOps.Models;

namespace SmartShift.Blazor.Services;

public class ShiftDataService : IShiftDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public ShiftDataService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("SmartOpsApi");
        var baseUrl = _configuration["SmartOpsApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        return client;
    }

    private string? GetUserIdentifierFromClaims()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        // Claim types to try, in order. This favors AD-style usernames where available.
        var claimTypesToTry = new[]
        {
            "preferred_username", // common in Azure AD tokens (could be user@domain)
            "upn",
            System.Security.Claims.ClaimTypes.Name, // display name or login depending on configuration
            System.Security.Claims.ClaimTypes.NameIdentifier,
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            "samaccountname",
        };

        foreach (var ct in claimTypesToTry)
        {
            var claim = user.FindFirst(ct);
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value;
            }
        }

        return null;
    }

    public async Task<EmployeeInfo> GetCurrentEmployeeAsync()
    {
        // Try to get the AD login from context if available. Prefer common username claims
        // (preferred_username, upn) and then fall back to other available name claims.
        var userId = GetUserIdentifierFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            // Fallback to a stubbed employee
            return new EmployeeInfo { Id = 0, EmployeeId = "unknown", Division = string.Empty, SupervisorId = string.Empty, SupervisorName = string.Empty, IsActive = false };
        }

        // Call API to get employee info if endpoint exists
        try
        {
            var client = CreateClient();
            var resp = await client.GetAsync($"api/schedules/employee/{Uri.EscapeDataString(userId)}/info");
            if (resp.IsSuccessStatusCode)
            {
                var employee = await resp.Content.ReadFromJsonAsync<EmployeeInfo>(cancellationToken: CancellationToken.None);
                if (employee != null) return employee;
            }
        }
        catch
        {
            // Ignore and fall back
        }

        return new EmployeeInfo { Id = 0, EmployeeId = userId, Division = string.Empty, SupervisorId = string.Empty, SupervisorName = string.Empty, IsActive = true };
    }

    public async Task<List<ScheduledShift>?> GetEmployeeShiftsAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var client = CreateClient();
            var url = $"api/schedules/employee/{Uri.EscapeDataString(employeeId)}";
            var query = new List<string>();
            if (startDate.HasValue) query.Add($"start={Uri.EscapeDataString(startDate.Value.ToString("o"))}");
            if (endDate.HasValue) query.Add($"end={Uri.EscapeDataString(endDate.Value.ToString("o"))}");
            if (query.Any()) url += "?" + string.Join("&", query);

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return new List<ScheduledShift>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var stream = await resp.Content.ReadAsStreamAsync();
            var shifts = await JsonSerializer.DeserializeAsync<List<ScheduledShift>>(stream, options);
            return shifts ?? new List<ScheduledShift>();
        }
        catch
        {
            return new List<ScheduledShift>();
        }
    }

    public async Task<List<EmployeeSkill>> GetEmployeeSkillsAsync(string employeeId)
    {
        try
        {
            var client = CreateClient();
            var resp = await client.GetAsync($"api/schedules/employee/{Uri.EscapeDataString(employeeId)}/skills");
            if (!resp.IsSuccessStatusCode) return new List<EmployeeSkill>();
            var skills = await resp.Content.ReadFromJsonAsync<List<EmployeeSkill>>();
            return skills ?? new List<EmployeeSkill>();
        }
        catch
        {
            return new List<EmployeeSkill>();
        }
    }

    public async Task<ScheduledShift?> GetNextShiftAsync(string employeeId)
    {
        try
        {
            var client = CreateClient();
            var resp = await client.GetAsync($"api/schedules/employee/{Uri.EscapeDataString(employeeId)}/next");
            if (!resp.IsSuccessStatusCode) return null;
            var shift = await resp.Content.ReadFromJsonAsync<ScheduledShift>();
            return shift;
        }
        catch
        {
            return null;
        }
    }

    public async Task<double> GetWeeklyHoursAsync(string employeeId, DateTime weekStart)
    {
        try
        {
            var client = CreateClient();
            var resp = await client.GetAsync($"api/schedules/employee/{Uri.EscapeDataString(employeeId)}/weeklyhours?weekStart={Uri.EscapeDataString(weekStart.ToString("o"))}");
            if (!resp.IsSuccessStatusCode) return 0;
            var content = await resp.Content.ReadAsStringAsync();
            if (double.TryParse(content, out var hours)) return hours;

            // Try JSON number
            var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Number) return doc.RootElement.GetDouble();

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
