using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartOps.Models;
using SmartOpsManagement.Bus;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for schedule-related operations used by the Blazor client.
/// Shifts are computed on demand from ScheduleTemplate + ScheduleShiftPattern records;
/// there are no persisted ScheduledShift rows.
/// </summary>
public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules")
            .WithTags("Schedules")
            .WithOpenApi();

        // Employee profile (minimal) for schedule-scoped lookups
        group.MapGet("/employee/{id}/info", GetEmployeeInfoById)
            .WithName("GetEmployeeInfoById")
            .WithSummary("Gets employee info by AD login (schedule scoped)")
            .Produces<EmployeeInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Employee skills lookup
        group.MapGet("/employee/{id}/skills", GetEmployeeSkillsById)
            .WithName("GetEmployeeSkillsById")
            .WithSummary("Gets employee skills by AD login")
            .Produces<List<EmployeeSkill>>(StatusCodes.Status200OK);

        // Computed shifts for a date range (used by EmployeeSchedule page)
        group.MapGet("/employee/{id}", GetEmployeeShiftsById)
            .WithName("GetEmployeeShiftsById")
            .WithSummary("Gets computed shifts for an employee within a date range")
            .Produces<List<ScheduledShift>>(StatusCodes.Status200OK);

        // Next upcoming shift
        group.MapGet("/employee/{id}/next", GetNextShiftForEmployee)
            .WithName("GetNextShiftForEmployee")
            .WithSummary("Gets the next upcoming shift for an employee")
            .Produces<ScheduledShift>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Weekly hours total
        group.MapGet("/employee/{id}/weeklyhours", GetWeeklyHoursForEmployee)
            .WithName("GetWeeklyHoursForEmployee")
            .WithSummary("Gets total scheduled hours for an employee for the week starting at weekStart")
            .Produces<double>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetEmployeeInfoById(
        string id,
        DateTime? startDate,
        DateTime? endDate,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.NotFound();

        var start  = DateOnly.FromDateTime(startDate ?? DateTime.UtcNow.Date);
        var end    = DateOnly.FromDateTime(endDate   ?? (startDate ?? DateTime.UtcNow.Date).AddDays(13));
        var shifts = await businessLogic.GetScheduledShiftsForUserAsync(id, start, end);

        if (!shifts.Any())
            return Results.NotFound();

        var employee = new EmployeeInfo
        {
            Id              = 0,
            EmployeeId      = id,
            Division        = shifts.First().Division,
            SupervisorId    = null,
            SupervisorName  = null,
            IsActive        = true,
            ScheduledShifts = shifts
        };

        return Results.Ok(employee);
    }

    private static async Task<IResult> GetEmployeeShiftsById(
        string id,
        DateTime? start,
        DateTime? end,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.Ok(new List<ScheduledShift>());

        var startDate = DateOnly.FromDateTime(start ?? DateTime.UtcNow.Date);
        var endDate   = DateOnly.FromDateTime(end   ?? (start ?? DateTime.UtcNow.Date).AddDays(13));
        var shifts    = await businessLogic.GetScheduledShiftsForUserAsync(id, startDate, endDate);
        return Results.Ok(shifts);
    }

    private static async Task<IResult> GetNextShiftForEmployee(
        string id,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.NotFound();

        var shift = await businessLogic.GetNextScheduledShiftForUserAsync(id);
        return shift is null ? Results.NotFound() : Results.Ok(shift);
    }

    private static async Task<IResult> GetWeeklyHoursForEmployee(
        string id,
        DateTime weekStart,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.Ok(0.0);

        var hours = await businessLogic.GetWeeklyHoursForUserAsync(id, DateOnly.FromDateTime(weekStart));
        return Results.Ok(hours);
    }

    private static Task<IResult> GetEmployeeSkillsById(string id)
    {
        // Return an empty skills list for now; replace with data-backed logic later
        return Task.FromResult<IResult>(Results.Ok(new List<EmployeeSkill>()));
    }
}
