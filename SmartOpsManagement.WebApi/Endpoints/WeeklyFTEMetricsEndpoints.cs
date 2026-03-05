using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for WeeklyFTEMetrics operations.
/// </summary>
public static class WeeklyFTEMetricsEndpoints
{
    /// <summary>
    /// Maps the WeeklyFTEMetrics endpoints to the application.
    /// </summary>
    public static void MapWeeklyFTEMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fte-metrics")
            .WithTags("FTE Metrics")
            .WithOpenApi();

        group.MapGet("/weekly", GetWeeklyMetrics)
            .WithName("GetWeeklyFTEMetrics")
            .WithSummary("Gets weekly FTE metrics for the specified week")
            .Produces<WeeklyFTEMetrics>(StatusCodes.Status200OK)
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

        var metrics = await businessLogic.GetWeeklyFTEMetricsAsync(
            weekStart,
            filter.WorkgroupIds,
            filter.ClientId);

        return Results.Ok(metrics);
    }
}

/// <summary>
/// Filter parameters for weekly FTE metrics.
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
