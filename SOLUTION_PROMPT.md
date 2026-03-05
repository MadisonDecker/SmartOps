# TimeManagement Solution - Development Prompt Document

## 1. Project Overview

**Solution Name:** TimeManagement Solution  
**Technology Stack:** Blazor WebAssembly, ASP.NET Core, Entity Framework Core, .NET 10, C# 14.0  
**Primary Purpose:** A time management and workforce tracking application that helps manage employee activities and time allocation across projects.

### Key Features (Planned/In Development)
- Time tracking for employee activities
- Workforce management and scheduling
- Activity logging and reporting
- User authentication and authorization
- Data persistence with relational database

### Key Stakeholders/Components
- **TimeKeeper Module:** Handles employee time tracking, punch logging, and activity logging
- **SmartOps Module:** Supervisor/Admin workforce management dashboard (staffing requirements, gap analysis, team management)
- **Workforce Module:** Manages employee information and scheduling
- **Repository Layer:** Data access and persistence


---

## 2. Architecture & Design

### Project Structure

```
TimeManagement Solution/
├── SmartShift.Blazor/                    # Employee-facing Blazor Server project
│   ├── SmartShift.Blazor/                # Server-side Blazor app (employee time tracking)
│   │   ├── Program.cs                    # Application startup and DI configuration
│   │   ├── Components/                   # Blazor components
│   │   │   ├── Account/                  # Identity and authentication components
│   │   │   ├── Pages/                    # Employee pages (Home, Schedule, Skills)
│   │   │   └── App.razor                 # Root component
│   │   ├── Services/                     # Service classes for API communication
│   │   │   ├── StubDataService.cs        # Development stub data service
│   │   │   └── SmartShiftClaims.cs       # Claims transformation for roles
│   │   └── appsettings.json              # Configuration (includes WebAPI base URL)
│   │
│   └── SmartShift.Blazor.Client/         # WebAssembly client project
│       ├── Program.cs                    # Client-side initialization
│       ├── Services/                     # Client-side service classes
│       └── Components/                   # Client-side components (if applicable)
│
├── SmartOps.Blazor/                      # Supervisor/Admin workforce management UI
│   └── SmartOps.Blazor/                  # Server-side Blazor app
│       ├── Program.cs                    # Application startup and DI configuration
│       ├── Components/                   # Blazor components
│       │   ├── Pages/                    # Supervisor pages
│       │   │   ├── Home.razor            # SmartOps landing page
│       │   │   └── SupervisorDashboard.razor  # Workforce management dashboard
│       │   ├── Layout/                   # Layout components
│       │   ├── StaffingRequirementsPanel.razor  # Staffing requirements management
│       │   ├── GapAnalysisPanel.razor    # Gap analysis and recommendations
│       │   ├── TeamManagementPanel.razor # Team member management
│       │   ├── ScheduleGraphPanel.razor  # Schedule visualization graph
│       │   └── ScheduleGrid.razor        # Hourly schedule grid view
│       ├── Services/                     # Service classes
│       │   └── SmartOpsClaimsTransformation.cs  # Claims transformation for roles
│       └── appsettings.json              # Configuration (Azure AD, roles)
│
├── SmartOpsManagement.WebApi/            # Web API layer (ASP.NET Core)
│   ├── Program.cs                        # API startup and DI configuration
│   ├── Controllers/                      # API controllers
│   └── SmartOpsManagement.WebApi.csproj
│
├── SmartOpsManagement.Bus/               # Business logic layer
│   ├── TimeKeeperBusinessLogic.cs        # Core time tracking logic
│   └── SmartOpsManagement.Bus.csproj
│
├── Workforce.Bus/                        # Workforce management business logic layer
│   ├── WorkforceBusinessLogic.cs         # Workforce management logic
│   └── Workforce.Bus.csproj
│
├── SmartManagement.Repo/                 # Repository/Data Access Layer
│   └── SmartManagement.Repo.csproj       # Data access abstractions and implementations 
│   └── Models/                           # Data access models
│
├── SmartOps.Models/                      # Shared data models project
│   └── SmartOps.Models.csproj            # Data model classes used across projects
│
└── ProcessSmartOpsActivity/              # Console application for background processing
    ├── Program.cs                        # Entry point
    └── ProcessSmartOpsActivity.csproj
```

### Technology Layers

