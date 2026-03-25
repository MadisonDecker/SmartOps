using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;
using TimeOffStatus = SmartOps.Models.TimeOffStatus;

namespace SmartShift.Blazor.Components.Pages;

public partial class EmployeeSchedule : IDisposable
{
    private List<ScheduledShift>? shifts;
    private ScheduledShift? nextShift;
    private decimal weeklyHours;
    private DateTime weekStart;
    private string? _realUserId;
    private string? currentUserId;
    private string? employeeDisplayName;

    // Time-off request state
    private bool showTimeOffModal;
    private ScheduledShift? selectedShift;
    private string timeOffReason = string.Empty;
    private string timeOffError = string.Empty;
    private List<TimeOffRequestDto> timeOffRequests = new();

    // Partial shift
    private bool   _isPartialShift;
    private string _partialStart = string.Empty;
    private string _partialEnd   = string.Empty;

    // Make-up time
    private bool      _planToMakeUpTime;
    private DateOnly? _makeUpDate;
    private string    _makeUpStartTime = string.Empty;
    private string    _makeUpEndTime   = string.Empty;

    [Inject]
    private IShiftDataService ShiftDataService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    private IRunAsService RunAsService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        RunAsService.OnRunAsChanged += OnRunAsChanged;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var identityName = authState.User?.Identity?.Name;
        _realUserId = ExtractLocalUsername(identityName);
        employeeDisplayName = authState.User?.FindFirst("name")?.Value ?? _realUserId;

        if (_realUserId == null)
        {
            NavigationManager.NavigateTo("/Account/Login");
            return;
        }

