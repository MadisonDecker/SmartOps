using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for staffing gap analysis and recommendations
    /// </summary>
    public interface IGapAnalysisService
    {
        /// <summary>
        /// Calculate if there's a staffing gap for a specific time slot
        /// </summary>
        Task<int> CalculateStaffingGapAsync(
            string division, 
            string? clientId,
            DateTime dateTime);

        /// <summary>
        /// Get all hours in a day with gaps
        /// </summary>
        Task<List<(int hour, int gap)>> GetGapsForDayAsync(
            string division, 
            string? clientId,
            DateTime date);

        /// <summary>
        /// Get all gaps for a week
        /// </summary>
        Task<Dictionary<DateTime, List<(int hour, int gap)>>> GetGapsForWeekAsync(
            string division, 
            string? clientId,
            DateTime weekStart);

        /// <summary>
        /// Get staffing efficiency percentage
        /// </summary>
        Task<decimal> GetStaffingEfficiencyAsync(
            string division, 
            DateTime fromDate, 
            DateTime toDate);

        /// <summary>
        /// Find qualified employees available for a time slot
        /// </summary>
        Task<List<string>> FindAvailableEmployeesAsync(
            string division, 
            string? clientId,
            DateTime startTime, 
            DateTime endTime,
            List<string>? requiredSkills = null);

        /// <summary>
        /// Get coverage for a specific time slot
        /// </summary>
        Task<(int required, int scheduled, int gap)> GetCoverageAsync(
            string division, 
            string? clientId,
            DateTime dateTime);
    }
}
