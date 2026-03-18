using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;

namespace SmartShift.Blazor.Components.Pages;

public partial class Skills : IDisposable
{
    private EmployeeInfo? employee;
    private List<EmployeeSkill>? skills;
    private string? _realUserId;
    private string? currentUserId;

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
        _realUserId = ExtractLocalUsername(identityName) ?? "stub-user-001";
        currentUserId = RunAsService.GetEffectiveLogin(_realUserId);

        await LoadData();
    }

    private void OnRunAsChanged()
    {
        _ = InvokeAsync(async () =>
        {
            currentUserId = RunAsService.GetEffectiveLogin(_realUserId ?? "stub-user-001");
            await LoadData();
            StateHasChanged();
        });
    }

    private async Task LoadData()
    {
        employee = await ShiftDataService.GetCurrentEmployeeAsync();
        skills = await ShiftDataService.GetEmployeeSkillsAsync(currentUserId!);
    }

    private static string GetProficiencyBadgeClass(string? proficiency) => proficiency?.ToLower() switch
    {
        "advanced" => "bg-success",
        "intermediate" => "bg-info",
        "basic" => "bg-warning",
        _ => "bg-secondary"
    };

    private static string? ExtractLocalUsername(string? identityName)
    {
        if (string.IsNullOrWhiteSpace(identityName)) return null;

        var lastBackslash = identityName.LastIndexOf('\\');
        if (lastBackslash >= 0 && lastBackslash < identityName.Length - 1)
            return identityName.Substring(lastBackslash + 1);

        var atIndex = identityName.IndexOf('@');
        if (atIndex > 0)
            return identityName.Substring(0, atIndex);

        return identityName;
    }

    public void Dispose()
    {
        RunAsService.OnRunAsChanged -= OnRunAsChanged;
    }
}
