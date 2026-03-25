using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;

namespace SmartShift.Blazor.Components.Pages;

public partial class Availability : IDisposable
{
    private static readonly string[] DayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    // Display order starts Monday (work week); index values are DayOfWeek (0=Sun…6=Sat)
    private static readonly int[] DayDisplayOrder = [1, 2, 3, 4, 5, 6, 0];

    // ── Loading / save state ──────────────────────────────────────────────────
    private bool _isLoading = true;
    private bool _isSaving  = false;
    private bool _saveSuccess = false;
    private string _saveError = string.Empty;

    // ── Lookups ───────────────────────────────────────────────────────────────
    private List<AlertContactMethodDto> _contactMethods = [];

    // ── Identity ──────────────────────────────────────────────────────────────
    private string? _realUserId;
    private string? _currentUserId;

    // ── Per-day UI state (index = DayOfWeek: 0=Sun … 6=Sat) ──────────────────
    private bool[]   _dayEnabled       = new bool[7];
    private string[] _dayEarliestStart = Enumerable.Repeat("07:00", 7).ToArray();
    private string[] _dayLatestStop    = Enumerable.Repeat("18:00", 7).ToArray();

    // ── Form fields ───────────────────────────────────────────────────────────
    private decimal _minWeeklyHours = 0;
    private decimal _maxWeeklyHours = 40;
    private bool    _isOpenToOvertime = false;
    private bool    _isOpenToVto      = false;
    private int     _preferredAlertContactMethodId = 1;
    private string  _notes = string.Empty;

    [Inject] private IShiftDataService             ShiftDataService             { get; set; } = null!;
    [Inject] private NavigationManager             NavigationManager            { get; set; } = null!;
    [Inject] private AuthenticationStateProvider   AuthenticationStateProvider  { get; set; } = null!;
    [Inject] private IRunAsService                 RunAsService                 { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        RunAsService.OnRunAsChanged += OnRunAsChanged;

        var authState    = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _realUserId      = ExtractLocalUsername(authState.User?.Identity?.Name);

        if (_realUserId == null)
        {
            NavigationManager.NavigateTo("/Account/Login");
            return;
        }

        _currentUserId = RunAsService.GetEffectiveLogin(_realUserId);
        await LoadAsync();
    }

    private void OnRunAsChanged()
    {
        _ = InvokeAsync(async () =>
        {
            _currentUserId = RunAsService.GetEffectiveLogin(_realUserId ?? string.Empty);
            await LoadAsync();
            StateHasChanged();
        });
    }

    private async Task LoadAsync()
    {
        _isLoading    = true;
        _saveSuccess  = false;
        _saveError    = string.Empty;

        _contactMethods = await ShiftDataService.GetContactMethodsAsync();

        var dto = await ShiftDataService.GetAvailabilityAsync(_currentUserId!);
        ApplyDto(dto);

        _isLoading = false;
    }

    private void ApplyDto(EmployeeAvailabilityDto? dto)
    {
        _dayEnabled       = new bool[7];
        _dayEarliestStart = Enumerable.Repeat("07:00", 7).ToArray();
        _dayLatestStop    = Enumerable.Repeat("18:00", 7).ToArray();

        if (dto == null)
        {
            _minWeeklyHours               = 0;
            _maxWeeklyHours               = 40;
            _isOpenToOvertime             = false;
            _isOpenToVto                  = false;
            _preferredAlertContactMethodId = 1;
            _notes                        = string.Empty;
            return;
        }

        _minWeeklyHours               = dto.MinWeeklyHours;
        _maxWeeklyHours               = dto.MaxWeeklyHours;
        _isOpenToOvertime             = dto.IsOpenToOvertime;
        _isOpenToVto                  = dto.IsOpenToVto;
        _preferredAlertContactMethodId = dto.PreferredAlertContactMethodId;
        _notes                        = dto.Notes ?? string.Empty;

        foreach (var day in dto.Days)
        {
            _dayEnabled[day.DayOfWeek]       = true;
            _dayEarliestStart[day.DayOfWeek] = day.EarliestStart.ToString("HH:mm");
            _dayLatestStop[day.DayOfWeek]    = day.LatestStop.ToString("HH:mm");
        }
    }

    private void OnDayToggled(int dow, ChangeEventArgs e)
    {
        _dayEnabled[dow] = e.Value is bool b && b;
    }

    private async Task SaveAsync()
    {
        _isSaving    = true;
        _saveSuccess = false;
        _saveError   = string.Empty;

        if (_maxWeeklyHours < _minWeeklyHours)
        {
            _saveError = "Maximum weekly hours must be greater than or equal to minimum weekly hours.";
            _isSaving  = false;
            return;
        }

        var dto = new EmployeeAvailabilityDto
        {
            AdloginName                   = _currentUserId!,
            MinWeeklyHours                = _minWeeklyHours,
            MaxWeeklyHours                = _maxWeeklyHours,
            IsOpenToOvertime              = _isOpenToOvertime,
            IsOpenToVto                   = _isOpenToVto,
            PreferredAlertContactMethodId = (byte)_preferredAlertContactMethodId,
            Notes                         = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim(),
            Days                          = []
        };

        for (int i = 0; i < 7; i++)
        {
            if (!_dayEnabled[i]) continue;

            dto.Days.Add(new EmployeeAvailabilityDayDto
            {
                DayOfWeek     = (byte)i,
                EarliestStart = TimeOnly.TryParseExact(_dayEarliestStart[i], "HH:mm", out var s) ? s : new TimeOnly(7, 0),
                LatestStop    = TimeOnly.TryParseExact(_dayLatestStop[i],    "HH:mm", out var e) ? e : new TimeOnly(18, 0)
            });
        }

        var result = await ShiftDataService.SaveAvailabilityAsync(_currentUserId!, dto);

        if (result == null)
            _saveError = "Failed to save availability. Please try again.";
        else
        {
            ApplyDto(result);
            _saveSuccess = true;
        }

        _isSaving = false;
    }

    private static string? ExtractLocalUsername(string? identityName)
    {
        if (string.IsNullOrWhiteSpace(identityName)) return null;
        var lastBackslash = identityName.LastIndexOf('\\');
        if (lastBackslash >= 0 && lastBackslash < identityName.Length - 1)
            return identityName[(lastBackslash + 1)..];
        var atIndex = identityName.IndexOf('@');
        if (atIndex > 0)
            return identityName[..atIndex];
        return identityName;
    }

    public void Dispose()
    {
        RunAsService.OnRunAsChanged -= OnRunAsChanged;
    }
}
