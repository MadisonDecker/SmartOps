using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for managing staffing requirements by division, client, and time slot
    /// </summary>
    public interface IStaffingRequirementService
    {
        Task<List<StaffingRequirement>> GetRequirementsAsync(
            string division, 
            string? clientId = null, 
            int? dayOfWeek = null,
            int? hourOfDay = null);

        Task<List<StaffingRequirement>> GetRequirementsForWeekAsync(
            string division, 
            string? clientId = null);

        Task<StaffingRequirement> GetOrCreateRequirementAsync(
            string division, 
            string? clientId, 
            int dayOfWeek, 
            int hourOfDay);

        Task<StaffingRequirement> UpdateRequirementAsync(
            int id, 
            int requiredHeadCount, 
            int? expectedCallVolume = null,
            string? requiredSkills = null,
            string? modifiedBy = null);

        Task<bool> DeleteRequirementAsync(int id);

        Task<List<string>> GetDivisionsAsync();
        Task<List<string>> GetClientsForDivisionAsync(string division);
    }
}
