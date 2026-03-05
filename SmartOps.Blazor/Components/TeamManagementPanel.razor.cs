using Microsoft.AspNetCore.Components;
using SmartOps.Models;

namespace SmartOps.Blazor.Components;

public partial class TeamManagementPanel
{
    [Parameter]
    public List<Workgroup> SelectedWorkgroups { get; set; } = [];

    [Parameter]
    public Client? SelectedClient { get; set; }

    private List<EmployeeInfo>? allEmployees;
    private List<EmployeeInfo>? filteredEmployees;
    private string searchTerm = "";
    private string selectedSkill = "";
    private List<string> availableSkills = [];

    protected override async Task OnParametersSetAsync()
    {
        if (SelectedWorkgroups.Count > 0)
        {
            await LoadTeamMembers();
        }
    }

    private async Task LoadTeamMembers()
    {
        // TODO: Call IEmployeeService.GetEmployeesByWorkgroupsAsync(SelectedWorkgroups)
        allEmployees = [];
        availableSkills = ["BILINGUAL_ES", "TIER2", "VOICE_QUALITY"];
        ApplyFilters();
        await Task.CompletedTask;
    }

    private void OnSearchChange()
    {
        ApplyFilters();
    }

    private void OnSkillFilterChange(ChangeEventArgs e)
    {
        selectedSkill = e.Value?.ToString() ?? "";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (allEmployees == null)
            return;

        filteredEmployees = allEmployees.Where(e =>
            (string.IsNullOrEmpty(searchTerm) || e.EmployeeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(selectedSkill) || e.Skills.Any(s => s.SkillCode == selectedSkill))
        ).ToList();
    }

    private static decimal GetEmployeeWeeklyHours(string employeeId)
    {
        // TODO: Call IEmployeeService.GetEmployeeHoursAsync()
        return 40;
    }

    private void ShowAssignShiftModal(string employeeId)
    {
        // TODO: Show modal for shift assignment
    }

    private void ShowDetailsModal(EmployeeInfo employee)
    {
        // TODO: Show employee details modal
    }
}
