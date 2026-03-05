namespace SmartOps.Models.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Gets the start of the week (Monday) for the given date.
    /// </summary>
    public static DateTime GetWeekStart(this DateTime date)
    {
        var daysToMonday = (int)date.DayOfWeek - 1;
        if (daysToMonday < 0) daysToMonday = 6; // If Sunday, go back 6 days
        return date.AddDays(-daysToMonday).Date;
    }

    /// <summary>
    /// Gets the end of the week (Sunday) for the given date.
    /// </summary>
    public static DateTime GetWeekEnd(this DateTime date)
    {
        return date.GetWeekStart().AddDays(6);
    }
}
