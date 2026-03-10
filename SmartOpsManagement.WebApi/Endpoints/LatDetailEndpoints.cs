using SmartManagement.Repo.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for LAT Detail operations.
/// </summary>
public static class LatDetailEndpoints
{
    /// <summary>
    /// Maps the LAT Detail endpoints to the application.
    /// </summary>
    public static void MapLatDetailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lat-details")
            .WithTags("LAT Details")
            .WithOpenApi();

        group.MapGet("/", GetAllLatDetails)
            .WithName("GetAllLatDetails")
            .WithSummary("Gets all LAT details")
            .Produces<List<Latdetail>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetLatDetailById)
            .WithName("GetLatDetailById")
            .WithSummary("Gets a LAT detail by ID")
            .Produces<Latdetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/by-client/{clientAbbr}", GetLatDetailsByClient)
            .WithName("GetLatDetailsByClient")
            .WithSummary("Gets LAT details by client abbreviation")
            .Produces<List<Latdetail>>(StatusCodes.Status200OK);

        group.MapGet("/by-date-range", GetLatDetailsByDateRange)
            .WithName("GetLatDetailsByDateRange")
            .WithSummary("Gets LAT details for a date range")
            .Produces<List<Latdetail>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/by-client-and-date-range", GetLatDetailsByClientAndDateRange)
            .WithName("GetLatDetailsByClientAndDateRange")
            .WithSummary("Gets LAT details by client and date range")
            .Produces<List<Latdetail>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/", CreateLatDetail)
            .WithName("CreateLatDetail")
            .WithSummary("Creates a new LAT detail")
            .Produces<Latdetail>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateLatDetail)
            .WithName("UpdateLatDetail")
            .WithSummary("Updates an existing LAT detail")
            .Produces<Latdetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteLatDetail)
            .WithName("DeleteLatDetail")
            .WithSummary("Deletes a LAT detail")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/batch", SaveLatDetailsBatch)
            .WithName("SaveLatDetailsBatch")
            .WithSummary("Saves multiple LAT details in a batch")
            .Produces<List<Latdetail>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllLatDetails(SmartOpsBusinessLogic businessLogic)
    {
        var details = await businessLogic.GetAllLatDetailsAsync();
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLatDetailById(int id, SmartOpsBusinessLogic businessLogic)
    {
        var detail = await businessLogic.GetLatDetailByIdAsync(id);
        return detail is not null ? Results.Ok(detail) : Results.NotFound();
    }

    private static async Task<IResult> GetLatDetailsByClient(string clientAbbr, SmartOpsBusinessLogic businessLogic)
    {
        var details = await businessLogic.GetLatDetailsByClientAsync(clientAbbr);
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLatDetailsByDateRange(
        DateOnly startDate,
        DateOnly endDate,
        SmartOpsBusinessLogic businessLogic)
    {
        if (startDate > endDate)
        {
            return Results.BadRequest("startDate must be less than or equal to endDate");
        }

        var details = await businessLogic.GetLatDetailsByDateRangeAsync(startDate, endDate);
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLatDetailsByClientAndDateRange(
        string clientAbbr,
        DateOnly startDate,
        DateOnly endDate,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(clientAbbr))
        {
            return Results.BadRequest("clientAbbr is required");
        }

        if (startDate > endDate)
        {
            return Results.BadRequest("startDate must be less than or equal to endDate");
        }

        var details = await businessLogic.GetLatDetailsByClientAndDateRangeAsync(clientAbbr, startDate, endDate);
        return Results.Ok(details);
    }

    private static async Task<IResult> CreateLatDetail(Latdetail latDetail, SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(latDetail.ClientAbbr))
        {
            return Results.BadRequest("ClientAbbr is required");
        }

        var created = await businessLogic.SaveLatDetailAsync(latDetail);
        return Results.Created($"/api/lat-details/{created?.LatdetailId}", created);
    }

    private static async Task<IResult> UpdateLatDetail(int id, Latdetail latDetail, SmartOpsBusinessLogic businessLogic)
    {
        if (id != latDetail.LatdetailId)
        {
            return Results.BadRequest("ID mismatch");
        }

        var updated = await businessLogic.SaveLatDetailAsync(latDetail);
        return updated is not null ? Results.Ok(updated) : Results.NotFound();
    }

    private static async Task<IResult> DeleteLatDetail(int id, SmartOpsBusinessLogic businessLogic)
    {
        var deleted = await businessLogic.DeleteLatDetailAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SaveLatDetailsBatch(List<Latdetail> latDetails, SmartOpsBusinessLogic businessLogic)
    {
        var saved = await businessLogic.SaveLatDetailsBatchAsync(latDetails);
        return Results.Ok(saved);
    }
}
