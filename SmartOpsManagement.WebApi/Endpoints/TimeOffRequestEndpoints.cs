using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

public static class TimeOffRequestEndpoints
{
    public static void MapTimeOffRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timeoff")
            .WithTags("TimeOff")
            .WithOpenApi();

        group.MapGet("/employee/{id}", GetTimeOffRequestsForEmployee)
            .WithName("GetTimeOffRequestsForEmployee")
            .WithSummary("Gets all time-off requests for an employee by AD login")
            .Produces<List<TimeOffRequestDto>>(StatusCodes.Status200OK);

        group.MapPost("/", SubmitTimeOffRequest)
            .WithName("SubmitTimeOffRequest")
            .WithSummary("Submits a new time-off request for a future date range")
            .Produces<TimeOffRequestDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPost("/{id:int}/approve", ApproveTimeOffRequest)
            .WithName("ApproveTimeOffRequest")
            .WithSummary("Approves a pending time-off request and creates a ScheduleException")
            .Produces<TimeOffRequestDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:int}/deny", DenyTimeOffRequest)
            .WithName("DenyTimeOffRequest")
            .WithSummary("Denies a pending time-off request")
            .Produces<TimeOffRequestDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetTimeOffRequestsForEmployee(
        string id,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.Ok(new List<TimeOffRequestDto>());

        var requests = await businessLogic.GetTimeOffRequestsForUserAsync(id);
        return Results.Ok(requests);
    }

    private static async Task<IResult> SubmitTimeOffRequest(
        TimeOffRequestDto dto,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(dto.AdloginName) || dto.StartDate == default)
            return Results.BadRequest("AdloginName and StartDate are required.");

        var (result, error) = await businessLogic.SubmitTimeOffRequestAsync(dto);
        if (error != null)
            return Results.BadRequest(error);

        return Results.Created($"/api/timeoff/{result!.TimeOffRequestId}", result);
    }

    private static async Task<IResult> ApproveTimeOffRequest(
        int id,
        ReviewDto review,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(review.ReviewedBy))
            return Results.BadRequest("ReviewedBy is required.");

        var (result, error) = await businessLogic.ReviewTimeOffRequestAsync(id, approved: true, review.ReviewedBy, review.Notes);
        if (error != null)
            return error.Contains("not found") ? Results.NotFound(error) : Results.BadRequest(error);

        return Results.Ok(result);
    }

    private static async Task<IResult> DenyTimeOffRequest(
        int id,
        ReviewDto review,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(review.ReviewedBy))
            return Results.BadRequest("ReviewedBy is required.");

        var (result, error) = await businessLogic.ReviewTimeOffRequestAsync(id, approved: false, review.ReviewedBy, review.Notes);
        if (error != null)
            return error.Contains("not found") ? Results.NotFound(error) : Results.BadRequest(error);

        return Results.Ok(result);
    }
}

/// <summary>Request body for approve/deny actions.</summary>
public record ReviewDto(string ReviewedBy, string? Notes);