1. **Presentation Layer - Employee UI:** `SmartShift.Blazor` (Server) + `SmartShift.Blazor.Client` (WebAssembly)
   - Employee-facing Blazor components for time tracking UI
   - Handles employee time punches, schedule viewing, skills management
   - Manages authentication state
   - **No direct data access** - all data operations go through service classes
   - Service classes (e.g., `SmartShiftApiService`) use `HttpClient` to communicate with `SmartOpsManagement.WebApi`
   - Razor components should be separated into UI and code files. For example, `TimeEntryForm.razor` for the UI and `TimeEntryForm.razor.cs` for the code-behind logic to maintain separation of concerns and improve readability.

2. **Presentation Layer - Supervisor/Admin UI:** `SmartOps.Blazor`
   - Supervisor and Admin workforce management dashboard
   - **Purpose:** Separate concerns - workforce management UI distinct from employee time tracking
   - **Key Features:**
     - Staffing requirements configuration
     - Gap analysis and recommendations
     - Team management and assignment
     - Schedule visualization (graphs and grids)
   - **Authorization:** Requires Supervisor or Admin role
   - Uses same authentication pattern as TimeKeeper.Blazor (Azure AD, claims transformation)
   - Independent deployment - can run separately from TimeKeeper.Blazor

3. **API Layer:** `SmartOpsManagement.WebApi`
   - ASP.NET Core REST API
   - Exposes endpoints for both Blazor front-ends
   - Coordinates business logic and data access
   - Handles request validation and response formatting

4. **Business Logic Layer:** `SmartOpsManagement.Bus`
   - Core business rules and workflows
   - Service logic for time tracking and workforce management
   - **Handles all data access** - uses data objects to interact with SQL database
   - Used by API layer and background processing
   - One core object is the `TimeKeeperBusinessLogic` class which will contain methods for handling time tracking operations, such as logging time entries, calculating total hours, and enforcing business rules.
   - Code is broken down into classes based on functionality (e.g., Schedule, Punch) to maintain separation of concerns and improve maintainability. These classes will be partial classes of type TimeKeeperBusinessLogic to allow for better organization and readability of the codebase.

5. **Workforce Business Logic Layer:** `Workforce.Bus`
   - Business logic specific to ADP workforce management.
   - Employee scheduling, and Timesheet management logic.
   - This is used to pay employees and is not the source for scheduling or time tracking data. It is only used to send data to ADP for payroll processing.

6. **Data Access Layer:** `SmartManagement.Repo`
   - Repository pattern implementations
   - Entity Framework Core integration
   - Direct database interaction

7. **Background Processing:** `ProcessSmartOpsActivity`
   - Console app for background tasks
   - Activity processing or scheduled jobs (TBD)
   - Uses SmartOpsManagement.Bus for business logic

### Dependency Flow
```
SmartShift.Blazor (Employee UI)          SmartOps.Blazor (Supervisor UI)
        ↓                                        ↓
        ↓ (HTTP/REST)                            ↓ (HTTP/REST)
        └─────────────→ SmartOpsManagement.WebApi ←──┘
                                ↓
                        SmartOpsManagement.Bus ←── ProcessSmartOpsActivity
                                ↓
                    SmartManagement.Repo, Workforce.Bus
                                ↓
                    Entity Framework Core + SQL Database
```

**Key Notes:**
- **SmartShift.Blazor** - Employee-facing UI for time tracking (punches, schedules, skills)
- **SmartOps.Blazor** - Supervisor/Admin UI for workforce management (staffing, gaps, team)
- Both Blazor apps have **no Data layer** - use service classes to call SmartOpsManagement.WebApi via HTTP
- Both apps can be deployed and run independently
- SmartOpsManagement.WebApi only depends on SmartOpsManagement.Bus
- SmartOpsManagement.Bus handles all business logic and data access with SQL
- Repositories and database access are handled exclusively by SmartOpsManagement.Bus
- Service classes in Blazor projects abstract HTTP communication with the WebAPI

---

## 3. Coding Standards & Conventions

### C# & .NET Version
- **C# Version:** 14.0 (Use latest C# 14 features where appropriate)
- **.NET Target:** .NET 10 (Use .NET 10 specific features and APIs)

