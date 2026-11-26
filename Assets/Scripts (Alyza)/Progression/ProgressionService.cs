using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PendingProgressSyncItem
{
    public string profileId;
    public string moduleId;
    public int phase;
    public string playedOn;
}

[Serializable]
public class PendingProgressSyncList
{
    public List<PendingProgressSyncItem> items = new List<PendingProgressSyncItem>();
}

/// <summary>
/// Local-first progression. Gates always use PlayerPrefs.
/// Cloud sync is queued and flushed when Firebase is ready / online.
/// </summary>
public static class ProgressionService
{
    const string PrefsPrefix = "LorR.Progression.";
    const string CloudSeededPrefix = "LorR.Progression.CloudSeeded.";
    const string CloudSeedVersionPrefix = "LorR.Progression.CloudSeedVersion.";
    const string PendingSyncKey = "LorR.Progression.PendingSync";
    /// <summary>Bump when cloud skeleton shape changes so existing profiles re-seed.</summary>
    const int CloudSeedSchemaVersion = 2;

    static readonly HashSet<string> pendingCloudSeeds = new HashSet<string>(StringComparer.Ordinal);
    static bool hookedFirebaseReady;

    public static ProfileProgressState Load(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        string key = PrefsPrefix + profileId;
        if (!PlayerPrefs.HasKey(key))
            return SeedLocal(profileId);

        try
        {
            ProfileProgressState state = JsonUtility.FromJson<ProfileProgressState>(PlayerPrefs.GetString(key));
            if (state == null || state.modules == null || state.modules.Count == 0)
                return SeedLocal(profileId);

            state.profileId = profileId;
            EnsureAllModules(state);
            return state;
        }
        catch (Exception)
        {
            return SeedLocal(profileId);
        }
    }

    public static void SaveLocal(ProfileProgressState state)
    {
        if (state == null || string.IsNullOrEmpty(state.profileId))
            return;

        PlayerPrefs.SetString(PrefsPrefix + state.profileId, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }

    public static ProfileProgressState SeedLocal(string profileId)
    {
        var state = new ProfileProgressState
        {
            profileId = profileId,
            modules = new List<ModuleProgress>()
        };
        EnsureAllModules(state);
        SaveLocal(state);
        return state;
    }

    public static void EnsureAllModules(ProfileProgressState state)
    {
        if (state == null)
            return;

        for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
            ModuleProgressUtil.GetOrCreateModule(state, ProgressionConfig.AllModules[i]);
    }

    public static void SeedProfile(string profileId, bool pushToCloud = true)
    {
        EnsureFirebaseReadyHook();

        if (string.IsNullOrEmpty(profileId))
            return;

        ProfileProgressState state = Load(profileId) ?? SeedLocal(profileId);
        SaveLocal(state);
        ClinicScheduleService.EnsureSchedule(profileId);

        if (!pushToCloud)
            return;

        pendingCloudSeeds.Add(profileId);
        TryPushCloudSeed(profileId, state, ignoreAlreadySeededFlag: false);
    }

    public static void SeedModulesAfterUserDocExists(string profileId)
    {
        EnsureFirebaseReadyHook();
        if (string.IsNullOrEmpty(profileId))
            return;

        ClearCloudSeededFlag(profileId);
        pendingCloudSeeds.Add(profileId);

        ProfileProgressState state = Load(profileId) ?? SeedLocal(profileId);
        TryPushCloudSeed(profileId, state, ignoreAlreadySeededFlag: true);
    }

    public static void EnsureCloudSeededForAllProfiles()
    {
        EnsureFirebaseReadyHook();

        if (!FirebaseBootstrap.Ready)
            return;

        if (ProfileManager.Instance != null && ProfileManager.Instance.Profiles != null)
        {
            List<UserProfile> profiles = ProfileManager.Instance.Profiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                UserProfile profile = profiles[i];
                if (profile == null || string.IsNullOrEmpty(profile.id))
                    continue;

                ClearCloudSeededFlag(profile.id);
                pendingCloudSeeds.Add(profile.id);
                FirebaseProfileSync.UpsertProfile(profile, isNew: false);
            }
        }

        string currentId = CurrentProfileId();
        if (!string.IsNullOrEmpty(currentId))
        {
            ClearCloudSeededFlag(currentId);
            pendingCloudSeeds.Add(currentId);
        }

        FlushPendingProgressSync();
    }

