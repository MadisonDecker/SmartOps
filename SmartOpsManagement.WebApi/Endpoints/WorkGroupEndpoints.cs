using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

public static class WorkGroupEndpoints
{
    public static void MapWorkGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workgroups")
            .WithTags("WorkGroups")
            .WithOpenApi();

        group.MapGet("/", GetAllWorkGroups)
            .WithName("GetAllWorkGroups")
            .WithSummary("Gets all active WorkGroups with their current members")
            .Produces<List<Workgroup>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetWorkGroupById)
            .WithName("GetWorkGroupById")
            .WithSummary("Gets a WorkGroup by ID with its current members")
            .Produces<Workgroup>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:int}/members", AddMember)
            .WithName("AddWorkGroupMember")
            .WithSummary("Adds an employee to a WorkGroup")
            .Produces<WorkGroupMemberDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}/members/{adloginName}", RemoveMember)
            .WithName("RemoveWorkGroupMember")
            .WithSummary("Removes an employee from a WorkGroup")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllWorkGroups(SmartOpsBusinessLogic businessLogic)
    {
        var groups = await businessLogic.GetWorkGroupsAsync();
        return Results.Ok(groups);
    }

    private static async Task<IResult> GetWorkGroupById(int id, SmartOpsBusinessLogic businessLogic)
    {
        var group = await businessLogic.GetWorkGroupByIdAsync(id);
        return group == null ? Results.NotFound() : Results.Ok(group);
    }

    private static async Task<IResult> AddMember(
        int id,
        AddWorkGroupMemberRequest request,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(request.AdloginName))
            return Results.BadRequest("AdloginName is required.");

        var (result, error) = await businessLogic.AddWorkGroupMemberAsync(id, request.AdloginName, request.AddedBy);
        if (error != null)
            return error.Contains("not found") ? Results.NotFound(error) : Results.BadRequest(error);

        return Results.Created($"/api/workgroups/{id}/members", result);
    }

    private static async Task<IResult> RemoveMember(
        int id,
        string adloginName,
        string removedBy,
        SmartOpsBusinessLogic businessLogic)
    {
        var (success, error) = await businessLogic.RemoveWorkGroupMemberAsync(id, adloginName, removedBy);
        if (error != null)
            return error.Contains("not found") ? Results.NotFound(error) : Results.BadRequest(error);

        return Results.NoContent();
    }
}
