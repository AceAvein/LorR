using System;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Bidirectional profile sync with Firestore.
/// Push: game create/update/delete → cloud.
/// Pull: cloud (incl. Console edits) → game via listener + server poll. Firebase wins.
/// </summary>
public static class FirebaseProfileSync
{
    const string UsersCollection = "users";
    const string DevicesCollection = "devices";
    const string ModulesCollection = "modules";
    const string KnownRemoteIdsKey = "Firebase.KnownUserIds";

    static ListenerRegistration usersListener;
    static bool initialSnapshotHandled;
    static bool pullInFlight;
    static bool permissionDeniedLogged;
    static HashSet<string> lastQueryIds = new HashSet<string>(StringComparer.Ordinal);

    static FirebaseFirestore Db
    {
        get
        {
            try { return FirebaseFirestore.DefaultInstance; }
            catch (Exception ex)
            {
                Debug.LogWarning("[Firebase] Firestore unavailable: " + ex.Message);
                return null;
            }
        }
    }

    static Query DeviceUsersQuery
    {
        get
        {
            var db = Db;
            if (db == null || string.IsNullOrEmpty(FirebaseBootstrap.DeviceId))
                return null;

            return db.Collection(UsersCollection)
                .WhereEqualTo("deviceId", FirebaseBootstrap.DeviceId);
        }
    }

    public static void StartSyncFromCloud()
    {
        if (!CanWrite())
            return;

        if (DeviceUsersQuery == null)
            return;

        StopSync();
        initialSnapshotHandled = false;
        lastQueryIds = new HashSet<string>(StringComparer.Ordinal);

        Query query = DeviceUsersQuery;

        try
        {
            usersListener = query.Listen(snapshot =>
            {
                FirebaseBootstrap.RunOnMainThread(() => ApplyQuerySnapshot(snapshot, fromListen: true));
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Firebase] Listen failed to start: " + ex.Message);
        }

        // Immediate server pull + short poll so Console edits always land even if Listen is flaky.
        PullOnceFromServer();
        FirebaseBootstrap.StartProfilePoll();

        Debug.Log("[Firebase] Listening + polling users for deviceId=" + FirebaseBootstrap.DeviceId);
    }

    public static void StopSync()
    {
        FirebaseBootstrap.StopProfilePoll();

        if (usersListener != null)
        {
            usersListener.Stop();
            usersListener = null;
        }

        initialSnapshotHandled = false;
        pullInFlight = false;
    }

    /// <summary>Force a server read (bypasses local cache when possible).</summary>
    public static void PullOnceFromServer()
    {
        if (!CanWrite() || pullInFlight)
            return;

        Query query = DeviceUsersQuery;
        if (query == null)
            return;

        pullInFlight = true;

        query.GetSnapshotAsync(Source.Server)
            .ContinueWithOnMainThread(task =>
            {
                pullInFlight = false;

                if (task.IsFaulted || task.IsCanceled)
                {
                    HandlePullPermissionOrError(task.Exception);
                    return;
                }

                permissionDeniedLogged = false;
                if (task.Result != null)
                    ApplyQuerySnapshot(task.Result, fromListen: false);
            });
    }

    static void HandlePullPermissionOrError(Exception exception)
    {
        string message = exception != null ? exception.ToString() : string.Empty;
        bool denied = message.IndexOf("insufficient permissions", StringComparison.OrdinalIgnoreCase) >= 0
                      || message.IndexOf("PermissionDenied", StringComparison.OrdinalIgnoreCase) >= 0;

        if (denied)
        {
            FirebaseBootstrap.StopProfilePoll();
            if (permissionDeniedLogged)
                return;

            permissionDeniedLogged = true;
            Debug.LogWarning(
                "[Firebase] Firestore READ blocked by Security Rules. " +
                "In Firebase Console → Firestore → Rules, paste firestore.test.rules (or firestore.rules) and click Publish. " +
                "Writes can work while queries/pulls still fail if read is denied.");
            return;
        }

        Debug.LogWarning("[Firebase] Pull failed: " + exception);
    }

    static void ApplyQuerySnapshot(QuerySnapshot snapshot, bool fromListen)
    {
        if (ProfileManager.Instance == null || snapshot == null)
            return;

        var remoteIds = new HashSet<string>(StringComparer.Ordinal);
        int applied = 0;

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            UserProfile remote = FromDocument(doc);
            if (remote == null || string.IsNullOrEmpty(remote.id))
                continue;

            remoteIds.Add(remote.id);
            if (ProfileManager.Instance.ApplyRemoteUpsert(remote))
            {
                applied++;
                Debug.Log("[Firebase] Remote profile applied: " + remote.id + " → " + remote.name);
            }

            RememberKnownRemoteId(remote.id);
        }

        if (!initialSnapshotHandled)
        {
            initialSnapshotHandled = true;
            HandleInitialOrphansAndDeletes(remoteIds);
            PullDeviceCurrentProfile();
            Debug.Log("[Firebase] Initial cloud sync done. remoteCount=" + remoteIds.Count);
        }
        else
        {
            // Docs that left this device query were deleted or deviceId changed in Console.
            foreach (string previousId in lastQueryIds)
            {
                if (remoteIds.Contains(previousId))
                    continue;

                ProfileManager.Instance.ApplyRemoteDelete(previousId);
                ForgetKnownRemoteId(previousId);
                Debug.Log("[Firebase] Remote profile removed: " + previousId);
            }
        }

        lastQueryIds = remoteIds;
        SaveKnownRemoteIds(remoteIds);

        if (applied > 0 && fromListen)
            Debug.Log("[Firebase] Listen snapshot applied changes=" + applied);
    }