    static void TryPushCloudSeed(
        string profileId,
        ProfileProgressState state,
        bool ignoreAlreadySeededFlag)
    {
        if (string.IsNullOrEmpty(profileId) || state == null)
            return;

        if (!ignoreAlreadySeededFlag && IsCloudSeeded(profileId) && HasCurrentSeedSchema(profileId))
        {
            pendingCloudSeeds.Remove(profileId);
            return;
        }

        if (!FirebaseBootstrap.Ready || !IsOnline())
            return;

        FirebaseProfileSync.SeedModuleProgress(profileId, state, success =>
        {
            if (success)
            {
                MarkCloudSeeded(profileId);
                pendingCloudSeeds.Remove(profileId);
                return;
            }

            Debug.LogWarning("[Progression] Cloud module seed failed for " + profileId + " — will retry.");
            pendingCloudSeeds.Add(profileId);
            ClearCloudSeededFlag(profileId);
        });
    }

    static bool IsCloudSeeded(string profileId)
    {
        return PlayerPrefs.GetInt(CloudSeededPrefix + profileId, 0) == 1;
    }

    static bool HasCurrentSeedSchema(string profileId)
    {
        return PlayerPrefs.GetInt(CloudSeedVersionPrefix + profileId, 0) >= CloudSeedSchemaVersion;
    }

    static void MarkCloudSeeded(string profileId)
    {
        PlayerPrefs.SetInt(CloudSeededPrefix + profileId, 1);
        PlayerPrefs.SetInt(CloudSeedVersionPrefix + profileId, CloudSeedSchemaVersion);
        PlayerPrefs.Save();
    }

    public static void ClearCloudSeededFlag(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return;

        PlayerPrefs.DeleteKey(CloudSeededPrefix + profileId);
        PlayerPrefs.DeleteKey(CloudSeedVersionPrefix + profileId);
        PlayerPrefs.Save();
        pendingCloudSeeds.Add(profileId);
    }

    static void EnsureFirebaseReadyHook()
    {
        if (hookedFirebaseReady)
            return;

        hookedFirebaseReady = true;
        FirebaseBootstrap.OnReady += OnFirebaseReady;
        if (FirebaseBootstrap.Ready)
            OnFirebaseReady();
    }

    static void OnFirebaseReady()
    {
        EnsureCloudSeededForAllProfiles();
        FlushPendingProgressSync();
    }

    public static ModuleProgress GetModule(string profileId, string moduleId)
    {
        ProfileProgressState state = Load(profileId);
        return ModuleProgressUtil.GetOrCreateModule(state, moduleId);
    }

    public static bool CanPlay(
        string profileId,
        string moduleId,
        int phase,
        out ProgressionBlockReason reason,
        out string message)
    {
        return CanPlay(profileId, moduleId, phase, ProgressionConfig.ModeUnguided, out reason, out message);
    }

    public static bool CanPlay(
        string profileId,
        string moduleId,
        int phase,
        string mode,
        out ProgressionBlockReason reason,
        out string message)
    {
        if (string.IsNullOrEmpty(profileId))
        {
            reason = ProgressionBlockReason.NoProfile;
            message = "No profile selected.";
            return false;
        }

        ProfileProgressState state = Load(profileId);
        ModuleProgress module = ModuleProgressUtil.GetOrCreateModule(state, moduleId);
        SaveLocal(state);
        string today = ProgressionConfig.TodayManila();
        if (!ProgressionGate.CanPlay(module, phase, today, mode, out reason, out message))
            return false;

        if (ProgressionConfig.IsUnguided(mode) &&
            !ProgressionGate.CanPlayClinicConsecutive(profileId, today, out reason, out message))
            return false;

        return true;
    }

    public static bool IsPhaseUnlocked(string profileId, string moduleId, int phase)
    {
        if (string.IsNullOrEmpty(profileId))
            return phase <= 1;

        ModuleProgress module = GetModule(profileId, moduleId);
        return ProgressionGate.IsPhaseUnlocked(module, phase);
    }

    public static bool RecordSession(
        string profileId,
        string moduleId,
        int phase,
        string mode,
        float primaryScore,
        Dictionary<string, object> metrics,
        out string message,
        out bool unlockedNextPhase)
    {
        unlockedNextPhase = false;
        message = null;

        if (string.IsNullOrEmpty(profileId))
        {
            message = "No profile selected.";
            return false;
        }

        EnsureFirebaseReadyHook();

        ProfileProgressState state = Load(profileId);
        ModuleProgress module = ModuleProgressUtil.GetOrCreateModule(state, moduleId);
        string today = ProgressionConfig.TodayManila();

        DaySessionSave day = ProgressionGate.RecordDaySession(
            module,
            phase,
            today,
            mode,
            primaryScore,
            metrics,
            out unlockedNextPhase,
            out message,
            out bool countsAsSessionDay);

        if (day == null)
            return false;

        // Always persist gates locally first (offline-safe).
        if (countsAsSessionDay)
        {
            SaveLocal(state);
            EnqueuePendingSync(profileId, moduleId, day.phase, day.playedOn);
            ClinicScheduleService.OnSimulationRecorded(profileId, moduleId, day.playedOn, day.phase);

            if (unlockedNextPhase)
                ClinicScheduleService.AppendPhaseWeekIfNeeded(profileId, moduleId, module.unlockedThroughPhase);

            if (FirebaseBootstrap.Ready && IsOnline())
                FirebaseProfileSync.PushModuleProgress(profileId, module, day);
            else
                Debug.Log("[Progression] Saved locally; cloud sync queued (offline or Firebase not ready).");
        }

        Debug.Log("[Progression] " + moduleId + " phase " + phase + " " + today + " score=" + primaryScore + " — " + message);
        return true;
    }

