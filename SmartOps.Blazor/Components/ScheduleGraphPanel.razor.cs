using Microsoft.AspNetCore.Components;
using SmartOps.Models;

namespace SmartOps.Blazor.Components;

public partial class ScheduleGraphPanel
{
    [Parameter]
    public List<Workgroup> SelectedWorkgroups { get; set; } = [];

    [Parameter]
    public Client? SelectedClient { get; set; }

    [Parameter]
    public DateTime WeekStart { get; set; }

    private string viewMode = "daily";
    private Random _random = new(42); // Fixed seed for consistent stub data

    // SVG dimensions
    private const int svgWidth = 700;
    private const int svgHeight = 220;
    private const int chartPadding = 50;
    private const int chartHeight = 170;

    private record DataPoint(double X, double Y, decimal Value);
    private record AxisLabel(double X, string Text, string? SubText = null);

    /// <summary>
    /// Gets the interval in minutes based on current view mode.
    /// </summary>
    private int GetIntervalMinutes() => viewMode switch
    {
        "daily" => 60,      // Daily aggregates hourly data
        "hourly" => 60,     // 60 minute intervals
        "halfhour" => 30,   // 30 minute intervals
        "quarter" => 15,    // 15 minute intervals
        _ => 60
    };

    /// <summary>
    /// Gets description of current interval for the legend.
    /// </summary>
    private string GetIntervalDescription() => viewMode switch
    {
        "daily" => "daily totals",
        "hourly" => "60 min intervals",
        "halfhour" => "30 min intervals",
        "quarter" => "15 min intervals",
        _ => "60 min intervals"
    };

    /// <summary>
    /// Gets the data point radius based on view mode (smaller for more data points).
    /// </summary>
    private int GetDataPointRadius() => viewMode switch
    {
        "daily" => 5,
        "hourly" => 5,
        "halfhour" => 4,
        "quarter" => 3,
        _ => 5
    };

    /// <summary>
    /// Gets the label font size based on view mode (smaller for more labels).
    /// </summary>
    private int GetLabelFontSize() => viewMode switch
    {
        "daily" => 11,
        "hourly" => 11,
        "halfhour" => 9,
        "quarter" => 8,
        _ => 11
    };

    private void SetViewMode(string mode)
    {
        viewMode = mode;
        _random = new Random(42); // Reset for consistent data
        StateHasChanged();
    }

    /// <summary>
    /// Gets the number of data points based on view mode.
    /// Daily: 7 days
    /// Hourly: 15 hours (7am-9pm)
    /// Half Hour: 30 slots (7am-9pm, every 30 min)
    /// Quarter: 60 slots (7am-9pm, every 15 min)
    /// </summary>
    private int GetDataPointCount() => viewMode switch
    {
        "daily" => 7,
        "hourly" => 15,
        "halfhour" => 30,
        "quarter" => 60,
        _ => 7
    };

    private decimal GetMaxValue()
    {
        decimal max = 0.1m;
        for (int i = 0; i < GetDataPointCount(); i++)
        {
            max = Math.Max(max, Math.Max(GetRequiredValue(i), GetAssignedValue(i)));
        }
        return max * 1.1m; // Add 10% padding at top
    }

    private decimal GetRequiredValue(int index)
    {
        return viewMode switch
        {
            "daily" => GetDailyRequiredFTE(WeekStart.AddDays(index)),
            "hourly" => GetTimeSlotRequiredFTE(7 * 60 + index * 60),      // Start at 7:00, 60 min intervals
            "halfhour" => GetTimeSlotRequiredFTE(7 * 60 + index * 30),    // Start at 7:00, 30 min intervals
            "quarter" => GetTimeSlotRequiredFTE(7 * 60 + index * 15),     // Start at 7:00, 15 min intervals
            _ => 0m
        };
    }

    private decimal GetAssignedValue(int index)
    {
        return viewMode switch
        {
            "daily" => GetDailyAssignedFTE(WeekStart.AddDays(index)),
            "hourly" => GetTimeSlotAssignedFTE(7 * 60 + index * 60),      // Start at 7:00, 60 min intervals
            "halfhour" => GetTimeSlotAssignedFTE(7 * 60 + index * 30),    // Start at 7:00, 30 min intervals
            "quarter" => GetTimeSlotAssignedFTE(7 * 60 + index * 15),     // Start at 7:00, 15 min intervals
            _ => 0m
        };
    }

    private string GetLinePath(bool isRequired)
    {
        var points = GetDataPoints(isRequired);
        return string.Join(" ", points.Select(p => $"{p.X:F1},{p.Y:F1}"));
    }

    private string GetAreaPath(bool isRequired)
    {
        var points = GetDataPoints(isRequired).ToList();
        if (points.Count == 0) return "";

        var path = $"M {points[0].X:F1},{chartPadding + chartHeight} ";
        path += string.Join(" ", points.Select(p => $"L {p.X:F1},{p.Y:F1}"));
        path += $" L {points[^1].X:F1},{chartPadding + chartHeight} Z";
        return path;
    }

