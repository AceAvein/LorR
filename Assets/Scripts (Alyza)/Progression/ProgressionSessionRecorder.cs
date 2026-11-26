using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Bridge from module results screens → ProgressionService.
/// Uses current module phase; Guided saves but does not unlock.
/// </summary>
public static class ProgressionSessionRecorder
{
    public static void Record(string moduleId, float primaryScore, Dictionary<string, object> metrics = null)
    {
        string profileId = ProgressionService.CurrentProfileId();
        if (string.IsNullOrEmpty(profileId))
            return;

        // Prefer phase chosen on the Menu phase canvas; fall back to module currentPhase.
        int phase = ProgressionSessionContext.SelectedPhase;
        if (!string.IsNullOrEmpty(ProgressionSessionContext.SelectedModuleId) &&
            ProgressionSessionContext.SelectedModuleId == moduleId)
        {
            phase = ProgressionConfig.ClampPhase(ProgressionSessionContext.SelectedPhase);
        }
        else
        {
            ModuleProgress module = ProgressionService.GetModule(profileId, moduleId);
            phase = module != null ? ProgressionConfig.ClampPhase(module.currentPhase) : 1;
        }

        string scene = SceneManager.GetActiveScene().name;
        string mode = ModuleScenes.IsGuided(scene)
            ? ProgressionConfig.ModeGuided
            : ProgressionConfig.ModeUnguided;

        // Pre-check Simulation day lock (Manila) so logs are clear when blocked.
        if (ProgressionConfig.IsUnguided(mode) &&
            !ProgressionService.CanPlay(profileId, moduleId, phase, mode, out _, out string blockMessage))
        {
            UnityEngine.Debug.LogWarning("[Progression] Blocked: " + blockMessage);
            return;
        }

        ProgressionService.RecordSession(
            profileId,
            moduleId,
            phase,
            mode,
            primaryScore,
            metrics,
            out string message,
            out bool unlocked);

        if (!string.IsNullOrEmpty(message))
            UnityEngine.Debug.Log("[Progression] " + message + (unlocked ? " (unlocked next phase)" : string.Empty));
    }
}
