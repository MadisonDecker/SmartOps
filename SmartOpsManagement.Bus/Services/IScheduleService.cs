using SmartOps.Models;

namespace SmartOpsManagement.Bus.Services
{
    /// <summary>
    /// Service for managing employee schedules and shift assignments
    /// </summary>
    public interface IScheduleService
    {
        // Shift Operations
        Task<List<ScheduledShift>> GetEmployeeShiftsAsync(
            string employeeId, 
            DateTime fromDate, 
            DateTime toDate);

        Task<List<ScheduledShift>> GetDivisionShiftsAsync(
            string division, 
            DateTime fromDate, 
            DateTime toDate,
            string? clientId = null);

        Task<ScheduledShift> AssignShiftAsync(
            string employeeId, 
            string division, 
            string? clientId,
            DateTime startTime, 
            DateTime endTime,
            string? modifiedBy = null);

        Task<ScheduledShift> UpdateShiftAsync(
            int shiftId, 
            DateTime startTime, 
            DateTime endTime,
            string? notes = null,
            string? modifiedBy = null);

        Task<bool> CancelShiftAsync(int shiftId, string? modifiedBy = null);

        // Break Operations
        Task<List<ScheduledBreak>> GetRequiredBreaksAsync(ScheduledShift shift);

        Task<ScheduledBreak> ScheduleBreakAsync(
            int shiftId, 
            DateTime startTime, 
            DateTime endTime,
            string breakType, 
            bool isPaid,
            string? modifiedBy = null);

        Task<bool> UpdateBreakAsync(
            int breakId, 
            DateTime startTime, 
            DateTime endTime,
            string? modifiedBy = null);

        Task<bool> MarkBreakAsTakenAsync(int breakId);

        // Shift Validation
        Task<bool> ValidateShiftAssignmentAsync(
            string employeeId, 
            DateTime startTime, 
            DateTime endTime,
            string? requiredSkills = null);

        Task<List<string>> GetConflictingShiftsAsync(
            string employeeId, 
            DateTime startTime, 
            DateTime endTime);
    }
}
