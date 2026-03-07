using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for WeeklyStaffingMetrics operations.
/// </summary>
public static class WeeklyStaffingMetricsEndpoints
{
    /// <summary>
    /// Maps the WeeklyStaffingMetrics endpoints to the application.
    /// </summary>
    public static void MapWeeklyStaffingMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staffing-metrics")
            .WithTags("Staffing Metrics")
            .WithOpenApi();

        group.MapGet("/weekly", GetWeeklyMetrics)
            .WithName("GetWeeklyStaffingMetrics")
            .WithSummary("Gets weekly staffing metrics for the specified week")
            .Produces<WeeklyStaffingMetrics>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetWeeklyMetrics(
        DateTime weekStart,
        [AsParameters] WeeklyMetricsFilter filter,
        SmartOpsBusinessLogic businessLogic)
    {
        if (weekStart == default)
        {
            return Results.BadRequest("weekStart is required");
        }

        var metrics = await businessLogic.GetWeeklyStaffingMetricsAsync(
            weekStart,
            filter.WorkgroupIds,
            filter.ClientId);

        return Results.Ok(metrics);
    }
}

/// <summary>
/// Filter parameters for weekly staffing metrics.
/// </summary>
public class WeeklyMetricsFilter
{
    /// <summary>
    /// Optional list of workgroup IDs to filter by.
    /// </summary>
    public int[]? WorkgroupIds { get; set; }

    /// <summary>
    /// Optional client ID to filter by.
    /// </summary>
    public int? ClientId { get; set; }
}
