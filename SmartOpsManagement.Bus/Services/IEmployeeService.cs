using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for retrieving and managing employee information
    /// </summary>
    public interface IEmployeeService
    {
        Task<EmployeeInfo?> GetEmployeeInfoAsync(string employeeId);

        Task<EmployeeInfo> GetOrCreateEmployeeInfoAsync(
            string employeeId, 
            string division,
            string? supervisorId = null);

        Task<EmployeeInfo> UpdateEmployeeInfoAsync(
            string employeeId, 
            string? division = null,
            string? supervisorId = null,
            bool? isActive = null,
            decimal? maxHoursPerWeek = null,
            string? notes = null);

        Task<List<EmployeeInfo>> GetEmployeesByDivisionAsync(string division);

        Task<List<EmployeeInfo>> GetEmployeesBySupervisorAsync(string supervisorId);

        Task<decimal> GetEmployeeHoursAsync(
            string employeeId, 
            DateTime fromDate, 
            DateTime toDate);

        Task<bool> ValidateMaxHoursAsync(
            string employeeId, 
            DateTime startTime, 
            DateTime endTime);
    }
}
