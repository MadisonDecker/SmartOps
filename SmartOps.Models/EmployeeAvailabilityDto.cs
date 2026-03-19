namespace SmartOps.Models;

public class EmployeeAvailabilityDto
{
    public int? EmployeeAvailabilityId { get; set; }
    public string AdloginName { get; set; } = string.Empty;
    public decimal MinWeeklyHours { get; set; }
    public decimal MaxWeeklyHours { get; set; }
    public bool IsOpenToOvertime { get; set; }
    public bool IsOpenToVto { get; set; }
    public byte PreferredAlertContactMethodId { get; set; } = 1;
    public string? Notes { get; set; }
    public List<EmployeeAvailabilityDayDto> Days { get; set; } = [];
}

public class EmployeeAvailabilityDayDto
{
    /// <summary>0 = Sunday … 6 = Saturday (matches .NET DayOfWeek)</summary>
    public byte DayOfWeek { get; set; }
    public TimeOnly EarliestStart { get; set; }
    public TimeOnly LatestStop { get; set; }
}

public class AlertContactMethodDto
{
    public byte ContactMethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
}
