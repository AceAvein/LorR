using System;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Pushes local profiles / sessions to Firestore.
/// Shape:
///   users/{profileId}  — patient profile
///   devices/{authUid}  — headset current selection
///   users/{profileId}/modules/{moduleId}/sessions/{sessionId}
/// </summary>
public static class FirebaseProfileSync
{
    const string UsersCollection = "users";
    const string DevicesCollection = "devices";
    const string ModulesCollection = "modules";
    const string SessionsCollection = "sessions";

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
                    Debug.LogWarning("[Firebase] Profile upsert failed: " + task.Exception);
            });
    }

    public static void DeleteProfile(string profileId)
    {
        if (!CanWrite() || string.IsNullOrEmpty(profileId))
            return;

        var db = Db;
        if (db == null)
            return;

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
    /// Append one completed session under a module. Call after Unguided (or Guided) results.
    /// </summary>
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

    public static void SyncAll(IList<UserProfile> profiles, UserProfile current)
    {
        if (!CanWrite() || profiles == null)
            return;

        for (int i = 0; i < profiles.Count; i++)
            UpsertProfile(profiles[i], false);

        SetCurrentProfile(current != null ? current.id : null);
    }

    static bool CanWrite()
    {
        return FirebaseBootstrap.Ready && !string.IsNullOrEmpty(FirebaseBootstrap.DeviceId);
    }
}