    static void HandleInitialOrphansAndDeletes(HashSet<string> remoteIds)
    {
        HashSet<string> previouslyKnown = LoadKnownRemoteIds();
        List<UserProfile> locals = new List<UserProfile>(ProfileManager.Instance.Profiles);

        for (int i = 0; i < locals.Count; i++)
        {
            UserProfile local = locals[i];
            if (local == null || string.IsNullOrEmpty(local.id))
                continue;
            if (remoteIds.Contains(local.id))
                continue;

            if (previouslyKnown.Contains(local.id))
            {
                ProfileManager.Instance.ApplyRemoteDelete(local.id);
                continue;
            }

            UpsertProfile(local, isNew: true);
            remoteIds.Add(local.id);
        }
    }

    static void PullDeviceCurrentProfile()
    {
        var db = Db;
        if (db == null || ProfileManager.Instance == null)
            return;

        db.Collection(DevicesCollection).Document(FirebaseBootstrap.DeviceId)
            .GetSnapshotAsync(Source.Server)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.Result == null || !task.Result.Exists)
                    return;

                string currentId = GetString(task.Result, "currentProfileId", string.Empty);
                if (string.IsNullOrEmpty(currentId))
                    return;

                UserProfile profile = ProfileManager.Instance.FindProfileById(currentId);
                if (profile != null)
                    ProfileManager.Instance.SelectProfile(profile, syncRemote: false);
            });
    }

    public static UserProfile FromDocument(DocumentSnapshot doc)
    {
        if (doc == null || !doc.Exists)
            return null;

        // Prefer displayName; accept "name" if Console/manual docs use that key.
        string rawName = GetString(doc, "displayName", string.Empty);
        if (string.IsNullOrEmpty(rawName))
            rawName = GetString(doc, "name", string.Empty);

        string name = ProfileManager.SanitizeName(rawName);
        if (string.IsNullOrEmpty(name))
            return null;

        return new UserProfile
        {
            id = doc.Id,
            name = name,
            imagePath = GetString(doc, "imagePath", string.Empty),
            avatarIndex = Mathf.Max(0, GetInt(doc, "avatarIndex", 0)),
            nonDominantIsLeft = GetBool(doc, "nonDominantIsLeft", true)
        };
    }

    public static void UpsertProfile(UserProfile profile, bool isNew = false)
    {
        if (!CanWrite() || profile == null || string.IsNullOrEmpty(profile.id))
        {
            // Queue cloud seed for when Auth is ready; user doc must exist first.
            if (profile != null && !string.IsNullOrEmpty(profile.id))
                ProgressionService.SeedProfile(profile.id, pushToCloud: true);
            return;
        }

        var db = Db;
        if (db == null)
            return;

        var data = new Dictionary<string, object>
        {
            { "deviceId", FirebaseBootstrap.DeviceId },
            { "displayName", profile.name ?? string.Empty },
            { "nonDominantIsLeft", profile.nonDominantIsLeft },
            { "avatarIndex", profile.avatarIndex },
            { "imagePath", profile.imagePath ?? string.Empty },
            { "updatedAt", FieldValue.ServerTimestamp }
        };

        if (isNew)
            data["createdAt"] = FieldValue.ServerTimestamp;

        string profileId = profile.id;
        db.Collection(UsersCollection).Document(profileId)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[Firebase] Profile upsert failed: " + task.Exception);
                    return;
                }

                RememberKnownRemoteId(profileId);
                // Modules rules require the parent user doc to exist — seed only after upsert succeeds.
                ProgressionService.SeedModulesAfterUserDocExists(profileId);
                ClinicScheduleService.EnsureSchedule(profileId);
            });
    }

    public static void PushClinicSchedule(ClinicScheduleState state)
    {
        if (!CanWrite() || state == null || string.IsNullOrEmpty(state.profileId) || state.days == null)
            return;

        var db = Db;
        if (db == null)
            return;

        DocumentReference userRef = db.Collection(UsersCollection).Document(state.profileId);
        userRef.SetAsync(new Dictionary<string, object>
        {
            { "trainingStartDate", state.trainingStartDate ?? string.Empty },
            { "updatedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);

        WriteBatch batch = db.StartBatch();
        int ops = 0;
        for (int i = 0; i < state.days.Count; i++)
        {
            ClinicDayPlan day = state.days[i];
            if (day == null || string.IsNullOrEmpty(day.date))
                continue;

            DocumentReference dayRef = userRef.Collection("clinicSchedule").Document(day.date);
            batch.Set(dayRef, new Dictionary<string, object>
            {
                { "date", day.date },
                { "type", day.type == (int)ClinicDayPlanType.Session ? "session" : "rest" },
                { "slot", day.slot },
                { "phase", day.phase },
                { "updatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);
            ops++;

            // Firestore batch max 500; clinic weeks are small.
            if (ops >= 400)
            {
                batch.CommitAsync();
                batch = db.StartBatch();
                ops = 0;
            }
        }

        if (ops > 0)
        {
            batch.CommitAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Firebase] Clinic schedule push failed: " + task.Exception);
                else
                    Debug.Log("[Firebase] Clinic schedule synced (" + state.days.Count + " days)");
            });
        }
    }

    public static void DeleteProfile(string profileId)
    {
        if (!CanWrite() || string.IsNullOrEmpty(profileId))
            return;

        var db = Db;
        if (db == null)
            return;

        ForgetKnownRemoteId(profileId);

        db.Collection(UsersCollection).Document(profileId)
            .DeleteAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Firebase] Profile delete failed: " + task.Exception);
            });
    }

    public static void SetCurrentProfile(string profileId)
    {
        if (!CanWrite())
            return;

        var db = Db;
        if (db == null)
            return;

        var data = new Dictionary<string, object>
        {
            { "currentProfileId", profileId ?? string.Empty },
            { "updatedAt", FieldValue.ServerTimestamp }
        };

        db.Collection(DevicesCollection).Document(FirebaseBootstrap.DeviceId)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Firebase] Device current profile failed: " + task.Exception);
            });
    }

    /// <summary>
    /// Create modules/phases skeleton. Commits in one batch.
    /// onComplete(true) only after Firestore accepts the write.
    /// </summary>
    public static bool SeedModuleProgress(
        string profileId,
        ProfileProgressState state,
        Action<bool> onComplete = null)
    {
        if (!CanWrite() || string.IsNullOrEmpty(profileId) || state == null)
        {
            onComplete?.Invoke(false);
            return false;
        }

        var db = Db;
        if (db == null)
        {
            onComplete?.Invoke(false);
            return false;
        }

        ProgressionService.EnsureAllModules(state);

        WriteBatch batch = db.StartBatch();
        int ops = 0;

        for (int i = 0; i < state.modules.Count; i++)
        {
            ModuleProgress module = state.modules[i];
            if (module == null || string.IsNullOrEmpty(module.moduleId))
                continue;

            DocumentReference moduleRef = db.Collection(UsersCollection)
                .Document(profileId)
                .Collection(ModulesCollection)
                .Document(module.moduleId);

            batch.Set(moduleRef, new Dictionary<string, object>
            {
                { "moduleId", module.moduleId },
                { "unlockedThroughPhase", module.unlockedThroughPhase },
                { "currentPhase", module.currentPhase },
                { "currentWeekStart", module.currentWeekStart ?? string.Empty },
                { "playDatesThisWeek", module.playDatesThisWeek ?? new List<string>() },
                { "updatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);
            ops++;

            for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
            {
                DocumentReference phaseRef = moduleRef.Collection("phases").Document(phase.ToString());
                batch.Set(phaseRef, BuildEmptyPhaseFields(phase), SetOptions.MergeAll);
                ops++;

                // 4 visible session slots under days/ so Console shows structure before play.
                for (int slot = 1; slot <= ProgressionConfig.MaxPlayDaysPerWeek; slot++)
                {
                    DocumentReference dayRef = phaseRef.Collection("days").Document("sessionSlot" + slot);
                    batch.Set(dayRef, BuildEmptyDayFields(phase, slot), SetOptions.MergeAll);
                    ops++;
                }
            }
        }

        if (ops == 0)
        {
            onComplete?.Invoke(false);
            return false;
        }

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning("[Firebase] Module seed FAILED for " + profileId + ": " + task.Exception);
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log("[Firebase] Seeded module progression for " + profileId + " (" + ops + " writes)");
            onComplete?.Invoke(true);
        });

        return true;
    }

    /// <summary>Push module meta + single day overwrite under phases/{phase}/days/{date}.</summary>
    public static void PushModuleProgress(string profileId, ModuleProgress module, DaySessionSave day)
    {
        if (!CanWrite() || string.IsNullOrEmpty(profileId) || module == null || day == null)
            return;

        var db = Db;
        if (db == null)
            return;

        WriteModuleDoc(db, profileId, module);

        PhaseWeekProgress active = null;
        if (module.phaseWeeks != null)
        {
            for (int i = module.phaseWeeks.Count - 1; i >= 0; i--)
            {
                PhaseWeekProgress week = module.phaseWeeks[i];
                if (week != null && week.phase == day.phase)
                {
                    active = week;
                    break;
                }
            }
        }

        DocumentReference phaseRef = db.Collection(UsersCollection)
            .Document(profileId)
            .Collection(ModulesCollection)
            .Document(module.moduleId)
            .Collection("phases")
            .Document(day.phase.ToString());

        phaseRef.SetAsync(new Dictionary<string, object>
        {
            { "phase", day.phase },
            { "weekStart", active != null ? (active.weekStart ?? string.Empty) : string.Empty },
            { "playDates", active != null && active.playDates != null ? active.playDates : new List<string>() },
            { "weekEvaluated", active != null && active.weekEvaluated },
            { "weekPassed", active != null && active.weekPassed },
            { "averageUnguidedScore", active != null ? active.averageUnguidedScore : 0f },
            { "sessionSlots", ProgressionConfig.MaxPlayDaysPerWeek },
            { "updatedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);

        // Fill pre-seeded sessionSlot1..4 (do not create parallel yyyy-MM-dd docs).
        int slot = ResolveSessionSlot(active, day.playedOn);
        var dayData = new Dictionary<string, object>
        {
            { "placeholder", false },
            { "sessionSlot", slot },
            { "playedOn", day.playedOn ?? string.Empty },
            { "phase", day.phase },
            { "mode", day.mode ?? ProgressionConfig.ModeUnguided },
            { "primaryScore", day.primaryScore },
            { "passed", day.passed },
            { "accuracy", day.accuracy },
            { "completionTimeSec", day.completionTimeSec },
            { "averageReactionMs", day.averageReactionMs },
            { "avgDeviationMm", day.avgDeviationMm },
            { "maxDeviationMm", day.maxDeviationMm },
            { "interceptionRate", day.interceptionRate },
            { "correct", day.correct },
            { "incorrect", day.incorrect },
            { "missed", day.missed },
            { "success", day.success },
            { "fail", day.fail },
            { "patternsCompleted", day.patternsCompleted },
            { "patternTotal", day.patternTotal },
            { "metricsJson", day.metricsJson ?? "{}" },
            { "savedAt", FieldValue.ServerTimestamp }
        };

        phaseRef.Collection("days").Document("sessionSlot" + slot)
            .SetAsync(dayData, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Firebase] Day save failed: " + task.Exception);
                else
                    Debug.Log("[Firebase] Session slot " + slot + " saved for phase " + day.phase + " (" + day.playedOn + ")");
            });
    }

    static int ResolveSessionSlot(PhaseWeekProgress week, string playedOn)
    {
        if (week != null && week.playDates != null && !string.IsNullOrEmpty(playedOn))
        {
            int index = week.playDates.IndexOf(playedOn);
            if (index >= 0)
                return Mathf.Clamp(index + 1, 1, ProgressionConfig.MaxPlayDaysPerWeek);
            return Mathf.Clamp(week.playDates.Count, 1, ProgressionConfig.MaxPlayDaysPerWeek);
        }

        return 1;
    }

    static void WriteModuleDoc(FirebaseFirestore db, string profileId, ModuleProgress module)
    {
        db.Collection(UsersCollection)
            .Document(profileId)
            .Collection(ModulesCollection)
            .Document(module.moduleId)
            .SetAsync(new Dictionary<string, object>
            {
                { "moduleId", module.moduleId },
                { "unlockedThroughPhase", module.unlockedThroughPhase },
                { "currentPhase", module.currentPhase },
                { "currentWeekStart", module.currentWeekStart ?? string.Empty },
                { "playDatesThisWeek", module.playDatesThisWeek ?? new List<string>() },
                { "updatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);
    }

    static Dictionary<string, object> BuildEmptyPhaseFields(int phase)
    {
        return new Dictionary<string, object>
        {
            { "phase", phase },
            { "weekStart", string.Empty },
            { "playDates", new List<string>() },
            { "weekEvaluated", false },
            { "weekPassed", false },
            { "averageUnguidedScore", 0f },
            { "sessionSlots", ProgressionConfig.MaxPlayDaysPerWeek },
            { "updatedAt", FieldValue.ServerTimestamp }
        };
    }

    static Dictionary<string, object> BuildEmptyDayFields(int phase, int slot)
    {
        return new Dictionary<string, object>
        {
            { "placeholder", true },
            { "sessionSlot", slot },
            { "playedOn", string.Empty },
            { "phase", phase },
            { "mode", string.Empty },
            { "primaryScore", 0f },
            { "passed", false },
            { "accuracy", 0f },
            { "completionTimeSec", 0f },
            { "averageReactionMs", 0f },
            { "avgDeviationMm", 0f },
            { "maxDeviationMm", 0f },
            { "interceptionRate", 0f },
            { "correct", 0 },
            { "incorrect", 0 },
            { "missed", 0 },
            { "success", 0 },
            { "fail", 0 },
            { "patternsCompleted", 0 },
            { "patternTotal", 0 },
            { "metricsJson", "{}" },
            { "savedAt", FieldValue.ServerTimestamp }
        };
    }

    /// <summary>Legacy helper → day-overwrite progression.</summary>
    public static void PushSession(
        string profileId,
        string moduleId,
        int phase,
        string mode,
        bool passed,
        string playedOnYyyyMmDd,
        Dictionary<string, object> metrics = null)
    {
        float score = 0f;
        if (metrics != null)
        {
            if (metrics.TryGetValue("primaryScore", out object primary) && primary != null)
                float.TryParse(primary.ToString(), out score);
            else if (metrics.TryGetValue("accuracy", out object accuracy) && accuracy != null)
                float.TryParse(accuracy.ToString(), out score);
        }
        else if (passed)
        {
            score = ProgressionConfig.PassAverageForPhase(phase);
        }

        ProgressionService.RecordSession(
            profileId,
            moduleId,
            phase,
            mode,
            score,
            metrics,
            out _,
            out _);
    }

    static bool CanWrite()
    {
        return FirebaseBootstrap.Ready && !string.IsNullOrEmpty(FirebaseBootstrap.DeviceId);
    }

    static HashSet<string> LoadKnownRemoteIds()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        string raw = PlayerPrefs.GetString(KnownRemoteIdsKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
            return set;

        string[] parts = raw.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
                set.Add(parts[i]);
        }

        return set;
    }

    static void SaveKnownRemoteIds(HashSet<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            PlayerPrefs.DeleteKey(KnownRemoteIdsKey);
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetString(KnownRemoteIdsKey, string.Join(",", ids));
        PlayerPrefs.Save();
    }

    static void RememberKnownRemoteId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        HashSet<string> set = LoadKnownRemoteIds();
        if (set.Add(id))
            SaveKnownRemoteIds(set);
    }

    static void ForgetKnownRemoteId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        HashSet<string> set = LoadKnownRemoteIds();
        if (set.Remove(id))
            SaveKnownRemoteIds(set);
    }

    static string GetString(DocumentSnapshot doc, string field, string fallback)
    {
        if (doc == null || !doc.ContainsField(field))
            return fallback;

        try
        {
            string value = doc.GetValue<string>(field);
            return value ?? fallback;
        }
        catch
        {
            try
            {
                object raw = doc.GetValue<object>(field);
                return raw != null ? raw.ToString() : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }

    static int GetInt(DocumentSnapshot doc, string field, int fallback)
    {
        if (doc == null || !doc.ContainsField(field))
            return fallback;

        try { return doc.GetValue<int>(field); }
        catch
        {
            try { return Convert.ToInt32(doc.GetValue<object>(field)); }
            catch { return fallback; }
        }
    }

    static bool GetBool(DocumentSnapshot doc, string field, bool fallback)
    {
        if (doc == null || !doc.ContainsField(field))
            return fallback;

        try { return doc.GetValue<bool>(field); }
        catch
        {
            try { return Convert.ToBoolean(doc.GetValue<object>(field)); }
            catch { return fallback; }
        }
    }
}