        currentUserId = RunAsService.GetEffectiveLogin(_realUserId);
        weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        await LoadSchedule();
    }

    private void OnRunAsChanged()
    {
        _ = InvokeAsync(async () =>
        {
            currentUserId = RunAsService.GetEffectiveLogin(_realUserId ?? string.Empty);
            await LoadSchedule();
            StateHasChanged();
        });
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

    // Returns any non-cancelled request covering the shift's date, for badge display.
    // Includes Denied so the employee can always see prior decisions.
    private TimeOffRequestDto? GetAnyRequest(ScheduledShift shift)
    {
        var date = DateOnly.FromDateTime(shift.StartTime.Date);
        return timeOffRequests.FirstOrDefault(r =>
            r.StartDate <= date &&
            r.EndDate   >= date &&
            r.Status    != TimeOffStatus.Cancelled);
    }

    // A shift is eligible for a new request only if it is more than 7 days out
    // and has no existing (Pending, Approved, or Denied) request already.
    private bool CanRequestTimeOff(ScheduledShift shift) =>
        shift.StartTime.Date > DateTime.Today.AddDays(7) &&
        GetAnyRequest(shift) == null;

    // Approved time-off requests overlapping the current week.
    // These are dates where the shift was suppressed by a ScheduleException —
    // we need to render explicit rows so the employee can see the approval.
    private IReadOnlyList<TimeOffRequestDto> GetApprovedTimeOffInWeek()
    {
        var weekStartDate = DateOnly.FromDateTime(weekStart);
        var weekEndDate   = DateOnly.FromDateTime(weekStart.AddDays(6));
        return timeOffRequests
            .Where(r =>
                r.Status    == TimeOffStatus.Approved &&
                r.StartDate <= weekEndDate &&
                r.EndDate   >= weekStartDate)
            .OrderBy(r => r.StartDate)
            .ToList();
    }

    // Approved requests whose make-up window falls within the current week.
    // Make-up time is a temporary addition to the schedule — not a ScheduleException —
    // so it must be surfaced separately.
    private IReadOnlyList<TimeOffRequestDto> GetMakeUpTimeInWeek()
    {
        var weekEnd = weekStart.AddDays(7);
        return timeOffRequests
            .Where(r =>
                r.PlanToMakeUpTime      &&
                r.MakeUpStart.HasValue  &&
                r.MakeUpStart.Value >= weekStart &&
                r.MakeUpStart.Value <  weekEnd)
            .OrderBy(r => r.MakeUpStart)
            .ToList();
    }

    private void OpenTimeOffModal(ScheduledShift shift)
    {
        selectedShift      = shift;
        timeOffReason      = string.Empty;
        timeOffError       = string.Empty;
        _isPartialShift    = false;
        _partialStart      = SnapToHalfHour(shift.StartTime).ToString("HH:mm");
        _partialEnd        = SnapToHalfHour(shift.EndTime).ToString("HH:mm");
        _planToMakeUpTime  = false;
        _makeUpDate        = null;
        _makeUpStartTime   = "08:00";
        _makeUpEndTime     = "08:30";
        showTimeOffModal   = true;
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
            AdloginName = currentUserId!,
            StartDate   = DateOnly.FromDateTime(selectedShift!.StartTime.Date),
            EndDate     = DateOnly.FromDateTime(selectedShift!.StartTime.Date),
            Reason      = timeOffReason.Trim()
        };

        if (_isPartialShift)
        {
            if (!TimeOnly.TryParseExact(_partialStart, "HH:mm", out var pStart) ||
                !TimeOnly.TryParseExact(_partialEnd,   "HH:mm", out var pEnd))
            {
                timeOffError = "Please select valid partial shift times.";
                return;
            }
            // Enforce times are within the scheduled shift
            var shiftStart = SnapToHalfHour(selectedShift!.StartTime);
            var shiftEnd   = SnapToHalfHour(selectedShift!.EndTime);
            if (pStart < TimeOnly.FromDateTime(shiftStart) || pEnd > TimeOnly.FromDateTime(shiftEnd))
            {
                timeOffError = $"Partial shift times must be within the scheduled shift ({shiftStart:HH:mm}–{shiftEnd:HH:mm}).";
                return;
            }
            dto.IsPartialShift = true;
            dto.PartialStart   = pStart;
            dto.PartialEnd     = pEnd;
        }

        if (_planToMakeUpTime)
        {
            if (!_makeUpDate.HasValue ||
                string.IsNullOrWhiteSpace(_makeUpStartTime) ||
                string.IsNullOrWhiteSpace(_makeUpEndTime))
            {
                timeOffError = "Please enter a make-up date, start time, and end time.";
                return;
            }
            if (!TimeOnly.TryParseExact(_makeUpStartTime, "HH:mm", out var makeUpStart) ||
                !TimeOnly.TryParseExact(_makeUpEndTime,   "HH:mm", out var makeUpEnd))
            {
                timeOffError = "Please enter valid make-up times.";
                return;
            }
            var makeUpDtStart = _makeUpDate.Value.ToDateTime(makeUpStart);
            var makeUpDtEnd   = _makeUpDate.Value.ToDateTime(makeUpEnd);

            // Check for conflicts with currently loaded shifts
            var conflict = shifts?.FirstOrDefault(s =>
                s.StartTime < makeUpDtEnd && s.EndTime > makeUpDtStart);
            if (conflict != null)
            {
                timeOffError = $"Make-up time conflicts with an existing shift on " +
                               $"{conflict.StartTime:MMM d} ({conflict.StartTime:HH:mm}–{conflict.EndTime:HH:mm}).";
                return;
            }

            dto.PlanToMakeUpTime = true;
            dto.MakeUpStart      = makeUpDtStart;
            dto.MakeUpEnd        = makeUpDtEnd;
        }

        var result = await ShiftDataService.SubmitTimeOffRequestAsync(dto);
        if (result == null)
        {
            timeOffError = "Failed to submit request. Please try again.";
            return;
        }

        timeOffRequests.Add(result);
        CloseTimeOffModal();
    }

    // ── Time slot helpers ─────────────────────────────────────────────────────

    /// <summary>Snaps a DateTime down to the nearest 30-minute boundary.</summary>
    private static DateTime SnapToHalfHour(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute >= 30 ? 30 : 0, 0);

    /// <summary>Half-hour slots the employee can request OFF — constrained to the scheduled shift.</summary>
    private IReadOnlyList<string> GetPartialStartSlots()
    {
        if (selectedShift == null) return [];
        var from = TimeOnly.FromDateTime(SnapToHalfHour(selectedShift.StartTime));
        var to   = TimeOnly.FromDateTime(SnapToHalfHour(selectedShift.EndTime));
        return HalfHourRange(from, to, inclusive: false);
    }

    /// <summary>Half-hour end slots — from (partial start + 30 min) up to shift end.</summary>
    private IReadOnlyList<string> GetPartialEndSlots()
    {
        if (selectedShift == null) return [];
        TimeOnly.TryParseExact(_partialStart, "HH:mm", out var start);
        var to = TimeOnly.FromDateTime(SnapToHalfHour(selectedShift.EndTime));
        return HalfHourRange(start.AddMinutes(30), to, inclusive: true);
    }

    /// <summary>All half-hour slots across the day for make-up start time.</summary>
    private static IReadOnlyList<string> GetAllHalfHourSlots()
    {
        var slots = new List<string>(48);
        for (int h = 0; h < 24; h++)
            for (int m = 0; m < 60; m += 30)
                slots.Add($"{h:D2}:{m:D2}");
        return slots;
    }

    /// <summary>Make-up end slots — all half-hour slots strictly after the selected start.</summary>
    private IReadOnlyList<string> GetMakeUpEndSlots()
    {
        if (!TimeOnly.TryParseExact(_makeUpStartTime, "HH:mm", out var start)) return [];
        var slots = new List<string>(48);
        for (int h = 0; h < 24; h++)
            for (int m = 0; m < 60; m += 30)
            {
                var t = new TimeOnly(h, m);
                if (t > start) slots.Add($"{h:D2}:{m:D2}");
            }
        return slots;
    }

    private static IReadOnlyList<string> HalfHourRange(TimeOnly from, TimeOnly to, bool inclusive)
    {
        var slots = new List<string>();
        for (int h = 0; h < 24; h++)
            for (int m = 0; m < 60; m += 30)
            {
                var t = new TimeOnly(h, m);
                if (t >= from && (inclusive ? t <= to : t < to))
                    slots.Add($"{h:D2}:{m:D2}");
            }
        return slots;
    }

    /// <summary>Formats an "HH:mm" slot value as "h:mm tt" for display.</summary>
    private static string FormatTimeSlot(string hhmm) =>
        TimeOnly.TryParseExact(hhmm, "HH:mm", out var t) ? t.ToString("h:mm tt") : hhmm;

    private void OnPartialStartChanged()
    {
        // If end is no longer after start, advance it one slot
        if (TimeOnly.TryParseExact(_partialStart, "HH:mm", out var start) &&
            TimeOnly.TryParseExact(_partialEnd,   "HH:mm", out var end) &&
            end <= start)
        {
            _partialEnd = start.AddMinutes(30).ToString("HH:mm");
        }
    }

    private void OnMakeUpStartTimeChanged()
    {
        if (TimeOnly.TryParseExact(_makeUpStartTime, "HH:mm", out var start) &&
            TimeOnly.TryParseExact(_makeUpEndTime,   "HH:mm", out var end) &&
            end <= start)
        {
            _makeUpEndTime = start.AddMinutes(30).ToString("HH:mm");
        }
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

    public void Dispose()
    {
        RunAsService.OnRunAsChanged -= OnRunAsChanged;
    }
}
