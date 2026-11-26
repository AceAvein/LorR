using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class UserProfile
{
    public string id;
    public string name;
    public string imagePath;
    public int avatarIndex;
    public bool nonDominantIsLeft = true;
}

[Serializable]
public class ProfileListWrapper
{
    public List<UserProfile> profiles = new List<UserProfile>();
}

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    public const int MaxNameLength = 16;
    const string SaveKey = "UserProfiles";
    const string CurrentKey = "UserProfiles.Current";

    public List<UserProfile> Profiles { get; private set; } = new List<UserProfile>();
    public UserProfile CurrentProfile { get; private set; }

    /// <summary>Raised after local list/current changes (including Firebase pull).</summary>
    public static event Action ProfilesChanged;

    bool applyingRemote;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfiles();
        FirebaseBootstrap.EnsureExists();
        FirebaseBootstrap.OnReady += OnFirebaseReady;
        if (FirebaseBootstrap.Ready)
            OnFirebaseReady();
    }

    void OnDestroy()
    {
        FirebaseBootstrap.OnReady -= OnFirebaseReady;
        FirebaseProfileSync.StopSync();
        if (Instance == this)
            Instance = null;
    }

    void OnFirebaseReady()
    {
        EnsureAllProfileIds(saveIfChanged: true);
        // Pull + live listen (Firebase wins). Do not blind-push SyncAll — that overwrote Console edits.
        FirebaseProfileSync.StartSyncFromCloud();
    }

    public void LoadProfiles()
    {
        Profiles.Clear();
        CurrentProfile = null;

        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    ProfileListWrapper wrapper = JsonUtility.FromJson<ProfileListWrapper>(json);
                    if (wrapper != null && wrapper.profiles != null)
                        Profiles = wrapper.profiles;
                }
                catch (Exception)
                {
                    Profiles = new List<UserProfile>();
                }
            }
        }

        RemoveInvalidProfiles();
        EnsureAllProfileIds(saveIfChanged: false);
        RestoreCurrentProfile();
    }

    public void SaveProfiles()
    {
        EnsureAllProfileIds(saveIfChanged: false);

        ProfileListWrapper wrapper = new ProfileListWrapper
        {
            profiles = Profiles ?? new List<UserProfile>()
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        if (CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.name))
            PlayerPrefs.SetString(CurrentKey, CurrentProfile.name);
        else
            PlayerPrefs.DeleteKey(CurrentKey);

        PlayerPrefs.Save();
    }

    public static string SanitizeName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return string.Empty;

        string trimmed = playerName.Trim();
        var builder = new StringBuilder(trimmed.Length);
        bool pendingSpace = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
            if (builder.Length >= MaxNameLength)
                break;
        }

        return builder.ToString();
    }

    public bool ProfileNameExists(string playerName)
    {
        return FindProfile(playerName) != null;
    }

    public UserProfile FindProfile(string playerName)
    {
        string sanitized = SanitizeName(playerName);
        if (string.IsNullOrEmpty(sanitized))
            return null;

        for (int i = 0; i < Profiles.Count; i++)
        {
            UserProfile profile = Profiles[i];
            if (profile != null &&
                !string.IsNullOrEmpty(profile.name) &&
                profile.name.Equals(sanitized, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }

    public UserProfile FindProfileById(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        for (int i = 0; i < Profiles.Count; i++)
        {
            UserProfile profile = Profiles[i];
            if (profile != null &&
                !string.IsNullOrEmpty(profile.id) &&
                profile.id.Equals(profileId, StringComparison.Ordinal))
            {
                return profile;
            }
        }

        return null;
    }

    /// <summary>Apply a cloud/Console upsert. Firebase wins field values. Returns true if local data changed.</summary>
    public bool ApplyRemoteUpsert(UserProfile remote)
    {
        if (remote == null || string.IsNullOrEmpty(remote.id))
            return false;

        applyingRemote = true;
        try
        {
            remote.name = SanitizeName(remote.name);
            if (string.IsNullOrEmpty(remote.name))
                return false;

            ResolveNameConflict(remote);

            UserProfile local = FindProfileById(remote.id);
            bool changed;
            if (local == null)
            {
                Profiles.Add(remote);
                local = remote;
                changed = true;
            }
            else
            {
                changed =
                    !string.Equals(local.name, remote.name, StringComparison.Ordinal) ||
                    !string.Equals(local.imagePath ?? string.Empty, remote.imagePath ?? string.Empty, StringComparison.Ordinal) ||
                    local.avatarIndex != Mathf.Max(0, remote.avatarIndex) ||
                    local.nonDominantIsLeft != remote.nonDominantIsLeft;

                if (changed)
                {
                    local.name = remote.name;
                    local.imagePath = remote.imagePath ?? string.Empty;
                    local.avatarIndex = Mathf.Max(0, remote.avatarIndex);
                    local.nonDominantIsLeft = remote.nonDominantIsLeft;
                }
            }

            if (!changed)
                return false;

            if (CurrentProfile != null &&
                !string.IsNullOrEmpty(CurrentProfile.id) &&
                CurrentProfile.id.Equals(local.id, StringComparison.Ordinal))
            {
                CurrentProfile = local;
            }

            SaveProfiles();
            NotifyProfilesChanged();
            return true;
        }
        finally
        {
            applyingRemote = false;
        }
    }

    /// <summary>Apply a cloud/Console delete.</summary>
    public void ApplyRemoteDelete(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return;

        UserProfile stored = FindProfileById(profileId);
        if (stored == null)
            return;

        applyingRemote = true;
        try
        {
            Profiles.Remove(stored);
            if (CurrentProfile == stored ||
                (CurrentProfile != null &&
                 !string.IsNullOrEmpty(CurrentProfile.id) &&
                 CurrentProfile.id.Equals(profileId, StringComparison.Ordinal)))
            {
                CurrentProfile = null;
            }

            SaveProfiles();
            NotifyProfilesChanged();
        }
        finally
        {
            applyingRemote = false;
        }
    }

    public bool AddProfile(string playerName, string imagePath)
    {
        return AddProfile(playerName, imagePath, 0, true);
    }

    public bool AddProfile(string playerName, string imagePath, int avatarIndex, bool nonDominantIsLeft)
    {
        playerName = SanitizeName(playerName);
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Profile name is empty.");
            return false;
        }

        if (ProfileNameExists(playerName))
        {
            Debug.LogWarning("Profile name already exists: " + playerName);
            return false;
        }

        UserProfile profile = new UserProfile
        {
            id = NewProfileId(),
            name = playerName,
            imagePath = imagePath ?? string.Empty,
            avatarIndex = Mathf.Max(0, avatarIndex),
            nonDominantIsLeft = nonDominantIsLeft
        };

        Profiles.Add(profile);
        SaveProfiles();
        if (!applyingRemote)
            FirebaseProfileSync.UpsertProfile(profile, isNew: true);
        NotifyProfilesChanged();
        return true;
    }

    public void DeleteProfile(UserProfile profile)
    {
        if (profile == null)
            return;

        UserProfile stored = Profiles.Contains(profile) ? profile : FindProfile(profile.name);
        if (stored == null)
            return;

        string deletedId = stored.id;

        Profiles.Remove(stored);
        if (CurrentProfile == stored ||
            (CurrentProfile != null &&
             !string.IsNullOrEmpty(stored.name) &&
             stored.name.Equals(CurrentProfile.name, StringComparison.OrdinalIgnoreCase)))
        {
            CurrentProfile = null;
        }

        SaveProfiles();
        if (!applyingRemote)
        {
            FirebaseProfileSync.DeleteProfile(deletedId);
            FirebaseProfileSync.SetCurrentProfile(CurrentProfile != null ? CurrentProfile.id : null);
        }

        NotifyProfilesChanged();
    }

    public void SelectProfile(UserProfile profile, bool syncRemote = true)
    {
        if (profile == null)
        {
            CurrentProfile = null;
            SaveProfiles();
            if (syncRemote && !applyingRemote)
                FirebaseProfileSync.SetCurrentProfile(null);
            NotifyProfilesChanged();
            return;
        }

        UserProfile stored = Profiles.Contains(profile)
            ? profile
            : (FindProfileById(profile.id) ?? FindProfile(profile.name));
        CurrentProfile = stored;
        SaveProfiles();
        if (syncRemote && !applyingRemote && stored != null)
            FirebaseProfileSync.SetCurrentProfile(stored.id);
        NotifyProfilesChanged();
    }

    public void UpdateProfile(UserProfile profile)
    {
        if (profile == null)
            return;

        UserProfile stored = Profiles.Contains(profile)
            ? profile
            : (FindProfileById(profile.id) ?? FindProfile(profile.name));
        if (stored == null)
            return;

        EnsureProfileId(stored);
        SaveProfiles();
        if (!applyingRemote)
            FirebaseProfileSync.UpsertProfile(stored, isNew: false);
        NotifyProfilesChanged();
    }

    public int GetProfileCount()
    {
        return Profiles.Count;
    }

    public UserProfile GetProfile(int index)
    {
        if (index < 0 || index >= Profiles.Count)
            return null;

        return Profiles[index];
    }

    public static bool TryGetNonDominantLeft(out bool left)
    {
        left = true;
        if (Instance == null || Instance.CurrentProfile == null)
            return false;

        left = Instance.CurrentProfile.nonDominantIsLeft;
        return true;
    }

    /// <summary>Profile non-dominant hand is the only training hand for all modules.</summary>
    public static bool IsNonDominantLeft()
    {
        TryGetNonDominantLeft(out bool left);
        return left;
    }

    public static bool MatchesNonDominantHand(bool interactorIsLeft)
    {
        return interactorIsLeft == IsNonDominantLeft();
    }

    void RemoveInvalidProfiles()
    {
        for (int i = Profiles.Count - 1; i >= 0; i--)
        {
            UserProfile profile = Profiles[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.name))
            {
                Profiles.RemoveAt(i);
                continue;
            }

            profile.name = SanitizeName(profile.name);
            if (string.IsNullOrEmpty(profile.name))
                Profiles.RemoveAt(i);
        }
    }

    void RestoreCurrentProfile()
    {
        if (!PlayerPrefs.HasKey(CurrentKey))
            return;

        CurrentProfile = FindProfile(PlayerPrefs.GetString(CurrentKey));
    }

    void EnsureAllProfileIds(bool saveIfChanged)
    {
        bool changed = false;
        for (int i = 0; i < Profiles.Count; i++)
        {
            if (EnsureProfileId(Profiles[i]))
                changed = true;
        }

        if (changed && saveIfChanged)
            SaveProfiles();
    }

    static bool EnsureProfileId(UserProfile profile)
    {
        if (profile == null)
            return false;

        if (!string.IsNullOrEmpty(profile.id))
            return false;

        profile.id = NewProfileId();
        return true;
    }

    static string NewProfileId()
    {
        return Guid.NewGuid().ToString("N");
    }

    void ResolveNameConflict(UserProfile remote)
    {
        UserProfile other = FindProfile(remote.name);
        if (other == null || other.id == remote.id)
            return;

        // Keep Firebase name on remote; nudge the other local name so Create still works.
        string baseName = other.name;
        if (baseName.Length > MaxNameLength - 5)
            baseName = baseName.Substring(0, MaxNameLength - 5);

        string suffix = string.IsNullOrEmpty(other.id) ? "2" : other.id.Substring(0, Math.Min(4, other.id.Length));
        other.name = SanitizeName(baseName + suffix);
        if (string.IsNullOrEmpty(other.name) || FindProfile(other.name) == other)
            return;

        other.name = SanitizeName(baseName + "x" + suffix);
    }

    void NotifyProfilesChanged()
    {
        ProfilesChanged?.Invoke();
    }
}
