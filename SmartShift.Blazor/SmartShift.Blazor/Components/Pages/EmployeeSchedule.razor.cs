using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;
using TimeOffStatus = SmartOps.Models.TimeOffStatus;

namespace SmartShift.Blazor.Components.Pages;

public partial class EmployeeSchedule
{
    private List<ScheduledShift>? shifts;
    private ScheduledShift? nextShift;
    private decimal weeklyHours;
    private DateTime weekStart;
    private string? currentUserId;

    // Time-off request state
    private bool showTimeOffModal;
    private ScheduledShift? selectedShift;
    private string timeOffReason = string.Empty;
    private string timeOffError = string.Empty;
    private List<TimeOffRequestDto> timeOffRequests = new();

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

        // Reload requests so the action column reflects current database state
        timeOffRequests = await ShiftDataService.GetTimeOffRequestsAsync(currentUserId!);
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

    // Returns the active (non-denied, non-cancelled) request for a shift, if one exists.
    private TimeOffRequestDto? GetActiveRequest(ScheduledShift shift) =>
        timeOffRequests.FirstOrDefault(r =>
            r.EtimeShiftId == shift.Id &&
            r.Status != TimeOffStatus.Denied &&
            r.Status != TimeOffStatus.Cancelled);

    // A shift is eligible for a new request only if it is more than 7 days out
    // and has no active (Pending/Approved) request already.
    private bool CanRequestTimeOff(ScheduledShift shift) =>
        shift.StartTime.Date > DateTime.Today.AddDays(7) &&
        GetActiveRequest(shift) == null;

    private void OpenTimeOffModal(ScheduledShift shift)
    {
        selectedShift = shift;
        timeOffReason = string.Empty;
        timeOffError = string.Empty;
        showTimeOffModal = true;
    }

    private void CloseTimeOffModal()
    {
        showTimeOffModal = false;
        selectedShift = null;
    }

    private async Task SubmitTimeOffRequest()
    {
        if (string.IsNullOrWhiteSpace(timeOffReason))
        {
            timeOffError = "Please enter a reason for your request.";
            return;
        }

        var dto = new TimeOffRequestDto
        {
            AdloginName  = currentUserId!,
            EtimeShiftId = selectedShift!.Id,
            ShiftStart   = selectedShift.StartTime,
            ShiftEnd     = selectedShift.EndTime,
            Reason       = timeOffReason.Trim()
        };

        var result = await ShiftDataService.SubmitTimeOffRequestAsync(dto);
        if (result == null)
        {
            timeOffError = "Failed to submit request. Please try again.";
            return;
        }

        timeOffRequests.Add(result);
        CloseTimeOffModal();
    }

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

