using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class UserProfile
{
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
        RestoreCurrentProfile();
    }

    public void SaveProfiles()
    {
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
            name = playerName,
            imagePath = imagePath ?? string.Empty,
            avatarIndex = Mathf.Max(0, avatarIndex),
            nonDominantIsLeft = nonDominantIsLeft
        };

        Profiles.Add(profile);
        SaveProfiles();
        return true;
    }

    public void DeleteProfile(UserProfile profile)
    {
        if (profile == null)
            return;

        UserProfile stored = Profiles.Contains(profile) ? profile : FindProfile(profile.name);
        if (stored == null)
            return;

        Profiles.Remove(stored);
        if (CurrentProfile == stored ||
            (CurrentProfile != null &&
             !string.IsNullOrEmpty(stored.name) &&
             stored.name.Equals(CurrentProfile.name, StringComparison.OrdinalIgnoreCase)))
        {
            CurrentProfile = null;
        }

        SaveProfiles();
    }

    public void SelectProfile(UserProfile profile)
    {
        if (profile == null)
        {
            CurrentProfile = null;
            SaveProfiles();
            return;
        }

        UserProfile stored = Profiles.Contains(profile) ? profile : FindProfile(profile.name);
        CurrentProfile = stored;
        SaveProfiles();
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
}
