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
}
