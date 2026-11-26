using System;
using System.Collections.Generic;

public enum ProgressionBlockReason
{
    None = 0,
    NoProfile,
    UnknownModule,
    PhaseLocked,
    WeeklyPlayDayLimit,
    WeekAlreadyPassed,
    AlreadyPlayedToday
}

/// <summary>
/// Pure rules:
/// - Phase I open; Phase N+1 unlocks after Phase N Unguided week pass
/// - Max 4 Unguided session days / phase-week; 5th day hard-blocked
/// - Unguided/Simulation: once per Manila calendar day (no overwrite)
/// - Guided: free practice (does not consume / overwrite session day)
/// </summary>
public static class ProgressionGate
{
    public static bool IsPhaseUnlocked(ModuleProgress module, int phase)
    {
        if (module == null)
            return false;

        phase = ProgressionConfig.ClampPhase(phase);
        return phase <= Math.Max(1, module.unlockedThroughPhase);
    }

    public static bool CanPlay(
        ModuleProgress module,
        int phase,
        string today,
        string mode,
        out ProgressionBlockReason reason,
        out string message)
    {
        reason = ProgressionBlockReason.None;
        message = null;

        if (module == null)
        {
            reason = ProgressionBlockReason.UnknownModule;
            message = "Unknown module.";
            return false;
        }

        phase = ProgressionConfig.ClampPhase(phase);
        if (!IsPhaseUnlocked(module, phase))
        {
            reason = ProgressionBlockReason.PhaseLocked;
            message = "Phase " + phase + " is locked. Finish previous phase Unguided week first.";
            return false;
        }

        // Guided = free practice; only phase unlock matters.
        if (ProgressionConfig.IsGuided(mode))
            return true;

        PhaseWeekProgress week = ModuleProgressUtil.GetOrCreateActiveWeek(module, phase, today);
        if (week == null)
        {
            reason = ProgressionBlockReason.UnknownModule;
            message = "Could not open phase week.";
            return false;
        }

        if (week.weekPassed)
        {
            reason = ProgressionBlockReason.WeekAlreadyPassed;
            message = "This phase week is already passed. Use the next unlocked phase.";
            return false;
        }

        if (week.playDates != null && week.playDates.Contains(today))
        {
            reason = ProgressionBlockReason.AlreadyPlayedToday;
            message = "Already completed today's Simulation. Next run unlocks after 12:00 AM Manila.";
            return false;
        }

        if (week.playDates != null &&
            week.playDates.Count >= ProgressionConfig.MaxPlayDaysPerWeek)
        {
            reason = ProgressionBlockReason.WeeklyPlayDayLimit;
            message = "Rest day: already used 4 Simulation days this week for this module.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Records an Unguided session day (once). Guided does not mutate week progress.
    /// Returns null if blocked; for Guided returns a non-persisted practice marker (caller may skip cloud).
    /// </summary>
    public static DaySessionSave RecordDaySession(
        ModuleProgress module,
        int phase,
        string today,
        string mode,
        float primaryScore,
        IDictionary<string, object> metrics,
        out bool unlockedNextPhase,
        out string message,
        out bool countsAsSessionDay)
    {
        unlockedNextPhase = false;
        countsAsSessionDay = false;
        message = null;

        if (!CanPlay(module, phase, today, mode, out _, out message))
            return null;

        phase = ProgressionConfig.ClampPhase(phase);

        if (ProgressionConfig.IsGuided(mode))
        {
            message = "Guided practice — does not use today's Simulation slot.";
            return new DaySessionSave
            {
                playedOn = today,
                phase = phase,
                mode = ProgressionConfig.ModeGuided,
                primaryScore = primaryScore,
                passed = false,
                metricsJson = MetricsToJson(metrics)
            };
        }

        PhaseWeekProgress week = ModuleProgressUtil.GetOrCreateActiveWeek(module, phase, today);
        if (week.playDates == null)
            week.playDates = new List<string>();
        if (week.days == null)
            week.days = new List<DaySessionSave>();

        // Hard: never overwrite an existing Unguided day save.
        if (week.playDates.Contains(today) || ModuleProgressUtil.FindDay(week, today) != null)
        {
            message = "Already completed today's Simulation. Next run unlocks after 12:00 AM Manila.";
            return null;
        }

        week.playDates.Add(today);
        countsAsSessionDay = true;

        var day = new DaySessionSave
        {
            playedOn = today,
            phase = phase,
            mode = ProgressionConfig.ModeUnguided,
            metricsJson = MetricsToJson(metrics)
        };
        ProgressionPassRules.FillDayMetrics(day, primaryScore, metrics);
        day.passed = ProgressionPassRules.DayQualifies(module.moduleId, phase, day);
        week.days.Add(day);

        module.currentPhase = phase;
        module.currentWeekStart = week.weekStart;
        module.playDatesThisWeek = new List<string>(week.playDates);

        if (week.playDates.Count >= ProgressionConfig.MaxPlayDaysPerWeek)
        {
            ProgressionPassRules.EvaluateWeek(module.moduleId, week, out string evalDetail);
            if (week.weekPassed && phase < ProgressionConfig.PhaseCount)
            {
                int next = phase + 1;
                if (module.unlockedThroughPhase < next)
                {
                    module.unlockedThroughPhase = next;
                    unlockedNextPhase = true;
                }

                module.currentPhase = next;
                message = "Phase " + phase + " week passed (" + evalDetail + "). Phase " + next + " unlocked.";
            }
            else if (week.weekEvaluated && !week.weekPassed)
            {
                message = "Phase " + phase + " week complete — not advancing. " + evalDetail;
            }
        }
        else
        {
            message = "Simulation day " + week.playDates.Count + "/" + ProgressionConfig.MaxPlayDaysPerWeek +
                      " saved (Manila " + today + "). Day qualifies=" + day.passed;
        }

        return day;
    }

    static string MetricsToJson(IDictionary<string, object> metrics)
    {
        if (metrics == null || metrics.Count == 0)
            return "{}";

        var parts = new List<string>();
        foreach (var kv in metrics)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                continue;

            string value;
            if (kv.Value is string s)
                value = "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            else if (kv.Value is bool b)
                value = b ? "true" : "false";
            else
                value = Convert.ToString(kv.Value, System.Globalization.CultureInfo.InvariantCulture);

            parts.Add("\"" + kv.Key + "\":" + value);
        }

        return "{" + string.Join(",", parts) + "}";
    }
}
