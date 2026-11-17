#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds Dr. Alex Rivera animator from Mixamo FBX clips.
/// Menu: LorR / NPC / Build Dr Rivera Animator
/// </summary>
public static class DrRiveraAnimatorBuilder
{
    const string MixamoFolder = "Assets/Scenes/Animation/Mixamo";
    const string ControllerPath = MixamoFolder + "/DrRivera.controller";

    static readonly string[] ClipSources =
    {
        "Happy Idle.fbx",
        "Talking.fbx",
        "Standing Pointing.fbx",
        "Looking Around.fbx",
        "Clapping.fbx",
        "Sad Idle.fbx",
        "Waving.fbx",
        "Agreeing.fbx",
        "Standing Greeting.fbx",
        "Walking.fbx",
    };

    static readonly string[] LoopingSources =
    {
        "Happy Idle.fbx",
        "Talking.fbx",
        "Looking Around.fbx",
        "Sad Idle.fbx",
        "Walking.fbx",
    };

    [MenuItem("LorR/NPC/Build Dr Rivera Animator")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(MixamoFolder))
        {
            Debug.LogError("Mixamo folder missing: " + MixamoFolder);
            return;
        }

        EnsureClipImportSettings();

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsObserving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSad", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Point", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Clap", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Agree", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Greet", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Remove auto-created empty state
        ChildAnimatorState[] existing = sm.states;
        for (int i = 0; i < existing.Length; i++)
            sm.RemoveState(existing[i].state);

        AnimationClip idleClip = LoadClip("Happy Idle.fbx");
        AnimationClip talkClip = LoadClip("Talking.fbx");
        AnimationClip pointClip = LoadClip("Standing Pointing.fbx");
        AnimationClip observeClip = LoadClip("Looking Around.fbx");
        AnimationClip clapClip = LoadClip("Clapping.fbx");
        AnimationClip sadClip = LoadClip("Sad Idle.fbx");
        AnimationClip waveClip = LoadClip("Waving.fbx");
        AnimationClip agreeClip = LoadClip("Agreeing.fbx");
        AnimationClip greetClip = LoadClip("Standing Greeting.fbx");
        AnimationClip walkClip = LoadClip("Walking.fbx");

        if (idleClip == null)
        {
            Debug.LogError("Could not load Happy Idle clip.");
            return;
        }

        AnimatorState idle = sm.AddState("Happy Idle", new Vector3(300, 120, 0));
        idle.motion = idleClip;
        sm.defaultState = idle;

        AnimatorState walk = sm.AddState("Walking", new Vector3(300, 0, 0));
        walk.motion = walkClip;

        AnimatorState talk = sm.AddState("Talking", new Vector3(560, 120, 0));
        talk.motion = talkClip;

        AnimatorState observe = sm.AddState("Looking Around", new Vector3(300, 240, 0));
        observe.motion = observeClip;

        AnimatorState sad = sm.AddState("Sad Idle", new Vector3(300, 360, 0));
        sad.motion = sadClip;

        AnimatorState point = sm.AddState("Standing Pointing", new Vector3(560, 0, 0));
        point.motion = pointClip;

        AnimatorState clap = sm.AddState("Clapping", new Vector3(560, 240, 0));
        clap.motion = clapClip;

        AnimatorState wave = sm.AddState("Waving", new Vector3(560, 360, 0));
        wave.motion = waveClip;

        AnimatorState agree = sm.AddState("Agreeing", new Vector3(800, 120, 0));
        agree.motion = agreeClip;

        AnimatorState greet = sm.AddState("Standing Greeting", new Vector3(800, 240, 0));
        greet.motion = greetClip;

        AddBoolPair(idle, walk, "IsWalking");
        AddBoolPair(idle, talk, "IsTalking");
        AddBoolPair(idle, observe, "IsObserving");
        AddBoolPair(idle, sad, "IsSad");

        AddTriggerOneShot(idle, point, "Point");
        AddTriggerOneShot(idle, clap, "Clap");
        AddTriggerOneShot(idle, wave, "Wave");
        AddTriggerOneShot(idle, agree, "Agree");
        AddTriggerOneShot(idle, greet, "Greet");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;
        Debug.Log("Built Dr Rivera animator: " + ControllerPath);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Dr Rivera Animator",
                "Created:\n" + ControllerPath +
                "\n\nAssign this controller to Dr. Rivera's Animator.\n" +
                "Optional: add DrRiveraAnimator to drive states from code.",
                "OK");
        }
    }

    public static void BuildFromBatch()
    {
        Build();
    }

    static void EnsureClipImportSettings()
    {
        foreach (string fileName in ClipSources)
        {
            string path = MixamoFolder + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
                continue;

            string nice = Path.GetFileNameWithoutExtension(fileName);
            bool shouldLoop = System.Array.IndexOf(LoopingSources, fileName) >= 0;
            bool changed = false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].name != nice)
                {
                    clips[i].name = nice;
                    changed = true;
                }

                if (shouldLoop && !clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }
    }

    static AnimationClip LoadClip(string fileName)
    {
        string path = MixamoFolder + "/" + fileName;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        AnimationClip first = null;
        string want = Path.GetFileNameWithoutExtension(fileName);

        for (int i = 0; i < assets.Length; i++)
        {
            var clip = assets[i] as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__"))
                continue;

            if (first == null)
                first = clip;

            if (clip.name == want || clip.name == "mixamo.com")
                return clip;
        }

        if (first == null)
            Debug.LogError("No AnimationClip in " + path);

        return first;
    }

    static void AddBoolPair(AnimatorState fromIdle, AnimatorState holdState, string param)
    {
        AnimatorStateTransition toHold = fromIdle.AddTransition(holdState);
        toHold.hasExitTime = false;
        toHold.hasFixedDuration = true;
        toHold.duration = 0.15f;
        toHold.AddCondition(AnimatorConditionMode.If, 0, param);

        AnimatorStateTransition toIdle = holdState.AddTransition(fromIdle);
        toIdle.hasExitTime = false;
        toIdle.hasFixedDuration = true;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, param);
    }

    static void AddTriggerOneShot(AnimatorState idle, AnimatorState oneShot, string trigger)
    {
        AnimatorStateTransition toShot = idle.AddTransition(oneShot);
        toShot.hasExitTime = false;
        toShot.hasFixedDuration = true;
        toShot.duration = 0.1f;
        toShot.AddCondition(AnimatorConditionMode.If, 0, trigger);

        AnimatorStateTransition back = oneShot.AddTransition(idle);
        back.hasExitTime = true;
        back.exitTime = 0.9f;
        back.hasFixedDuration = true;
        back.duration = 0.15f;
    }
}
#endif
