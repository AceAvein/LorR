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
    const string SessionsCollection = "sessions";
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
            return;

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

        db.Collection(UsersCollection).Document(profile.id)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[Firebase] Profile upsert failed: " + task.Exception);
                    return;
                }

                RememberKnownRemoteId(profile.id);
            });
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

    public static void PushSession(
        string profileId,
        string moduleId,
        int phase,
        string mode,
        bool passed,
        string playedOnYyyyMmDd,
        Dictionary<string, object> metrics = null)
    {
        if (!CanWrite() ||
            string.IsNullOrEmpty(profileId) ||
            string.IsNullOrEmpty(moduleId))
            return;

        var db = Db;
        if (db == null)
            return;

        string playedOn = string.IsNullOrEmpty(playedOnYyyyMmDd)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : playedOnYyyyMmDd;

        string sessionId = playedOn + "_" + phase + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        var data = new Dictionary<string, object>
        {
            { "playedOn", playedOn },
            { "phase", phase },
            { "mode", mode ?? "Unguided" },
            { "passed", passed },
            { "savedAt", FieldValue.ServerTimestamp }
        };

        if (metrics != null)
        {
            foreach (var kv in metrics)
            {
                if (kv.Key != null && !data.ContainsKey(kv.Key))
                    data[kv.Key] = kv.Value;
            }
        }

        DocumentReference moduleRef = db.Collection(UsersCollection)
            .Document(profileId)
            .Collection(ModulesCollection)
            .Document(moduleId);

        moduleRef.SetAsync(new Dictionary<string, object>
        {
            { "updatedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);

        moduleRef.Collection(SessionsCollection).Document(sessionId)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogWarning("[Firebase] Session push failed: " + task.Exception);
            });
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
