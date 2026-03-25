# TimeManagement Solution - Development Prompt Document

## 1. Project Overview

**Solution Name:** SmartOps Solution
**Technology Stack:** Blazor WebAssembly, ASP.NET Core, Entity Framework Core, .NET 10, C# 14.0
**Primary Purpose:** A workforce scheduling and time management application that helps manage employee schedules, availability, time-off requests, staffing requirements, and gap analysis.

### Key Features (In Development / Active)
- Employee schedule viewing and availability management
- Time-off request submission and approval
- Workforce staffing requirements and gap analysis
- Supervisor/Admin workforce management dashboard
- WorkGroup management (team assignments, skill tracking)
- Weekly staffing metrics and FTE reporting
- Line adherence tracking
- ADP Workforce Software integration (via Workforce.Bus)
- Etime legacy system integration (via Etime.Bus)
- User authentication and authorization (Azure AD + ASP.NET Core Identity)

### Key Stakeholders/Components
- **SmartShift Module:** Employee-facing UI for schedule viewing, availability, and time-off requests
- **SmartOps Module:** Supervisor/Admin workforce management dashboard (staffing requirements, gap analysis, team management)
- **Workforce Module:** ADP Workforce Software integration for payroll data export
- **Etime Module:** Legacy Etime system integration for schedule data import
- **Repository Layer:** Data access and persistence via Entity Framework Core


---

## 2. Architecture & Design

### Project Structure

