using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartOps.Models;
using SmartOpsManagement.Bus;
using SmartManagement.Repo.Models;

namespace SmartOpsManagement.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for Schedule-related operations used by the Blazor client.
/// Currently exposes simple employee info and skills lookups under the schedules
/// route so the UI can call schedule-scoped endpoints without relying on an
/// "employees" controller.
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
            .WithSummary("Gets employee info by id (schedule scoped)")
            .Produces<EmployeeInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Employee skills lookup
        group.MapGet("/employee/{id}/skills", GetEmployeeSkillsById)
            .WithName("GetEmployeeSkillsById")
            .WithSummary("Gets employee skills by id (schedule scoped)")
            .Produces<List<EmployeeSkill>>(StatusCodes.Status200OK);

        // Shifts for a date range (used by EmployeeSchedule page)
        group.MapGet("/employee/{id}", GetEmployeeShiftsById)
            .WithName("GetEmployeeShiftsById")
            .WithSummary("Gets scheduled shifts for an employee by AD login within a date range")
            .Produces<List<ScheduledShift>>(StatusCodes.Status200OK);

        // Next upcoming shift
        group.MapGet("/employee/{id}/next", GetNextShiftForEmployee)
            .WithName("GetNextShiftForEmployee")
            .WithSummary("Gets the next upcoming shift for an employee by AD login")
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

        var start = startDate ?? DateTime.UtcNow.Date;
        var end = endDate ?? start.AddDays(7);

        var userShifts = await businessLogic.GetEtimeShiftsByUserAndDateRangeAsync(id, start, end);

        if (!userShifts.Any())
            return Results.NotFound();

        var first = userShifts.First();
        var employee = new EmployeeInfo
        {
            Id = 0,
            EmployeeId = id,
            Division = first.PayGroup ?? string.Empty,
            SupervisorId = null,
            SupervisorName = null,
            IsActive = true,
            ScheduledShifts = userShifts.Select(s => MapToScheduledShift(s, id)).ToList()
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

        var startDate = start ?? DateTime.UtcNow.Date;
        var endDate = end ?? startDate.AddDays(7);

        var etimeShifts = await businessLogic.GetEtimeShiftsByUserAndDateRangeAsync(id, startDate, endDate);
        var shifts = etimeShifts.Select(s => MapToScheduledShift(s, id)).ToList();
        return Results.Ok(shifts);
    }

    private static async Task<IResult> GetNextShiftForEmployee(
        string id,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.NotFound();

        var etimeShift = await businessLogic.GetNextEtimeShiftForUserAsync(id);
        if (etimeShift == null)
            return Results.NotFound();

        return Results.Ok(MapToScheduledShift(etimeShift, id));
    }

    private static async Task<IResult> GetWeeklyHoursForEmployee(
        string id,
        DateTime weekStart,
        SmartOpsBusinessLogic businessLogic)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.Ok(0.0);

        var weekEnd = weekStart.AddDays(7);
        var etimeShifts = await businessLogic.GetEtimeShiftsByUserAndDateRangeAsync(id, weekStart, weekEnd);
        var totalHours = etimeShifts.Sum(s => (s.ShiftEnd - s.ShiftStart).TotalHours - s.BreakMin / 60.0);
        return Results.Ok(totalHours);
    }

    private static Task<IResult> GetEmployeeSkillsById(string id)
    {
        // Return an empty skills list for now; replace with data-backed logic later
        var skills = new List<EmployeeSkill>();
        return Task.FromResult<IResult>(Results.Ok(skills));
    }

    private static ScheduledShift MapToScheduledShift(EtimeShift s, string employeeId)
    {
        var now = DateTime.UtcNow;
        var status = s.ShiftEnd < now
            ? ShiftStatus.Completed
            : s.ShiftStart <= now && s.ShiftEnd >= now
                ? ShiftStatus.InProgress
                : ShiftStatus.Scheduled;

        var shift = new ScheduledShift
        {
            Id = s.EtimeShiftId,
            EmployeeId = employeeId,
            Division = s.PayGroup ?? string.Empty,
            StartTime = s.ShiftStart,
            EndTime = s.ShiftEnd,
            Status = status
        };

        if (s.BreakMin > 0)
        {
            var breakStart = s.ShiftStart.AddHours(4);
            shift.Breaks.Add(new ScheduledBreak
            {
                StartTime = breakStart,
                EndTime = breakStart.AddMinutes(s.BreakMin),
                BreakType = "Break",
                IsPaid = false
            });
        }

        return shift;
    }
}
