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

    [Inject]
    private IRunAsService RunAsService { get; set; } = null!;

    // ── Members tab ──────────────────────────────────────────────────────────

    private List<(Workgroup Group, WorkGroupMemberDto Member)> allMembers = [];
    private List<(Workgroup Group, WorkGroupMemberDto Member)> filteredMembers = [];
    private string searchTerm = "";

    private bool showAddForm = false;
    private int addToWorkGroupId;
    private string newMemberLogin = "";
    private string addError = "";

    // ── Time-off tab ─────────────────────────────────────────────────────────

    private List<TimeOffRequestDto> teamTimeOffRequests = [];
    private List<TimeOffRequestDto> filteredTimeOffRequests = [];
    private bool showReviewedRequests = false;

    private int? reviewingRequestId;
    private bool reviewingApprove;
    private string reviewNotes = "";
    private string reviewError = "";

    private int PendingCount => teamTimeOffRequests.Count(r => r.Status == TimeOffStatus.Pending);

    // ── Shared ───────────────────────────────────────────────────────────────

    private string activeSubTab = "members";
    private string? _realUserId;
    private string CurrentUserId => RunAsService.GetEffectiveLogin(_realUserId ?? "system");

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _realUserId = authState.User?.Identity?.Name ?? "system";
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SelectedWorkgroups.Count > 0)
        {
            await LoadMembers();
            await LoadTimeOffRequests();
        }
        else
        {
            allMembers.Clear();
            teamTimeOffRequests.Clear();
        }

        ApplyFilters();
        ApplyTimeOffFilters();
    }

    // ── Members ───────────────────────────────────────────────────────────────

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

    private void CancelAddForm() => showAddForm = false;

    private async Task SubmitAddMember()
    {
        if (string.IsNullOrWhiteSpace(newMemberLogin))
        {
            addError = "AD login name is required.";
            return;
        }

        var result = await SmartOpsDataService.AddWorkGroupMemberAsync(addToWorkGroupId, newMemberLogin.Trim(), CurrentUserId);
        if (result == null)
        {
            addError = "Failed to add member. The employee may already be in this workgroup.";
            return;
        }

        var group = SelectedWorkgroups.First(g => g.Id == addToWorkGroupId);
        group.Members.Add(result);
        showAddForm = false;
        await LoadMembers();
        ApplyFilters();
    }

    private async Task RemoveMember(int workGroupId, string adloginName)
    {
        var success = await SmartOpsDataService.RemoveWorkGroupMemberAsync(workGroupId, adloginName, CurrentUserId);
        if (!success) return;

        var group = SelectedWorkgroups.First(g => g.Id == workGroupId);
        group.Members.RemoveAll(m => m.AdloginName == adloginName);
        await LoadMembers();
        ApplyFilters();
    }

    // ── Time Off ──────────────────────────────────────────────────────────────

    private async Task LoadTimeOffRequests()
    {
        var logins = SelectedWorkgroups
            .SelectMany(g => g.Members)
            .Select(m => m.AdloginName)
            .Distinct()
            .ToList();

        teamTimeOffRequests = logins.Count > 0
            ? await SmartOpsDataService.GetTeamTimeOffRequestsAsync(logins)
            : [];

        ApplyTimeOffFilters();
    }

    private void ApplyTimeOffFilters()
    {
        filteredTimeOffRequests = showReviewedRequests
            ? [.. teamTimeOffRequests]
            : teamTimeOffRequests.Where(r => r.Status == TimeOffStatus.Pending).ToList();
    }

    private void BeginReview(int requestId, bool approve)
    {
        reviewingRequestId = requestId;
        reviewingApprove   = approve;
        reviewNotes        = "";
        reviewError        = "";
    }

    private void CancelReview()
    {
        reviewingRequestId = null;
        reviewNotes        = "";
        reviewError        = "";
    }

    private async Task SubmitReview()
    {
        if (reviewingRequestId == null) return;

        var notes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();

        TimeOffRequestDto? updated = reviewingApprove
            ? await SmartOpsDataService.ApproveTimeOffRequestAsync(reviewingRequestId.Value, CurrentUserId, notes)
            : await SmartOpsDataService.DenyTimeOffRequestAsync(reviewingRequestId.Value, CurrentUserId, notes);

        if (updated == null)
        {
            reviewError = "Failed to submit review. Please try again.";
            return;
        }

        // Replace in-memory entry with the returned (server-confirmed) DTO
        var idx = teamTimeOffRequests.FindIndex(r => r.TimeOffRequestId == updated.TimeOffRequestId);
        if (idx >= 0) teamTimeOffRequests[idx] = updated;

        CancelReview();
        ApplyTimeOffFilters();
    }

    private static string GetStatusBadgeClass(TimeOffStatus status) => status switch
    {
        TimeOffStatus.Pending   => "bg-warning text-dark",
        TimeOffStatus.Approved  => "bg-success",
        TimeOffStatus.Denied    => "bg-danger",
        TimeOffStatus.Cancelled => "bg-secondary",
        _                       => "bg-secondary"
    };

    private static string FormatDateRange(DateOnly start, DateOnly end) =>
        start == end
            ? start.ToString("MMM d, yyyy")
            : $"{start:MMM d} – {end:MMM d, yyyy}";
}
