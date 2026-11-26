using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-module adaptive clinic calendar: P-R-P-R-P-R-P seed, replans after each play
/// (including play-on-rest → that day becomes a session and future days shift).
/// Local-first; Firebase sync when online.
/// </summary>
public static class ClinicScheduleService
{
    const string PrefsPrefix = "LorR.ClinicSchedule.";

    public static ClinicScheduleState Load(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        string key = PrefsPrefix + profileId;
        if (!PlayerPrefs.HasKey(key))
            return EnsureSchedule(profileId);

        try
        {
            ClinicScheduleState state = JsonUtility.FromJson<ClinicScheduleState>(PlayerPrefs.GetString(key));
            if (state == null)
                return EnsureSchedule(profileId);

            state.profileId = profileId;
            MigrateToPerModuleIfNeeded(state);
            EnsureAllModuleSchedules(state);
            return state;
        }
        catch
        {
            return EnsureSchedule(profileId);
        }
    }

    public static void Save(ClinicScheduleState state)
    {
        if (state == null || string.IsNullOrEmpty(state.profileId))
            return;

        state.schemaVersion = ClinicScheduleState.CurrentSchema;
        PlayerPrefs.SetString(PrefsPrefix + state.profileId, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }

    public static ClinicScheduleState EnsureSchedule(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        string key = PrefsPrefix + profileId;
        if (PlayerPrefs.HasKey(key))
        {
            try
            {
                ClinicScheduleState existing = JsonUtility.FromJson<ClinicScheduleState>(PlayerPrefs.GetString(key));
                if (existing != null)
                {
                    existing.profileId = profileId;
                    MigrateToPerModuleIfNeeded(existing);
                    bool changed = EnsureAllModuleSchedules(existing);
                    changed |= ReconcileFromProgress(existing);
                    if (changed)
                    {
                        Save(existing);
                        FirebaseProfileSync.PushClinicSchedule(existing);
                    }

                    return existing;
                }
            }
            catch { /* rebuild */ }
        }

        string start = ProgressionConfig.TodayManila();
        var state = new ClinicScheduleState
        {
            profileId = profileId,
            trainingStartDate = start,
            schemaVersion = ClinicScheduleState.CurrentSchema,
            days = new List<ClinicDayPlan>(),
            modules = new List<ModuleClinicSchedule>()
        };
        EnsureAllModuleSchedules(state);
        ReconcileFromProgress(state);
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
        Debug.Log("[ClinicSchedule] Seeded per-module Phases 1–4 from " + start + " for " + profileId);
        return state;
    }

    /// <summary>
    /// If progress already has play dates but schedule wasn't replanned (legacy), rebuild each module plot.
    /// </summary>
    static bool ReconcileFromProgress(ClinicScheduleState state)
    {
        if (state == null || string.IsNullOrEmpty(state.profileId) || state.modules == null)
            return false;

        bool changed = false;
        for (int i = 0; i < state.modules.Count; i++)
        {
            ModuleClinicSchedule mod = state.modules[i];
            if (mod == null || string.IsNullOrEmpty(mod.moduleId))
                continue;

            ModuleProgress progress = ProgressionService.GetModule(state.profileId, mod.moduleId);
            if (progress == null || progress.phaseWeeks == null)
                continue;

            for (int w = 0; w < progress.phaseWeeks.Count; w++)
            {
                PhaseWeekProgress week = progress.phaseWeeks[w];
                if (week == null || week.playDates == null || week.playDates.Count == 0)
                    continue;

                if (!ModuleScheduleNeedsReconcile(mod, week))
                    continue;

                List<string> dates = new List<string>(week.playDates);
                dates.Sort(StringComparer.Ordinal);
                for (int d = 0; d < dates.Count; d++)
                    ClinicScheduleUtil.ReplanAfterPlay(mod, dates[d], week.phase, d + 1);
                ClinicScheduleUtil.RebuildLaterPhases(mod, week.phase);
                changed = true;
            }
        }

        return changed;
    }

    static bool ModuleScheduleNeedsReconcile(ModuleClinicSchedule mod, PhaseWeekProgress week)
    {
        if (mod == null || week == null || week.playDates == null)
            return false;

        // Each completed slot must have a credited Session day (may be later than early play-on-rest).
        for (int i = 0; i < week.playDates.Count; i++)
        {
            int slot = i + 1;
            bool found = false;
            if (mod.days != null)
            {
                for (int d = 0; d < mod.days.Count; d++)
                {
                    ClinicDayPlan plan = mod.days[d];
                    if (plan == null || plan.phase != week.phase)
                        continue;
                    if (plan.type == (int)ClinicDayPlanType.Session && plan.slot == slot)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                return true;
        }

        return false;
    }

    static void MigrateToPerModuleIfNeeded(ClinicScheduleState state)
    {
        if (state == null)
            return;

        if (state.modules == null)
            state.modules = new List<ModuleClinicSchedule>();

        if (state.schemaVersion >= ClinicScheduleState.CurrentSchema && state.modules.Count > 0)
            return;

        string start = !string.IsNullOrEmpty(state.trainingStartDate)
            ? state.trainingStartDate
            : ProgressionConfig.TodayManila();

        // Legacy shared days → copy template into each module (then they diverge on play).
        for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
        {
            string moduleId = ProgressionConfig.AllModules[i];
            if (ClinicScheduleUtil.FindModule(state, moduleId) != null)
                continue;

            var mod = new ModuleClinicSchedule
            {
                moduleId = moduleId,
                trainingStartDate = start,
                days = new List<ClinicDayPlan>()
            };

            if (state.days != null && state.days.Count > 0)
            {
                for (int d = 0; d < state.days.Count; d++)
                {
                    ClinicDayPlan src = state.days[d];
                    if (src == null) continue;
                    mod.days.Add(new ClinicDayPlan
                    {
                        date = src.date,
                        type = src.type,
                        slot = src.slot,
                        phase = src.phase,
                        moduleId = moduleId
                    });
                }

                EnsurePhasesThroughFour(mod, start);
            }
            else
            {
                mod.days = ClinicScheduleUtil.BuildFullProgram(start, moduleId);
            }

            state.modules.Add(mod);
        }

        state.schemaVersion = ClinicScheduleState.CurrentSchema;
        state.days = new List<ClinicDayPlan>(); // legacy cleared
        Save(state);
        Debug.Log("[ClinicSchedule] Migrated profile " + state.profileId + " to per-module schema v3 (Phases 1–4)");
    }

    /// <returns>True if anything was created/changed.</returns>
    static bool EnsureAllModuleSchedules(ClinicScheduleState state)
    {
        if (state == null)
            return false;

        if (state.modules == null)
            state.modules = new List<ModuleClinicSchedule>();

        bool changed = false;
        string start = !string.IsNullOrEmpty(state.trainingStartDate)
            ? state.trainingStartDate
            : ProgressionConfig.TodayManila();
        if (string.IsNullOrEmpty(state.trainingStartDate))
        {
            state.trainingStartDate = start;
            changed = true;
        }

        for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
        {
            string moduleId = ProgressionConfig.AllModules[i];
            ModuleClinicSchedule mod = ClinicScheduleUtil.FindModule(state, moduleId);
            if (mod == null)
            {
                mod = new ModuleClinicSchedule
                {
                    moduleId = moduleId,
                    trainingStartDate = start,
                    days = ClinicScheduleUtil.BuildFullProgram(start, moduleId)
                };
                state.modules.Add(mod);
                changed = true;
                continue;
            }

            if (mod.days == null || mod.days.Count == 0)
            {
                mod.trainingStartDate = start;
                mod.days = ClinicScheduleUtil.BuildFullProgram(start, moduleId);
                changed = true;
                continue;
            }

            if (EnsurePhasesThroughFour(mod, start))
                changed = true;
        }

        return changed;
    }

    /// <summary>Backfill missing Phase 2–4 weeks after the last plotted day.</summary>
    static bool EnsurePhasesThroughFour(ModuleClinicSchedule mod, string fallbackStart)
    {
        if (mod == null)
            return false;

        if (mod.days == null)
            mod.days = new List<ClinicDayPlan>();

        bool changed = false;
        for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            if (ClinicScheduleUtil.HasPhasePlotted(mod, phase))
                continue;

            string weekStart = phase == 1 && mod.days.Count == 0
                ? (!string.IsNullOrEmpty(mod.trainingStartDate) ? mod.trainingStartDate : fallbackStart)
                : ClinicScheduleUtil.NextDayAfterLast(mod, fallbackStart);

            mod.days.AddRange(ClinicScheduleUtil.BuildPhaseWeek(weekStart, phase, mod.moduleId));
            changed = true;
        }

        if (changed)
        {
            mod.days.Sort((a, b) => string.CompareOrdinal(
                a != null ? a.date : string.Empty,
                b != null ? b.date : string.Empty));
        }

        return changed;
    }

    /// <summary>
    /// Wipe and reseed Phases 1–4 from startDate, then reconcile from progression playDates (SuperAdmin clear).
    /// </summary>
    public static ClinicScheduleState ResetAndSeedFullProgram(string profileId, string startDate)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        if (string.IsNullOrEmpty(startDate))
            startDate = ProgressionConfig.TodayManila();

        var state = new ClinicScheduleState
        {
            profileId = profileId,
            trainingStartDate = startDate,
            schemaVersion = ClinicScheduleState.CurrentSchema,
            days = new List<ClinicDayPlan>(),
            modules = new List<ModuleClinicSchedule>()
        };

        for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
        {
            string moduleId = ProgressionConfig.AllModules[i];
            state.modules.Add(new ModuleClinicSchedule
            {
                moduleId = moduleId,
                trainingStartDate = startDate,
                days = ClinicScheduleUtil.BuildFullProgram(startDate, moduleId)
            });
        }

        ReconcileFromProgress(state);
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
        Debug.Log("[ClinicSchedule] Reset full program from " + startDate + " for " + profileId);
        return state;
    }

    public static ModuleClinicSchedule GetModuleSchedule(string profileId, string moduleId)
    {
        ClinicScheduleState state = EnsureSchedule(profileId);
        return ClinicScheduleUtil.FindModule(state, moduleId);
    }

    public static ClinicDayPlan GetModuleDayPlan(string profileId, string moduleId, string date)
    {
        ModuleClinicSchedule mod = GetModuleSchedule(profileId, moduleId);
        return ClinicScheduleUtil.FindDay(mod, date);
    }

    /// <summary>When a phase unlocks for a module, append next week plot after last planned day.</summary>
    public static void AppendPhaseWeekIfNeeded(string profileId, string moduleId, int phase)
    {
        ClinicScheduleState state = EnsureSchedule(profileId);
        ModuleClinicSchedule mod = ClinicScheduleUtil.FindModule(state, moduleId);
        if (mod == null)
            return;

        phase = ProgressionConfig.ClampPhase(phase);
        for (int i = 0; i < mod.days.Count; i++)
        {
            if (mod.days[i] != null && mod.days[i].phase == phase)
                return;
        }

        string start = ProgressionConfig.TodayManila();
        if (ProgressionConfig.TryParseDay(start, out DateTime today))
        {
            DateTime nextStart = today;
            for (int i = 0; i < mod.days.Count; i++)
            {
                if (mod.days[i] == null) continue;
                if (ProgressionConfig.TryParseDay(mod.days[i].date, out DateTime d) && d >= nextStart)
                    nextStart = d.AddDays(1);
            }

            start = nextStart.ToString("yyyy-MM-dd");
        }

        mod.days.AddRange(ClinicScheduleUtil.BuildPhaseWeek(start, phase, moduleId));
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
    }

    /// <summary>Legacy overload — append for every module that just unlocked this phase.</summary>
    public static void AppendPhaseWeekIfNeeded(string profileId, int phase)
    {
        for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
            AppendPhaseWeekIfNeeded(profileId, ProgressionConfig.AllModules[i], phase);
    }

    public static HashSet<string> CollectAllPlayDates(string profileId)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        ProfileProgressState progress = ProgressionService.Load(profileId);
        if (progress == null || progress.modules == null)
            return set;

        for (int m = 0; m < progress.modules.Count; m++)
        {
            ModuleProgress module = progress.modules[m];
            if (module == null || module.phaseWeeks == null)
                continue;

            for (int w = 0; w < module.phaseWeeks.Count; w++)
            {
                PhaseWeekProgress week = module.phaseWeeks[w];
                if (week == null || week.playDates == null)
                    continue;

                for (int d = 0; d < week.playDates.Count; d++)
                {
                    if (!string.IsNullOrEmpty(week.playDates[d]))
                        set.Add(week.playDates[d]);
                }
            }
        }

        return set;
    }

