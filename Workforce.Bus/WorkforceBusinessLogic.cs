using System.Net.Http.Json;
using System.Web;
using Workforce.Bus.Models;

namespace Workforce.Bus;

public class WorkforceBusinessLogic
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public WorkforceBusinessLogic(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    #region Bank APIs

    /// <summary>
    /// Retrieves bank balance updates from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve updates from. If null, retrieves from first available.</param>
    /// <param name="count">Limit on number of updates to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<Bank>> GetBankUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<Bank>>(
            $"{_baseUrl}/bank/v1{query}",
            cancellationToken) ?? new();
    }

    /// <summary>
    /// Retrieves bank event updates (accruals and usage events) from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<BankEvent>> GetBankEventUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<BankEvent>>(
            $"{_baseUrl}/bank-events/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Calculated Time APIs

    /// <summary>
    /// Retrieves calculated time records after they have been calculated for employee pay.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<CalculatedTime>> GetCalculatedTimeUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<CalculatedTime>>(
            $"{_baseUrl}/calculated-time/v1{query}",
            cancellationToken) ?? new();
    }

    /// <summary>
    /// Retrieves calculated time records for specified employee(s) and date range.
    /// </summary>
    /// <param name="externalMatchIds">Comma-separated list of employee IDs</param>
    /// <param name="date">Specific date to retrieve records for (optional)</param>
    /// <param name="fromDate">Earliest date for which to retrieve data (optional)</param>
    /// <param name="toDate">Latest date for which to retrieve data (optional)</param>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<CalculatedTime>> GetEmployeeCalculatedTimeAsync(
        string externalMatchIds,
        string? date = null,
        string? fromDate = null,
        string? toDate = null,
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("externalMatchId", externalMatchIds),
            ("date", date),
            ("fromDate", fromDate),
            ("toDate", toDate),
            ("cursor", cursor),
            ("count", count?.ToString()));

        return await _httpClient.GetFromJsonAsync<DataFeedResponse<CalculatedTime>>(
            $"{_baseUrl}/calculated-time/employee/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Clock Event APIs

    /// <summary>
    /// Creates or updates a clock event for an employee.
    /// </summary>
    /// <param name="clockEvent">Clock event details to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CreateOrUpdateClockEventAsync(
        ClockEvent clockEvent,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/clock/v1", clockEvent, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Employee APIs

    /// <summary>
    /// Retrieves employee and job record updates from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<Employee>> GetEmployeeUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<Employee>>(
            $"{_baseUrl}/employee/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Shift APIs

    /// <summary>
    /// Retrieves in/out schedule data from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<ShiftDay>> GetShiftUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<ShiftDay>>(
            $"{_baseUrl}/shift/v1{query}",
            cancellationToken) ?? new();
    }

    /// <summary>
    /// Retrieves shift data for specified employee(s) and date range.
    /// </summary>
    /// <param name="externalMatchIds">Comma-separated list of employee IDs</param>
    /// <param name="fromDate">Earliest date for which to retrieve shift data (optional)</param>
    /// <param name="toDate">Latest date for which to retrieve shift data (optional)</param>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<ShiftDay>> GetEmployeeShiftsAsync(
        string externalMatchIds,
        string? fromDate = null,
        string? toDate = null,
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("externalMatchId", externalMatchIds),
            ("fromDate", fromDate),
            ("toDate", toDate),
            ("cursor", cursor),
            ("count", count?.ToString()));

        return await _httpClient.GetFromJsonAsync<DataFeedResponse<ShiftDay>>(
            $"{_baseUrl}/shift/employee/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Swipe APIs

    /// <summary>
    /// Retrieves processed clock swipes from devices from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve updates from.</param>
    /// <param name="count">Limit on number of updates to retrieve (default 2000, max 10000)</param>
    /// <param name="last">Number of records to retrieve from latest records (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<Swipe>> GetSwipeUpdatesAsync(
        string? cursor = null,
        int? count = null,
        int? last = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("cursor", cursor),
            ("count", count?.ToString()),
            ("last", last?.ToString()));

        return await _httpClient.GetFromJsonAsync<DataFeedResponse<Swipe>>(
            $"{_baseUrl}/swipe/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Time Off APIs

    /// <summary>
    /// Retrieves all time off requests from the data feed regardless of status.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<TimeOff>> GetTimeOffUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<TimeOff>>(
            $"{_baseUrl}/time-off/v1{query}",
            cancellationToken) ?? new();
    }

    /// <summary>
    /// Retrieves approved time off records for specified employee(s) and date range.
    /// </summary>
    /// <param name="externalMatchIds">Comma-separated list of employee IDs</param>
    /// <param name="fromDate">Earliest date for which to retrieve time off data (optional)</param>
    /// <param name="toDate">Latest date for which to retrieve time off data (optional)</param>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<TimeOff>> GetEmployeeTimeOffAsync(
        string externalMatchIds,
        string? fromDate = null,
        string? toDate = null,
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("externalMatchId", externalMatchIds),
            ("fromDate", fromDate),
            ("toDate", toDate),
            ("cursor", cursor),
            ("count", count?.ToString()));

        return await _httpClient.GetFromJsonAsync<DataFeedResponse<TimeOff>>(
            $"{_baseUrl}/time-off/employee/v1{query}",
            cancellationToken) ?? new();
    }

    /// <summary>
    /// Creates an approved time off request. Only available for Demand Scheduling customers.
    /// </summary>
    /// <param name="timeOff">Time off details to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CreateTimeOffAsync(
        TimeOff timeOff,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/time-off/v1", timeOff, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region User APIs

    /// <summary>
    /// Retrieves user record updates from the data feed.
    /// </summary>
    /// <param name="cursor">Position in sequence to retrieve records from.</param>
    /// <param name="count">Limit on number of records to retrieve (default 2000, max 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DataFeedResponse<User>> GetUserUpdatesAsync(
        string? cursor = null,
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(("cursor", cursor), ("count", count?.ToString()));
        return await _httpClient.GetFromJsonAsync<DataFeedResponse<User>>(
            $"{_baseUrl}/user/v1{query}",
            cancellationToken) ?? new();
    }

    #endregion

    #region Person APIs

    /// <summary>
    /// Retrieves all data for a person by querying on externalMatchId, displayId, or loginId.
    /// </summary>
    /// <param name="externalMatchId">Person's external match ID (optional)</param>
    /// <param name="displayId">Person's display ID (optional)</param>
    /// <param name="loginId">Person's login ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<Person?> GetPersonAsync(
        string? externalMatchId = null,
        string? displayId = null,
        string? loginId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(externalMatchId) && string.IsNullOrEmpty(displayId) && string.IsNullOrEmpty(loginId))
        {
            throw new ArgumentException("At least one of externalMatchId, displayId, or loginId must be provided.");
        }

        var query = BuildQueryString(
            ("externalMatchId", externalMatchId),
            ("displayId", displayId),
            ("loginId", loginId));

        return await _httpClient.GetFromJsonAsync<Person>(
            $"{_baseUrl}/person/v2{query}",
            cancellationToken);
    }

    /// <summary>
    /// Creates or updates a person (employee and/or user).
    /// </summary>
    /// <param name="person">Person data to create or update</param>
    /// <param name="notificationEmailAddress">Destination email for validation responses (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CreateOrUpdatePersonAsync(
        Person person,
        string? notificationEmailAddress = null,
        CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrEmpty(notificationEmailAddress)
            ? string.Empty
            : $"?notificationEmailAddress={HttpUtility.UrlEncode(notificationEmailAddress)}";

        var response = await _httpClient.PutAsJsonAsync(
            $"{_baseUrl}/person/v2{query}",
            person,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes a person from the Suite and all downstream services.
    /// </summary>
    /// <param name="externalMatchId">The externalMatchId of the person to delete</param>
    /// <param name="notificationEmailAddress">Destination email for deletion validation responses (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeletePersonAsync(
        string externalMatchId,
        string? notificationEmailAddress = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("externalMatchId", externalMatchId),
            ("notificationEmailAddress", notificationEmailAddress));

        var response = await _httpClient.DeleteAsync($"{_baseUrl}/person/v2{query}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Time APIs

    /// <summary>
    /// Creates or updates time data for an employee and their jobs.
    /// </summary>
    /// <param name="timeRequest">Time request containing employee and time data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CreateOrUpdateTimeAsync(
        TimeRequest timeRequest,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/time/v2", timeRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds a query string from key-value pairs, excluding null or empty values.
    /// </summary>
    private static string BuildQueryString(params (string key, string? value)[] parameters)
    {
        var validParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.value))
            .Select(p => $"{p.key}={HttpUtility.UrlEncode(p.value)}")
            .ToList();

        return validParams.Count == 0 ? string.Empty : $"?{string.Join("&", validParams)}";
    }

    /// <summary>
    /// Retrieves all records from a paginated data feed using cursor-based pagination.
    /// </summary>
    /// <param name="getFeedPage">Function to get a page of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<List<T>> GetAllPaginatedResultsAsync<T>(
        Func<string?, CancellationToken, Task<DataFeedResponse<T>>> getFeedPage,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<T>();
        string? cursor = null;

        while (true)
        {
            var response = await getFeedPage(cursor, cancellationToken);
            allResults.AddRange(response.UpdateSequence);

            if (string.IsNullOrEmpty(response.NextCursor))
            {
                break;
            }

            cursor = response.NextCursor;
        }

        return allResults;
    }

    #endregion
}
