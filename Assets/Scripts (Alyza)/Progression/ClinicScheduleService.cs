using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared clinic calendar per profile: pre-plot P-R-P-R-P-R-P, marks from actual play.
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
            if (state == null || state.days == null || state.days.Count == 0)
                return EnsureSchedule(profileId);

            state.profileId = profileId;
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
                if (existing != null && existing.days != null && existing.days.Count > 0)
                {
                    existing.profileId = profileId;
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
            days = ClinicScheduleUtil.BuildPhaseWeek(start, 1)
        };
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
        Debug.Log("[ClinicSchedule] Seeded P-R-P-R-P-R-P from " + start + " for " + profileId);
        return state;
    }

    /// <summary>When a phase unlocks, append next week plot starting tomorrow (or today if none left).</summary>
    public static void AppendPhaseWeekIfNeeded(string profileId, int phase)
    {
        ClinicScheduleState state = Load(profileId);
        if (state == null)
            return;

        for (int i = 0; i < state.days.Count; i++)
        {
            if (state.days[i] != null && state.days[i].phase == phase)
                return; // already plotted
        }

        string start = ProgressionConfig.TodayManila();
        if (ProgressionConfig.TryParseDay(start, out DateTime today))
        {
            // Start next plot the day after the last planned day, or today.
            DateTime nextStart = today;
            for (int i = 0; i < state.days.Count; i++)
            {
                if (state.days[i] == null) continue;
                if (ProgressionConfig.TryParseDay(state.days[i].date, out DateTime d) && d >= nextStart)
                    nextStart = d.AddDays(1);
            }

            start = nextStart.ToString("yyyy-MM-dd");
        }

        state.days.AddRange(ClinicScheduleUtil.BuildPhaseWeek(start, phase));
        Save(state);
        FirebaseProfileSync.PushClinicSchedule(state);
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

    public static ClinicDayMark GetMark(string profileId, string date, HashSet<string> playDates = null)
    {
        ClinicScheduleState state = Load(profileId);
        ClinicDayPlan plan = ClinicScheduleUtil.FindDay(state, date);
        playDates = playDates ?? CollectAllPlayDates(profileId);
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

    /// <summary>True if playing today would create a 5th consecutive play day.</summary>
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

    public static void OnSimulationRecorded(string profileId, string playedOn)
    {
        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(playedOn))
            return;

        // Ensure schedule exists; mark sync of play is derived from progression playDates.
        ClinicScheduleState state = EnsureSchedule(profileId);
        FirebaseProfileSync.PushClinicSchedule(state);
    }
}
