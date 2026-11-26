using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures a local+Firebase test profile "SuperAdmin" with all modules unlocked through Phase IV.
/// </summary>
public static class SuperAdminBootstrap
{
    public const string DisplayName = "SuperAdmin";
    public const string StableId = "superadmin";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureDelayed();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
            TryEnsureDelayed();
    }

    static void TryEnsureDelayed()
    {
        // ProfileManager may spawn one frame later on Menu.
        var runner = Object.FindFirstObjectByType<SuperAdminBootstrapRunner>();
        if (runner == null)
        {
            GameObject go = new GameObject("SuperAdminBootstrap");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<SuperAdminBootstrapRunner>();
        }

        runner.Kick();
    }

    public static UserProfile Ensure()
    {
        if (ProfileManager.Instance == null)
            return null;

        UserProfile existing = ProfileManager.Instance.FindProfileById(StableId)
                               ?? ProfileManager.Instance.FindProfile(DisplayName);

        if (existing == null)
        {
            existing = new UserProfile
            {
                id = StableId,
                name = DisplayName,
                imagePath = string.Empty,
                avatarIndex = 0,
                nonDominantIsLeft = true
            };
            ProfileManager.Instance.Profiles.Add(existing);
            ProfileManager.Instance.SaveProfiles();
            Debug.Log("[SuperAdmin] Created profile " + DisplayName + " (" + StableId + ")");
        }
        else
        {
            // Normalize name/id for testing.
            bool changed = false;
            if (!string.Equals(existing.id, StableId, System.StringComparison.Ordinal))
            {
                existing.id = StableId;
                changed = true;
            }

            if (!string.Equals(existing.name, DisplayName, System.StringComparison.Ordinal))
            {
                existing.name = DisplayName;
                changed = true;
            }

            if (changed)
                ProfileManager.Instance.SaveProfiles();
        }

        ProgressionService.SeedProfile(existing.id, pushToCloud: false);
        ProgressionService.EnsureSuperAdminProgress(existing.id);
        FirebaseProfileSync.UpsertProfile(existing, isNew: true);

        return existing;
    }

    sealed class SuperAdminBootstrapRunner : MonoBehaviour
    {
        bool pending;

        public void Kick()
        {
            if (pending)
                return;
            pending = true;
            StartCoroutine(Run());
        }

        System.Collections.IEnumerator Run()
        {
            // Wait until ProfileManager exists (Menu / bootstrap).
            float timeout = 8f;
            while (ProfileManager.Instance == null && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
            Ensure();

            // Retry unlock push once Firebase is ready.
            timeout = 15f;
            while (timeout > 0f && (!FirebaseBootstrap.Ready || !ProgressionService.IsOnline()))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (FirebaseBootstrap.Ready)
                Ensure();

            pending = false;
        }
    }
}
