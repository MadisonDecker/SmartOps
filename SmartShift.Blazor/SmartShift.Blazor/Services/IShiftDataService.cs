using SmartOps.Models;

namespace SmartShift.Blazor.Services;

public interface IShiftDataService
{
    Task<EmployeeInfo> GetCurrentEmployeeAsync();
    Task<List<ScheduledShift>?> GetEmployeeShiftsAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<EmployeeSkill>> GetEmployeeSkillsAsync(string employeeId);
    Task<ScheduledShift?> GetNextShiftAsync(string employeeId);
    Task<double> GetWeeklyHoursAsync(string employeeId, DateTime weekStart);
}
