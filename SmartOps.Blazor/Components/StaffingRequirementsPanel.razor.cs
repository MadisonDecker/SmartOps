using Microsoft.AspNetCore.Components;
using SmartOps.Models;

namespace SmartOps.Blazor.Components;

public partial class StaffingRequirementsPanel
{
    [Parameter]
    public List<Workgroup> SelectedWorkgroups { get; set; } = [];

    [Parameter]
    public Client? SelectedClient { get; set; }

    /// <summary>
    /// Interval in minutes for FTE calculation. Default is 60 minutes.
    /// Supported values: 15, 30, 60
    /// </summary>
    [Parameter]
    public int IntervalMinutes { get; set; } = 60;

    private List<StaffingRequirement>? requirements;

    protected override async Task OnParametersSetAsync()
    {
        if (SelectedWorkgroups.Count > 0)
        {
            await LoadRequirements();
        }
    }

    private async Task LoadRequirements()
    {
        // TODO: Call IStaffingRequirementService.GetRequirementsForWorkgroupsAsync(SelectedWorkgroups)
        requirements = [];
        await Task.CompletedTask;
    }

    private async Task SaveRequirement(StaffingRequirement requirement)
    {
        // TODO: Call IStaffingRequirementService.UpdateRequirementAsync()
        await Task.CompletedTask;
    }

    private async Task DeleteRequirement(int id)
    {
        // TODO: Call IStaffingRequirementService.DeleteRequirementAsync()
        if (requirements != null)
        {
            requirements.RemoveAll(r => r.Id == id);
        }
        await Task.CompletedTask;
    }

    private void ShowAddRequirementModal()
    {
        // TODO: Show modal to add new requirement
    }

    private static string GetDayOfWeekName(int dayOfWeek) => dayOfWeek switch
    {
        0 => "Sunday",
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        _ => "Unknown"
    };

    /// <summary>
    /// Formats the time slot based on the current interval setting.
    /// </summary>
    private string FormatTimeSlot(int hourOfDay)
    {
        var hour = hourOfDay / 60;
        var minute = hourOfDay % 60;

        // If storing as hour index (0-23), format as time
        if (hourOfDay < 24)
        {
            return $"{hourOfDay}:00";
        }

        // If storing as minutes from midnight
        return $"{hour:D2}:{minute:D2}";
    }
}
