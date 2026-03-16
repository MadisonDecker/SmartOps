using SmartManagement.Repo.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for Line Adherence operations.
/// </summary>
public static class LineAdherenceEndpoints
{
    /// <summary>
    /// Maps the Line Adherence endpoints to the application.
    /// </summary>
    public static void MapLineAdherenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/line-adherences")
            .WithTags("Line Adherences")
            .WithOpenApi();

        group.MapGet("/", GetAllLineAdherences)
            .WithName("GetAllLineAdherences")
            .WithSummary("Gets all line adherence records")
            .Produces<List<LineAdherence>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetLineAdherenceById)
            .WithName("GetLineAdherenceById")
            .WithSummary("Gets a line adherence record by ID")
            .Produces<LineAdherence>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/by-client/{clientAbbr}", GetLineAdherencesByClient)
            .WithName("GetLineAdherencesByClient")
            .WithSummary("Gets line adherence records by client abbreviation")
            .Produces<List<LineAdherence>>(StatusCodes.Status200OK);

        group.MapGet("/by-date-range", GetLineAdherencesByDateRange)
            .WithName("GetLineAdherencesByDateRange")
            .WithSummary("Gets line adherence records for a date range")
            .Produces<List<LineAdherence>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/by-client-and-date-range", GetLineAdherencesByClientAndDateRange)
            .WithName("GetLineAdherencesByClientAndDateRange")
            .WithSummary("Gets line adherence records by client and date range")
            .Produces<List<LineAdherence>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/", CreateLineAdherence)
            .WithName("CreateLineAdherence")
            .WithSummary("Creates a new line adherence record")
            .Produces<LineAdherence>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateLineAdherence)
            .WithName("UpdateLineAdherence")
            .WithSummary("Updates an existing line adherence record")
            .Produces<LineAdherence>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteLineAdherence)
            .WithName("DeleteLineAdherence")
            .WithSummary("Deletes a line adherence record")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/batch", SaveLineAdherencesBatch)
            .WithName("SaveLineAdherencesBatch")
            .WithSummary("Saves multiple line adherence records in a batch")
            .Produces<List<LineAdherence>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllLineAdherences(SmartOpsBusinessLogic businessLogic)
    {
        var details = await businessLogic.GetAllLineAdherencesAsync();
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLineAdherenceById(int id, SmartOpsBusinessLogic businessLogic)
    {
        var detail = await businessLogic.GetLineAdherenceByIdAsync(id);
        return detail is not null ? Results.Ok(detail) : Results.NotFound();
    }

    private static async Task<IResult> GetLineAdherencesByClient(string clientAbbr, SmartOpsBusinessLogic businessLogic)
    {
        var details = await businessLogic.GetLineAdherencesByClientAsync(clientAbbr);
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLineAdherencesByDateRange(
        DateOnly startDate,
        DateOnly endDate,
        SmartOpsBusinessLogic businessLogic)
    {
        if (startDate > endDate)
        {
            return Results.BadRequest("startDate must be less than or equal to endDate");
        }

        var details = await businessLogic.GetLineAdherencesByDateRangeAsync(startDate, endDate);
        return Results.Ok(details);
    }

    private static async Task<IResult> GetLineAdherencesByClientAndDateRange(
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

        var details = await businessLogic.GetLineAdherencesByClientAndDateRangeAsync(clientAbbr, startDate, endDate);
        return Results.Ok(details);
    }

    private static async Task<IResult> CreateLineAdherence(LineAdherence lineAdherence, SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(lineAdherence.ClientAbbr))
        {
            return Results.BadRequest("ClientAbbr is required");
        }

        var created = await businessLogic.SaveLineAdherenceAsync(lineAdherence);
        return Results.Created($"/api/line-adherences/{created?.LineAdherenceId}", created);
    }

    private static async Task<IResult> UpdateLineAdherence(int id, LineAdherence lineAdherence, SmartOpsBusinessLogic businessLogic)
    {
        if (id != lineAdherence.LineAdherenceId)
        {
            return Results.BadRequest("ID mismatch");
        }

        var updated = await businessLogic.SaveLineAdherenceAsync(lineAdherence);
        return updated is not null ? Results.Ok(updated) : Results.NotFound();
    }

    private static async Task<IResult> DeleteLineAdherence(int id, SmartOpsBusinessLogic businessLogic)
    {
        var deleted = await businessLogic.DeleteLineAdherenceAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SaveLineAdherencesBatch(List<LineAdherence> lineAdherences, SmartOpsBusinessLogic businessLogic)
    {
        var saved = await businessLogic.SaveLineAdherencesBatchAsync(lineAdherences);
        return Results.Ok(saved);
    }
}
