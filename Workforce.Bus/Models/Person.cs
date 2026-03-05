namespace Workforce.Bus.Models;

public class Person
{
    public string ExternalMatchId { get; set; } = string.Empty;
    public PersonName? Name { get; set; }
    public string? EmailAddress { get; set; }
    public DateOnly? BirthDate { get; set; }
    public EmployeeInfo? Employee { get; set; }
    public UserInfo? User { get; set; }
}

public class EmployeeInfo
{
    public string DisplayId { get; set; } = string.Empty;
    public string? OriginalHireDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BusinessPhoneNumber { get; set; }
    public string? MobilePhoneNumber { get; set; }
    public bool? IsFullEmployeeHistory { get; set; }
    public string? ClockPin { get; set; }
    public List<EffectiveDatedEmployeeInfoV2>? EffectiveDatedInfo { get; set; }
    public List<EmployeeJobV2>? Jobs { get; set; }
}

public class EffectiveDatedEmployeeInfoV2
{
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? LatestHireDate { get; set; }
    public DateOnly? AccrualDate { get; set; }
    public DateOnly? SeniorityDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? LeaveCode { get; set; }
    public DateOnly? LeaveStartDate { get; set; }
    public string? HrStatus { get; set; }
    public string? Gender { get; set; }
    public string? GovernmentId { get; set; }
    public string? ImportedBadgeId { get; set; }
    public bool? ManagerIndicator { get; set; }
    public string? ManagedJobsMatchValue { get; set; }
    public string? Country { get; set; }
    public string? StateOrProvince { get; set; }
    public Address? HomeAddress { get; set; }
    public string? ShiftCode { get; set; }
    public DateOnly? ExpectedTerminationDate { get; set; }
    public DateOnly? LeaveOfAbsenceReturnDate { get; set; }
    public DateOnly? CreditedServiceDate { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}

public class Address
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? LineOne { get; set; }
    public string? LineTwo { get; set; }
    public string? LineThree { get; set; }
    public string? PostalCode { get; set; }
    public string? StateOrProvince { get; set; }
}

public class EmployeeJobV2
{
    public string ExternalJobId { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public DateOnly? OriginalStartDate { get; set; }
    public bool? IsFullJobHistory { get; set; }
    public List<EffectiveDatedJobInfoV2> EffectiveDatedJobInfo { get; set; } = new();
}

public class EffectiveDatedJobInfoV2
{
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public bool? Primary { get; set; }
    public DateOnly? AccrualStartDate { get; set; }
    public DateOnly? LatestStartDate { get; set; }
    public DateOnly? SeniorityDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? HrStatus { get; set; }
    public bool? IsFullTime { get; set; }
    public string? StandardDailyHours { get; set; }
    public string? StandardWeeklyHours { get; set; }
    public string? StandardPeriodHours { get; set; }
    public string? FtePercent { get; set; }
    public string? PayrollId { get; set; }
    public string? PayrollSystemId { get; set; }
    public string? PayType { get; set; }
    public string? EmploymentType { get; set; }
    public string? PolicyProfile { get; set; }
    public string? JobLevel { get; set; }
    public string? JobCode { get; set; }
    public string? JobTitle { get; set; }
    public string? PositionId { get; set; }
    public string? JobFunctionCode { get; set; }
    public string? PayFrequencyName { get; set; }
    public string? RateType { get; set; }
    public string? JobFamilyCode { get; set; }
    public string? PayFrequencyCode { get; set; }
    public string? ActionReasonCode { get; set; }
    public string? WorkShiftCode { get; set; }
    public string? BargainingUnitCode { get; set; }
    public string? CostNumberCode { get; set; }
    public string? JobClassCode { get; set; }
    public string? IndustryClassificationCode { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? District { get; set; }
    public string? Division { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? StateOrProvince { get; set; }
    public string? FlsaStatus { get; set; }
    public string? LocationId { get; set; }
    public string? ManagedJobsMatchValue { get; set; }
    public string? CostCenter { get; set; }
    public string? ImportedBadgeId { get; set; }
    public string? PayCurrency { get; set; }
    public string? PayGrade { get; set; }
    public string? ScheduleTemplateId { get; set; }
    public string? ShiftCode { get; set; }
    public int? ShiftIndicator { get; set; }
    public string? TimeOffApproverMatchId { get; set; }
    public string? TimeSheetApproverMatchId { get; set; }
    public string? UnionCode { get; set; }
    public Address? BusinessAddress { get; set; }
    public List<PayRate>? PayRates { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}

public class PayRate
{
    public string? Rate { get; set; }
    public string? Frequency { get; set; }
    public string? PayGroup { get; set; }
}

public class UserInfo
{
    public string LoginId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? SsoMatchId { get; set; }
    public string? Timezone { get; set; }
    public string? BusinessObjectsId { get; set; }
    public string? LocalePolicy { get; set; }
    public bool? AllowUnlimitedNonBiometricLogins { get; set; }
    public List<string>? ManagedSchedulingUnits { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}