### Naming Conventions
- **Namespaces:** `YourCompany.Module.SubModule` (e.g., `SmartOpsManagement.Bus`, `Workforce.Bus`)
- **Classes:** PascalCase (e.g., `TimeKeeperBusinessLogic`, `ApplicationUser`)
- **Methods:** PascalCase (e.g., `GetEmployeeActivities()`, `LogTimeEntry()`)
- **Properties:** PascalCase (e.g., `EmployeeId`, `ActivityDate`)
- **Private fields:** camelCase with underscore prefix (e.g., `_logger`, `_context`)
- **Constants:** UPPER_SNAKE_CASE (e.g., `MAX_HOURS_PER_DAY`)

### Code Style
- **Indentation:** Use spaces (2 or 4 spaces, be consistent with existing code)
- **Async/Await:** Use async/await for all I/O operations and service methods
- **LINQ:** Prefer LINQ query syntax where readable, method syntax for simple operations
- **Null Handling:** Use null-coalescing operators and pattern matching where appropriate
- **Comments:** Only add comments for complex logic; code should be self-documenting

### Design Patterns
- **Dependency Injection:** Use constructor injection for all dependencies
- **Repository Pattern:** Data access through repository abstractions in `SmartManagement.Repo`
- **Service Pattern:** Business logic encapsulated in service classes (`TimeKeeperBusinessLogic`, `WorkforceBusinessLogic`)
- **API Service Pattern:** Blazor projects use service classes (e.g., `TimeKeeperApiService`) that wrap `HttpClient` calls to `SmartOpsManagement.WebApi` - no direct data access from Blazor
- **Async/Await:** All async operations should propagate through layers

### File Organization
```csharp
// Order of declarations in class files:
1. Namespace declaration
2. Using statements (sorted alphabetically)
3. Class/Interface declaration
4. Fields/Properties (public first, then protected, then private)
5. Constructors
6. Public methods
7. Protected/Private methods
```

---

## 4. Database & Entity Framework Core

### Current Setup
- **ORM:** Entity Framework Core (used in `SmartOpsManagement.Bus` layer)
- **DbContext:** Located in `SmartOpsManagement.Bus` or `SmartManagement.Repo` project (NOT in SmartShift.Blazor)
- **Authentication:** ASP.NET Core Identity (managed via WebAPI)
- **Migrations:** Located in the data access layer (`SmartManagement.Repo` or `SmartOpsManagement.Bus`)

**Important:** SmartShift.Blazor does NOT have direct database access. All data operations flow through:
1. Blazor Service Classes → 2. SmartOpsManagement.WebApi → 3. SmartOpsManagement.Bus → 4. SQL Database

### Data Models (Planned)
- `ApplicationUser` - Identity user with time tracking extensions
- Workforce entities (Employees, Departments, Roles, etc.)
- Time tracking entities (TimeEntries, Activities, Projects, etc.)
- Supporting entities for business logic

### Key Entities (To Be Defined)
```csharp
// Example structure (to be implemented)
- Employee/User (linked to ApplicationUser)
- TimeEntry (Date, Hours, Activity, Employee)
- Activity (Name, Description, Category)
- Project (Name, Description, Team)
- Department (Name, Manager)
```

---

## 5. Business Logic & Domain Rules

### TimeKeeper Module
**Purpose:** Track employee time and activities

- **Core Concepts:**
  - Time entries with date, start/end times, and duration
  - Activities categorized by type (development, meetings, admin, etc.)
  - Project associations for billing/tracking
  
- **Business Rules:**
  - Maximum work hours per day (e.g., 8 hours)
  - Time entries cannot be created for future dates
  - Activities must be linked to valid projects
  - Approval workflow for manager review (if applicable)

### Workforce Module
**Purpose:** Manage employee information and schedules

- **Core Concepts:**
  - Employee profiles with roles and departments
  - Work schedules and availability
  - Manager hierarchies
  - Team assignments

- **Business Rules:**
  - Each employee belongs to exactly one department
  - Managers can only view their team's time entries
  - Schedule conflicts should be detected
  - Role-based access control (RBAC) enforced

### Integration Points
- TimeKeeper activities reference Workforce employees
- Workforce hierarchy determines access permissions
- Reports aggregate data from both modules

---

## 6. Authentication & Authorization

### Identity Setup
- **Provider:** ASP.NET Core Identity
- **User Model:** `ApplicationUser` (extends IdentityUser)
- **Auth Method:** Forms-based (Blazor built-in)
- **Passkey Support:** Implemented via `PasskeyOperation` and `PasskeyInputModel`

