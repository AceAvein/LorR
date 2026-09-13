using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserProfile
{
    public string name;
    public string imagePath;
}

[Serializable]
public class ProfileListWrapper
{
    public List<UserProfile> profiles = new List<UserProfile>();
}

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    private const string SAVE_KEY = "UserProfiles";

    public List<UserProfile> Profiles { get; private set; } = new List<UserProfile>();

    public UserProfile CurrentProfile { get; private set; }

    private void Awake()
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

        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(SAVE_KEY);

        if (string.IsNullOrEmpty(json))
            return;

        ProfileListWrapper wrapper =
            JsonUtility.FromJson<ProfileListWrapper>(json);

        if (wrapper != null && wrapper.profiles != null)
        {
            Profiles = wrapper.profiles;
        }
    }

    public void SaveProfiles()
    {
        ProfileListWrapper wrapper = new ProfileListWrapper();

        wrapper.profiles = Profiles;

        string json = JsonUtility.ToJson(wrapper);

        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public bool ProfileNameExists(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        foreach (UserProfile profile in Profiles)
        {
            if (profile != null &&
                !string.IsNullOrEmpty(profile.name) &&
                profile.name.Equals(
                    playerName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool AddProfile(string playerName, string imagePath)
    {
        playerName = playerName.Trim();

        // Prevent duplicate names
        if (ProfileNameExists(playerName))
        {
            Debug.LogWarning(
                "Profile name already exists: " + playerName);

            return false;
        }

        UserProfile profile = new UserProfile();

        profile.name = playerName;
        profile.imagePath = imagePath;

        Profiles.Add(profile);

        SaveProfiles();

        return true;
    }

    public void DeleteProfile(UserProfile profile)
    {
        if (Profiles.Contains(profile))
        {
            Profiles.Remove(profile);

            SaveProfiles();

            if (CurrentProfile == profile)
                CurrentProfile = null;
        }
    }

    public void SelectProfile(UserProfile profile)
    {
        CurrentProfile = profile;
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
}