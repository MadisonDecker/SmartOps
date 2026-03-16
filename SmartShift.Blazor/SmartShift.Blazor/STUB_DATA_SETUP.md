# Smart Shift UI Stub Data Setup

## Overview
I've created a comprehensive stub data service that provides realistic sample data for UI development and testing. This allows you to work on the UI without needing a real backend database.

## What's Been Added

### 1. **IStubDataService & StubDataService** 
**Location:** `SmartShift.Blazor/Services/StubDataService.cs`

This service provides the following methods:
- `GetCurrentEmployeeAsync()` - Returns employee information (division, supervisor, start date, etc.)
- `GetEmployeeShiftsAsync()` - Returns a list of scheduled shifts with breaks
- `GetEmployeeSkillsAsync()` - Returns employee skills and certifications
- `GetNextShiftAsync()` - Returns the next upcoming shift
- `GetWeeklyHoursAsync()` - Calculates total hours scheduled for a week

**Sample Data Includes:**
- 5 scheduled shifts across different days
- Multiple break types (Lunch, 15-minute breaks)
- Realistic employee divisions (Customer Service, Quality Assurance)
- Skills with proficiency levels (Advanced, Intermediate)
- Expiration dates for certifications

### 2. **Updated Program.cs**
Registered the stub data service as a scoped service:
```csharp
builder.Services.AddScoped<IStubDataService, StubDataService>();
builder.Services.AddHttpContextAccessor();
```

### 3. **Updated Pages**

#### Home.razor (`/`)
- Shows next shift card with timing information
- Displays weekly hours summary
- Links to schedule and skills pages
- Enhanced welcome message with employee name

#### EmployeeSchedule.razor (`/schedule`)
- Updated to use `IStubDataService` for data loading
- Displays shifts in a weekly table view
- Shows break information for each shift
- Navigation buttons for previous/next week
- Status badges for shift status

#### Skills.razor (`/skills`) - NEW
- Complete page for viewing skills and certifications
- Displays employee information
- Shows all skills with proficiency levels
- Displays expiration dates
- Skill activation status
- Placeholder for add skill functionality

### 4. **Updated Navigation**
Added a new "Skills & Certs" link to the navigation menu.

## Sample Data Structure

### Employee Information
```
- EmployeeId: stub-user-001
- Division: Customer Service / Quality Assurance
- Supervisor: Jane Smith
- Start Date: Jan 15, 2022
- Status: Active
```

### Shifts Include
- **Today:** 8:00 AM - 5:00 PM (Customer Service, 2 breaks)
- **Tomorrow:** 9:00 AM - 6:00 PM (Customer Service, 1 break)
- **In 2 days:** 10:00 AM - 7:00 PM (Secondary Client, 2 breaks)
- **In 4 days:** 8:00 AM - 4:00 PM (Quality Assurance, 1 break)
- **Next week:** 8:00 AM - 5:00 PM (Customer Service, 1 break)

### Skills Include
1. **Spanish - Bilingual** (Advanced, No expiration)
2. **Tier 2 Support** (Intermediate, Expires Mar 15, 2025)
3. **Quality Assurance** (Advanced, No expiration)

## How to Use

1. **Run the application** - The stub data will automatically be provided when pages load
2. **Navigate to pages** - Use the navigation menu to visit:
   - Home (`/`) - Dashboard overview
   - My Schedule (`/schedule`) - Weekly schedule view
   - Skills & Certs (`/skills`) - Skills management

3. **The data is tied to the current user** - The service reads from the authentication context, so if authenticated, it uses the real user ID. If not authenticated, it uses a stub user ID.

## Next Steps for Development

### To Replace with Real Data:
1. Create a real `IScheduleService` that calls your backend API
2. Replace the `StubDataService` registration with your real service
3. Update the interfaces to call your actual data endpoints

### To Extend Stub Data:
- Modify `StubDataService.cs` to add more sample shifts, skills, or employees
- Create additional methods for other data needs
- Keep all methods async for consistency with real service implementation

### UI Enhancements to Consider:
- Add filtering/sorting to the schedule table
- Implement time-off request forms
- Add employee profile page
- Create supervisor dashboard
- Add shift swap functionality
- Implement time tracking features

## File Changes Summary

| File | Changes |
|------|---------|
| `Program.cs` | Added IStubDataService registration |
| `Home.razor` | Added next shift display and weekly hours summary |
| `EmployeeSchedule.razor` | Integrated IStubDataService for data loading |
| `Skills.razor` | Created new page for skills management |
| `NavMenu.razor` | Added Skills & Certs navigation link |
| `StubDataService.cs` | New file - All stub data generation |

## Notes

- The stub data service is stateless - data is regenerated on each call based on the current date/time
- All shifts and dates are relative to today's date, so they'll always show current data
- The service includes proper null-checking and empty state handling
- Break information includes paid/unpaid status and whether breaks were taken
- Employee skills include proficiency levels and expiration tracking

You're all set to start designing your UI! The sample data will automatically populate your pages.