### Access Control
- Role-based: Employee, Supervisor, Administrator
- Employee: View own time entries, schedule, skills
- Supervisor: View team's time entries, manage staffing requirements, gap analysis
- Administrator: View all data, manage users, full workforce management
- Role assignment via Azure AD groups or email-based configuration (see `appsettings.json`)

### Claims Transformation
Both Blazor apps use claims transformation to assign roles:
- `SmartShiftClaims` (SmartShift.Blazor)
- `SmartOpsClaimsTransformation` (SmartOps.Blazor)

Configuration in `appsettings.json`:
```json
{
  "Authorization": {
    "Roles": {
      "SupervisorEmails": ["supervisor@company.com"],
      "SupervisorGroupIds": ["azure-ad-group-id"],
      "AdminEmails": ["admin@company.com"],
      "AdminGroupIds": ["azure-ad-admin-group-id"]
    }
  }
}
```

---

## 7. Frontend (Blazor Applications)

### Application Separation
The solution uses two separate Blazor applications to maintain separation of concerns:

| Application | Purpose | Target Users | Key Features |
|-------------|---------|--------------|--------------|
| **SmartShift.Blazor** | Employee time tracking | All employees | Punch logging, schedule viewing, skills management |
| **SmartOps.Blazor** | Workforce management | Supervisors, Admins | Staffing requirements, gap analysis, team management |

### SmartShift.Blazor (Employee UI)
**Component Structure:**
- Server-side rendering in `SmartShift.Blazor`
- WebAssembly client in `SmartShift.Blazor.Client`
- Shared components and layouts

**Key Pages:**
- `Home.razor` - Employee dashboard
- `EmployeeSchedule.razor` - View personal schedule
- `Skills.razor` - Manage skills and certifications

### SmartOps.Blazor (Supervisor/Admin UI)
**Component Structure:**
- Server-side Blazor app (Interactive Server mode)
- Requires Supervisor or Admin role for access

**Key Pages:**
- `Home.razor` - SmartOps landing page with feature cards
- `SupervisorDashboard.razor` - Main workforce management dashboard

**Key Components:**
- `ScheduleGraphPanel.razor` - Visual graph of required vs. assigned staff
- `ScheduleGrid.razor` - Hourly grid view of staffing
- `StaffingRequirementsPanel.razor` - Configure staffing needs by day/hour
- `GapAnalysisPanel.razor` - Identify understaffing and get recommendations
- `TeamManagementPanel.razor` - Manage team members and assignments

### Service Classes
Service classes in the Blazor projects handle all communication with `SmartOpsManagement.WebApi`:
- `TimeKeeperApiService` - Handles time tracking API calls (time entries, activities, etc.)
- `WorkforceApiService` - Handles workforce-related API calls (employees, schedules, etc.)

**Service Class Pattern:**
```csharp
public class TimeKeeperApiService(HttpClient httpClient)
{
    public async Task<List<TimeEntry>> GetTimeEntriesAsync() 
        => await httpClient.GetFromJsonAsync<List<TimeEntry>>("api/timeentries") ?? [];

    public async Task<TimeEntry?> CreateTimeEntryAsync(TimeEntry entry)
        => await httpClient.PostAsJsonAsync("api/timeentries", entry)
            .ContinueWith(r => r.Result.Content.ReadFromJsonAsync<TimeEntry>()).Unwrap();
}
```

### Key Components (SmartShift.Blazor - To Be Created)
- `TimeEntryForm.razor` - Create/edit time entries
- `TimeEntryList.razor` - Display employee time entries
- `PunchClock.razor` - Clock in/out interface
- `Dashboard.razor` - Employee overview and analytics

---

## 8. Development Workflow & Best Practices

### When Adding Features
1. **Define Domain Model** → Create entity in appropriate layer
2. **Create Repository** → Implement CRUD operations in `SmartManagement.Repo`
3. **Add Business Logic** → Implement service methods in `*.Bus` projects
4. **Create UI Components** → Build Blazor components in appropriate project:
   - Employee features → `SmartShift.Blazor`
   - Supervisor/Admin features → `SmartOps.Blazor`
5. **Write Tests** → Unit tests for logic, integration tests for repositories (TBD)
6. **Database Migration** → Create EF Core migration if schema changed