    static void EnqueuePendingSync(string profileId, string moduleId, int phase, string playedOn)
    {
        PendingProgressSyncList list = LoadPendingSync();
        for (int i = 0; i < list.items.Count; i++)
        {
            PendingProgressSyncItem existing = list.items[i];
            if (existing == null)
                continue;
            if (existing.profileId == profileId &&
                existing.moduleId == moduleId &&
                existing.phase == phase &&
                existing.playedOn == playedOn)
            {
                SavePendingSync(list);
                return;
            }
        }

        list.items.Add(new PendingProgressSyncItem
        {
            profileId = profileId,
            moduleId = moduleId,
            phase = phase,
            playedOn = playedOn
        });
        SavePendingSync(list);
    }

    public static void FlushPendingProgressSync()
    {
        if (!FirebaseBootstrap.Ready || !IsOnline())
            return;

        PendingProgressSyncList list = LoadPendingSync();
        if (list.items == null || list.items.Count == 0)
            return;

        var remaining = new List<PendingProgressSyncItem>();
        for (int i = 0; i < list.items.Count; i++)
        {
            PendingProgressSyncItem item = list.items[i];
            if (item == null || string.IsNullOrEmpty(item.profileId))
                continue;

            ProfileProgressState state = Load(item.profileId);
            ModuleProgress module = ModuleProgressUtil.GetOrCreateModule(state, item.moduleId);
            PhaseWeekProgress week = null;
            if (module.phaseWeeks != null)
            {
                for (int w = module.phaseWeeks.Count - 1; w >= 0; w--)
                {
                    if (module.phaseWeeks[w] != null && module.phaseWeeks[w].phase == item.phase)
                    {
                        week = module.phaseWeeks[w];
                        break;
                    }
                }
            }

            DaySessionSave day = ModuleProgressUtil.FindDay(week, item.playedOn);
            if (day == null)
                continue;

            try
            {
                FirebaseProfileSync.PushModuleProgress(item.profileId, module, day);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Progression] Pending sync push failed: " + ex.Message);
                remaining.Add(item);
            }
        }

        list.items = remaining;
        SavePendingSync(list);

