using UnityEngine;

/// <summary>
/// Menu → module run context (selected phase / mode / module).
/// Survives scene load via PlayerPrefs + static cache.
/// </summary>
public static class ProgressionSessionContext
{
    const string PhaseKey = "LorR.SelectedPhase";
    const string ModuleKey = "LorR.SelectedModuleId";
    const string ModeKey = "LorR.SelectedMode";

    public static int SelectedPhase
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(PhaseKey, 1), 1, ProgressionConfig.PhaseCount);
        set
        {
            PlayerPrefs.SetInt(PhaseKey, ProgressionConfig.ClampPhase(value));
            PlayerPrefs.Save();
        }
    }

    public static string SelectedModuleId
    {
        get => PlayerPrefs.GetString(ModuleKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(ModuleKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public static string SelectedMode
    {
        get => PlayerPrefs.GetString(ModeKey, ProgressionConfig.ModeUnguided);
        set
        {
            PlayerPrefs.SetString(ModeKey, value ?? ProgressionConfig.ModeUnguided);
            PlayerPrefs.Save();
        }
    }

    public static void Set(string moduleId, string mode, int phase)
    {
        SelectedModuleId = moduleId;
        SelectedMode = mode;
        SelectedPhase = phase;

        // Align local progression "current phase" so recorders use the chosen phase.
        string profileId = ProgressionService.CurrentProfileId();
        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(moduleId))
            return;

        ProfileProgressState state = ProgressionService.Load(profileId);
        ModuleProgress module = ModuleProgressUtil.GetOrCreateModule(state, moduleId);
        if (module == null)
            return;

        module.currentPhase = ProgressionConfig.ClampPhase(phase);
        ProgressionService.SaveLocal(state);
    }

    public static string ModuleIdFromPendingScene(string pendingSceneBase)
    {
        if (string.IsNullOrEmpty(pendingSceneBase))
            return string.Empty;

        if (ModuleScenes.IsPenTrace(pendingSceneBase) || pendingSceneBase.IndexOf("Pen", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return ProgressionConfig.PenTrace;
        if (ModuleScenes.IsReactionLight(pendingSceneBase) || pendingSceneBase.IndexOf("Reaction", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return ProgressionConfig.ReactionLight;
        if (ModuleScenes.IsBallDrop(pendingSceneBase) || pendingSceneBase.IndexOf("Ball", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return ProgressionConfig.BallDrop;
        if (ModuleScenes.IsColorSort(pendingSceneBase) ||
            pendingSceneBase.IndexOf("Color", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            pendingSceneBase.IndexOf("Sort", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            pendingSceneBase.IndexOf("Object", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return ProgressionConfig.ObjectPlacement;

        return string.Empty;
    }
}