```
SmartOps Solution/
├── SmartShift.Blazor/                    # Employee-facing Blazor Server project
│   ├── SmartShift.Blazor/                # Server-side Blazor app (employee schedule/availability)
│   │   ├── Program.cs                    # Application startup and DI configuration
│   │   ├── Components/                   # Blazor components
│   │   │   ├── Pages/                    # Employee pages
│   │   │   │   ├── Home.razor / .cs      # Employee dashboard
│   │   │   │   ├── EmployeeSchedule.razor / .cs  # View personal schedule
│   │   │   │   ├── Availability.razor / .cs      # Manage availability
│   │   │   │   ├── Skills.razor / .cs    # Skills and certifications
│   │   │   │   ├── About.razor           # About page
│   │   │   │   ├── NotFound.razor        # 404 page
│   │   │   │   └── Error.razor           # Error page
│   │   │   ├── Layout/                   # Layout components
│   │   │   └── App.razor                 # Root component
│   │   ├── Services/                     # Service classes for API communication
│   │   │   ├── IShiftDataService.cs      # Interface for shift/schedule data
│   │   │   ├── ShiftDataService.cs       # HTTP service calling SmartOpsManagement.WebApi
│   │   │   ├── RunAsService.cs           # "Run As" impersonation support
│   │   │   ├── SmartShiftClaims.cs       # Claims transformation for roles
│   │   │   └── DevelopmentClaimsTransformation.cs  # Dev-mode claims override
│   │   └── appsettings.json              # Configuration (includes WebAPI base URL)
│   │
│   └── SmartShift.Blazor.Client/         # WebAssembly client project
│       ├── Program.cs                    # Client-side initialization
│       ├── Pages/                        # Client-side pages (Auth.razor)
│       └── RedirectToLogin.razor         # Auth redirect helper
│
├── SmartOps.Blazor/                      # Supervisor/Admin workforce management UI
│   ├── Program.cs                        # Application startup and DI configuration
│   ├── Components/                       # Blazor components
│   │   ├── Pages/                        # Supervisor pages
│   │   │   ├── Home.razor                # SmartOps landing page
│   │   │   ├── SupervisorDashboard.razor / .cs  # Main workforce management dashboard
│   │   │   ├── NotFound.razor            # 404 page
│   │   │   └── Error.razor               # Error page
│   │   ├── Layout/                       # Layout components
│   │   ├── StaffingRequirementsPanel.razor / .cs  # Staffing requirements management
│   │   ├── GapAnalysisPanel.razor / .cs  # Gap analysis and recommendations
│   │   ├── TeamManagementPanel.razor / .cs  # Team member management
│   │   ├── ScheduleGraphPanel.razor / .cs   # Schedule visualization graph
│   │   └── ScheduleGrid.razor / .cs         # Hourly schedule grid view
│   ├── Services/                         # Service classes
│   │   ├── ISmartOpsDataService.cs       # Interface for SmartOps data
│   │   ├── SmartOpsDataService.cs        # HTTP service calling SmartOpsManagement.WebApi
│   │   ├── RunAsService.cs               # "Run As" impersonation support
│   │   └── SmartOpsClaimsTransformation.cs  # Claims transformation for roles
│   └── appsettings.json                  # Configuration (Azure AD, roles)
│
├── SmartOps.Shared.UI/                   # Shared Razor component library
│   ├── wwwroot/css/                      # Shared CSS styles
│   └── SmartOps.Shared.UI.csproj         # Razor class library (net10.0)
│
├── SmartOpsManagement.WebApi/            # Web API layer (ASP.NET Core Minimal API)
│   ├── Program.cs                        # API startup and DI configuration
│   ├── Endpoints/                        # Minimal API endpoint registrations
│   │   ├── EmployeeAvailabilityEndpoints.cs
│   │   ├── LineAdherenceEndpoints.cs
│   │   ├── ScheduleEndpoints.cs
│   │   ├── TimeOffRequestEndpoints.cs
│   │   ├── WeeklyStaffingMetricsEndpoints.cs
│   │   └── WorkGroupEndpoints.cs
│   └── SmartOpsManagement.WebApi.csproj
│
├── SmartOpsManagement.Bus/               # Business logic layer
│   ├── SmartOpsBusinessLogic.cs          # Core partial class (DI entry point)
│   ├── Schedule.cs                       # Partial class: schedule logic
│   ├── LineAdherence.cs                  # Partial class: line adherence
│   ├── EmployeeAvailabilityLogic.cs      # Partial class: availability logic
│   ├── TimeOffRequestLogic.cs            # Partial class: time-off request logic
│   ├── WorkGroupLogic.cs                 # Partial class: work group management
│   ├── WeeklyStaffingMetrics.cs          # Partial class: staffing metrics
│   ├── WeeklyFTEMetrics.cs               # Partial class: FTE metrics
│   ├── Services/                         # Service interfaces
│   │   ├── IBreakManagementService.cs
│   │   ├── IEmployeeService.cs
│   │   ├── IEmployeeSkillService.cs
│   │   ├── IGapAnalysisService.cs
│   │   ├── IScheduleService.cs
│   │   └── IStaffingRequirementService.cs
│   └── SmartOpsManagement.Bus.csproj
│
├── Workforce.Bus/                        # ADP Workforce Software integration layer
│   ├── WorkforceBusinessLogic.cs         # Workforce payroll data export logic
│   └── Workforce.Bus.csproj
│
├── Etime.Bus/                            # Legacy Etime system integration layer
│   ├── EtimeBusinessLogic.cs             # Schedule import from Etime SQL database
│   └── Etime.Bus.csproj
│
├── SmartManagement.Repo/                 # Repository/Data Access Layer (EF Core)
│   ├── Models/                           # EF Core entity models
│   │   ├── SmartOpsContext.cs            # EF Core DbContext
│   │   ├── EmployeeAvailability.cs
│   │   ├── EmployeeAvailabilityDay.cs
│   │   ├── EtimeShift.cs
│   │   ├── LineAdherence.cs / Latdetail.cs
│   │   ├── ScheduleException.cs / ScheduleExceptionType.cs
│   │   ├── ScheduleShiftPattern.cs / ScheduleTemplate.cs
│   │   ├── TimeOffRequest.cs / TimeOffRequestStatus.cs
│   │   ├── WorkGroupMember.cs / Workgroup.cs
│   │   ├── AlertContactMethod.cs
│   │   └── VwCurrentEmployeeAvailability.cs
│   ├── Sql/                              # Raw SQL scripts (schema, migrations)
│   ├── LineAdherenceRepository.cs        # Repository implementation
│   └── SmartManagement.Repo.csproj
│
├── SmartOps.Models/                      # Shared DTOs and data model classes
│   ├── BreakTemplate.cs
│   ├── Client.cs
│   ├── DateTimeExtensions.cs
│   ├── EmployeeAvailabilityDto.cs
│   ├── EmployeeInfo.cs
│   ├── EmployeeSkill.cs
│   ├── ScheduledShift.cs
│   ├── StaffingRequirement.cs
│   ├── TimeOffRequestDto.cs
│   ├── WeeklyStaffingMetrics.cs
│   ├── WorkGroupMemberDto.cs
│   ├── Workgroup.cs
│   └── SmartOps.Models.csproj
│
├── ProcessSmartOpsActivity/              # Console application for background processing
│   ├── Program.cs                        # Entry point
│   └── ProcessSmartOpsActivity.csproj
│
└── PdfExtractTool/                       # Developer utility: PDF text extraction
    ├── Program.cs                        # Extracts text from WorkForce Software REST API Guide PDF
    └── PdfExtractTool.csproj
```

