using SmartManagement.Repo.Models;
using SmartOps.Models;

namespace SmartOpsManagement.Bus;

public partial class SmartOpsBusinessLogic
{
    public async Task<List<AlertContactMethodDto>> GetContactMethodsAsync()
    {
        return await Task.FromResult(
            _context.AlertContactMethods
                .OrderBy(m => m.ContactMethodId)
                .Select(m => new AlertContactMethodDto
                {
                    ContactMethodId = m.ContactMethodId,
                    MethodName      = m.MethodName
                })
                .ToList());
    }

    public async Task<EmployeeAvailabilityDto?> GetAvailabilityAsync(string adLoginName)
    {
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var entity = _context.EmployeeAvailabilities
            .Where(a => a.AdloginName == adLoginName
                     && a.EffectiveDate <= today
                     && (a.EndDate == null || a.EndDate > today))
            .OrderByDescending(a => a.EffectiveDate)
            .FirstOrDefault();

        if (entity == null) return null;

        // Load days separately (avoids EF lazy-load issues)
        var days = _context.EmployeeAvailabilityDays
            .Where(d => d.EmployeeAvailabilityId == entity.EmployeeAvailabilityId)
            .OrderBy(d => d.DayOfWeek)
            .ToList();

        return await Task.FromResult(MapAvailabilityToDto(entity, days));
    }

    public async Task<EmployeeAvailabilityDto> SaveAvailabilityAsync(string adLoginName, EmployeeAvailabilityDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now   = DateTime.UtcNow;

        var entity = _context.EmployeeAvailabilities
            .Where(a => a.AdloginName == adLoginName
                     && a.EffectiveDate <= today
                     && (a.EndDate == null || a.EndDate > today))
            .OrderByDescending(a => a.EffectiveDate)
            .FirstOrDefault();

        if (entity == null)
        {
            entity = new EmployeeAvailability
            {
                AdloginName     = adLoginName,
                EffectiveDate   = today,
                InsertedDateUtc = now,
                LastUpdatedUtc  = now
            };
            _context.EmployeeAvailabilities.Add(entity);
        }
        else
        {
            entity.LastUpdatedUtc = now;
        }

        entity.MinWeeklyHours               = dto.MinWeeklyHours;
        entity.MaxWeeklyHours               = dto.MaxWeeklyHours;
        entity.IsOpenToOvertime             = dto.IsOpenToOvertime;
        entity.IsOpenToVto                  = dto.IsOpenToVto;
        entity.PreferredAlertContactMethodId = dto.PreferredAlertContactMethodId;
        entity.Notes                        = dto.Notes;

        // Persist so we have a valid PK before syncing day rows
        await _context.SaveChangesAsync();

        // Sync day rows: remove days not in the incoming set, add/update the rest
        var existingDays   = _context.EmployeeAvailabilityDays
            .Where(d => d.EmployeeAvailabilityId == entity.EmployeeAvailabilityId)
            .ToList();

        var incomingByDow  = dto.Days.ToDictionary(d => d.DayOfWeek);

        var toRemove = existingDays.Where(d => !incomingByDow.ContainsKey(d.DayOfWeek)).ToList();
        _context.EmployeeAvailabilityDays.RemoveRange(toRemove);

        foreach (var dayDto in dto.Days)
        {
            var dayEntity = existingDays.FirstOrDefault(d => d.DayOfWeek == dayDto.DayOfWeek);
            if (dayEntity == null)
            {
                dayEntity = new EmployeeAvailabilityDay
                {
                    EmployeeAvailabilityId = entity.EmployeeAvailabilityId,
                    DayOfWeek              = dayDto.DayOfWeek,
                    InsertedDateUtc        = now,
                    LastUpdatedUtc         = now
                };
                _context.EmployeeAvailabilityDays.Add(dayEntity);
            }
            else
            {
                dayEntity.LastUpdatedUtc = now;
            }

            dayEntity.EarliestStart = dayDto.EarliestStart;
            dayEntity.LatestStop    = dayDto.LatestStop;
        }

        await _context.SaveChangesAsync();

        var savedDays = _context.EmployeeAvailabilityDays
            .Where(d => d.EmployeeAvailabilityId == entity.EmployeeAvailabilityId)
            .OrderBy(d => d.DayOfWeek)
            .ToList();

        return MapAvailabilityToDto(entity, savedDays);
    }

    private static EmployeeAvailabilityDto MapAvailabilityToDto(
        EmployeeAvailability entity,
        List<EmployeeAvailabilityDay> days) => new()
    {
        EmployeeAvailabilityId       = entity.EmployeeAvailabilityId,
        AdloginName                  = entity.AdloginName,
        MinWeeklyHours               = entity.MinWeeklyHours,
        MaxWeeklyHours               = entity.MaxWeeklyHours,
        IsOpenToOvertime             = entity.IsOpenToOvertime,
        IsOpenToVto                  = entity.IsOpenToVto,
        PreferredAlertContactMethodId = entity.PreferredAlertContactMethodId,
        Notes                        = entity.Notes,
        Days                         = days.Select(d => new EmployeeAvailabilityDayDto
        {
            DayOfWeek     = d.DayOfWeek,
            EarliestStart = d.EarliestStart,
            LatestStop    = d.LatestStop
        }).ToList()
    };
}
