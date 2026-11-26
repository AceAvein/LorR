using System;
using System.Collections.Generic;

public enum ClinicDayPlanType
{
    Session = 0,
    Rest = 1
}

public enum ClinicDayMark
{
    None = 0,
    PlannedSession = 1,
    PlannedRest = 2,
    CompletedOnPlan = 3,
    CompletedEarly = 4,
    Missed = 5,
    Today = 6
}

[Serializable]
public class ClinicDayPlan
{
    public string date;
    public int type; // ClinicDayPlanType
    public int slot; // 1..4 when session
    public int phase;
    public string moduleId; // per-module adaptive schedule
}

[Serializable]
public class ModuleClinicSchedule
{
    public string moduleId;
    public string trainingStartDate;
    public List<ClinicDayPlan> days = new List<ClinicDayPlan>();
}

[Serializable]
public class ClinicScheduleState
{
    public const int CurrentSchema = 3;

    public string profileId;
    public string trainingStartDate;
    public int schemaVersion = CurrentSchema;
    /// <summary>Legacy shared days (schema &lt; 3). Migrated into modules.</summary>
    public List<ClinicDayPlan> days = new List<ClinicDayPlan>();
    public List<ModuleClinicSchedule> modules = new List<ModuleClinicSchedule>();
}

public static class ClinicScheduleUtil
{
    /// <summary>P-R-P-R-P-R-P from start (4 sessions, 3 rests).</summary>
    public static List<ClinicDayPlan> BuildPhaseWeek(string startYyyyMmDd, int phase, string moduleId = null)
    {
        var list = new List<ClinicDayPlan>();
        if (!ProgressionConfig.TryParseDay(startYyyyMmDd, out DateTime start))
            return list;

        int sessionSlot = 0;
        for (int i = 0; i < ProgressionConfig.WeekLengthDays; i++)
        {
            bool isSession = (i % 2) == 0; // 0,2,4,6 session; 1,3,5 rest
            var plan = new ClinicDayPlan
            {
                date = start.AddDays(i).ToString("yyyy-MM-dd"),
                phase = phase,
                moduleId = moduleId,
                type = isSession ? (int)ClinicDayPlanType.Session : (int)ClinicDayPlanType.Rest,
                slot = 0
            };
            if (isSession)
            {
                sessionSlot++;
                plan.slot = sessionSlot;
            }

            list.Add(plan);
        }

        return list;
    }

    /// <summary>
    /// Soft pre-plot Phases 1..PhaseCount back-to-back (each week P-R-P-R-P-R-P).
    /// Adaptive replan after play may still shift remaining days per module.
    /// </summary>
    public static List<ClinicDayPlan> BuildFullProgram(string startYyyyMmDd, string moduleId = null)
    {
        var list = new List<ClinicDayPlan>();
        if (!ProgressionConfig.TryParseDay(startYyyyMmDd, out DateTime start))
            return list;

        DateTime cursor = start;
        for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            string weekStart = cursor.ToString("yyyy-MM-dd");
            list.AddRange(BuildPhaseWeek(weekStart, phase, moduleId));
            cursor = cursor.AddDays(ProgressionConfig.WeekLengthDays);
        }

