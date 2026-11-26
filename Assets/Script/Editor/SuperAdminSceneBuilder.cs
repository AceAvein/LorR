#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: LorR / Dev / Ensure SuperAdmin Profile
/// </summary>
public static class SuperAdminSceneBuilder
{
    [MenuItem("LorR/Dev/Ensure SuperAdmin Profile")]
    public static void EnsureSuperAdmin()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "SuperAdmin",
                "Enter Play Mode on Menu first, then run this again.\n\n" +
                "(Needs ProfileManager + Firebase runtime.)",
                "OK");
            return;
        }

        UserProfile profile = SuperAdminBootstrap.Ensure();
        if (profile == null)
        {
            EditorUtility.DisplayDialog("SuperAdmin", "ProfileManager not ready.", "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "SuperAdmin",
            "Ensured profile \"" + profile.name + "\" (" + profile.id + ").\n" +
            "Perfect clear: all modules · Phases 1–" + ProgressionConfig.PhaseCount +
            " · 4 sessions each with results.\n" +
            "Local save + Firebase history push triggered.",
            "OK");
    }
}
#endif
