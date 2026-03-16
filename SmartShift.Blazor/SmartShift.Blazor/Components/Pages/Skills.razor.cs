using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartShift.Blazor.Services;
using SmartOps.Models;

namespace SmartShift.Blazor.Components.Pages;

public partial class Skills
{
    private EmployeeInfo? employee;
    private List<EmployeeSkill>? skills;
    private string? currentUserId;

    [Inject]
    private IShiftDataService ShiftDataService { get; set; } = null!;

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
            // Use stub user if not authenticated
            currentUserId = "stub-user-001";
        }

        await LoadData();
    }

    private async Task LoadData()
    {
        employee = await ShiftDataService.GetCurrentEmployeeAsync();
        skills = await ShiftDataService.GetEmployeeSkillsAsync(currentUserId);
    }

    private static string GetProficiencyBadgeClass(string? proficiency) => proficiency?.ToLower() switch
    {
        "advanced" => "bg-success",
        "intermediate" => "bg-info",
        "basic" => "bg-warning",
        _ => "bg-secondary"
    };
}