        return list;
    }

    public static bool HasPhasePlotted(ModuleClinicSchedule mod, int phase)
    {
        if (mod == null || mod.days == null)
            return false;

        phase = ProgressionConfig.ClampPhase(phase);
        for (int i = 0; i < mod.days.Count; i++)
        {
            if (mod.days[i] != null && mod.days[i].phase == phase)
                return true;
        }

        return false;
    }

    public static string NextDayAfterLast(ModuleClinicSchedule mod, string fallbackYyyyMmDd)
    {
        string latest = fallbackYyyyMmDd;
        if (mod == null || mod.days == null)
            return latest;

        for (int i = 0; i < mod.days.Count; i++)
        {
            if (mod.days[i] == null || string.IsNullOrEmpty(mod.days[i].date))
                continue;
            if (string.CompareOrdinal(mod.days[i].date, latest) > 0)
                latest = mod.days[i].date;
        }

        if (!ProgressionConfig.TryParseDay(latest, out DateTime d))
            return fallbackYyyyMmDd;

        return d.AddDays(1).ToString("yyyy-MM-dd");
    }

    public static string LastDateForPhase(ModuleClinicSchedule mod, int phase)
    {
        string latest = null;
        if (mod == null || mod.days == null)
            return null;

        phase = ProgressionConfig.ClampPhase(phase);
        for (int i = 0; i < mod.days.Count; i++)
        {
            ClinicDayPlan d = mod.days[i];
            if (d == null || d.phase != phase || string.IsNullOrEmpty(d.date))
                continue;
            if (latest == null || string.CompareOrdinal(d.date, latest) > 0)
                latest = d.date;
        }

        return latest;
    }

    /// <summary>
    /// After adaptive replan of a phase, rebuild Phase+1..4 so they stay after the current phase ends.
    /// </summary>
    public static void RebuildLaterPhases(ModuleClinicSchedule mod, int fromPhaseExclusive)
    {
        if (mod == null)
            return;

        if (mod.days == null)
            mod.days = new List<ClinicDayPlan>();

        int keepThrough = ProgressionConfig.ClampPhase(fromPhaseExclusive);
        mod.days.RemoveAll(d => d != null && d.phase > keepThrough);

        string last = LastDateForPhase(mod, keepThrough);
        if (string.IsNullOrEmpty(last) || !ProgressionConfig.TryParseDay(last, out DateTime lastDay))
            return;

        DateTime cursor = lastDay.AddDays(1);
        for (int phase = keepThrough + 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            string weekStart = cursor.ToString("yyyy-MM-dd");
            mod.days.AddRange(BuildPhaseWeek(weekStart, phase, mod.moduleId));
            cursor = cursor.AddDays(ProgressionConfig.WeekLengthDays);
        }

        mod.days.Sort((a, b) => string.CompareOrdinal(
            a != null ? a.date : string.Empty,
            b != null ? b.date : string.Empty));
    }

    /// <summary>
    /// After a play on playedOn (session slot just completed), clear future days for this phase
    /// and replot Rest/Session alternating until remaining sessions are scheduled.
    /// </summary>
    public static void ReplanAfterPlay(ModuleClinicSchedule moduleSched, string playedOn, int phase, int completedSlot)
    {
        if (moduleSched == null || string.IsNullOrEmpty(playedOn))
            return;

        phase = ProgressionConfig.ClampPhase(phase);
        completedSlot = Math.Max(1, Math.Min(completedSlot, ProgressionConfig.MaxPlayDaysPerWeek));

        if (moduleSched.days == null)
            moduleSched.days = new List<ClinicDayPlan>();

        // Upsert played day as session.
        ClinicDayPlan played = FindDay(moduleSched, playedOn);
        if (played == null)
        {
            played = new ClinicDayPlan { date = playedOn, moduleId = moduleSched.moduleId };
            moduleSched.days.Add(played);
        }

        played.type = (int)ClinicDayPlanType.Session;
        played.phase = phase;
        played.slot = completedSlot;
        played.moduleId = moduleSched.moduleId;

        // Drop future days for this phase (keep other phases / past).
        moduleSched.days.RemoveAll(d =>
            d != null &&
            d.phase == phase &&
            string.CompareOrdinal(d.date, playedOn) > 0);

        if (completedSlot >= ProgressionConfig.MaxPlayDaysPerWeek)
            return; // phase sessions done — caller may append next phase

        if (!ProgressionConfig.TryParseDay(playedOn, out DateTime playDay))
            return;

        // Next day = Rest, then Session, Rest, Session... until remaining slots plotted.
        int nextSlot = completedSlot + 1;
        DateTime cursor = playDay.AddDays(1);
        bool expectRest = true;
        int guard = 0;
        while (nextSlot <= ProgressionConfig.MaxPlayDaysPerWeek && guard++ < 21)
        {
            string date = cursor.ToString("yyyy-MM-dd");
            if (expectRest)
            {
                moduleSched.days.Add(new ClinicDayPlan
                {
                    date = date,
                    moduleId = moduleSched.moduleId,
                    phase = phase,
                    type = (int)ClinicDayPlanType.Rest,
                    slot = 0
                });
                expectRest = false;
            }
            else
            {
                moduleSched.days.Add(new ClinicDayPlan
                {
                    date = date,
                    moduleId = moduleSched.moduleId,
                    phase = phase,
                    type = (int)ClinicDayPlanType.Session,
                    slot = nextSlot
                });
                nextSlot++;
                expectRest = true;
            }

            cursor = cursor.AddDays(1);
        }

        moduleSched.days.Sort((a, b) => string.CompareOrdinal(
            a != null ? a.date : string.Empty,
            b != null ? b.date : string.Empty));
    }

    public static ModuleClinicSchedule FindModule(ClinicScheduleState state, string moduleId)
    {
        if (state == null || state.modules == null || string.IsNullOrEmpty(moduleId))
            return null;

        for (int i = 0; i < state.modules.Count; i++)
        {
            if (state.modules[i] != null &&
                string.Equals(state.modules[i].moduleId, moduleId, StringComparison.Ordinal))
                return state.modules[i];
        }

        return null;
    }

    public static ClinicDayPlan FindDay(ModuleClinicSchedule moduleSched, string date)
    {
        if (moduleSched == null || moduleSched.days == null || string.IsNullOrEmpty(date))
            return null;

        for (int i = 0; i < moduleSched.days.Count; i++)
        {
            if (moduleSched.days[i] != null &&
                string.Equals(moduleSched.days[i].date, date, StringComparison.Ordinal))
                return moduleSched.days[i];
        }

        return null;
    }

    public static ClinicDayPlan FindDay(ClinicScheduleState state, string date)
    {
        // Legacy shared lookup (schema &lt; 3).
        if (state == null || state.days == null || string.IsNullOrEmpty(date))
            return null;

        for (int i = 0; i < state.days.Count; i++)
        {
            if (state.days[i] != null &&
                string.Equals(state.days[i].date, date, StringComparison.Ordinal))
                return state.days[i];
        }

        return null;
    }
}
