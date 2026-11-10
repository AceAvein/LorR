#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds soft hospital ceiling lights under existing HallLightGlow_* / RoomLightGlow_*
/// in the current scene or all module scenes — does not rebuild prefabs.
/// </summary>
public static class LorRHospitalLightsOnGlows
{
    static readonly string[] ModuleScenes =
    {
        "Assets/Scenes/Scene Simulation/Pen Control Tracing.unity",
        "Assets/Scenes/Scene Simulation/Reaction Light.unity",
        "Assets/Scenes/Scene Simulation/Color Sort.unity",
        "Assets/Scenes/Scene Simulation/Ball Drop.unity",
        "Assets/Scenes/Scene Guided/Guided Pen Control Tracing.unity",
        "Assets/Scenes/Scene Guided/Guided Reaction Light.unity",
        "Assets/Scenes/Scene Guided/Guided Color Sort.unity",
        "Assets/Scenes/Scene Guided/Guided Ball Drop.unity"
    };

    [MenuItem("LorR/Environments/Add Hospital Lights To Open Scene Glows")]
    public static void AddToOpenScene()
    {
        int count = AddLightsInActiveScenes();
        if (count > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }

        EditorUtility.DisplayDialog(
            "Hospital Lights",
            "Added / updated lights on " + count + " glow fixtures in open scene(s).",
            "OK");
    }

    [MenuItem("LorR/Environments/Add Hospital Lights To All Module Scenes")]
    public static void AddToAllModuleScenes()
    {
        if (!EditorUtility.DisplayDialog(
                "Hospital Lights",
                "Ilalagay ang hospital lights sa lahat ng HallLightGlow / RoomLightGlow\nsa module scenes. Magse-save ang scenes. Continuar?",
                "Yes", "Cancel"))
            return;

        string previous = SceneManager.GetActiveScene().path;
        int total = 0;

        for (int i = 0; i < ModuleScenes.Length; i++)
        {
            string path = ModuleScenes[i];
            if (!System.IO.File.Exists(path))
                continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = AddLightsInScene(scene);
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            total += n;
        }

        if (!string.IsNullOrEmpty(previous) && System.IO.File.Exists(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Hospital Lights",
            "Done. Lights on " + total + " glow fixtures across module scenes.",
            "OK");
    }

    static int AddLightsInActiveScenes()
    {
        int total = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            total += AddLightsInScene(SceneManager.GetSceneAt(i));
        return total;
    }

    static int AddLightsInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return 0;

        List<Transform> glows = new List<Transform>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            CollectGlows(roots[i].transform, glows);

        int count = 0;
        for (int i = 0; i < glows.Count; i++)
        {
            if (EnsureHospitalLight(glows[i]))
                count++;
        }

        return count;
    }

    static void CollectGlows(Transform t, List<Transform> list)
    {
        string n = t.name;
        if (n.StartsWith("HallLightGlow") || n.StartsWith("RoomLightGlow"))
            list.Add(t);

        for (int i = 0; i < t.childCount; i++)
            CollectGlows(t.GetChild(i), list);
    }

    static bool EnsureHospitalLight(Transform glow)
    {
        if (glow == null)
            return false;

        // Reuse existing lamp child if present
        Transform existing = glow.Find("HospitalLamp");
        Light light;
        if (existing != null)
        {
            light = existing.GetComponent<Light>();
            if (light == null)
                light = existing.gameObject.AddComponent<Light>();
        }
        else
        {
            // Also skip if any child already has a Light (avoid duplicates)
            Light[] childLights = glow.GetComponentsInChildren<Light>(true);
            if (childLights != null && childLights.Length > 0)
            {
                light = childLights[0];
                light.gameObject.name = "HospitalLamp";
            }
            else
            {
                GameObject lamp = new GameObject("HospitalLamp");
                Undo.RegisterCreatedObjectUndo(lamp, "Add Hospital Lamp");
                lamp.transform.SetParent(glow, false);
                lamp.transform.localPosition = new Vector3(0f, -0.12f, 0f);
                light = lamp.AddComponent<Light>();
            }
        }

        bool hallway = glow.name.StartsWith("HallLightGlow");
        light.type = LightType.Point;
        light.color = new Color(0.95f, 0.96f, 0.92f);
        light.intensity = hallway ? 1.35f : 1.55f;
        light.range = hallway ? 5.5f : 6.5f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.45f;
        light.enabled = true;

        EditorUtility.SetDirty(light);
        EditorUtility.SetDirty(glow.gameObject);
        return true;
    }
}
#endif
