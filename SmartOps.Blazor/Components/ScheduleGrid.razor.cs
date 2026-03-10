using Microsoft.AspNetCore.Components;
using SmartOps.Models;

namespace SmartOps.Blazor.Components;

public partial class ScheduleGrid
{
    [Parameter]
    public List<Workgroup> Workgroups { get; set; } = [];

    [Parameter]
    public List<Workgroup> SelectedWorkgroups { get; set; } = [];

    [Parameter]
    public Client? SelectedClient { get; set; }

    [Parameter]
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Interval in minutes for FTE calculation. Default is 60 minutes.
    /// Supported values: 15, 30, 60
    /// </summary>
    [Parameter]
    public int IntervalMinutes { get; set; } = 60;

    private readonly Random _random = new();

    /// <summary>
    /// Generates time slots based on the selected interval from 7:00 to 23:59
    /// </summary>
    private IEnumerable<TimeSpan> GetTimeSlots()
    {
        for (int minutes = 7 * 60; minutes < 24 * 60; minutes += IntervalMinutes)
        {
            yield return TimeSpan.FromMinutes(minutes);
        }
    }

    /// <summary>
    /// Gets the required staffing for a given date/time slot.
    /// FTE represents the fraction of a full-time person needed for the interval.
    /// 1.0 FTE = one person working the full interval.
    /// </summary>
    private decimal GetRequiredStaffing(DateTime dateTime)
    {
        // DEVNOTE: Development stub data - generates staffing requirements based on hour with randomness
        // Production: Call IStaffingRequirementService to get required staffing for SelectedWorkgroups
        // Staffing Curve: 7-8am: 0.5 → 9am: 2.5 → 2pm: 15.0 (peak) → 8pm: 2.5 → 9pm+: 0.5

        int hour = dateTime.Hour;
        decimal baseFTE = 0m;
        decimal variance = 0m;

        // Scale factor based on interval (60 min = 1.0, 30 min = 0.5, 15 min = 0.25)
        decimal intervalScale = IntervalMinutes / 60m;

        if (hour < 7 || hour >= 21)
        {
            baseFTE = 0.5m;
            variance = (decimal)(_random.NextDouble() * 0.2 - 0.1); // ±0.1
        }
        else if (hour >= 7 && hour <= 8)
        {
            baseFTE = 0.5m;
            variance = (decimal)(_random.NextDouble() * 0.3 - 0.1); // -0.1 to +0.2
        }
        else if (hour >= 9 && hour <= 14)
        {
            // Linear ramp from 2.5 (9am) to 15.0 (2pm)
            baseFTE = 2.5m + (hour - 9) * 2.5m;
            variance = (decimal)(_random.NextDouble() * 1.5 - 0.75); // ±0.75
        }
        else if (hour >= 15 && hour <= 20)
        {
            // Linear ramp down from 15.0 (2pm) to 2.5 (8pm)
            baseFTE = 15.0m - ((hour - 14) * 12.5m / 6m);
            variance = (decimal)(_random.NextDouble() * 1.5 - 0.75); // ±0.75
        }

        return Math.Max(0m, (baseFTE + variance) * intervalScale);
    }

    /// <summary>
    /// Gets the assigned FTE (Full Time Equivalent) for a given date/time slot.
    /// FTE represents the fraction of a full-time person assigned for the interval.
    /// </summary>
    private decimal GetAssignedStaffing(DateTime dateTime)
    {
        // DEVNOTE: Development stub data - generates assigned FTE based on required with variance
        // Production: Call IScheduleService to get assigned FTE count for SelectedWorkgroups

        int hour = dateTime.Hour;
        decimal baseFTE = 0m;
        decimal variance = 0m;

        // Scale factor based on interval (60 min = 1.0, 30 min = 0.5, 15 min = 0.25)
        decimal intervalScale = IntervalMinutes / 60m;

        if (hour < 7 || hour >= 21)
        {
            baseFTE = 0.5m;
            variance = (decimal)(_random.NextDouble() * 0.2 - 0.1); // ±0.1
        }
        else if (hour >= 7 && hour <= 8)
        {
            baseFTE = 0.5m;
            variance = (decimal)(_random.NextDouble() * 0.3 - 0.1); // -0.1 to +0.2
        }
        else if (hour >= 9 && hour <= 14)
        {
            // Linear ramp from 2.5 (9am) to 15.0 (2pm)
            baseFTE = 2.5m + (hour - 9) * 2.5m;
            variance = (decimal)(_random.NextDouble() * 1.5 - 0.75); // ±0.75
        }
        else if (hour >= 15 && hour <= 20)
        {
            // Linear ramp down from 15.0 (2pm) to 2.5 (8pm)
            baseFTE = 15.0m - ((hour - 14) * 12.5m / 6m);
            variance = (decimal)(_random.NextDouble() * 1.5 - 0.75); // ±0.75
        }

        return Math.Max(0m, (baseFTE + variance) * intervalScale);
    }
}
