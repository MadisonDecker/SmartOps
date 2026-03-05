using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for managing break templates and enforcement
    /// </summary>
    public interface IBreakManagementService
    {
        Task<BreakTemplate?> GetBreakTemplateAsync(string division);

        Task<BreakTemplate> CreateOrUpdateBreakTemplateAsync(
            string division, 
            int minShiftDurationMinutes,
            List<(string breakType, int minutesInto, int duration, bool isPaid)> breakRules,
            string? modifiedBy = null);

        Task<List<BreakRule>> GetBreakRulesAsync(string division);

        Task<List<ScheduledBreak>> CalculateMandatoryBreaksAsync(
            ScheduledShift shift, 
            BreakTemplate template);

        Task<bool> ValidateBreakComplianceAsync(ScheduledShift shift);

        Task<List<string>> GetBreakComplianceReportAsync(
            string division, 
            DateTime fromDate, 
            DateTime toDate);
    }
}
