#if UNITY_EDITOR
using System.Collections;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Static + Play Mode smoke checks for Color Sort intro flow.
/// Menu: LorR / NPC / Validate Object Placement Intro
/// Menu: LorR / NPC / Playmode Smoke Object Placement Intro
/// </summary>
public static class ObjectPlacementIntroSmokeTest
{
    const string ColorSortScenePath = "Assets/Scenes/Scene Simulation/Color Sort.unity";
    const string PrefsKey = "LorR.ObjectPlacementIntro.AutoSmoke";

    [MenuItem("LorR/NPC/Validate Object Placement Intro")]
    public static void ValidateStatic()
    {
        StringBuilder log = new StringBuilder();
        int fails = 0;

        Scene scene = EditorSceneManager.OpenScene(ColorSortScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Intro Smoke", "Failed to open Color Sort scene.", "OK");
            return;
        }

        Check(log, ref fails, GameObject.Find("Dr. Rivera") != null, "Dr. Rivera exists");
        Check(log, ref fails, GameObject.Find("Dialogue circle") != null, "Dialogue circle exists");
        Check(log, ref fails, GameObject.Find("Dialogue") == null,
            "No orphan GameObject named Dialogue");
        Check(log, ref fails, GameObject.Find("ObjectiveScreen") != null, "ObjectiveScreen exists");

        ObjectPlacementIntroSceneSetup introSetup =
            Object.FindAnyObjectByType<ObjectPlacementIntroSceneSetup>();
        Check(log, ref fails, introSetup != null,
            "IntroCutsceneControls / ObjectPlacementIntroSceneSetup exists (run LorR/NPC/Create Intro Cutscene Scene Controls if missing)");
        if (introSetup != null)
        {
            Check(log, ref fails, introSetup.shotWide != null, "shotWide assigned");
            Check(log, ref fails, introSetup.guidePanel != null, "guidePanel assigned");
        }

        GameObject rivera = GameObject.Find("Dr. Rivera");
        if (rivera != null)
        {
            Animator anim = rivera.GetComponent<Animator>();
            Check(log, ref fails, anim != null, "Dr. Rivera has Animator");
            Check(log, ref fails,
                anim != null && anim.runtimeAnimatorController != null &&
                anim.runtimeAnimatorController.name.IndexOf("DrRivera", System.StringComparison.OrdinalIgnoreCase) >= 0,
                "Dr. Rivera uses DrRivera.controller");

            Transform talk = FindDeep(rivera.transform, "Talk");
            Check(log, ref fails, talk != null, "Talk canvas under Rivera");
            TMP_Text talkText = talk != null ? talk.GetComponentInChildren<TMP_Text>(true) : null;
            Check(log, ref fails, talkText != null, "Talk Text (TMP) under Talk");
        }

        ObjectPlacementStation station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        Check(log, ref fails, station != null, "ObjectPlacementStation in scene");
        if (station != null)
        {
            Check(log, ref fails, station.yellowBox != null, "station.yellowBox assigned");
            Check(log, ref fails, station.redBox != null, "station.redBox assigned");
        }

        Button start = FindButtonNamed("Btn_Start");
        Check(log, ref fails, start != null, "UI Btn_Start exists");

        RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Scenes/Animation/Mixamo/DrRivera.controller");
        Check(log, ref fails, ctrl != null, "DrRivera.controller asset exists");

        Check(log, ref fails, MonoScriptFromType(typeof(ObjectPlacementIntroDirector)) != null,
            "ObjectPlacementIntroDirector script present");
        Check(log, ref fails, MonoScriptFromType(typeof(ColorSortWalk)) != null,
            "ColorSortWalk script present");
        Check(log, ref fails, MonoScriptFromType(typeof(DrRiveraAnimator)) != null,
            "DrRiveraAnimator script present");

        // Dialogue circle scale warning (informational)
        GameObject dialogue = GameObject.Find("Dialogue circle");
        if (dialogue == null)
            dialogue = GameObject.Find("Dialogue");
        if (dialogue != null)
        {
            Vector3 s = dialogue.transform.lossyScale;
            bool skewed = s.y < 0.5f || Mathf.Abs(s.x - s.y) > 0.2f;
            Check(log, ref fails, true,
                skewed
                    ? "Dialogue circle scale flat (OK — IntroTrigger counters) scale=" + s
                    : "Dialogue circle scale OK");
        }

        string summary = fails == 0
            ? "PASS — all static checks OK."
            : "FAIL — " + fails + " check(s) failed.";
        Debug.Log("[ObjectPlacementIntroSmoke]\n" + summary + "\n" + log);
        EditorUtility.DisplayDialog("Object Placement Intro Smoke", summary + "\n\nSee Console for details.", "OK");
    }

