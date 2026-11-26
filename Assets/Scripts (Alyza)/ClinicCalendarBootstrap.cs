using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the scene-placed ClinicCalendarUI on Menu. Does not spawn a duplicate panel.
/// </summary>
public static class ClinicCalendarBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryWire(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryWire(scene);
    }

    static void TryWire(Scene scene)
    {
        if (scene.name != "Menu")
            return;

        ClinicCalendarUI ui = Object.FindFirstObjectByType<ClinicCalendarUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            Debug.LogWarning("[ClinicCalendar] ClinicCalendarPanel missing from Menu. Run LorR → Clinic Calendar → Build Into Menu Scene.");
            return;
        }

        ui.EnsureWired();
    }
}
