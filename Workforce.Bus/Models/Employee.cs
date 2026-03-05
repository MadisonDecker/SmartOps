namespace Workforce.Bus.Models;

public class Employee
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? WtaInternalId { get; set; }
    public string ExternalMatchId { get; set; } = string.Empty;
    public string DisplayId { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateOnly? OriginalHireDate { get; set; }
    public PersonName? Name { get; set; }
    public List<EffectiveDatedEmployeeInfo> EffectiveDatedInfo { get; set; } = new();
    public List<EmployeeJob> Jobs { get; set; } = new();
}

public class PersonName
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleNames { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? LegalName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class EffectiveDatedEmployeeInfo
{
    public DateOnly EffectiveDate { get; set; }
    public DateOnly EndEffectiveDate { get; set; }
    public DateOnly? LatestHireDate { get; set; }
    public DateOnly? AccrualDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? Gender { get; set; }
    public string? ManagedJobsMatchValue { get; set; }
    public string? Country { get; set; }
    public string? StateOrProvince { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}

public class EmployeeJob
{
    public string Id { get; set; } = string.Empty;
    public string? WtaInternalId { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly? OriginalStartDate { get; set; }
    public List<EffectiveDatedJobInfo> EffectiveDatedJobInfo { get; set; } = new();
    public string? EmployeeJob_ { get; set; }
}

public class EffectiveDatedJobInfo
{
    public DateOnly EffectiveDate { get; set; }
    public DateOnly EndEffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public bool? Primary { get; set; }
    public DateOnly? AccrualStartDate { get; set; }
    public DateOnly? LatestStartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsFullTime { get; set; }
    public string? StandardWeeklyHours { get; set; }
    public string? StandardDailyHours { get; set; }
    public string? FtePercent { get; set; }
    public string? PayrollId { get; set; }
    public string? PayrollSystemId { get; set; }
    public string? PayType { get; set; }
    public string? EmploymentType { get; set; }
    public string? PolicyProfile { get; set; }
    public string? Country { get; set; }
    public string? StateOrProvince { get; set; }
    public string? Timezone { get; set; }
    public string? ManagedJobsMatchValue { get; set; }
    public string? CostCenter { get; set; }
    public BusinessAddress? BusinessAddress { get; set; }
}

public class BusinessAddress
{
    public string? City { get; set; }
    public string? LineOne { get; set; }
    public string? LineTwo { get; set; }
    public string? LineThree { get; set; }
    public string? PostalCode { get; set; }
}
