using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmartOps.Blazor.Services;
using SmartOps.Models;

namespace SmartOps.Blazor.Components;

public partial class TeamManagementPanel
{
    [Parameter]
    public List<Workgroup> SelectedWorkgroups { get; set; } = [];

    [Parameter]
    public Client? SelectedClient { get; set; }

    [Inject]
    private ISmartOpsDataService SmartOpsDataService { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    // All members across selected workgroups, flat list for display
    private List<(Workgroup Group, WorkGroupMemberDto Member)> allMembers = [];
    private List<(Workgroup Group, WorkGroupMemberDto Member)> filteredMembers = [];
    private string searchTerm = "";

    // Add-member form state
    private bool showAddForm = false;
    private int addToWorkGroupId;
    private string newMemberLogin = "";
    private string addError = "";

    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User?.Identity?.Name ?? "system";
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SelectedWorkgroups.Count > 0)
            await LoadMembers();
        else
            allMembers.Clear();

        ApplyFilters();
    }

    private async Task LoadMembers()
    {
        allMembers = SelectedWorkgroups
            .SelectMany(g => g.Members.Select(m => (g, m)))
            .ToList();

        await Task.CompletedTask;
    }

    private void ApplyFilters()
    {
        filteredMembers = string.IsNullOrWhiteSpace(searchTerm)
            ? [.. allMembers]
            : allMembers
                .Where(x => x.Member.AdloginName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private void OpenAddForm(int workGroupId)
    {
        addToWorkGroupId = workGroupId;
        newMemberLogin   = "";
        addError         = "";
        showAddForm      = true;
    }

    private void CancelAddForm()
    {
        showAddForm = false;
    }

    private async Task SubmitAddMember()
    {
        if (string.IsNullOrWhiteSpace(newMemberLogin))
        {
            addError = "AD login name is required.";
            return;
        }

        var result = await SmartOpsDataService.AddWorkGroupMemberAsync(addToWorkGroupId, newMemberLogin.Trim(), currentUserId!);
        if (result == null)
        {
            addError = "Failed to add member. The employee may already be in this workgroup.";
            return;
        }

        // Update the in-memory list so the UI reflects the change immediately
        var group = SelectedWorkgroups.First(g => g.Id == addToWorkGroupId);
        group.Members.Add(result);
        showAddForm = false;
        await LoadMembers();
        ApplyFilters();
    }

    private async Task RemoveMember(int workGroupId, string adloginName)
    {
        var success = await SmartOpsDataService.RemoveWorkGroupMemberAsync(workGroupId, adloginName, currentUserId!);
        if (!success) return;

        var group = SelectedWorkgroups.First(g => g.Id == workGroupId);
        group.Members.RemoveAll(m => m.AdloginName == adloginName);
        await LoadMembers();
        ApplyFilters();
    }
}