    private IEnumerable<DataPoint> GetDataPoints(bool isRequired)
    {
        int count = GetDataPointCount();
        double chartWidth = svgWidth - chartPadding - 40;
        double spacing = chartWidth / Math.Max(1, count - 1);
        decimal maxValue = GetMaxValue();

        for (int i = 0; i < count; i++)
        {
            decimal value = isRequired ? GetRequiredValue(i) : GetAssignedValue(i);
            double x = chartPadding + (i * spacing);
            double y = chartPadding + chartHeight - (double)(value * chartHeight / maxValue);
            yield return new DataPoint(x, y, value);
        }
    }

    private IEnumerable<AxisLabel> GetXAxisLabels()
    {
        int count = GetDataPointCount();
        double chartWidth = svgWidth - chartPadding - 40;
        double spacing = chartWidth / Math.Max(1, count - 1);

        // For dense views, only show every Nth label to avoid overlap
        int labelInterval = viewMode switch
        {
            "daily" => 1,
            "hourly" => 1,
            "halfhour" => 2,    // Show every hour (every 2nd 30-min slot)
            "quarter" => 4,     // Show every hour (every 4th 15-min slot)
            _ => 1
        };

        for (int i = 0; i < count; i++)
        {
            if (i % labelInterval != 0) continue;

            double x = chartPadding + (i * spacing);

            if (viewMode == "daily")
            {
                var date = WeekStart.AddDays(i);
                yield return new AxisLabel(x, date.ToString("ddd"), date.ToString("MMM dd"));
            }
            else
            {
                // Calculate time from minutes since midnight
                int minutesSinceMidnight = viewMode switch
                {
                    "hourly" => 7 * 60 + i * 60,
                    "halfhour" => 7 * 60 + i * 30,
                    "quarter" => 7 * 60 + i * 15,
                    _ => 7 * 60 + i * 60
                };

                int hour = minutesSinceMidnight / 60;
                int minute = minutesSinceMidnight % 60;
                yield return new AxisLabel(x, $"{hour}:{minute:D2}");
            }
        }
    }

    /// <summary>
    /// Gets daily required FTE (Full Time Equivalent) for a given date.
    /// </summary>
    private decimal GetDailyRequiredFTE(DateTime date)
    {
        // DEVNOTE: Development stub data - generates daily FTE totals
        // Production: Call IStaffingRequirementService to get daily required FTE
        int dayOfWeek = (int)date.DayOfWeek;

        decimal baseFTE = dayOfWeek switch
        {
            0 => 6.0m,   // Sunday - lower
            6 => 7.5m,   // Saturday - moderate
            _ => 12.0m   // Weekdays - higher
        };

        decimal variance = (decimal)(_random.NextDouble() * 2.0 - 1.0); // ±1.0
        return baseFTE + variance;
    }

    /// <summary>
    /// Gets daily assigned FTE (Full Time Equivalent) for a given date.
    /// </summary>
    private decimal GetDailyAssignedFTE(DateTime date)
    {
        // DEVNOTE: Development stub data
        // Production: Call IScheduleService to get assigned FTE count
        var required = GetDailyRequiredFTE(date);
        return required * (0.75m + (decimal)_random.NextDouble() * 0.35m);
    }

    /// <summary>
    /// Gets required FTE for a specific time slot (minutes since midnight).
    /// Scales based on current interval setting.
    /// </summary>
    private decimal GetTimeSlotRequiredFTE(int minutesSinceMidnight)
    {
        // DEVNOTE: Development stub data - generates FTE based on time of day
        // Production: Call IStaffingRequirementService

        int hour = minutesSinceMidnight / 60;
        int intervalMinutes = GetIntervalMinutes();

        // Scale factor: FTE per interval (60 min = 1.0, 30 min = 0.5, 15 min = 0.25)
        decimal intervalScale = intervalMinutes / 60m;

        // Base FTE demand curve (per hour)
        decimal baseFTEPerHour = hour switch
        {
            7 or 8 => 0.5m,
            9 or 10 => 2.0m,
            11 or 12 or 13 or 14 => 4.0m,
            15 or 16 or 17 => 3.0m,
            18 or 19 => 2.0m,
            20 or 21 => 1.0m,
            _ => 0.5m
        };

        // Add some variance
        decimal variance = (decimal)(_random.NextDouble() * 0.5 - 0.25);

        return Math.Max(0m, (baseFTEPerHour + variance) * intervalScale);
    }

    /// <summary>
    /// Gets assigned FTE for a specific time slot (minutes since midnight).
    /// </summary>
    private decimal GetTimeSlotAssignedFTE(int minutesSinceMidnight)
    {
        // DEVNOTE: Development stub data
        // Production: Call IScheduleService
        var required = GetTimeSlotRequiredFTE(minutesSinceMidnight);
        return required * (0.7m + (decimal)_random.NextDouble() * 0.4m);
    }
}