    public static HashSet<string> CollectModulePlayDates(string profileId, string moduleId)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        ModuleProgress module = ProgressionService.GetModule(profileId, moduleId);
        if (module == null || module.phaseWeeks == null)
            return set;

        for (int w = 0; w < module.phaseWeeks.Count; w++)
        {
            PhaseWeekProgress week = module.phaseWeeks[w];
            if (week == null || week.playDates == null)
                continue;
            for (int d = 0; d < week.playDates.Count; d++)
            {
                if (!string.IsNullOrEmpty(week.playDates[d]))
                    set.Add(week.playDates[d]);
            }
        }

        return set;
    }

    /// <summary>Aggregate mark for month cell (any module).</summary>
    public static ClinicDayMark GetMark(string profileId, string date, HashSet<string> playDates = null)
    {
        playDates = playDates ?? CollectAllPlayDates(profileId);
        bool played = playDates.Contains(date);
        string today = ProgressionConfig.TodayManila();
        bool isPast = string.CompareOrdinal(date, today) < 0;
        bool isToday = date == today;

        ClinicScheduleState state = EnsureSchedule(profileId);
        bool anySession = false;
        bool anyRest = false;
        bool anyEarlyPlay = false;

        if (state != null && state.modules != null)
        {
            for (int i = 0; i < state.modules.Count; i++)
            {
                ModuleClinicSchedule mod = state.modules[i];
                ClinicDayPlan plan = ClinicScheduleUtil.FindDay(mod, date);
                HashSet<string> modPlays = CollectModulePlayDates(profileId, mod != null ? mod.moduleId : null);
                bool modPlayed = modPlays.Contains(date);

                if (plan == null)
                {
                    if (modPlayed) anyEarlyPlay = true;
                    continue;
                }

                bool plannedSession = plan.type == (int)ClinicDayPlanType.Session;
                if (modPlayed && !plannedSession)
                    anyEarlyPlay = true;
                if (plannedSession)
                    anySession = true;
                else
                    anyRest = true;
            }
        }

        if (played)
            return anyEarlyPlay ? ClinicDayMark.CompletedEarly : ClinicDayMark.CompletedOnPlan;

        if (anySession && isPast)
            return ClinicDayMark.Missed;

        if (anySession)
            return ClinicDayMark.PlannedSession;

        if (anyRest)
            return ClinicDayMark.PlannedRest;

        return isToday ? ClinicDayMark.Today : ClinicDayMark.None;
    }

    public static ClinicDayMark GetModuleMark(string profileId, string moduleId, string date)
    {
        ClinicDayPlan plan = GetModuleDayPlan(profileId, moduleId, date);
        HashSet<string> playDates = CollectModulePlayDates(profileId, moduleId);
        bool played = playDates.Contains(date);
        string today = ProgressionConfig.TodayManila();
        bool isPast = string.CompareOrdinal(date, today) < 0;
        bool isToday = date == today;

        if (plan == null)
        {
            if (played) return ClinicDayMark.CompletedEarly;
            return isToday ? ClinicDayMark.Today : ClinicDayMark.None;
        }

        bool plannedSession = plan.type == (int)ClinicDayPlanType.Session;
        if (played)
            return plannedSession ? ClinicDayMark.CompletedOnPlan : ClinicDayMark.CompletedEarly;

        if (plannedSession && isPast)
            return ClinicDayMark.Missed;

        if (plannedSession)
            return ClinicDayMark.PlannedSession;

        return ClinicDayMark.PlannedRest;
    }

    /// <summary>True if playing today would create a 5th consecutive play day (clinic-wide).</summary>
    public static bool WouldCreateFiveConsecutivePlayDays(string profileId, string today)
    {
        HashSet<string> playDates = CollectAllPlayDates(profileId);
        if (playDates.Contains(today))
            return false;

        if (!ProgressionConfig.TryParseDay(today, out DateTime day))
            return false;

        int streakBefore = 0;
        DateTime cursor = day.AddDays(-1);
        for (int i = 0; i < 8; i++)
        {
            string key = cursor.ToString("yyyy-MM-dd");
            if (!playDates.Contains(key))
                break;
            streakBefore++;
            cursor = cursor.AddDays(-1);
        }

        return streakBefore >= 4;
    }

    public static void OnSimulationRecorded(string profileId, string moduleId, string playedOn, int phase)
    {
        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(playedOn))
            return;

        ClinicScheduleState state = EnsureSchedule(profileId);
        ModuleClinicSchedule mod = ClinicScheduleUtil.FindModule(state, moduleId);
        if (mod == null)
            return;

        phase = ProgressionConfig.ClampPhase(phase);
        int completedSlot = CountCompletedSessionsInPhase(profileId, moduleId, phase);
        if (completedSlot < 1)
            completedSlot = 1;

        ClinicScheduleUtil.ReplanAfterPlay(mod, playedOn, phase, completedSlot);
        ClinicScheduleUtil.RebuildLaterPhases(mod, phase);
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
        Debug.Log("[ClinicSchedule] Replanned " + moduleId + " after play " + playedOn +
                  " phase " + phase + " session " + completedSlot);
    }

    /// <summary>Legacy signature — prefer module-aware overload.</summary>
    public static void OnSimulationRecorded(string profileId, string playedOn)
    {
        OnSimulationRecorded(profileId, ProgressionConfig.ReactionLight, playedOn, 1);
    }

    static int CountCompletedSessionsInPhase(string profileId, string moduleId, int phase)
    {
        ModuleProgress module = ProgressionService.GetModule(profileId, moduleId);
        if (module == null || module.phaseWeeks == null)
            return 0;

        phase = ProgressionConfig.ClampPhase(phase);
        int count = 0;
        for (int i = 0; i < module.phaseWeeks.Count; i++)
        {
            PhaseWeekProgress week = module.phaseWeeks[i];
            if (week == null || week.phase != phase || week.playDates == null)
                continue;
            count += week.playDates.Count;
        }

        return Math.Min(count, ProgressionConfig.MaxPlayDaysPerWeek);
    }
}
