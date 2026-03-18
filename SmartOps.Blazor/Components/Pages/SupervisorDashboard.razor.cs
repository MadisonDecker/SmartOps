using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartOps.Blazor.Services;
using SmartOps.Models;
using SmartOps.Models.Extensions;

namespace SmartOps.Blazor.Components.Pages;

public partial class SupervisorDashboard
{
    protected string activeTab = "schedule";
    private bool workgroupDropdownOpen = false;
    private List<Workgroup> selectedWorkgroups = [];
    private Client? selectedClient;
    private DateTime weekStart;
    private List<Workgroup> workgroups = [];
    private List<Client> clients = [];

    private WeeklyStaffingMetrics staffingMetrics = new();
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        // Verify user is authenticated and has supervisor role
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (currentUserId == null || !authState.User.IsInRole("Supervisor"))
        {
            NavigationManager.NavigateTo("/Unauthorized");
            return;
        }

        // Initialize week start to the most recent Monday.
        weekStart = DateTime.Today.GetWeekStart();
        await LoadWorkgroups();
        await RefreshData();
    }

    private async Task LoadWorkgroups()
    {
        workgroups = await SmartOpsDataService.GetWorkGroupsAsync();
    }

    private async Task RefreshData()
    {
        // Load clients when workgroups are selected
        if (selectedWorkgroups.Count > 0)
        {
            // TODO: Call IStaffingRequirementService.GetClientsForWorkgroupsAsync()
            clients =
            [
                new Client { Id = 1, Code = "ACME", Name = "Acme Corp", WorkgroupId = 1 },
                new Client { Id = 2, Code = "CONTOSO", Name = "Contoso Ltd", WorkgroupId = 1 },
                new Client { Id = 3, Code = "FABRIKAM", Name = "Fabrikam Inc", WorkgroupId = 2 }
            ];
        }
        else
        {
            clients.Clear();
        }

        // Get staffing metrics from the service
        var workgroupIds = selectedWorkgroups.Select(w => w.Id);
        staffingMetrics = await SmartOpsDataService.GetWeeklyMetricsAsync(
            weekStart,
            workgroupIds.Any() ? workgroupIds : null,
            selectedClient?.Id);

        StateHasChanged();
    }

    private async Task PreviousWeek() => await ChangeWeek(-7);
    private async Task NextWeek() => await ChangeWeek(7);

    private async Task ChangeWeek(int days)
    {
        weekStart = weekStart.AddDays(days);
        await RefreshData();
    }

    private void ToggleWorkgroupDropdown()
    {
        workgroupDropdownOpen = !workgroupDropdownOpen;
    }

    protected void SelectTab(string tabName)
    {
        activeTab = tabName;
        StateHasChanged();
    }

    private string GetWorkgroupSelectionText()
    {
        if (selectedWorkgroups.Count == 0)
            return "Select Workgroups...";
        if (selectedWorkgroups.Count == 1)
            return selectedWorkgroups[0].Name;
        if (selectedWorkgroups.Count == workgroups.Count)
            return "All Workgroups";
        return $"{selectedWorkgroups.Count} Workgroups Selected";
    }

    private async Task OnSelectAllWorkgroupsChanged(ChangeEventArgs e)
    {
        if (e.Value is bool isChecked && isChecked)
        {
            selectedWorkgroups = [.. workgroups];
        }
        else
        {
            selectedWorkgroups.Clear();
        }
        selectedClient = null;
        clients.Clear();
        await RefreshData();
    }

    private async Task OnWorkgroupToggled(Workgroup workgroup, ChangeEventArgs e)
    {
        if (e.Value is bool isChecked)
        {
            if (isChecked && !selectedWorkgroups.Any(w => w.Id == workgroup.Id))
            {
                selectedWorkgroups.Add(workgroup);
            }
            else if (!isChecked)
            {
                selectedWorkgroups.RemoveAll(w => w.Id == workgroup.Id);
            }
        }
        selectedClient = null;
        clients.Clear();
        await RefreshData();
    }

    private async Task OnClientChanged(ChangeEventArgs e)
    {
        var clientId = e.Value?.ToString();
        selectedClient = string.IsNullOrEmpty(clientId) ? null : clients.FirstOrDefault(c => c.Id.ToString() == clientId);
        await RefreshData();
    }

    private async Task OnWeekChanged(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
        {
            weekStart = newDate;
            await RefreshData();
        }
    }

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    private ISmartOpsDataService SmartOpsDataService { get; set; } = null!;
}