### Technology Layers

1. **Presentation Layer - Employee UI:** `SmartShift.Blazor` (Server) + `SmartShift.Blazor.Client` (WebAssembly)
   - Employee-facing Blazor components for schedule viewing, availability, and time-off requests
   - Handles schedule display, availability management, skills management
   - Manages authentication state
   - **No direct data access** - all data operations go through service classes
   - Service classes (`IShiftDataService` / `ShiftDataService`) use `HttpClient` to communicate with `SmartOpsManagement.WebApi`
   - Supports "Run As" impersonation for development/testing via `RunAsService`
   - Razor components are separated into UI (`.razor`) and code-behind (`.razor.cs`) files

2. **Presentation Layer - Supervisor/Admin UI:** `SmartOps.Blazor`
   - Supervisor and Admin workforce management dashboard
   - **Purpose:** Separate concerns - workforce management UI distinct from employee schedule UI
   - **Key Features:** Staffing requirements, gap analysis, team management, schedule visualization
   - **Authorization:** Requires Supervisor or Admin role
   - Service classes (`ISmartOpsDataService` / `SmartOpsDataService`) communicate with WebApi
   - Independent deployment - can run separately from SmartShift.Blazor

3. **Shared UI Library:** `SmartOps.Shared.UI`
   - Razor class library for UI elements and CSS shared across Blazor apps

4. **API Layer:** `SmartOpsManagement.WebApi`
   - ASP.NET Core **Minimal API** (uses Endpoint classes, not MVC Controllers)
   - Endpoint files: `ScheduleEndpoints`, `EmployeeAvailabilityEndpoints`, `TimeOffRequestEndpoints`, `WorkGroupEndpoints`, `LineAdherenceEndpoints`, `WeeklyStaffingMetricsEndpoints`

5. **Business Logic Layer:** `SmartOpsManagement.Bus`
   - Central class: `SmartOpsBusinessLogic` (partial, takes `SmartOpsContext` via DI)
   - Domain split into partial classes: `Schedule`, `LineAdherence`, `EmployeeAvailabilityLogic`, `TimeOffRequestLogic`, `WorkGroupLogic`, `WeeklyStaffingMetrics`, `WeeklyFTEMetrics`
   - Service interfaces in `Services/`: `IBreakManagementService`, `IEmployeeService`, `IEmployeeSkillService`, `IGapAnalysisService`, `IScheduleService`, `IStaffingRequirementService`

6. **Workforce Business Logic Layer:** `Workforce.Bus`
   - ADP Workforce Software payroll export logic
   - Not the source for scheduling data; exports to ADP only

7. **Etime Integration Layer:** `Etime.Bus`
   - Reads schedule data from legacy Etime SQL Server database (SvrPitSqlAdp/SSO, Windows auth)
   - Exports schedule records to JSON for import into SmartOps
   - Key class: `EtimeBusinessLogic` with `GetAndExportSchedules()`

8. **Data Access Layer:** `SmartManagement.Repo`
   - EF Core; DbContext: `SmartOpsContext`
   - Entity models for all domains; raw SQL scripts in `Sql/`

9. **Shared Models:** `SmartOps.Models`
   - DTOs shared across projects: `EmployeeInfo`, `ScheduledShift`, `StaffingRequirement`, `EmployeeAvailabilityDto`, `TimeOffRequestDto`, `WeeklyStaffingMetrics`, `Workgroup`, `WorkGroupMemberDto`, `EmployeeSkill`, `BreakTemplate`, `Client`

10. **Background Processing:** `ProcessSmartOpsActivity`
    - Console app for background/scheduled tasks; uses SmartOpsManagement.Bus

11. **Developer Utility:** `PdfExtractTool`
    - One-off tool to extract text from the WorkForce Software REST API Guide PDF; not production

