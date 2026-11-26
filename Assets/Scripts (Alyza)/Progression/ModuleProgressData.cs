using System;
using System.Collections.Generic;

[Serializable]
public class DaySessionSave
{
    public string playedOn;
    public int phase;
    public string mode;
    public float primaryScore;
    public bool passed;
    public string metricsJson;

    // Expanded results for Console + doc pass rules (also kept in metricsJson).
    public float accuracy;
    public float completionTimeSec;
    public float averageReactionMs;
    public float avgDeviationMm;
    public float maxDeviationMm;
    public float interceptionRate;
    public int correct;
    public int incorrect;
    public int missed;
    public int success;
    public int fail;
    public int patternsCompleted;
    public int patternTotal;
}

[Serializable]
public class PhaseWeekProgress
{
    public int phase = 1;
    public string weekStart;
    public List<string> playDates = new List<string>();
    public bool weekEvaluated;
    public bool weekPassed;
    public float averageUnguidedScore;
    public List<DaySessionSave> days = new List<DaySessionSave>();
}

[Serializable]
public class ModuleProgress
{
    public string moduleId;
    public int unlockedThroughPhase = 1;
    public int currentPhase = 1;
    public string currentWeekStart;
    public List<string> playDatesThisWeek = new List<string>();
    public List<PhaseWeekProgress> phaseWeeks = new List<PhaseWeekProgress>();
}

[Serializable]
public class ProfileProgressState
{
    public string profileId;
    public List<ModuleProgress> modules = new List<ModuleProgress>();
}

public static class ModuleProgressUtil
{
    public static ModuleProgress GetOrCreateModule(ProfileProgressState state, string moduleId)
    {
        if (state == null)
            return null;

        if (state.modules == null)
            state.modules = new List<ModuleProgress>();

        for (int i = 0; i < state.modules.Count; i++)
        {
            if (state.modules[i] != null &&
                string.Equals(state.modules[i].moduleId, moduleId, StringComparison.Ordinal))
                return state.modules[i];
        }

        var created = new ModuleProgress
        {
            moduleId = moduleId,
            unlockedThroughPhase = 1,
            currentPhase = 1
        };
        state.modules.Add(created);
        return created;
    }

    public static PhaseWeekProgress GetOrCreateActiveWeek(ModuleProgress module, int phase, string today)
    {
        if (module == null)
            return null;

        phase = ProgressionConfig.ClampPhase(phase);
        if (module.phaseWeeks == null)
            module.phaseWeeks = new List<PhaseWeekProgress>();

        // Prefer an open (not evaluated) week for this phase.
        for (int i = module.phaseWeeks.Count - 1; i >= 0; i--)
        {
            PhaseWeekProgress week = module.phaseWeeks[i];
            if (week == null || week.phase != phase)
                continue;
            if (!week.weekEvaluated)
                return EnsureWeekWindow(module, week, today);
        }

        var fresh = new PhaseWeekProgress
        {
            phase = phase,
            weekStart = today,
            playDates = new List<string>(),
            days = new List<DaySessionSave>()
        };
        module.phaseWeeks.Add(fresh);
        module.currentWeekStart = today;
        module.playDatesThisWeek = new List<string>();
        return fresh;
    }

    public static PhaseWeekProgress EnsureWeekWindow(ModuleProgress module, PhaseWeekProgress week, string today)
    {
        if (module == null || week == null)
            return week;

        if (string.IsNullOrEmpty(week.weekStart))
            week.weekStart = today;

        if (!ProgressionConfig.TryParseDay(week.weekStart, out DateTime start) ||
            !ProgressionConfig.TryParseDay(today, out DateTime day))
            return week;

        // New calendar week after 7 days → if previous week had 4 days, it should already be evaluated.
        // If not passed / not full, start a fresh attempt week for same phase.
        if ((day - start).TotalDays >= ProgressionConfig.WeekLengthDays)
        {
            if (!week.weekEvaluated && week.playDates != null &&
                week.playDates.Count >= ProgressionConfig.MaxPlayDaysPerWeek)
            {
                EvaluateWeekInPlace(module.moduleId, week);
            }

            if (week.weekPassed)
                return week;

            var fresh = new PhaseWeekProgress
            {
                phase = week.phase,
                weekStart = today,
                playDates = new List<string>(),
                days = new List<DaySessionSave>()
            };
            module.phaseWeeks.Add(fresh);
            module.currentWeekStart = today;
            module.playDatesThisWeek = new List<string>();
            return fresh;
        }

        module.currentWeekStart = week.weekStart;
        module.playDatesThisWeek = week.playDates != null
            ? new List<string>(week.playDates)
            : new List<string>();
        return week;
    }

    public static DaySessionSave FindDay(PhaseWeekProgress week, string playedOn)
    {
        if (week == null || week.days == null || string.IsNullOrEmpty(playedOn))
            return null;

        for (int i = 0; i < week.days.Count; i++)
        {
            if (week.days[i] != null &&
                string.Equals(week.days[i].playedOn, playedOn, StringComparison.Ordinal))
                return week.days[i];
        }

        return null;
    }

    public static void EvaluateWeekInPlace(PhaseWeekProgress week)
    {
        EvaluateWeekInPlace(null, week);
    }

    public static void EvaluateWeekInPlace(string moduleId, PhaseWeekProgress week)
    {
        if (week == null)
            return;

        ProgressionPassRules.EvaluateWeek(moduleId, week, out _);
    }
}
