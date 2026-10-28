namespace OpenWish.Shared.Models;

/// <summary>Month-and-day birthday rules. The birth year is never collected.</summary>
public static class BirthdayCalendar
{
    // A leap year, so February 29 is a valid birthday.
    private const int ReferenceLeapYear = 2024;

    public static bool IsValid(int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(ReferenceLeapYear, month);

    /// <summary>The next date on or after <paramref name="today"/> that falls on the birthday. February 29 is observed on February 28 in common years.</summary>
    public static DateOnly NextOccurrence(int month, int day, DateOnly today)
    {
        var thisYear = Observed(today.Year, month, day);
        return thisYear >= today ? thisYear : Observed(today.Year + 1, month, day);
    }

    public static int DaysUntil(int month, int day, DateOnly today) =>
        NextOccurrence(month, day, today).DayNumber - today.DayNumber;

    public static string Format(int month, int day) =>
        new DateOnly(ReferenceLeapYear, month, day).ToString("MMMM d", System.Globalization.CultureInfo.CurrentCulture);

    private static DateOnly Observed(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}