### Dependency Flow
```
SmartShift.Blazor (Employee UI)          SmartOps.Blazor (Supervisor UI)
        ↓                                        ↓
        ↓ (HTTP/REST)                            ↓ (HTTP/REST)
        └─────────────→ SmartOpsManagement.WebApi ←──┘
                                ↓
                        SmartOpsManagement.Bus ←── ProcessSmartOpsActivity
                                ↓
              SmartManagement.Repo (SmartOpsContext / EF Core)
                                ↓
                           SQL Database
              (Workforce.Bus → ADP, Etime.Bus → Etime SQL)
```

**Key Notes:**
- **SmartShift.Blazor** - Employee UI: schedule, availability, time-off, skills
- **SmartOps.Blazor** - Supervisor/Admin UI: staffing requirements, gap analysis, team management
- Both Blazor apps have **no Data layer** - call SmartOpsManagement.WebApi via HTTP service classes
- WebApi uses **Minimal API Endpoints** (not MVC Controllers)
- `SmartOpsBusinessLogic` is the core BL class (partial), injected with `SmartOpsContext`
- `Workforce.Bus` and `Etime.Bus` are integration-only; not used for live scheduling data

---

## 3. Coding Standards & Conventions

### C# & .NET Version
- **C# Version:** 14.0 (Use latest C# 14 features where appropriate)
- **.NET Target:** .NET 10 (Use .NET 10 specific features and APIs)

### Naming Conventions
- **Namespaces:** `YourCompany.Module.SubModule` (e.g., `SmartOpsManagement.Bus`, `Workforce.Bus`)
- **Classes:** PascalCase (e.g., `SmartOpsBusinessLogic`, `ApplicationUser`)
- **Methods:** PascalCase (e.g., `GetEmployeeSchedule()`, `GetAvailability()`)
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
- **Partial Classes:** Business logic split into partial classes by domain in `SmartOpsManagement.Bus`
- **Service Interfaces:** Business capabilities exposed via interfaces in `SmartOpsManagement.Bus/Services/`
- **Minimal API Endpoints:** WebApi uses endpoint class files, not MVC controllers
- **API Service Pattern:** Blazor projects use `IShiftDataService`/`ShiftDataService` and `ISmartOpsDataService`/`SmartOpsDataService` to wrap `HttpClient` calls - no direct data access from Blazor
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
- **ORM:** Entity Framework Core
- **DbContext:** `SmartOpsContext` in `SmartManagement.Repo` project (NOT in Blazor apps)
- **Authentication:** Azure AD (Microsoft Identity Web) + ASP.NET Core Identity
- **Schema scripts:** Raw SQL in `SmartManagement.Repo/Sql/`

**Important:** Blazor apps do NOT have direct database access. All data operations flow through:
1. Blazor Service Classes → 2. SmartOpsManagement.WebApi → 3. SmartOpsManagement.Bus → 4. `SmartOpsContext` → SQL Database

### Key Entities (Active)
- `EmployeeAvailability` / `EmployeeAvailabilityDay` - Employee availability windows
- `VwCurrentEmployeeAvailability` - View for current availability
- `TimeOffRequest` / `TimeOffRequestStatus` - Time-off request workflow
- `ScheduleShiftPattern` / `ScheduleTemplate` - Shift pattern definitions
- `ScheduleException` / `ScheduleExceptionType` - Schedule exception tracking
- `EtimeShift` - Imported Etime schedule records
- `Workgroup` / `WorkGroupMember` - WorkGroup/team assignments
- `LineAdherence` / `Latdetail` - Line adherence tracking data
- `AlertContactMethod` - Alert/notification contact configuration

---

## 5. Business Logic & Domain Rules

### SmartShift Module (Employee-Facing)
**Purpose:** Employee schedule viewing and self-service

- **Core Concepts:**
  - View personal scheduled shifts
  - Manage availability windows
  - Submit and track time-off requests
  - View assigned skills/certifications

- **Business Rules:**
  - Employees can only view/edit their own data
  - Availability changes may require supervisor approval
  - Time-off requests trigger approval workflow

### SmartOps Module (Supervisor/Admin)
**Purpose:** Workforce management and staffing oversight

