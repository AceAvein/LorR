using System;

/// <summary>
/// Clinic progression constants aligned to LorR schedule + unlock rules.
/// Calendar day = Asia/Manila (resets 12:00 AM Manila).
/// </summary>
public static class ProgressionConfig
{
    public const int PhaseCount = 4;
    public const int MaxPlayDaysPerWeek = 4;
    public const int WeekLengthDays = 7;

    public const string ModeGuided = "Guided";
    public const string ModeUnguided = "Unguided";
    public const string ManilaTimeZoneId = "Asia/Manila";

    public const string ReactionLight = "reactionLight";
    public const string BallDrop = "ballDrop";
    public const string ObjectPlacement = "objectPlacement";
    public const string PenTrace = "penTrace";

    public static readonly string[] AllModules =
    {
        ReactionLight,
        BallDrop,
        ObjectPlacement,
        PenTrace
    };

    static TimeZoneInfo manilaZone;

    /// <summary>
    /// Fallback average primary-score gate when a module has no exact Comprehensive %.
    /// Phase I is baseline-complete (4 days), not this number.
    /// </summary>
    public static float PassAverageForPhase(int phase)
    {
        switch (ClampPhase(phase))
        {
            case 1: return 0f;   // baseline week — completion of 4 days
            case 2: return 70f;
            case 3: return 80f;
            case 4: return 85f;
            default: return 70f;
        }
    }

    public static int ClampPhase(int phase)
    {
        if (phase < 1) return 1;
        if (phase > PhaseCount) return PhaseCount;
        return phase;
    }

    public static DateTime NowManila()
    {
        try
        {
            if (manilaZone == null)
                manilaZone = TimeZoneInfo.FindSystemTimeZoneById(ManilaTimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, manilaZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows sometimes uses a different id.
            try
            {
                manilaZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, manilaZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }
        catch (Exception)
        {
            return DateTime.UtcNow.AddHours(8);
        }
    }

    /// <summary>Clinic session day in Asia/Manila (yyyy-MM-dd). Resets at 12:00 AM Manila.</summary>
    public static string TodayManila()
    {
        return NowManila().ToString("yyyy-MM-dd");
    }

    [Obsolete("Use TodayManila()")]
    public static string TodayLocal()
    {
        return TodayManila();
    }

    public static bool TryParseDay(string yyyyMmDd, out DateTime day)
    {
        return DateTime.TryParseExact(
            yyyyMmDd,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out day);
    }

    public static bool IsUnguided(string mode)
    {
        return string.IsNullOrEmpty(mode)
               || string.Equals(mode, ModeUnguided, StringComparison.OrdinalIgnoreCase)
               || string.Equals(mode, "Simulation", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsGuided(string mode)
    {
        return string.Equals(mode, ModeGuided, StringComparison.OrdinalIgnoreCase);
    }
}