### Adding a New Feature Example
```
Feature: "Track lunch breaks"

1. Model Layer:
   - Add `LunchBreak` entity with date, start, end times in SmartOps.Models
   - Add DbSet<LunchBreak> to DbContext in SmartOpsManagement.Bus or SmartManagement.Repo

2. Repository Layer:
   - Create `ILunchBreakRepository` interface
   - Implement `LunchBreakRepository` in SmartManagement.Repo

3. Business Logic Layer:
   - Add `LogLunchBreak()` method to `TimeKeeperBusinessLogic`
   - Add validation rules (duration limits, overlapping times, etc.)

4. API Layer:
   - Add endpoint in SmartOpsManagement.WebApi controller (e.g., POST /api/lunchbreaks)

5. Blazor Service Layer:
   - Add method in service class (e.g., `TimeKeeperApiService.LogLunchBreakAsync()`)
   - Service calls WebAPI endpoint via HttpClient

6. UI Layer:
   - Create `LunchBreakForm.razor` component
   - Inject service class and call service methods
   - Integrate with time entry tracking

7. Dependency Injection:
   - Register HttpClient and service classes in Blazor Program.cs
   - Register repository in WebAPI Program.cs
```

### Error Handling
- Use try-catch in service methods
- Return meaningful error messages
- Log exceptions appropriately
- Use validation attributes on models
- Provide user-friendly error messages in Blazor components

---

## 9. Project Dependencies

### Key NuGet Packages (Current/Planned)
- **Entity Framework Core** - ORM and database context
- **ASP.NET Core Identity** - Authentication/Authorization
- **Blazor Web Framework** - UI framework
- **Microsoft.Extensions.DependencyInjection** - Dependency injection

### Dependency Guidelines
- Only add packages when solving a real problem
- Prefer built-in .NET functionality where available
- Keep dependency versions current but stable
- Document why non-standard packages are needed

---

## 10. External Resources & Documentation

### Workforce Software API Documentation
**For Workforce.Bus Project Development:**
- **API YAML:** https://docs.integration.wfs.cloud/wfs-integration-api.yaml
  - OpenAPI specification for Workforce Software REST API
  - Use for generating client code or understanding API endpoints and data models
- **API Documentation:** https://docs.integration.wfs.cloud/#/Employee%20Group/get_employee_group_v1
  - Reference for integrating with Workforce Software REST API
  - Contains endpoint specifications for employee groups, data structures, and integration patterns
  **Shared File Location:** https://login.microsoftonline.com/4c2c8480-d3f0-485b-b750-807ff693802f/oauth2/authorize?client_id=00000003-0000-0ff1-ce00-000000000000&response_mode=form_post&response_type=code%20id_token&resource=00000003-0000-0ff1-ce00-000000000000&scope=openid&nonce=38C7B86047DCE34CCF3257B30DFB8A6ECBC5279C027C59BE-50F18AF77FC494C46526619D940B5635998F9FE3E7FAFDDD769534F2519A3FD9&redirect_uri=https%3A%2F%2Fadponline%2Esharepoint%2Ecom%2F_forms%2Fdefault%2Easpx&state=OD0w&claims=%7B"id_token"%3A%7B"xms_cc"%3A%7B"values"%3A%5B"CP1"%5D%7D%7D%7D&wsucxt=1&cobrandid=11bd8083-87e0-41b5-bb78-0bc43c8a8e8a&client-request-id=4929faa1-f0ec-b000-d71f-1c198d62a236
  - Contains files used to share with Workforce Software for integration purposes, including the API specification and documentation

- **Local Documentation:** `WorkForce Software Rest API Guide.pdf` (in Solution Items)
  - Comprehensive guide for API integration
  - Use when implementing data import/export features in `Workforce.Bus`
  - Reference for data mapping and API response structures

**Usage:** When generating or modifying code for the `Workforce.Bus` project, consult these resources to ensure compatibility with Workforce Software APIs and proper data structure handling.

---

## 11. Testing Strategy

### Unit Testing (Planned)
- Test business logic in `SmartOpsManagement.Bus` and `Workforce.Bus`
- Mock repositories with interfaces
- Test validation rules and business logic

### Integration Testing (Planned)
- Test repository operations with in-memory database
- Test full feature workflows through layers

### Current Status
- No test projects yet; to be created as needed

---

## 12. Important Notes & Conventions

- **Blazor Rendering Mode:** 
  - SmartShift.Blazor: Interactive Server + WebAssembly (hybrid)
  - SmartOps.Blazor: Interactive Server