- **Core Concepts:**
  - Staffing requirements by day/hour
  - Gap analysis (required vs. assigned staff)
  - WorkGroup management
  - Schedule visualization (graph + grid)
  - Weekly FTE and staffing metrics
  - Line adherence monitoring

- **Business Rules:**
  - Supervisors manage their assigned WorkGroups/teams
  - Gap analysis compares staffing requirements vs. scheduled employees
  - Role-based access: Supervisor sees team data, Admin sees all

### Integration Points
- `Etime.Bus` imports schedule data from legacy Etime SQL database
- `Workforce.Bus` exports payroll data to ADP Workforce Software
- Both integrations are one-directional; SmartOps is the system of record for scheduling

---

## 6. Authentication & Authorization

### Identity Setup
- **Provider:** Azure AD (Microsoft Identity Web) + ASP.NET Core Identity
- **Auth Method:** Azure AD OAuth2 with claims transformation for roles
- Claims transformation assigns application roles based on Azure AD group membership or email

### Access Control
- Role-based: Employee, Supervisor, Administrator
- Employee: View own schedule, manage availability, submit time-off requests, view skills
- Supervisor: View team schedules, manage staffing requirements, gap analysis, WorkGroup management
- Administrator: Full access — all data, user management, complete workforce management
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
- WebAssembly client in `SmartShift.Blazor.Client` (Auth.razor)
- Code-behind pattern: `.razor` + `.razor.cs` for all pages

**Key Pages:**
- `Home.razor / .cs` - Employee dashboard
- `EmployeeSchedule.razor / .cs` - View personal schedule
- `Availability.razor / .cs` - Manage availability windows
- `Skills.razor / .cs` - Manage skills and certifications
- `About.razor` - About page

**Key Services:**
- `IShiftDataService` / `ShiftDataService` - HTTP calls to WebApi
- `RunAsService` - "Run As" impersonation support
- `SmartShiftClaims` - Claims transformation (prod)
- `DevelopmentClaimsTransformation` - Dev-mode claims override

### SmartOps.Blazor (Supervisor/Admin UI)
**Component Structure:**
- Server-side Blazor app (Interactive Server mode)
- Requires Supervisor or Admin role for access

**Key Pages:**
- `Home.razor` - SmartOps landing page with feature cards
- `SupervisorDashboard.razor / .cs` - Main workforce management dashboard

**Key Components:**
- `ScheduleGraphPanel.razor / .cs` - Visual graph of required vs. assigned staff
- `ScheduleGrid.razor / .cs` - Hourly grid view of staffing
- `StaffingRequirementsPanel.razor / .cs` - Configure staffing needs by day/hour
- `GapAnalysisPanel.razor / .cs` - Identify understaffing and get recommendations
- `TeamManagementPanel.razor / .cs` - Manage team members and assignments

**Key Services:**
- `ISmartOpsDataService` / `SmartOpsDataService` - HTTP calls to WebApi
- `RunAsService` - "Run As" impersonation support
- `SmartOpsClaimsTransformation` - Claims transformation for roles

### Service Class Pattern
```csharp
public class ShiftDataService(HttpClient httpClient) : IShiftDataService
{
    public async Task<List<ScheduledShift>> GetScheduleAsync(string employeeId)
        => await httpClient.GetFromJsonAsync<List<ScheduledShift>>($"api/schedule/{employeeId}") ?? [];
}
```

---

## 8. Development Workflow & Best Practices

### When Adding Features
1. **Define Domain Model** → Add entity to `SmartManagement.Repo/Models/` and update `SmartOpsContext`
2. **Add SQL Script** → Add schema script to `SmartManagement.Repo/Sql/` if needed
3. **Add DTO** → Create DTO in `SmartOps.Models` if needed for API responses
4. **Add Business Logic** → Add partial class method to appropriate class in `SmartOpsManagement.Bus`
5. **Add API Endpoint** → Add to appropriate `*Endpoints.cs` file in `SmartOpsManagement.WebApi/Endpoints/`
6. **Update Service Interface** → Add method to `IShiftDataService` or `ISmartOpsDataService`
7. **Create UI Components** → Build Blazor components in appropriate project:
   - Employee features → `SmartShift.Blazor`
   - Supervisor/Admin features → `SmartOps.Blazor`
8. **Write Tests** → Unit tests for logic, integration tests for repositories (TBD)

