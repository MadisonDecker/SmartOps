using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;

namespace SmartShift.Blazor.Components.Pages;

public partial class EmployeeSchedule
{
    private List<ScheduledShift>? shifts;
    private ScheduledShift? nextShift;
    private decimal weeklyHours;
    private DateTime weekStart;
    private string? currentUserId;

    [Inject]
    private IStubDataService StubDataService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Get current user ID from authentication
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (currentUserId == null)
        {
            // Redirect to login
            NavigationManager.NavigateTo("/Account/Login");
            return;
        }

        weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        await LoadSchedule();
    }

    private async Task LoadSchedule()
    {
        var weekEnd = weekStart.AddDays(7);
        shifts = await StubDataService.GetEmployeeShiftsAsync(currentUserId, weekStart, weekEnd);

        if (shifts.Any())
        {
            nextShift = await StubDataService.GetNextShiftAsync(currentUserId);
            weeklyHours = (decimal)await StubDataService.GetWeeklyHoursAsync(currentUserId, weekStart);
        }
    }

    private async Task PreviousWeek()
    {
        weekStart = weekStart.AddDays(-7);
        await LoadSchedule();
    }

    private async Task NextWeek()
    {
        weekStart = weekStart.AddDays(7);
        await LoadSchedule();
    }

    private static string GetStatusBadgeClass(ShiftStatus status) => status switch
    {
        ShiftStatus.Scheduled => "bg-primary",
        ShiftStatus.InProgress => "bg-warning",
        ShiftStatus.Completed => "bg-success",
        ShiftStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };
}