    [MenuItem("LorR/NPC/Playmode Smoke Object Placement Intro")]
    public static void PlaymodeSmoke()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Intro Smoke", "Exit Play Mode first.", "OK");
            return;
        }

        EditorPrefs.SetBool(PrefsKey, true);
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(ColorSortScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void WatchPlaymodeSmoke()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        if (!EditorPrefs.GetBool(PrefsKey, false))
            return;

        EditorPrefs.SetBool(PrefsKey, false);
        Scene scene = SceneManager.GetActiveScene();
        if (!ModuleScenes.IsColorSort(scene.name))
            return;

        GameObject host = new GameObject("ObjectPlacementIntroSmokeRunner");
        host.AddComponent<ObjectPlacementIntroSmokeRunner>();
    }

    static void Check(StringBuilder log, ref int fails, bool ok, string label)
    {
        log.AppendLine((ok ? "[PASS] " : "[FAIL] ") + label);
        if (!ok)
            fails++;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindDeep(root.GetChild(i), name);
            if (hit != null)
                return hit;
        }
        return null;
    }

    static Button FindButtonNamed(string name)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == name)
                return buttons[i];
        }
        return null;
    }

    static MonoScript MonoScriptFromType(System.Type type)
    {
        string[] guids = AssetDatabase.FindAssets(type.Name + " t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == type)
                return script;
        }
        return null;
    }
}

/// <summary>Drives intro without headset after Playmode Smoke menu.</summary>
public class ObjectPlacementIntroSmokeRunner : MonoBehaviour
{
    readonly StringBuilder log = new StringBuilder();
    int fails;

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(0.75f);

        ObjectPlacementIntroDirector intro = Object.FindAnyObjectByType<ObjectPlacementIntroDirector>();
        Check(intro != null, "IntroDirector spawned");
        if (intro == null)
        {
            Finish();
            yield break;
        }

        Check(ColorSortWalk.MovementLocked, "Walk locked at start");

        bool consumed = ObjectPlacementIntroDirector.TryConsumeStartButton();
        Check(consumed, "Btn_Start consumed by intro");
        Check(!ColorSortWalk.MovementLocked, "Walk unlocked after Start");

        intro.OnPlayerEnteredDialogue();
        Check(ColorSortWalk.MovementLocked, "Walk locked during dialogue/cutscene");

        float wait = 80f;
        while (!ObjectPlacementIntroDirector.HasCompleted && wait > 0f)
        {
            wait -= Time.deltaTime;
            yield return null;
        }

        Check(ObjectPlacementIntroDirector.HasCompleted, "Intro HasCompleted (" + (80f - wait).ToString("0.0") + "s)");
        Check(!ColorSortWalk.MovementLocked, "Walk unlocked after cutscene");

        ObjectPlacementStation station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        ObjectPlacementPhaseIRound phase = Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>();
        Check(phase != null, "PhaseIRound present");
        if (station != null && station.setScreenLabel != null)
            Check(!string.IsNullOrEmpty(station.setScreenLabel.text),
                "Station screen text = \"" + station.setScreenLabel.text + "\"");

        // Demo props should be cleaned up
        GameObject leftoverBanana = GameObject.Find("IntroDemo_banana");
        GameObject leftoverTomato = GameObject.Find("IntroDemo_tomato");
        Check(leftoverBanana == null && leftoverTomato == null, "Demo props cleaned up");

        Finish();
    }

    void Check(bool ok, string label)
    {
        log.AppendLine((ok ? "[PASS] " : "[FAIL] ") + label);
        if (!ok)
        {
            fails++;
            Debug.LogError("[IntroSmoke] FAIL " + label);
        }
        else
        {
            Debug.Log("[IntroSmoke] PASS " + label);
        }
    }

    void Finish()
    {
        string summary = fails == 0 ? "PASS" : "FAIL (" + fails + ")";
        Debug.Log("[ObjectPlacementIntroSmoke] " + summary + "\n" + log);
        EditorApplication.isPlaying = false;
        EditorUtility.DisplayDialog(
            "Object Placement Intro Playmode Smoke",
            summary + "\n\n" + log,
            "OK");
        Destroy(gameObject);
    }
}
#endif
