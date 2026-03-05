using Microsoft.AspNetCore.Components;
using SmartShift.Blazor.Services;
using SmartOps.Models;

namespace SmartShift.Blazor.Components.Pages
{
    public partial class Home
    {
        private ScheduledShift? nextShift;
        private decimal weeklyHours;
        private string? currentUserId;
        private string pageTitle = "Home - TimeKeeper";

        [Inject]
        private IStubDataService StubDataService { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

            // Build page title based on user roles
            var roles = new List<string>();
            if (authState.User?.IsInRole("Supervisor") == true)
                roles.Add("Supervisor");
            if (authState.User?.IsInRole("Admin") == true)
                roles.Add("Admin");

            pageTitle = roles.Count > 0
                ? $"Home ({string.Join(" | ", roles)}) - TimeKeeper"
                : "Home - TimeKeeper";

            // If user is a Supervisor or Admin, redirect to the Supervisor Dashboard
            if (authState.User?.IsInRole("Supervisor") == true || authState.User?.IsInRole("Admin") == true)
            {
                NavigationManager.NavigateTo("/supervisor/dashboard", replace: true);
                return;
            }

            // Get current user ID from authentication
            currentUserId = authState.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                // Use stub user if not authenticated
                currentUserId = "stub-user-001";
            }

            // Load stub data
            nextShift = await StubDataService.GetNextShiftAsync(currentUserId);

            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            weeklyHours = (decimal)await StubDataService.GetWeeklyHoursAsync(currentUserId, weekStart);
        }
    }
}

