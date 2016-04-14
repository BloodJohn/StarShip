using System;
using System.Globalization;

public static class JSTime
{
    private static readonly CultureInfo provider = CultureInfo.GetCultureInfo("ru-RU");
    public static readonly DateTime minDate = new DateTime(1970, 1, 1);
    public const long second = 1000;
    public const long minute = second * 60;
    public const long hour = minute * 60;
    public const long day = hour * 24;
    public const long week = day * 7;
    public const long zero = 0;

    /// <summary>Выдает время по гринвичу в миллисекундах</summary>
    public static Int64 GetMilliseconds(DateTime time)
    {
        return (Int64)(
            (time.ToUniversalTime() - minDate).TotalMilliseconds
            );
    }

    public static DateTime GetTime(Int64 milliseconds)
    {
        DateTime result = minDate;
        return result.AddMilliseconds(milliseconds).ToLocalTime();
    }

    public static string ToTimeString(Int64 time)
    {
        TimeSpan sTime = new TimeSpan(time * TimeSpan.TicksPerMillisecond);
        return ToTimeString(sTime);
    }

    public static string ToTimeString(TimeSpan sTime)
    {
        if (sTime.TotalDays >= 1) return ((int)sTime.TotalDays) + "д " + sTime.Hours + "ч";
        if (sTime.TotalHours >= 1) return ((int)sTime.TotalHours) + "ч " + sTime.Minutes + "мин";
        if (sTime.Minutes >= 1) return sTime.Minutes + "мин " + sTime.Seconds + "сек";
        return sTime.Seconds + "сек";
    }

    /// <summary>Текстовый формат времени для SQL команд</summary>
    public static string ToSQL(DateTime t)
    {
        return string.Format("convert(datetime,'{0}', 104)",

            t.ToString("dd/MM/yyyy HH:mm:ss.fff"));
    }

    /// <summary>Выдает текущее время по гринвичу в миллисекундах</summary>
    public static Int64 Now
    {
        get { return GetMilliseconds(DateTime.UtcNow); }
    }

    /// <summary>Время на русском</summary>
    public static string GetTime(DateTime time)
    {
        return time.ToString("HH:mm:ss", provider);
    }

    /// <summary>Дата на русском</summary>
    public static string GetDate(DateTime date)
    {
        return date.ToString("d MMMM yyyy", provider);
    }

    /// <summary>Дата и время на русском</summary>
    public static string GetDateTime(DateTime date)
    {
        return date.ToString("d MMMM HH:mm", provider);
    }

    /// <summary>Канун праздника Halloween</summary>
    public static bool GetHalloweenEve(double days)
    {
        DateTime now = DateTime.Now;

        //окончание 1го ноября 4 часа ночи
        DateTime halloween = new DateTime(now.Year, 11, 1, 4, 0, 0);

        if (halloween < now) return false;

        return ((halloween - now).TotalDays <= days);
    }
}