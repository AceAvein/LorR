using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Pen Control Tracing intro: Objective Start → Dialogue circle → Rivera dialogue (from docs)
/// → cinema (2 cameras): PenSelectStation shot → TraceDesk Easy top view / Set1 paper demo
/// → 2nd path shows wrong draw + Tracing Error line → unlock gameplay.
/// </summary>
[DisallowMultipleComponent]
public class PenTraceIntroDirector : MonoBehaviour
{
    // Exact lines from Docs/LorR_VR_Pen_Trace_Dialogue.md
    public const string WelcomeEnvironment =
        "Welcome to the Pen Tracing station. Please get comfortable at your desk. In this activity, you will use the virtual pen to follow the tracing path with your non-dominant hand. Focus on maintaining controlled movements and staying within the path.";

    public const string WelcomeTraining =
        "Use your non-dominant controller for this activity. Press and hold the Trigger using your index finger to begin tracing. Release the Trigger when you want to stop drawing. Follow the path carefully and try to maintain consistent movement.";

    public const string PenSelectLine =
        "First, choose a pen at the selection station. For this demonstration I will use the pencil. Watch how the tip follows the path on the first tracing table.";

    public const string YourTurnLine =
        "Your turn. Select a pen, then trace each path carefully with your non-dominant hand. Press Start when you are ready.";

    // Docs/LorR_VR_Pen_Trace_Dialogue.md — Tracing Error
    public const string TracingErrorLine =
        "You moved outside the tracing path. Slow down and guide the pen back toward the line. Focus on controlled movement rather than rushing.";

    static PenTraceIntroDirector instance;

    [Header("Timing")]
    [SerializeField] float typeCharsPerSecond = 42f;
    [SerializeField] float pauseBetweenLines = 0.55f;
    [SerializeField] float greetHoldSeconds = 1.1f;
    [SerializeField] float fadeSeconds = 0.45f;
    [SerializeField] float beatPause = 1.15f;
    [SerializeField] float patternHoldSeconds = 0.9f;
    [SerializeField] float patternTraceSeconds = 4.5f;
    [SerializeField] float pencilPickSeconds = 1.6f;
    [SerializeField] float penSelectHoldSeconds = 1.35f;
    [SerializeField] float shotCutFadeSeconds = 0.35f;
    [SerializeField] float talkFontSize = 0.55f;
    [SerializeField] float guideFontSize = 30f;
    [SerializeField] float cinemaFov = 58f;

    [Header("Demo on TracingBoard_Set1 (Easy desk)")]
    [SerializeField] int demoPathCount = 3;
    [SerializeField] float wrongPathDrift = 0.055f;
    [SerializeField] float riveraStationSide = 1.05f;
    [SerializeField] float riveraStationBack = 0.2f;
    [SerializeField] float riveraDeskSide = 0.7f;
    [SerializeField] float riveraDeskForward = 0.2f;

    public static bool HasCompleted { get; private set; }
    public static bool IsTrainingUnlocked => instance == null || HasCompleted;

    /// <summary>True while intro director exists and has not finished (PatternManager defers live path).</summary>
    public static bool InstancePending() => instance != null && !HasCompleted;

    PenTraceIntroSceneSetup sceneSetup;
    PatternManager patternManager;
    PathManager pathManager;
    PenSelect penSelect;
    DrRiveraAnimator rivera;
    Transform riveraRoot;
    GameObject talkCanvas;
    TMP_Text talkText;
    GameObject objectiveScreen;
    GameObject dialogueZone;
    Collider dialoguePadCollider;
    XROrigin xrOrigin;
    Camera hmdCamera;
    Camera cinemaCam;
    Canvas effectsCanvas;
    Image fadeImage;
    Image letterboxTop;
    Image letterboxBottom;
    RectTransform guidePanel;
    TMP_Text guideText;
    CanvasGroup guideGroup;

    bool startConsumed;
    bool dialogueStarted;
    bool cutsceneRunning;
    bool cinemaActive;
    bool camPoseSaved;
    Vector3 camSavedWorldPos;
    Quaternion camSavedWorldRot;
    float camSavedFov;
    bool pathManagerWasEnabled;
    bool riveraPoseSaved;
    Vector3 riveraSavedPos;
    Quaternion riveraSavedRot;
    readonly List<MonoBehaviour> disabledPoseDrivers = new List<MonoBehaviour>();
    readonly List<GameObject> demoProps = new List<GameObject>();
    readonly List<Vector3> demoPathWorld = new List<Vector3>();
    Transform demoPencil;
    Vector3 demoTipLocal = new Vector3(0f, 3.035f, -0.267f);
    LineRenderer demoInk;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    static void TryCreate(Scene scene)
    {
        if (!scene.IsValid() || !ModuleScenes.IsPenTrace(scene.name))
            return;
        if (ModuleScenes.IsGuided(scene.name))
            return;
        if (Object.FindAnyObjectByType<PenTraceIntroDirector>() != null)
            return;

        GameObject host = new GameObject("PenTraceIntroDirector");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PenTraceIntroDirector>();
    }

    public static bool TryConsumeStartButton()
    {
        if (instance == null || HasCompleted)
            return false;
        return instance.HandleStartPressed();
    }

