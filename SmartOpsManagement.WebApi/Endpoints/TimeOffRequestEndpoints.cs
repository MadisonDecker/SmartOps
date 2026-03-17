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
            .WithSummary("Submits a new time-off request for a future shift")
            .Produces<TimeOffRequestDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);
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
        if (string.IsNullOrWhiteSpace(dto.AdloginName) || dto.EtimeShiftId <= 0)
            return Results.BadRequest("AdloginName and EtimeShiftId are required.");

        var (result, error) = await businessLogic.SubmitTimeOffRequestAsync(dto);
        if (error != null)
            return Results.BadRequest(error);

        return Results.Created($"/api/timeoff/{result!.TimeOffRequestId}", result);
    }
}