        if (remaining.Count == 0)
            Debug.Log("[Progression] Pending cloud sync flushed.");
    }

    static PendingProgressSyncList LoadPendingSync()
    {
        if (!PlayerPrefs.HasKey(PendingSyncKey))
            return new PendingProgressSyncList();

        try
        {
            PendingProgressSyncList list = JsonUtility.FromJson<PendingProgressSyncList>(PlayerPrefs.GetString(PendingSyncKey));
            if (list == null)
                return new PendingProgressSyncList();
            if (list.items == null)
                list.items = new List<PendingProgressSyncItem>();
            return list;
        }
        catch
        {
            return new PendingProgressSyncList();
        }
    }

    static void SavePendingSync(PendingProgressSyncList list)
    {
        if (list == null)
            list = new PendingProgressSyncList();
        if (list.items == null)
            list.items = new List<PendingProgressSyncItem>();

        PlayerPrefs.SetString(PendingSyncKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    public static bool IsOnline()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    public static string CurrentProfileId()
    {
        if (ProfileManager.Instance == null || ProfileManager.Instance.CurrentProfile == null)
            return null;
        return ProfileManager.Instance.CurrentProfile.id;
    }

    /// <summary>
    /// Dev/test: SuperAdmin looks fully cleared — every module Phase I–IV week passed
    /// with 4 perfect Simulation sessions + results, clinic schedule marked Done, Firebase pushed.
    /// </summary>
    public static void EnsureSuperAdminProgress(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return;

        EnsureFirebaseReadyHook();

        ProfileProgressState state = Load(profileId) ?? SeedLocal(profileId);
        EnsureAllModules(state);

        string programStart = ProgressionConfig.TodayManila();
        if (ProgressionConfig.TryParseDay(programStart, out DateTime today))
        {
            // Anchor completion in the recent past so "today" is after the full program.
            // 4 phases × 7 days = 28 days ending yesterday.
            programStart = today.AddDays(-(ProgressionConfig.PhaseCount * ProgressionConfig.WeekLengthDays)).ToString("yyyy-MM-dd");
        }

        for (int i = 0; i < state.modules.Count; i++)
        {
            ModuleProgress module = state.modules[i];
            if (module == null || string.IsNullOrEmpty(module.moduleId))
                continue;

            FillPerfectModuleHistory(module, programStart);
            module.unlockedThroughPhase = ProgressionConfig.PhaseCount;
            module.currentPhase = ProgressionConfig.PhaseCount;
        }

        SaveLocal(state);

        // Reset clinic schedule to the same program window, then reconcile Done marks from playDates.
        ClinicScheduleService.ResetAndSeedFullProgram(profileId, programStart);

        ClearCloudSeededFlag(profileId);
        pendingCloudSeeds.Add(profileId);

        if (FirebaseBootstrap.Ready && IsOnline())
        {
            TryPushCloudSeed(profileId, state, ignoreAlreadySeededFlag: true);
            // Reload after schedule reset so cloud gets the reconciled Done plot.
            state = Load(profileId) ?? state;
            FirebaseProfileSync.PushFullProgressHistory(profileId, state);
            FirebaseProfileSync.PushClinicSchedule(ClinicScheduleService.EnsureSchedule(profileId));
            Debug.Log("[Progression] SuperAdmin perfect clear pushed for " + profileId);
        }
        else
        {
            Debug.Log("[Progression] SuperAdmin perfect clear saved locally; cloud push queued for " + profileId);
        }
    }

    static void FillPerfectModuleHistory(ModuleProgress module, string programStartYyyyMmDd)
    {
        if (module == null || !ProgressionConfig.TryParseDay(programStartYyyyMmDd, out DateTime start))
            return;

        module.phaseWeeks = new List<PhaseWeekProgress>();
        module.playDatesThisWeek = new List<string>();

        for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            DateTime weekStart = start.AddDays((phase - 1) * ProgressionConfig.WeekLengthDays);
            var week = new PhaseWeekProgress
            {
                phase = phase,
                weekStart = weekStart.ToString("yyyy-MM-dd"),
                playDates = new List<string>(),
                days = new List<DaySessionSave>(),
                weekEvaluated = false,
                weekPassed = false
            };

            // P-R-P-R-P-R-P session days at offsets 0,2,4,6.
            for (int slot = 1; slot <= ProgressionConfig.MaxPlayDaysPerWeek; slot++)
            {
                int dayOffset = (slot - 1) * 2;
                string playedOn = weekStart.AddDays(dayOffset).ToString("yyyy-MM-dd");
                DaySessionSave day = BuildPerfectDay(module.moduleId, phase, playedOn);
                week.playDates.Add(playedOn);
                week.days.Add(day);
            }

            ProgressionPassRules.EvaluateWeek(module.moduleId, week, out _);
            // Force pass for the test profile even if a rule edge-case trips.
            week.weekEvaluated = true;
            week.weekPassed = true;
            week.averageUnguidedScore = 100f;

            module.phaseWeeks.Add(week);

            if (phase == ProgressionConfig.PhaseCount)
            {
                module.currentWeekStart = week.weekStart;
                module.playDatesThisWeek = new List<string>(week.playDates);
            }
        }
    }

    static DaySessionSave BuildPerfectDay(string moduleId, int phase, string playedOn)
    {
        var metrics = new Dictionary<string, object>
        {
            { "accuracy", 100f },
            { "completionTime", 15f },
            { "timeUsed", 15f },
            { "averageReactionMs", 240f },
            { "averageReaction", 0.24f },
            { "avgDeviationMm", 0.4f },
            { "maxDeviationMm", 0.7f },
            { "interceptionRate", 100f },
            { "correct", 30 },
            { "incorrect", 0 },
            { "missed", 0 },
            { "success", 30 },
            { "fail", 0 },
            { "patternsCompleted", 9 },
            { "patternTotal", 9 }
        };

        var day = new DaySessionSave
        {
            playedOn = playedOn,
            phase = phase,
            mode = ProgressionConfig.ModeUnguided,
            primaryScore = 100f,
            metricsJson = "{}"
        };
        ProgressionPassRules.FillDayMetrics(day, 100f, metrics);
        day.passed = true;
        day.metricsJson =
            "{\"accuracy\":100,\"completionTime\":15,\"averageReactionMs\":240," +
            "\"avgDeviationMm\":0.4,\"maxDeviationMm\":0.7,\"interceptionRate\":100," +
            "\"correct\":30,\"incorrect\":0,\"missed\":0,\"success\":30,\"fail\":0," +
            "\"patternsCompleted\":9,\"patternTotal\":9,\"moduleId\":\"" + (moduleId ?? "") + "\"}";
        return day;
    }
}
