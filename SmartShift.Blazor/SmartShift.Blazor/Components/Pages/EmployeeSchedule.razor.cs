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
    private IShiftDataService ShiftDataService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Get current user ID from authentication and normalize to AD username
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var identityName = authState.User?.Identity?.Name;
        currentUserId = ExtractLocalUsername(identityName);

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
        shifts = await ShiftDataService.GetEmployeeShiftsAsync(currentUserId, weekStart, weekEnd);

        if (shifts != null && shifts.Any())
        {
            nextShift = await ShiftDataService.GetNextShiftAsync(currentUserId);
            weeklyHours = (decimal)await ShiftDataService.GetWeeklyHoursAsync(currentUserId, weekStart);
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

    private static string? ExtractLocalUsername(string? identityName)
    {
        if (string.IsNullOrWhiteSpace(identityName)) return null;

        // If DOMAIN\username format, take the part after the backslash
        var lastBackslash = identityName.LastIndexOf('\\');
        if (lastBackslash >= 0 && lastBackslash < identityName.Length - 1)
        {
            return identityName.Substring(lastBackslash + 1);
        }

        // If email style (user@domain), take the part before '@'
        var atIndex = identityName.IndexOf('@');
        if (atIndex > 0)
        {
            return identityName.Substring(0, atIndex);
        }

        // Otherwise return the original value
        return identityName;
    }
}

