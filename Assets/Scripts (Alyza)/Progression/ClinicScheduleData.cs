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
    /// After a play (session slot just completed):
    /// - If played early on a Rest day, credit the next planned Session day for that slot (e.g. S2 Done on day 3).
    /// - Played-on rest day stays Rest in the plot (early play is tracked via progress playDates).
    /// - Clear/replot only days after the credited session date.
    /// </summary>
    public static void ReplanAfterPlay(ModuleClinicSchedule moduleSched, string playedOn, int phase, int completedSlot)
    {
        if (moduleSched == null || string.IsNullOrEmpty(playedOn))
            return;

        phase = ProgressionConfig.ClampPhase(phase);
        completedSlot = Math.Max(1, Math.Min(completedSlot, ProgressionConfig.MaxPlayDaysPerWeek));

        if (moduleSched.days == null)
            moduleSched.days = new List<ClinicDayPlan>();

        ClinicDayPlan playedPlan = FindDay(moduleSched, playedOn);
        string creditDate = ResolveCreditSessionDate(moduleSched, playedOn, playedPlan, phase, completedSlot);

        // Early play on rest: keep that day as Rest.
        if (playedPlan != null &&
            playedPlan.type == (int)ClinicDayPlanType.Rest &&
            !string.Equals(creditDate, playedOn, StringComparison.Ordinal))
        {
            playedPlan.phase = phase;
            playedPlan.moduleId = moduleSched.moduleId;
            playedPlan.slot = 0;
            playedPlan.type = (int)ClinicDayPlanType.Rest;
        }

        // Credit the planned (or same-day) session slot.
        ClinicDayPlan credited = FindDay(moduleSched, creditDate);
        if (credited == null)
        {
            credited = new ClinicDayPlan { date = creditDate, moduleId = moduleSched.moduleId };
            moduleSched.days.Add(credited);
        }

        credited.type = (int)ClinicDayPlanType.Session;
        credited.phase = phase;
        credited.slot = completedSlot;
        credited.moduleId = moduleSched.moduleId;

        // Drop future days for this phase after the credited session day.
        moduleSched.days.RemoveAll(d =>
            d != null &&
            d.phase == phase &&
            string.CompareOrdinal(d.date, creditDate) > 0);

        if (completedSlot >= ProgressionConfig.MaxPlayDaysPerWeek)
        {
            SortDays(moduleSched);
            return;
        }

        if (!ProgressionConfig.TryParseDay(creditDate, out DateTime creditDay))
            return;

        // After credited session: Rest, Session, Rest, Session... until slots filled.
        int nextSlot = completedSlot + 1;
        DateTime cursor = creditDay.AddDays(1);
        bool expectRest = true;
        int guard = 0;
        while (nextSlot <= ProgressionConfig.MaxPlayDaysPerWeek && guard++ < 21)
        {
            string date = cursor.ToString("yyyy-MM-dd");
            // Don't overwrite the early-play rest day if it's still in the past relative to credit.
            if (expectRest)
            {
                ClinicDayPlan existing = FindDay(moduleSched, date);
                if (existing == null)
                {
                    moduleSched.days.Add(new ClinicDayPlan
                    {
                        date = date,
                        moduleId = moduleSched.moduleId,
                        phase = phase,
                        type = (int)ClinicDayPlanType.Rest,
                        slot = 0
                    });
                }
                else if (string.CompareOrdinal(date, creditDate) > 0)
                {
                    existing.type = (int)ClinicDayPlanType.Rest;
                    existing.phase = phase;
                    existing.slot = 0;
                    existing.moduleId = moduleSched.moduleId;
                }

                expectRest = false;
            }
            else
            {
                ClinicDayPlan existing = FindDay(moduleSched, date);
                if (existing == null)
                {
                    moduleSched.days.Add(new ClinicDayPlan
                    {
                        date = date,
                        moduleId = moduleSched.moduleId,
                        phase = phase,
                        type = (int)ClinicDayPlanType.Session,
                        slot = nextSlot
                    });
                }
                else
                {
                    existing.type = (int)ClinicDayPlanType.Session;
                    existing.phase = phase;
                    existing.slot = nextSlot;
                    existing.moduleId = moduleSched.moduleId;
                }

                nextSlot++;
                expectRest = true;
            }

            cursor = cursor.AddDays(1);
        }

        SortDays(moduleSched);
    }

    /// <summary>
    /// Early on Rest → credit the next planned Session day for this slot (or soonest session day after play).
    /// On-plan / same-day session → credit playedOn.
    /// </summary>
    static string ResolveCreditSessionDate(
        ModuleClinicSchedule moduleSched,
        string playedOn,
        ClinicDayPlan playedPlan,
        int phase,
        int completedSlot)
    {
        bool earlyOnRest = playedPlan != null && playedPlan.type == (int)ClinicDayPlanType.Rest;
        if (!earlyOnRest)
        {
            // Already a session day, or unplanned day: credit the play date.
            return playedOn;
        }

        // Prefer existing planned day with this slot number on/after play date.
        ClinicDayPlan bySlot = FindSessionDayForSlot(moduleSched, phase, completedSlot);
        if (bySlot != null && string.CompareOrdinal(bySlot.date, playedOn) >= 0)
            return bySlot.date;

        // Else first planned session day strictly after play (soft slot).
        ClinicDayPlan nextSession = FindNextSessionDayAfter(moduleSched, phase, playedOn);
        if (nextSession != null)
            return nextSession.date;

        // No future session plotted — credit play day itself.
        return playedOn;
    }

    static ClinicDayPlan FindSessionDayForSlot(ModuleClinicSchedule moduleSched, int phase, int slot)
    {
        if (moduleSched == null || moduleSched.days == null)
            return null;

        for (int i = 0; i < moduleSched.days.Count; i++)
        {
            ClinicDayPlan d = moduleSched.days[i];
            if (d == null || d.phase != phase)
                continue;
            if (d.type == (int)ClinicDayPlanType.Session && d.slot == slot)
                return d;
        }

        return null;
    }

    static ClinicDayPlan FindNextSessionDayAfter(ModuleClinicSchedule moduleSched, int phase, string afterDate)
    {
        if (moduleSched == null || moduleSched.days == null)
            return null;

        ClinicDayPlan best = null;
        for (int i = 0; i < moduleSched.days.Count; i++)
        {
            ClinicDayPlan d = moduleSched.days[i];
            if (d == null || d.phase != phase)
                continue;
            if (d.type != (int)ClinicDayPlanType.Session)
                continue;
            if (string.CompareOrdinal(d.date, afterDate) <= 0)
                continue;
            if (best == null || string.CompareOrdinal(d.date, best.date) < 0)
                best = d;
        }

        return best;
    }

    static void SortDays(ModuleClinicSchedule moduleSched)
    {
        if (moduleSched == null || moduleSched.days == null)
            return;

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
