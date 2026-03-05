using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for managing employee skills and qualifications
    /// </summary>
    public interface IEmployeeSkillService
    {
        Task<List<EmployeeSkill>> GetEmployeeSkillsAsync(string employeeId);

        Task<bool> HasSkillAsync(string employeeId, string skillCode);

        Task<bool> HasAllSkillsAsync(string employeeId, List<string> skillCodes);

        Task<List<string>> GetEmployeesWithSkillAsync(string skillCode);

        Task<EmployeeSkill> AddSkillAsync(
            string employeeId, 
            string skillCode, 
            string skillName,
            string? proficiencyLevel = null,
            DateTime? expirationDate = null);

        Task<bool> UpdateSkillAsync(
            int skillId, 
            string? proficiencyLevel = null,
            DateTime? expirationDate = null,
            bool? isActive = null);

        Task<bool> RemoveSkillAsync(int skillId);

        Task<List<EmployeeSkill>> GetExpiringSkillsAsync(int daysUntilExpiration = 30);

        Task<List<string>> GetAvailableSkillCodesAsync();
    }
}