    void Awake()
    {
        instance = this;
        HasCompleted = false;
        PenTraceWalk.MovementLocked = true;

        sceneSetup = Object.FindAnyObjectByType<PenTraceIntroSceneSetup>();
        patternManager = Object.FindFirstObjectByType<PatternManager>(FindObjectsInactive.Include);
        pathManager = Object.FindFirstObjectByType<PathManager>(FindObjectsInactive.Include);
        penSelect = Object.FindFirstObjectByType<PenSelect>(FindObjectsInactive.Include);
        objectiveScreen = GameObject.Find("ObjectiveScreen");
        xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        hmdCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        if (penSelect != null)
            demoTipLocal = penSelect.pencilTip;

        ResolveRivera();
        ResolveTalkUi();
        SetupDialoguePad();
        EnsureEffectsUi();
        EnsureCinemaCamera();
        SetTalkVisible(false);
        SetPadVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
        RestorePoseDrivers();
        RestorePathManager();
        RestoreRivera();
        ClearDemoProps();
        if (patternManager != null)
            patternManager.HideIntroDemoPath();
        // Only hide if intro aborted mid-cutscene; after success pens must stay for selection.
        if (penSelect != null && !HasCompleted)
            penSelect.HideStationPensAfterIntro();
        PenTraceWalk.MovementLocked = false;
    }

    bool HandleStartPressed()
    {
        if (startConsumed || HasCompleted)
            return false;

        startConsumed = true;
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        PenTraceWalk.MovementLocked = false;
        if (rivera != null)
            rivera.Wave();
        SetPadVisible(true);
        return true;
    }

    public void OnPlayerEnteredDialogue()
    {
        if (!startConsumed || dialogueStarted || HasCompleted || cutsceneRunning)
            return;

        dialogueStarted = true;
        PenTraceWalk.MovementLocked = true;
        ConsumeDialoguePad();
        StartCoroutine(WelcomeThenCutscene());
    }

    void ConsumeDialoguePad()
    {
        if (dialoguePadCollider != null)
            dialoguePadCollider.enabled = false;
        if (dialogueZone != null)
        {
            ObjectPlacementDialoguePadPulse pulse = dialogueZone.GetComponent<ObjectPlacementDialoguePadPulse>();
            if (pulse != null)
                pulse.enabled = false;
            dialogueZone.SetActive(false);
        }
    }

    IEnumerator WelcomeThenCutscene()
    {
        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.Greet();
            yield return new WaitForSeconds(greetHoldSeconds);
            rivera.SetTalking(true);
        }

        SetTalkVisible(true);
        yield return TypeLine(WelcomeEnvironment);
        StopTalk();
        yield return new WaitForSeconds(pauseBetweenLines);

        if (rivera != null)
            rivera.SetTalking(true);
        yield return TypeLine(WelcomeTraining);
        StopTalk();
        yield return new WaitForSeconds(pauseBetweenLines);

        // Pen-select explanation happens on camera at the station (pens visible).
        SetTalkVisible(false);
        yield return RunCinematicDemo();