### Adding a New Feature Example
```
Feature: "Track shift swaps"

1. Model Layer:
   - Add `ShiftSwapRequest` entity in SmartManagement.Repo/Models/
   - Add DbSet<ShiftSwapRequest> to SmartOpsContext

2. SQL Layer:
   - Add CreateShiftSwapRequest.sql to SmartManagement.Repo/Sql/

3. DTO Layer:
   - Add `ShiftSwapRequestDto` to SmartOps.Models

4. Business Logic Layer:
   - Add `ShiftSwapLogic.cs` partial class to SmartOpsManagement.Bus
   - Add `IShiftSwapService` interface to SmartOpsManagement.Bus/Services/

5. API Layer:
   - Add `ShiftSwapEndpoints.cs` to SmartOpsManagement.WebApi/Endpoints/

6. Blazor Service Layer:
   - Add method to IShiftDataService / ShiftDataService

7. UI Layer:
   - Create `ShiftSwapForm.razor / .cs` in SmartShift.Blazor

8. Dependency Injection:
   - Register new services in WebApi Program.cs
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
- **State Management:** Service-based state via injected data services
- **API Layer:** Minimal API Endpoints in SmartOpsManagement.WebApi (no MVC controllers)
- **Logging:** Built-in .NET logging; Serilog recommended for production
- **Configuration:** appsettings.json in each Blazor project; environment-specific configs for dev/prod
- **Authentication:** Azure AD with Microsoft Identity Web; claims transformation for role assignment
- **"Run As" / Impersonation:** Both Blazor apps support RunAsService for dev/supervisor impersonation

---

## 13. Getting Started With AI Assistance

When requesting help with this solution, provide:
1. **What you're building:** Feature name and purpose
2. **Where it belongs:** Which layer/project (UI, Business Logic, Data)
3. **Related entities:** What data models are involved
4. **Business rules:** Any validation or constraints
5. **Current code:** If modifying existing, share relevant snippets

### Example Request:
> "I need to add a feature for employees to request shift swaps. Create:
> - A ShiftSwapRequest entity in SmartManagement.Repo
> - A ShiftSwapLogic partial class in SmartOpsManagement.Bus
> - An endpoint in ShiftSwapEndpoints.cs
> - A method in IShiftDataService/ShiftDataService
> - A Blazor form component in SmartShift.Blazor"

> "I need to add a staffing forecast feature. Create:
> - Components in SmartOps.Blazor for supervisors
> - Integration with existing StaffingRequirement model
> - Visualization showing predicted vs. actual staffing"

---

## 14. Version History
- **Created:** 2025 (Initial Setup)
- **Last Updated:** 2026-03-25 - Full review; updated to reflect active project state

### Change Log
- **2025-XX-XX:** Initial solution structure with TimeKeeper.Blazor
- **2025-XX-XX:** Added SmartOps.Blazor project to separate supervisor/admin UI from employee time tracking
- **2025-XX-XX:** Renamed TimeManagement.Models → SmartOps.Models
- **2025-XX-XX:** Renamed TimeKeeper.Blazor → SmartShift.Blazor; renamed TimeTrackerClaims.cs → SmartShiftClaims.cs
- **2025-XX-XX:** Renamed TimeManagement.WebAPI → SmartOpsManagement.WebApi
- **2025-XX-XX:** Renamed TimeManagement.Bus → SmartOpsManagement.Bus
- **2025-XX-XX:** Renamed TimeManagement.Repo → SmartManagement.Repo
- **2025-XX-XX:** Renamed ProcessTimeKeeperActivity → ProcessSmartOpsActivity
- **2025-XX-XX:** Migrated WebApi from MVC Controllers to Minimal API Endpoints
- **2025-XX-XX:** Added employee availability and time-off request features
- **2025-XX-XX:** Added WorkGroup management (backend, API, UI)
- **2025-XX-XX:** Added "Run As" impersonation support in both Blazor apps
- **2025-XX-XX:** Added Etime.Bus for legacy Etime schedule import
- **2025-XX-XX:** Added SmartOps.Shared.UI shared Razor component library
- **2025-XX-XX:** Added PdfExtractTool developer utility
- **2026-03-25:** SOLUTION_PROMPT.md fully reviewed and updated to match current solution state