- **UI Separation:** Employee features in SmartShift.Blazor, Supervisor/Admin features in SmartOps.Blazor
- **State Management:** TBD (consider Cascading Parameters or service-based state)
- **API Layer:** Currently no explicit API layer; data flows through services
- **Logging:** To be configured (Serilog or built-in logging recommended)
- **Configuration:** appsettings.json in each Blazor project; environment-specific configs for dev/prod
- **Authentication:** Azure AD with Microsoft Identity Web; claims transformation for role assignment

---

## 13. Getting Started With AI Assistance

When requesting help with this solution, provide:
1. **What you're building:** Feature name and purpose
2. **Where it belongs:** Which layer/project (UI, Business Logic, Data)
3. **Related entities:** What data models are involved
4. **Business rules:** Any validation or constraints
5. **Current code:** If modifying existing, share relevant snippets

### Example Request:
> "I need to add a feature to log employee time entries. Create:
> - A TimeEntry entity with Date, Hours, ActivityType, EmployeeId
> - Repository for CRUD operations
> - A LogTimeEntry() method in TimeKeeperBusinessLogic with validation (max 8 hours/day)
> - A Blazor component for the form in SmartShift.Blazor (employee-facing)"

> "I need to add a staffing forecast feature. Create:
> - Components in SmartOps.Blazor for supervisors
> - Integration with existing StaffingRequirement model
> - Visualization showing predicted vs. actual staffing"

---

## 14. Version History
- **Created:** 2025 (Initial Setup)
- **Last Updated:** 2025 - Renamed TimeManagement.Bus to SmartOpsManagement.Bus, TimeManagement.Repo to SmartManagement.Repo
- **Next Review:** After first major feature implementation

### Change Log
- **2025-XX-XX:** Initial solution structure with TimeKeeper.Blazor
- **2025-XX-XX:** Added SmartOps.Blazor project to separate supervisor/admin UI from employee time tracking
  - Moved SupervisorDashboard, StaffingRequirementsPanel, GapAnalysisPanel, TeamManagementPanel, ScheduleGraphPanel, ScheduleGrid from TimeKeeper.Blazor to SmartOps.Blazor
  - TimeKeeper.Blazor now focuses on employee-facing features (punches, schedule viewing, skills)
  - SmartOps.Blazor handles supervisor/admin workforce management
- **2025-XX-XX:** Renamed TimeManagement.Models project to SmartOps.Models
  - Moved project folder from `TimeManagement.Models/` to `SmartOps.Models/`
  - Updated all project references (TimeManagement.Bus, TimeKeeper.Blazor, SmartOps.Blazor)
  - Updated namespaces from `TimeManagement.Models` to `SmartOps.Models`
- **2025-XX-XX:** Renamed TimeKeeper.Blazor project to SmartShift.Blazor
  - Renamed `TimeKeeper.Blazor/` folder to `SmartShift.Blazor/`
  - Renamed `TimeKeeper.Blazor.Client/` to `SmartShift.Blazor.Client/`
  - Updated all namespaces from `TimeKeeper.Blazor` to `SmartShift.Blazor`
  - Renamed `TimeTrackerClaims.cs` to `SmartShiftClaims.cs`
  - SmartShift.Blazor continues as the employee-facing time tracking application
- **2025-XX-XX:** Renamed TimeManagement.WebAPI project to SmartOpsManagement.WebApi
  - Renamed `TimeManagement.WebAPI/` folder to `SmartOpsManagement.WebApi/`
  - Updated all project references and namespaces
- **2025-XX-XX:** Renamed TimeManagement.Bus project to SmartOpsManagement.Bus
  - Renamed `TimeManagement.Bus/` folder to `SmartOpsManagement.Bus/`
  - Updated all namespaces from `TimeManagement.Bus` to `SmartOpsManagement.Bus`
  - Updated all project references in the solution
- **2025-XX-XX:** Renamed TimeManagement.Repo project to SmartManagement.Repo
  - Renamed `TimeManagement.Repo/` folder to `SmartManagement.Repo/`
  - Updated solution file reference
- **2025-XX-XX:** Renamed ProcessTimeKeeperActivity project to ProcessSmartOpsActivity
  - Renamed `ProcessTimeKeeperActivity/` folder to `ProcessSmartOpsActivity/`
  - Updated solution file reference
  - ProcessSmartOpsActivity is the console app for background processing tasks