        RestoreRivera();
        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.PlayObserve();
            rivera.SetTalking(true);
        }

        SetTalkVisible(true);
        yield return TypeLine(YourTurnLine);
        StopTalk();
        yield return new WaitForSeconds(1.0f);

        SetTalkVisible(false);
        if (talkText != null)
            talkText.text = string.Empty;

        HasCompleted = true;
        if (patternManager != null)
            patternManager.BeginTrainingPaths();
        // Cutscene hid station pens after the select beat — put them back for player choice.
        if (penSelect != null)
            penSelect.ShowStationPensForIntro();
        PenTraceWalk.MovementLocked = false;
        SetPadVisible(false);
    }

    IEnumerator RunCinematicDemo()
    {
        cutsceneRunning = true;
        PenTraceWalk.MovementLocked = true;
        SetTalkVisible(false);
        ClearDemoProps();
        SuspendPathManager();
        if (patternManager != null)
            patternManager.HideIntroDemoPath();

        // Camera 1 — PenSelectStation (show pens + explain pencil sample)
        if (penSelect != null)
            penSelect.ShowStationPensForIntro();

        yield return Fade(1f);
        Vector3 penPos;
        Quaternion penRot;
        ResolvePenSelectPose(out penPos, out penRot);
        ApplyCinemaPose(penPos, penRot);
        PlaceRiveraBesidePenSelectStation();
        BeginCinemaView();
        yield return null;
        yield return Fade(0f);
        yield return AnimateLetterbox(1f, 0.4f);

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.SetTalking(true);
        }

        yield return ShowGuide("PenSelectStation — Pencil, Ballpen, or Highlighter", penSelectHoldSeconds);
        SetTalkVisible(true);
        yield return TypeLine(PenSelectLine);
        StopTalk();
        SetTalkVisible(false);
        yield return ShowGuide("Sample pen for this demo: PENCIL", penSelectHoldSeconds);
        SetGuideVisible(false);
        yield return DemonstratePencilPickup();
        yield return new WaitForSeconds(0.55f);

        // Camera 2 — TraceDesk Easy top view (hard cut via short fade)
        yield return FadeTo(1f, shotCutFadeSeconds);
        if (penSelect != null)
            penSelect.HideStationPensAfterIntro();
        PlaceRiveraBeside(GetTableFocus(), riveraDeskSide, riveraDeskForward);
        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.SetTalking(true);
        }

        Vector3 deskPos;
        Quaternion deskRot;
        ResolveDeskTopPose(out deskPos, out deskRot);
        ApplyCinemaPose(deskPos, deskRot);
        yield return null;
        yield return FadeTo(0f, shotCutFadeSeconds);

        yield return ShowGuide("TraceDesk Easy — path on TracingBoard_Set1", 0.9f);
        SetGuideVisible(false);

        int count = Mathf.Clamp(demoPathCount, 2, 3);
        for (int i = 0; i < count; i++)
        {
            bool wrongBeat = i == 1;
            yield return ShowGuide(wrongBeat ? "Watch carefully…" : ("Path " + (i + 1) + " of " + count), 0.65f);
            SetGuideVisible(false);

            if (wrongBeat)
                yield return DemonstrateWrongSet1Path(i);
            else
                yield return DemonstrateSet1Path(i, i + 1);

            if (wrongBeat)
            {
                if (rivera != null)
                    rivera.SetTalking(true);
                SetTalkVisible(true);
                yield return TypeLine(TracingErrorLine);
                StopTalk();
                SetTalkVisible(false);
                SetDemoInkColor(new Color(0.08f, 0.08f, 0.1f, 1f));
            }

            yield return new WaitForSeconds(patternHoldSeconds);
        }

        yield return ShowGuide("Hold Trigger to draw. Stay on the path.", 1.1f);

        yield return Fade(1f);
        yield return AnimateLetterbox(0f, 0.35f);
        EndCinemaView();
        ClearDemoProps();
        if (patternManager != null)
            patternManager.HideIntroDemoPath();
        RestorePathManager();
        yield return Fade(0f);
        cutsceneRunning = false;
    }

    IEnumerator DemonstratePencilPickup()
    {
        GameObject source = ResolvePencilSource();
        if (source == null)
        {
            Debug.LogError("PenTraceIntroDirector: Pencil not found for demo.");
            yield break;
        }

        source.SetActive(true);
        Vector3 home = source.transform.position;
        Quaternion homeRot = source.transform.rotation;

        // Brief pulse so the chosen pencil is obvious before it lifts.
        Vector3 pulseUp = home + Vector3.up * 0.04f;
        float pulseT = 0f;
        while (pulseT < 0.45f)
        {
            pulseT += Time.deltaTime;
            float u = Mathf.PingPong(pulseT * 2f, 1f);
            source.transform.position = Vector3.Lerp(home, pulseUp, u);
            yield return null;
        }
        source.transform.SetPositionAndRotation(home, homeRot);

        GameObject clone = Object.Instantiate(source);
        clone.name = "Demo_Pencil";
        StripGameplay(clone);
        demoPencil = clone.transform;
        demoPencil.SetPositionAndRotation(home, homeRot);
        demoProps.Add(clone);

        // Soft-hide the table pencil while the demo clone is selected.
        source.SetActive(false);

        Vector3 lift = home + Vector3.up * 0.22f;
        float t = 0f;
        float d = Mathf.Max(0.35f, pencilPickSeconds);
        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / d);
            demoPencil.position = Vector3.Lerp(home, lift, u);
            yield return null;
        }

        if (rivera != null)
            rivera.SetTalking(true);
        yield return ShowGuide("Pencil selected — watch the tip on the tracing path.", 1.25f);
        if (rivera != null)
            rivera.SetTalking(false);
        SetGuideVisible(false);
        EnsureDemoInk();
    }

    GameObject ResolvePencilSource()
    {
        if (penSelect != null)
        {
            GameObject stationPen = penSelect.FindStationPen(PenGrabKind.Pencil);
            if (stationPen != null)
                return stationPen;
            if (penSelect.pencil != null)
                return penSelect.pencil;
        }
        return GameObject.Find("Pencil");
    }

    IEnumerator DemonstrateSet1Path(int variant, int displayNumber)
    {
        if (patternManager == null || demoPencil == null)
            yield break;

        if (!patternManager.TryPrepareIntroDemoPath(variant, demoPathWorld) || demoPathWorld.Count < 2)
        {
            Debug.LogError("PenTraceIntroDirector: Failed to place intro path on TracingBoard_Set1.");
            yield break;
        }

        EnsureDemoInk();
        SetDemoInkColor(new Color(0.08f, 0.08f, 0.1f, 1f));
        demoInk.positionCount = 0;

        PlacePencilTipAt(demoPathWorld[0]);
        yield return new WaitForSeconds(0.35f);

        float duration = Mathf.Max(1.5f, patternTraceSeconds);
        float traveled = 0f;
        float total = PathLength(demoPathWorld);
        if (total < 0.001f)
            yield break;

        List<Vector3> inkPts = new List<Vector3>();
        inkPts.Add(demoPathWorld[0]);
        demoInk.positionCount = 1;
        demoInk.SetPosition(0, demoPathWorld[0]);

        while (traveled < total)
        {
            traveled += Time.deltaTime * (total / duration);
            float clamped = Mathf.Min(traveled, total);
            Vector3 tip = PointAlong(demoPathWorld, clamped);
            PlacePencilTipAt(tip);

            if (inkPts.Count == 0 || Vector3.Distance(inkPts[inkPts.Count - 1], tip) >= 0.004f)
            {
                inkPts.Add(tip);
                demoInk.positionCount = inkPts.Count;
                demoInk.SetPositions(inkPts.ToArray());
            }

            yield return null;
        }

        PlacePencilTipAt(demoPathWorld[demoPathWorld.Count - 1]);
        demoInk.positionCount = inkPts.Count;
        if (inkPts.Count > 0)
            demoInk.SetPositions(inkPts.ToArray());
        yield return ShowGuide("Path " + displayNumber + " — correct.", 0.65f);
        SetGuideVisible(false);
    }

    IEnumerator DemonstrateWrongSet1Path(int variant)
    {
        if (patternManager == null || demoPencil == null)
            yield break;

        if (!patternManager.TryPrepareIntroDemoPath(variant, demoPathWorld) || demoPathWorld.Count < 2)
        {
            Debug.LogError("PenTraceIntroDirector: Failed to place wrong-path demo on TracingBoard_Set1.");
            yield break;
        }

        EnsureDemoInk();
        // Keep pencil trail dark — mistake is the off-path drift, not red ink.
        SetDemoInkColor(new Color(0.08f, 0.08f, 0.1f, 1f));
        demoInk.positionCount = 0;

        PlacePencilTipAt(demoPathWorld[0]);
        yield return new WaitForSeconds(0.3f);

        float duration = Mathf.Max(1.5f, patternTraceSeconds);
        float traveled = 0f;
        float total = PathLength(demoPathWorld);
        if (total < 0.001f)
            yield break;

        Vector3 paperNormal = Vector3.up;
        Transform paper = PenTraceBoards.FindSet(0);
        if (paper != null)
            paperNormal = PenTraceBoards.BoardNormal(paper);

        List<Vector3> inkPts = new List<Vector3>();
        Vector3 start = demoPathWorld[0];
        inkPts.Add(start);
        demoInk.positionCount = 1;
        demoInk.SetPosition(0, start);

        while (traveled < total)
        {
            traveled += Time.deltaTime * (total / duration);
            float clamped = Mathf.Min(traveled, total);
            float u = total > 0.0001f ? clamped / total : 1f;
            Vector3 onPath = PointAlong(demoPathWorld, clamped);

            // Drift off the guide in the middle third so the mistake is obvious.
            float drift = 0f;
            if (u > 0.22f && u < 0.78f)
            {
                float mid = (u - 0.22f) / 0.56f;
                drift = Mathf.Sin(mid * Mathf.PI) * wrongPathDrift;
            }

            Vector3 tangent = PointAlong(demoPathWorld, Mathf.Min(total, clamped + 0.02f)) - onPath;
            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.right;
            Vector3 side = Vector3.Cross(paperNormal, tangent.normalized);
            if (side.sqrMagnitude < 0.000001f)
                side = Vector3.Cross(Vector3.up, tangent.normalized);
            side.Normalize();

            Vector3 tip = onPath + side * drift;
            PlacePencilTipAt(tip);

            if (inkPts.Count == 0 || Vector3.Distance(inkPts[inkPts.Count - 1], tip) >= 0.004f)
            {
                inkPts.Add(tip);
                demoInk.positionCount = inkPts.Count;
                demoInk.SetPositions(inkPts.ToArray());
            }

            yield return null;
        }

        yield return ShowGuide("Incorrect — outside the path.", 0.85f);
        SetGuideVisible(false);
    }

    void PlacePencilTipAt(Vector3 worldTip)
    {
        if (demoPencil == null)
            return;

        Vector3 focus = GetTableFocus();
        Vector3 flat = Vector3.ProjectOnPlane(focus - demoPencil.position, Vector3.up);
        if (flat.sqrMagnitude > 0.0001f)
            demoPencil.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(75f, 0f, 0f);

        Vector3 tipWorld = demoPencil.TransformPoint(demoTipLocal);
        demoPencil.position += worldTip - tipWorld;
    }

    static float PathLength(List<Vector3> points)
    {
        float len = 0f;
        for (int i = 1; i < points.Count; i++)
            len += Vector3.Distance(points[i - 1], points[i]);
        return len;
    }

    static Vector3 PointAlong(List<Vector3> points, float distance)
    {
        float walked = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            float seg = Vector3.Distance(points[i], points[i + 1]);
            if (walked + seg >= distance)
            {
                float u = seg > 0.0001f ? (distance - walked) / seg : 0f;
                return Vector3.Lerp(points[i], points[i + 1], u);
            }
            walked += seg;
        }
        return points[points.Count - 1];
    }

    void EnsureDemoInk()
    {
        if (demoInk != null)
            return;
        GameObject go = new GameObject("Demo_InkTrail");
        demoInk = go.AddComponent<LineRenderer>();
        demoInk.useWorldSpace = true;
        demoInk.widthMultiplier = 0.012f;
        demoInk.numCapVertices = 4;
        demoInk.numCornerVertices = 2;
        demoInk.sortingOrder = 25;
        demoInk.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        demoInk.receiveShadows = false;
        Color ink = new Color(0.08f, 0.08f, 0.1f, 1f);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = ink;
            demoInk.material = mat;
        }
        demoInk.startColor = ink;
        demoInk.endColor = ink;
        demoInk.positionCount = 0;
        demoProps.Add(go);
    }

    void SetDemoInkColor(Color color)
    {
        EnsureDemoInk();
        if (demoInk == null)
            return;
        demoInk.startColor = color;
        demoInk.endColor = color;
        if (demoInk.material != null)
            demoInk.material.color = color;
    }

    static void StripGameplay(GameObject root)
    {
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
            Object.Destroy(bodies[i]);
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string n = behaviours[i].GetType().Name;
            if (n.Contains("Grab") || n.Contains("Interactable") || n.Contains("PenGrab"))
                Object.Destroy(behaviours[i]);
        }
    }

    void SuspendPathManager()
    {
        if (pathManager == null)
            return;
        pathManagerWasEnabled = pathManager.enabled;
        pathManager.enabled = false;
    }

    void RestorePathManager()
    {
        if (pathManager == null)
            return;
        pathManager.enabled = pathManagerWasEnabled;
    }

    void ClearDemoProps()
    {
        for (int i = 0; i < demoProps.Count; i++)
        {
            if (demoProps[i] != null)
                Object.Destroy(demoProps[i]);
        }
        demoProps.Clear();
        demoPencil = null;
        demoInk = null;
        demoPathWorld.Clear();
    }

    // --- Dialogue pad ---

    void SetupDialoguePad()
    {
        dialogueZone = GameObject.Find("Dialogue circle");
        if (dialogueZone == null)
        {
            Debug.LogError("PenTraceIntroDirector: 'Dialogue circle' not found.");
            return;
        }

        MeshRenderer mr = dialogueZone.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = MakePadMaterial();

        CapsuleCollider capsule = dialogueZone.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = dialogueZone.AddComponent<CapsuleCollider>();
        dialoguePadCollider = capsule;

        Vector3 ls = dialogueZone.transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(ls.x));
        float sy = Mathf.Max(0.01f, Mathf.Abs(ls.y));
        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.radius = 0.45f / sx;
        capsule.height = 2.0f / sy;
        capsule.center = new Vector3(0f, 1.0f / sy, 0f);
        capsule.enabled = false;

        PenTraceDialogueTrigger trigger = dialogueZone.GetComponent<PenTraceDialogueTrigger>();
        if (trigger == null)
            trigger = dialogueZone.AddComponent<PenTraceDialogueTrigger>();
        trigger.Bind(this);

        if (dialogueZone.GetComponent<ObjectPlacementDialoguePadPulse>() == null)
            dialogueZone.AddComponent<ObjectPlacementDialoguePadPulse>();

        dialogueZone.SetActive(false);
    }

    void SetPadVisible(bool visible)
    {
        if (dialogueZone == null)
            return;
        dialogueZone.SetActive(visible);
        if (dialoguePadCollider != null)
            dialoguePadCollider.enabled = visible;
    }

    static Material MakePadMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        Color c = new Color(0.15f, 0.85f, 1f, 0.85f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 1.8f);
        }
        return mat;
    }

    // --- Rivera ---

    void ResolveRivera()
    {
        GameObject go = GameObject.Find("Dr. Rivera");
        if (go == null)
            return;
        riveraRoot = go.transform;
        rivera = go.GetComponent<DrRiveraAnimator>();
        if (rivera == null)
            rivera = go.AddComponent<DrRiveraAnimator>();
        Animator anim = go.GetComponent<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;
    }

    void ResolveTalkUi()
    {
        if (riveraRoot == null)
            return;
        Transform talk = null;
        foreach (Transform t in riveraRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Talk")
            {
                talk = t;
                break;
            }
        }
        if (talk == null)
            return;
        talkCanvas = talk.gameObject;
        talkText = talk.GetComponentInChildren<TMP_Text>(true);
        if (talkText == null)
            return;
        talkText.text = string.Empty;
        talkText.enableAutoSizing = false;
        talkText.fontSize = talkFontSize > 0.05f ? talkFontSize : 0.55f;
    }

    void SetTalkVisible(bool visible)
    {
        if (talkCanvas != null)
            talkCanvas.SetActive(visible);
    }

    void StopTalk()
    {
        if (rivera != null)
            rivera.SetTalking(false);
    }

    IEnumerator TypeLine(string line)
    {
        if (talkText == null)
        {
            yield return new WaitForSeconds(1.2f);
            yield break;
        }

        talkText.text = string.Empty;
        float delay = 1f / Mathf.Max(8f, typeCharsPerSecond);
        for (int i = 0; i < line.Length; i++)
        {
            talkText.text = line.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }
    }

    // --- Cinema ---

    void EnsureCinemaCamera()
    {
        if (cinemaCam != null)
            return;
        GameObject rig = GameObject.Find("IntroCinemaRig");
        if (rig == null)
            rig = new GameObject("IntroCinemaRig");
        Transform camT = rig.transform.Find("IntroCinemaCamera");
        GameObject camGo = camT != null ? camT.gameObject : new GameObject("IntroCinemaCamera");
        if (camT == null)
            camGo.transform.SetParent(rig.transform, false);
        cinemaCam = camGo.GetComponent<Camera>();
        if (cinemaCam == null)
            cinemaCam = camGo.AddComponent<Camera>();
        cinemaCam.enabled = false;
        cinemaCam.fieldOfView = cinemaFov;
        AudioListener al = camGo.GetComponent<AudioListener>();
        if (al != null)
            Destroy(al);
    }

    void ResolvePenSelectPose(out Vector3 pos, out Quaternion rot)
    {
        // Always front-centered on PenSelectStation (ignore side-offset authored shots).
        Vector3 focus = GetPenSelectFocus();
        Transform station = ResolvePenSelectStation();
        Vector3 approach = FrontApproach(station);
        pos = focus + approach * 1.4f + Vector3.up * 0.55f;
        rot = Quaternion.LookRotation((focus - pos).normalized, Vector3.up);

        if (sceneSetup != null && sceneSetup.shotPenSelect != null)
            sceneSetup.shotPenSelect.SetPositionAndRotation(pos, rot);
    }

    void ResolveDeskTopPose(out Vector3 pos, out Quaternion rot)
    {
        // Front-centered elevated view of TraceDesk Easy / TracingBoard_Set1.
        Vector3 focus = GetTableFocus();
        Transform desk = ResolveEasyDesk();
        Vector3 approach = FrontApproach(desk);
        pos = focus + approach * 1.2f + Vector3.up * 1.05f;
        rot = Quaternion.LookRotation((focus - pos).normalized, Vector3.up);

        if (sceneSetup != null && sceneSetup.shotTableOverview != null)
            sceneSetup.shotTableOverview.SetPositionAndRotation(pos, rot);
    }

    static Transform ResolvePenSelectStation()
    {
        GameObject go = GameObject.Find("PenSelectStation");
        return go != null ? go.transform : null;
    }

    static Transform ResolveEasyDesk()
    {
        GameObject go = GameObject.Find("TraceDesk Easy");
        if (go != null)
            return go.transform;
        Transform paper = PenTraceBoards.FindSet(0);
        return paper != null ? (paper.parent != null ? paper.parent : paper) : null;
    }

    /// <summary>
    /// Stand centered in front of the table (desk/station facing), never off to the side.
    /// </summary>
    static Vector3 FrontApproach(Transform subject)
    {
        Vector3 facing = Vector3.zero;
        if (subject != null)
        {
            facing = Vector3.ProjectOnPlane(subject.forward, Vector3.up);
            if (facing.sqrMagnitude < 0.001f)
                facing = Vector3.ProjectOnPlane(subject.right, Vector3.up);
        }

        if (facing.sqrMagnitude < 0.001f)
        {
            Transform paper = PenTraceBoards.FindSet(0);
            if (paper != null)
                facing = PenTraceBoards.BoardFacing(paper);
        }

        if (facing.sqrMagnitude < 0.001f)
            facing = Vector3.forward;

        facing.Normalize();
        // In front of the furniture = opposite of its forward (player side).
        return -facing;
    }

    Vector3 GetPenSelectFocus()
    {
        if (sceneSetup != null && sceneSetup.penSelectFocus != null)
            return sceneSetup.penSelectFocus.position;
        GameObject station = GameObject.Find("PenSelectStation");
        if (station != null)
            return station.transform.position + Vector3.up * 0.9f;
        return new Vector3(0.2f, 0.95f, -2.2f);
    }

    Vector3 GetTableFocus()
    {
        if (sceneSetup != null && sceneSetup.tableFocus != null)
            return sceneSetup.tableFocus.position;

        Transform paper = PenTraceBoards.FindSet(0);
        if (paper != null)
            return PenTraceBoards.BoardCenter(paper);

        GameObject desk = GameObject.Find("TraceDesk Easy");
        if (desk != null)
            return desk.transform.position + Vector3.up * 0.95f;
        return new Vector3(0f, 1.0f, 1.2f);
    }

    void PlaceRiveraBesidePenSelectStation()
    {
        if (riveraRoot == null)
            return;

        SaveRiveraPoseIfNeeded();

        Transform station = ResolvePenSelectStation();
        Vector3 focus = GetPenSelectFocus();
        Vector3 approach = FrontApproach(station);
        // Camera-right side of the table, slightly toward the back — clears the pen view.
        Vector3 side = Vector3.Cross(Vector3.up, approach).normalized;
        Vector3 stand = focus + side * riveraStationSide - approach * riveraStationBack;
        stand.y = riveraSavedPos.y;

        Vector3 look = focus - stand;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f)
            look = -side;
        riveraRoot.SetPositionAndRotation(stand, Quaternion.LookRotation(look.normalized, Vector3.up));
    }

    void PlaceRiveraBeside(Vector3 focus, float sideOffset, float forwardOffset)
    {
        if (riveraRoot == null)
            return;

        SaveRiveraPoseIfNeeded();

        if (sceneSetup != null && sceneSetup.riveraPresenterMark != null
            && Vector3.Distance(focus, GetTableFocus()) < 0.35f)
        {
            Transform mark = sceneSetup.riveraPresenterMark;
            riveraRoot.SetPositionAndRotation(mark.position, mark.rotation);
            return;
        }

        Vector3 towardViewer = Vector3.back;
        if (cinemaCam != null)
        {
            towardViewer = cinemaCam.transform.position - focus;
            towardViewer.y = 0f;
        }
        else if (hmdCamera != null)
        {
            towardViewer = hmdCamera.transform.position - focus;
            towardViewer.y = 0f;
        }
        if (towardViewer.sqrMagnitude < 0.001f)
            towardViewer = FrontApproach(ResolveEasyDesk());
        towardViewer.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, towardViewer).normalized;
        Vector3 stand = focus + side * sideOffset + towardViewer * forwardOffset;
        stand.y = riveraSavedPos.y;

        Vector3 look = focus - stand;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f)
            look = -towardViewer;
        riveraRoot.SetPositionAndRotation(stand, Quaternion.LookRotation(look.normalized, Vector3.up));
    }

    void SaveRiveraPoseIfNeeded()
    {
        if (riveraPoseSaved || riveraRoot == null)
            return;
        riveraSavedPos = riveraRoot.position;
        riveraSavedRot = riveraRoot.rotation;
        riveraPoseSaved = true;
    }

    void RestoreRivera()
    {
        if (riveraRoot == null || !riveraPoseSaved)
            return;
        riveraRoot.SetPositionAndRotation(riveraSavedPos, riveraSavedRot);
        riveraPoseSaved = false;
    }

    void ApplyCinemaPose(Vector3 pos, Quaternion rot)
    {
        EnsureCinemaCamera();
        if (cinemaCam == null)
            return;
        cinemaCam.transform.SetPositionAndRotation(pos, rot);
        cinemaCam.fieldOfView = cinemaFov;
        if (hmdCamera != null && cinemaActive)
            hmdCamera.fieldOfView = cinemaFov;
    }

    void BeginCinemaView()
    {
        if (hmdCamera == null || cinemaCam == null)
            return;
        camSavedWorldPos = hmdCamera.transform.position;
        camSavedWorldRot = hmdCamera.transform.rotation;
        camSavedFov = hmdCamera.fieldOfView;
        camPoseSaved = true;
        DisablePoseDrivers();
        cinemaActive = true;
        cinemaCam.enabled = false;
        hmdCamera.fieldOfView = cinemaFov;
        hmdCamera.transform.SetPositionAndRotation(cinemaCam.transform.position, cinemaCam.transform.rotation);
        EnsureEffectsUi();
        if (effectsCanvas != null)
            effectsCanvas.worldCamera = hmdCamera;
    }

    void EndCinemaView()
    {
        cinemaActive = false;
        if (hmdCamera != null && camPoseSaved)
        {
            hmdCamera.transform.SetPositionAndRotation(camSavedWorldPos, camSavedWorldRot);
            hmdCamera.fieldOfView = camSavedFov;
        }
        RestorePoseDrivers();
        camPoseSaved = false;
        if (cinemaCam != null)
            cinemaCam.enabled = false;
    }

    void LateUpdate()
    {
        if (!cinemaActive || hmdCamera == null || cinemaCam == null)
            return;
        hmdCamera.transform.SetPositionAndRotation(cinemaCam.transform.position, cinemaCam.transform.rotation);
    }

    void DisablePoseDrivers()
    {
        disabledPoseDrivers.Clear();
        if (hmdCamera == null)
            return;
#if ENABLE_INPUT_SYSTEM
        TrackedPoseDriver[] drivers = hmdCamera.GetComponentsInParent<TrackedPoseDriver>(true);
        for (int i = 0; i < drivers.Length; i++)
        {
            if (drivers[i] != null && drivers[i].enabled)
            {
                drivers[i].enabled = false;
                disabledPoseDrivers.Add(drivers[i]);
            }
        }
#endif
        MonoBehaviour[] behaviours = hmdCamera.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b == null || !b.enabled)
                continue;
            if (b.GetType().Name.IndexOf("TrackedPose", System.StringComparison.OrdinalIgnoreCase) >= 0
                && !disabledPoseDrivers.Contains(b))
            {
                b.enabled = false;
                disabledPoseDrivers.Add(b);
            }
        }
    }

    void RestorePoseDrivers()
    {
        for (int i = 0; i < disabledPoseDrivers.Count; i++)
        {
            if (disabledPoseDrivers[i] != null)
                disabledPoseDrivers[i].enabled = true;
        }
        disabledPoseDrivers.Clear();
    }

    // --- Effects UI ---

    void EnsureEffectsUi()
    {
        if (hmdCamera == null)
            hmdCamera = Camera.main;
        GameObject canvasGo = null;
        if (sceneSetup != null && sceneSetup.effectsCanvas != null)
            canvasGo = sceneSetup.effectsCanvas.gameObject;
        if (canvasGo == null)
            canvasGo = GameObject.Find("IntroEffectsCanvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("IntroEffectsCanvas");
            fadeImage = MakeFullImage(canvasGo.transform, "Fade", new Color(0, 0, 0, 0), 0);
            letterboxTop = MakeBar(canvasGo.transform, "LetterboxTop", true);
            letterboxBottom = MakeBar(canvasGo.transform, "LetterboxBottom", false);
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = hmdCamera;
            c.planeDistance = 0.45f;
            c.sortingOrder = 5000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            effectsCanvas = c;
        }
        else
        {
            effectsCanvas = canvasGo.GetComponent<Canvas>();
            if (effectsCanvas != null && hmdCamera != null)
                effectsCanvas.worldCamera = hmdCamera;
            fadeImage = FindImage(canvasGo.transform, "Fade");
            letterboxTop = FindImage(canvasGo.transform, "LetterboxTop");
            letterboxBottom = FindImage(canvasGo.transform, "LetterboxBottom");
        }

        if (sceneSetup != null && sceneSetup.guidePanel != null)
        {
            guidePanel = sceneSetup.guidePanel;
            guideText = sceneSetup.guideText;
            guideGroup = guidePanel.GetComponent<CanvasGroup>();
            if (guideGroup == null)
                guideGroup = guidePanel.gameObject.AddComponent<CanvasGroup>();
        }
        else
            EnsureGuideFallback(canvasGo.transform);

        SetGuideVisible(false);
    }

    void EnsureGuideFallback(Transform canvas)
    {
        Transform existing = canvas.Find("GuidePanel");
        GameObject panelGo = existing != null ? existing.gameObject : new GameObject("GuidePanel");
        if (existing == null)
            panelGo.transform.SetParent(canvas, false);
        guidePanel = panelGo.GetComponent<RectTransform>();
        if (guidePanel == null)
            guidePanel = panelGo.AddComponent<RectTransform>();
        guidePanel.anchorMin = new Vector2(0.1f, 0.12f);
        guidePanel.anchorMax = new Vector2(0.9f, 0.28f);
        guidePanel.offsetMin = Vector2.zero;
        guidePanel.offsetMax = Vector2.zero;
        guideGroup = panelGo.GetComponent<CanvasGroup>();
        if (guideGroup == null)
            guideGroup = panelGo.AddComponent<CanvasGroup>();
        if (panelGo.GetComponent<Image>() == null)
        {
            Image plate = panelGo.AddComponent<Image>();
            plate.color = new Color(0, 0, 0, 0.7f);
        }

        Transform textT = panelGo.transform.Find("GuideText");
        GameObject textGo = textT != null ? textT.gameObject : new GameObject("GuideText");
        if (textT == null)
            textGo.transform.SetParent(panelGo.transform, false);
        guideText = textGo.GetComponent<TMP_Text>();
        if (guideText == null)
            guideText = textGo.AddComponent<TextMeshProUGUI>();
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.fontSize = guideFontSize;
        guideText.color = Color.white;
        RectTransform tr = guideText.rectTransform;
        tr.anchorMin = new Vector2(0.04f, 0.1f);
        tr.anchorMax = new Vector2(0.96f, 0.9f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }

    static Image FindImage(Transform root, string name)
    {
        Transform t = root.Find(name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    static Image MakeFullImage(Transform parent, string name, Color color, int sibling)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(sibling);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    static Image MakeBar(Transform parent, string name, bool top)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        RectTransform rt = img.rectTransform;
        if (top)
        {
            rt.anchorMin = new Vector2(0, 0.9f);
            rt.anchorMax = Vector2.one;
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1, 0.08f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    IEnumerator Fade(float target)
    {
        yield return FadeTo(target, fadeSeconds);
    }

    IEnumerator FadeTo(float target, float duration)
    {
        EnsureEffectsUi();
        if (fadeImage == null)
            yield break;
        Color c = fadeImage.color;
        float start = c.a;
        float t = 0f;
        float d = Mathf.Max(0.05f, duration);
        while (t < d)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, target, t / d);
            fadeImage.color = c;
            yield return null;
        }
        c.a = target;
        fadeImage.color = c;
    }

    IEnumerator AnimateLetterbox(float target, float duration)
    {
        EnsureEffectsUi();
        float t = 0f;
        float d = Mathf.Max(0.05f, duration);
        float startTop = letterboxTop != null ? letterboxTop.color.a : 0f;
        float startBot = letterboxBottom != null ? letterboxBottom.color.a : 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / d);
            if (letterboxTop != null)
            {
                Color c = letterboxTop.color;
                c.a = Mathf.Lerp(startTop, target, u);
                letterboxTop.color = c;
            }
            if (letterboxBottom != null)
            {
                Color c = letterboxBottom.color;
                c.a = Mathf.Lerp(startBot, target, u);
                letterboxBottom.color = c;
            }
            yield return null;
        }
    }

    IEnumerator ShowGuide(string text, float hold)
    {
        EnsureEffectsUi();
        SetGuideVisible(true);
        if (guideText != null)
            guideText.text = text;
        yield return new WaitForSeconds(hold);
    }

    void SetGuideVisible(bool visible)
    {
        if (guidePanel != null)
            guidePanel.gameObject.SetActive(visible);
        if (guideGroup != null)
            guideGroup.alpha = visible ? 1f : 0f;
    }
}

public class PenTraceDialogueTrigger : MonoBehaviour
{
    PenTraceIntroDirector director;
    bool fired;

    public void Bind(PenTraceIntroDirector host)
    {
        director = host;
        fired = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fired || director == null || other == null || !IsPlayer(other))
            return;
        fired = true;
        director.OnPlayerEnteredDialogue();
    }

    static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;
        return other.GetComponentInParent<XROrigin>() != null
               || other.GetComponentInParent<CharacterController>() != null;
    }
}
