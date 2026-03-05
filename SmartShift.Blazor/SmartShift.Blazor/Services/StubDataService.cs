using SmartOps.Models;

namespace SmartShift.Blazor.Services;

/// <summary>
/// Provides stub/sample data for UI development and testing.
/// This service generates realistic sample data for employees, shifts, and schedules.
/// </summary>
public interface IStubDataService
{
    Task<EmployeeInfo> GetCurrentEmployeeAsync();
    Task<List<ScheduledShift>> GetEmployeeShiftsAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<EmployeeSkill>> GetEmployeeSkillsAsync(string employeeId);
    Task<ScheduledShift?> GetNextShiftAsync(string employeeId);
    Task<double> GetWeeklyHoursAsync(string employeeId, DateTime weekStart);
}

public class StubDataService : IStubDataService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StubDataService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<EmployeeInfo> GetCurrentEmployeeAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? "stub-user-001";
        var displayName = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "John Doe";

        var employee = new EmployeeInfo
        {
            Id = 1,
            EmployeeId = userId,
            Division = "Customer Service",
            SupervisorId = "supervisor-001",
            SupervisorName = "Jane Smith",
            IsActive = true,
            StartDate = new DateTime(2022, 1, 15),
            PhoneNumber = "(555) 123-4567"
        };

        return Task.FromResult(employee);
    }

    public Task<List<ScheduledShift>> GetEmployeeShiftsAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Today.AddDays(-7);
        endDate ??= DateTime.Today.AddDays(30);

        var shifts = new List<ScheduledShift>
        {
            // Today's shift
            new ScheduledShift
            {
                Id = 1,
                EmployeeId = employeeId,
                Division = "Customer Service",
                ClientName = "MainClient",
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(17),
                Status = ShiftStatus.Scheduled,
                Breaks = new List<ScheduledBreak>
                {
                    new ScheduledBreak
                    {
                        Id = 1,
                        ScheduledShiftId = 1,
                        StartTime = DateTime.Today.AddHours(12),
                        EndTime = DateTime.Today.AddHours(13),
                        BreakType = "Lunch",
                        IsPaid = false,
                        IsTaken = false
                    },
                    new ScheduledBreak
                    {
                        Id = 2,
                        ScheduledShiftId = 1,
                        StartTime = DateTime.Today.AddHours(15),
                        EndTime = DateTime.Today.AddHours(15).AddMinutes(15),
                        BreakType = "Break",
                        IsPaid = true,
                        IsTaken = false
                    }
                },
                Notes = "Standard shift"
            },
            // Tomorrow's shift
            new ScheduledShift
            {
                Id = 2,
                EmployeeId = employeeId,
                Division = "Customer Service",
                ClientName = "MainClient",
                StartTime = DateTime.Today.AddDays(1).AddHours(9),
                EndTime = DateTime.Today.AddDays(1).AddHours(18),
                Status = ShiftStatus.Scheduled,
                Breaks = new List<ScheduledBreak>
                {
                    new ScheduledBreak
                    {
                        Id = 3,
                        ScheduledShiftId = 2,
                        StartTime = DateTime.Today.AddDays(1).AddHours(12),
                        EndTime = DateTime.Today.AddDays(1).AddHours(13),
                        BreakType = "Lunch",
                        IsPaid = false,
                        IsTaken = false
                    }
                }
            },
            // Shift in 2 days
            new ScheduledShift
            {
                Id = 3,
                EmployeeId = employeeId,
                Division = "Customer Service",
                ClientName = "SecondaryClient",
                StartTime = DateTime.Today.AddDays(2).AddHours(10),
                EndTime = DateTime.Today.AddDays(2).AddHours(19),
                Status = ShiftStatus.Scheduled,
                Breaks = new List<ScheduledBreak>
                {
                    new ScheduledBreak
                    {
                        Id = 4,
                        ScheduledShiftId = 3,
                        StartTime = DateTime.Today.AddDays(2).AddHours(13),
                        EndTime = DateTime.Today.AddDays(2).AddHours(14),
                        BreakType = "Lunch",
                        IsPaid = false,
                        IsTaken = false
                    },
                    new ScheduledBreak
                    {
                        Id = 5,
                        ScheduledShiftId = 3,
                        StartTime = DateTime.Today.AddDays(2).AddHours(16),
                        EndTime = DateTime.Today.AddDays(2).AddHours(16).AddMinutes(15),
                        BreakType = "Break",
                        IsPaid = true,
                        IsTaken = false
                    }
                }
            },
            // Shift in 4 days
            new ScheduledShift
            {
                Id = 4,
                EmployeeId = employeeId,
                Division = "Quality Assurance",
                ClientName = "MainClient",
                StartTime = DateTime.Today.AddDays(4).AddHours(8),
                EndTime = DateTime.Today.AddDays(4).AddHours(16),
                Status = ShiftStatus.Scheduled,
                Breaks = new List<ScheduledBreak>
                {
                    new ScheduledBreak
                    {
                        Id = 6,
                        ScheduledShiftId = 4,
                        StartTime = DateTime.Today.AddDays(4).AddHours(12),
                        EndTime = DateTime.Today.AddDays(4).AddHours(13),
                        BreakType = "Lunch",
                        IsPaid = false,
                        IsTaken = false
                    }
                }
            },
            // Shift next week
            new ScheduledShift
            {
                Id = 5,
                EmployeeId = employeeId,
                Division = "Customer Service",
                ClientName = "MainClient",
                StartTime = DateTime.Today.AddDays(7).AddHours(8),
                EndTime = DateTime.Today.AddDays(7).AddHours(17),
                Status = ShiftStatus.Scheduled,
                Breaks = new List<ScheduledBreak>
                {
                    new ScheduledBreak
                    {
                        Id = 7,
                        ScheduledShiftId = 5,
                        StartTime = DateTime.Today.AddDays(7).AddHours(12),
                        EndTime = DateTime.Today.AddDays(7).AddHours(13),
                        BreakType = "Lunch",
                        IsPaid = false,
                        IsTaken = false
                    }
                }
            }
        };

        return Task.FromResult(shifts.Where(s => s.StartTime >= startDate && s.StartTime <= endDate).ToList());
    }

    public Task<List<EmployeeSkill>> GetEmployeeSkillsAsync(string employeeId)
    {
        var skills = new List<EmployeeSkill>
        {
            new EmployeeSkill
            {
                Id = 1,
                EmployeeId = employeeId,
                SkillCode = "BILINGUAL_ES",
                SkillName = "Spanish - Bilingual",
                ProficiencyLevel = "Advanced",
                EffectiveDate = new DateTime(2021, 6, 1),
                ExpirationDate = null,
                IsActive = true,
                Notes = "Fluent in Spanish, certified by HR"
            },
            new EmployeeSkill
            {
                Id = 2,
                EmployeeId = employeeId,
                SkillCode = "TIER2",
                SkillName = "Tier 2 Support",
                ProficiencyLevel = "Intermediate",
                EffectiveDate = new DateTime(2022, 3, 15),
                ExpirationDate = new DateTime(2025, 3, 15),
                IsActive = true,
                Notes = "Can handle escalated issues"
            },
            new EmployeeSkill
            {
                Id = 3,
                EmployeeId = employeeId,
                SkillCode = "QUALITY",
                SkillName = "Quality Assurance",
                ProficiencyLevel = "Advanced",
                EffectiveDate = new DateTime(2023, 1, 1),
                ExpirationDate = null,
                IsActive = true,
                Notes = "Monthly QA trainer"
            }
        };

        return Task.FromResult(skills);
    }

    public async Task<ScheduledShift?> GetNextShiftAsync(string employeeId)
    {
        var shifts = await GetEmployeeShiftsAsync(employeeId);
        return shifts.FirstOrDefault(s => s.StartTime > DateTime.Now && s.Status == ShiftStatus.Scheduled);
    }

    public async Task<double> GetWeeklyHoursAsync(string employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var shifts = await GetEmployeeShiftsAsync(employeeId, weekStart, weekEnd);
        
        var totalHours = shifts.Sum(s => (s.EndTime - s.StartTime).TotalHours);
        return totalHours;
    }
}

