using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

public static class EmployeeAvailabilityEndpoints
{
    public static void MapEmployeeAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/availability")
            .WithTags("Availability")
            .WithOpenApi();

        group.MapGet("/contact-methods", GetContactMethods)
            .WithName("GetContactMethods")
            .WithSummary("Returns all alert contact method options")
            .Produces<List<AlertContactMethodDto>>(StatusCodes.Status200OK);

        group.MapGet("/{adLoginName}", GetAvailability)
            .WithName("GetAvailability")
            .WithSummary("Returns the current active availability profile for an employee")
            .Produces<EmployeeAvailabilityDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{adLoginName}", SaveAvailability)
            .WithName("SaveAvailability")
            .WithSummary("Creates or updates the availability profile for an employee")
            .Produces<EmployeeAvailabilityDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetContactMethods(SmartOpsBusinessLogic businessLogic)
    {
        var methods = await businessLogic.GetContactMethodsAsync();
        return Results.Ok(methods);
    }

    private static async Task<IResult> GetAvailability(
        string adLoginName,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(adLoginName))
            return Results.BadRequest("adLoginName is required.");

        var result = await businessLogic.GetAvailabilityAsync(adLoginName);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> SaveAvailability(
        string adLoginName,
        EmployeeAvailabilityDto dto,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(adLoginName))
            return Results.BadRequest("adLoginName is required.");

        if (dto.MaxWeeklyHours < dto.MinWeeklyHours)
            return Results.BadRequest("MaxWeeklyHours must be >= MinWeeklyHours.");

        if (dto.MaxWeeklyHours > 168)
            return Results.BadRequest("MaxWeeklyHours cannot exceed 168.");

        dto.AdloginName = adLoginName;

        var result = await businessLogic.SaveAvailabilityAsync(adLoginName, dto);
        return Results.Ok(result);
    }
}
