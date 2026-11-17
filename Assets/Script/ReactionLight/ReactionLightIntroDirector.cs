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
/// Reaction Light intro: Objective Start → Dialogue circle → Rivera → poke demo → unlock play.
/// Demo: left hand then right hand; green = success poke, red = error poke. Yellow removed.
/// </summary>
[DisallowMultipleComponent]
public class ReactionLightIntroDirector : MonoBehaviour
{
    public const string WelcomeEnvironment =
        "Welcome to the Reaction Light station. Lights will appear on the training board, and your task is to respond to the active target using your non-dominant controller. Watch the lights carefully and respond as quickly and accurately as possible.";

    public const string WelcomeTraining =
        "Let us begin with the basic reaction task. One light will flash every three seconds. Identify the active target and respond using the assigned controller input before the next light appears.";

    static ReactionLightIntroDirector instance;

    [Header("Timing")]
    [SerializeField] float typeCharsPerSecond = 42f;
    [SerializeField] float pauseBetweenLines = 0.55f;
    [SerializeField] float greetHoldSeconds = 1.1f;
    [SerializeField] float fadeSeconds = 0.45f;
    [SerializeField] float beatPause = 1.1f;
    [SerializeField] float pokeHoldSeconds = 1.15f;
    [SerializeField] float handTravelSeconds = 0.95f;
    [SerializeField] float explainHoldSeconds = 1.65f;
    [SerializeField] float betweenPokePause = 0.45f;
    [SerializeField] float cinemaFov = 60f;
    [SerializeField] float talkFontSize = 0.55f;
    [SerializeField] float guideFontSize = 30f;

    [Header("Rivera beside console")]
    [SerializeField] float riveraConsoleSideOffset = 0.85f;
    [SerializeField] float riveraConsoleForwardOffset = 0.2f;

    [Header("Demo")]
    [SerializeField] int demoGreenLightIndex = 5;
    [SerializeField] int demoRedLightIndex = 6;
    [SerializeField] GameObject leftHandPrefab;
    [SerializeField] GameObject rightHandPrefab;

    public static bool HasCompleted { get; private set; }
    public static bool IsTrainingUnlocked => instance == null || HasCompleted;

    ReactionLightIntroSceneSetup sceneSetup;
    ReactionLightRound round;
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
    Vector3 riveraSavedPos;
    Quaternion riveraSavedRot;
    bool riveraPoseSaved;
    Transform consoleRoot;
    readonly List<MonoBehaviour> disabledPoseDrivers = new List<MonoBehaviour>();
    readonly List<GameObject> demoProps = new List<GameObject>();
    Transform demoLeftHand;
    Transform demoRightHand;
    Animator demoLeftAnim;
    Animator demoRightAnim;
    DemoIndexPokePose demoLeftPose;
    DemoIndexPokePose demoRightPose;

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
        if (!scene.IsValid() || !ModuleScenes.IsReactionLight(scene.name))
            return;
        if (ModuleScenes.IsGuided(scene.name))
            return;
        if (Object.FindAnyObjectByType<ReactionLightIntroDirector>() != null)
            return;

        GameObject host = new GameObject("ReactionLightIntroDirector");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ReactionLightIntroDirector>();
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
        ReactionLightWalk.MovementLocked = true;

        sceneSetup = Object.FindAnyObjectByType<ReactionLightIntroSceneSetup>();
        round = Object.FindFirstObjectByType<ReactionLightRound>(FindObjectsInactive.Include);
        objectiveScreen = GameObject.Find("ObjectiveScreen");
        xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        hmdCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        ResolveRivera();
        ResolveTalkUi();
        ResolveHandPrefabs();
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
        RestoreRivera();
        RestorePoseDrivers();
        ClearDemoProps();
    }

    bool HandleStartPressed()
    {
        if (startConsumed || HasCompleted)
            return false;

        startConsumed = true;
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        ReactionLightWalk.MovementLocked = false;
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
        ReactionLightWalk.MovementLocked = true;
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
        // First dialogue at Rivera's original spot.
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

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.85f);
            rivera.SetTalking(true);
        }

        yield return TypeLine("Watch this demonstration. I will show you how to poke green lights — and why red is wrong.");
        StopTalk();
        yield return new WaitForSeconds(0.6f);

        yield return RunCinematicDemo();

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.PlayObserve();
            rivera.SetTalking(true);
        }

        SetTalkVisible(true);
        string ndh = "non-dominant";
        if (ProfileManager.TryGetNonDominantLeft(out bool leftNdh))
            ndh = leftNdh ? "left (non-dominant)" : "right (non-dominant)";
        yield return TypeLine("Your turn. In training, poke green lights with your " + ndh + " hand only. Press Start when ready.");
        StopTalk();
        yield return new WaitForSeconds(1.0f);

        SetTalkVisible(false);
        if (talkText != null)
            talkText.text = string.Empty;

        HasCompleted = true;
        ReactionLightWalk.MovementLocked = false;
        SetPadVisible(false);
        if (round != null)
            round.ResetResultLabelReady();
    }

    IEnumerator RunCinematicDemo()
    {
        cutsceneRunning = true;
        ReactionLightWalk.MovementLocked = true;
        SetTalkVisible(false);
        ClearDemoProps();

        // Round is also AfterSceneLoad-created; Awake cache can be null — resolve now and wait briefly.
        for (int i = 0; i < 45 && !ResolveRound(); i++)
            yield return null;

        if (round == null)
        {
            Debug.LogError("ReactionLightIntroDirector: ReactionLightRound missing — intro board/hands demo skipped.");
            cutsceneRunning = false;
            yield break;
        }

        ResolveHandPrefabs();
        ResolveConsole();
        PlaceRiveraBesideConsole();
        round.PrepareBoardForIntro();
        if (round.IntroLightCount == 0)
            Debug.LogError("ReactionLightIntroDirector: no Ball lights found for intro board.");

        // Snap shot before spawning hands so ViewerApproachFrom uses the board cinema pose.
        SnapBoardShot();
        SpawnDemoHands();
        if (demoLeftHand == null && demoRightHand == null)
            Debug.LogError("ReactionLightIntroDirector: demo hand prefabs missing (PlayerHands).");

        yield return Fade(1f);
        SnapBoardShot();
        BeginCinemaView();
        yield return null;
        // Ensure lights stay visible after cinema swap.
        round.PrepareBoardForIntro();
        yield return Fade(0f);
        yield return AnimateLetterbox(1f, 0.4f);

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.SetTalking(true);
        }

        yield return ShowGuide(
            "Watch carefully.\nGREEN = poke (correct).\nRED = do not poke (Wrong).",
            Mathf.Max(beatPause, 1.4f));
        StopTalk();
        if (rivera != null)
            rivera.PlayObserve();

        // Left hand: 3 explained pokes (green → red → green)
        yield return DemoHandSeries(true);
        if (demoLeftHand != null)
            demoLeftHand.gameObject.SetActive(false);

        // Right hand: same 3 examples
        yield return DemoHandSeries(false);
        if (demoRightHand != null)
            demoRightHand.gameObject.SetActive(false);

        if (rivera != null)
            rivera.SetTalking(true);
        string ndh = ProfileManager.TryGetNonDominantLeft(out bool left)
            ? (left ? "LEFT" : "RIGHT")
            : "NON-DOMINANT";
        yield return ShowGuide(
            "In play, use your " + ndh + " hand only.\nGrip + index poke on GREEN.",
            1.8f);

        if (round != null)
        {
            round.PrepareBoardForIntro();
            round.ResetResultLabelReady();
        }

        yield return Fade(1f);
        yield return AnimateLetterbox(0f, 0.35f);
        EndCinemaView();
        RestoreRivera();
        ClearDemoProps();
        if (round != null)
            round.ResetResultLabelReady();
        yield return Fade(0f);
        cutsceneRunning = false;
    }

    IEnumerator DemoHandSeries(bool leftHand)
    {
        string label = leftHand ? "LEFT" : "RIGHT";

        // Steady poke pose for this hand's entire series (approach, contact, return).
        if (demoLeftHand != null)
            demoLeftHand.gameObject.SetActive(leftHand);
        if (demoRightHand != null)
            demoRightHand.gameObject.SetActive(!leftHand);
        Transform hand = leftHand ? demoLeftHand : demoRightHand;
        if (hand != null)
            hand.gameObject.SetActive(true);
        SetPokePose(leftHand, true);
        yield return null;
        yield return null;

        yield return ExplainedPoke(
            leftHand,
            true,
            label + " HAND — Example 1 of 3\nSqueeze GRIP, keep TRIGGER released.\nPoke GREEN with your index — correct!");
        yield return new WaitForSeconds(betweenPokePause);

        yield return ExplainedPoke(
            leftHand,
            false,
            label + " HAND — Example 2 of 3\nGreen is the correct target — but the hand pokes RED.\nThis shows Wrong (time penalty in play).");
        yield return new WaitForSeconds(betweenPokePause);

        yield return ExplainedPoke(
            leftHand,
            true,
            label + " HAND — Example 3 of 3\nAgain: only GREEN is a success.\nWatch the poke — fingertip to the light.");

        // Stay in poke pose until the hand is hidden by the caller.
        SetPokePose(leftHand, true);
    }

    IEnumerator ExplainedPoke(bool leftHand, bool greenTarget, string explanation)
    {
        if (rivera != null)
            rivera.SetTalking(true);
        yield return ShowGuide(explanation, explainHoldSeconds);
        if (rivera != null)
            rivera.PlayObserve();
        // Keep guide visible during the longer poke so the player can read + watch.
        yield return DemoPoke(leftHand, greenTarget);
        SetGuideVisible(false);
        StopTalk();
    }

    IEnumerator DemoPoke(bool leftHand, bool greenTarget)
    {
        ResolveRound();
        Transform hand = leftHand ? demoLeftHand : demoRightHand;
        if (hand == null)
        {
            Debug.LogError("ReactionLightIntroDirector: demo " + (leftHand ? "left" : "right") + " hand missing.");
            yield break;
        }

        if (round == null)
        {
            Debug.LogError("ReactionLightIntroDirector: ReactionLightRound missing during DemoPoke.");
            yield break;
        }

        // Success: poke the green light. Wrong: green is lit (correct target) but hand pokes a red light.
        int pokeIndex = greenTarget ? demoGreenLightIndex : demoRedLightIndex;
        Transform light = round.GetLightTransform(pokeIndex);
        if (light == null)
        {
            Debug.LogError("ReactionLightIntroDirector: light index " + pokeIndex + " missing (balls=" + round.IntroLightCount + ").");
            yield break;
        }

        round.PaintIntroBoardRed();
        // Always show a green target so Wrong reads as "correct light exists, but red was poked".
        round.SetIntroLightGreen(demoGreenLightIndex, true);

        if (demoLeftHand != null)
            demoLeftHand.gameObject.SetActive(leftHand);
        if (demoRightHand != null)
            demoRightHand.gameObject.SetActive(!leftHand);
        hand.gameObject.SetActive(true);

        Vector3 viewer = ViewerApproachFrom(light.position);
        Vector3 rest = light.position + viewer * 0.42f + ViewerSide(viewer) * (leftHand ? -0.18f : 0.18f) + Vector3.up * -0.02f;
        hand.position = rest;
        FaceHandToward(hand, light.position);
        Quaternion restRot = hand.rotation;

        // Contact: tip reaches light center, nudged slightly toward viewer so contact is readable.
        Vector3 contactTip = light.position + viewer * 0.01f;

        // Keep poke pose the whole cutscene motion — never release mid-demo.
        SetPokePose(leftHand, true);
        yield return null;
        yield return null;

        // Travel hand root so index3 fingertip reaches the light center.
        yield return MoveHandSoTipReaches(hand, contactTip, handTravelSeconds);
        SetPokePose(leftHand, true);

        // Readable hold at contact; reuse gameplay Great/Wrong visuals (no score/timer).
        float hold = Mathf.Max(0.85f, pokeHoldSeconds);
        if (greenTarget)
        {
            round.ShowIntroFeedback(true);
            round.SetIntroLightGreen(demoGreenLightIndex, false); // success paints red like Press()
        }
        else
        {
            // Green stays lit — Wrong = poked red while a green target was available.
            round.ShowIntroFeedback(false);
        }

        yield return new WaitForSeconds(hold);

        // Return while still in poke pose.
        SetPokePose(leftHand, true);
        yield return MoveTransform(hand, rest, handTravelSeconds * 0.85f);
        hand.rotation = restRot;
        SetPokePose(leftHand, true);
    }

    void SpawnDemoHands()
    {
        ClearDemoProps();
        Transform focus = round != null ? round.GetLightTransform(demoGreenLightIndex) : null;
        Vector3 board = focus != null ? focus.position : new Vector3(0.41f, 1.35f, 2.2f);
        Vector3 towardViewer = ViewerApproachFrom(board);
        Vector3 side = ViewerSide(towardViewer);

        if (leftHandPrefab != null)
        {
            GameObject left = Instantiate(leftHandPrefab);
            left.name = "Demo_LeftHand";
            StripHand(left);
            // Place between camera and board (viewer side), not behind the console.
            left.transform.position = board + towardViewer * 0.42f + side * -0.22f + Vector3.up * -0.05f;
            FaceHandToward(left.transform, board);
            demoLeftHand = left.transform;
            demoLeftAnim = left.GetComponentInChildren<Animator>();
            demoLeftPose = AttachDemoPose(left);
            demoLeftPose.SetPoking(true);
            left.SetActive(false);
            demoProps.Add(left);
        }

        if (rightHandPrefab != null)
        {
            GameObject rightGo = Instantiate(rightHandPrefab);
            rightGo.name = "Demo_RightHand";
            StripHand(rightGo);
            rightGo.transform.position = board + towardViewer * 0.42f + side * 0.22f + Vector3.up * -0.05f;
            FaceHandToward(rightGo.transform, board);
            demoRightHand = rightGo.transform;
            demoRightAnim = rightGo.GetComponentInChildren<Animator>();
            demoRightPose = AttachDemoPose(rightGo);
            demoRightPose.SetPoking(true);
            rightGo.SetActive(false);
            demoProps.Add(rightGo);
        }
    }

    bool ResolveRound()
    {
        if (round != null)
            return true;
        round = ReactionLightRound.Instance;
        if (round == null)
            round = Object.FindFirstObjectByType<ReactionLightRound>(FindObjectsInactive.Include);
        return round != null;
    }

    /// <summary>Flat direction from board point toward the cinema/HMD viewer (front of board).</summary>
    Vector3 ViewerApproachFrom(Vector3 boardPoint)
    {
        Vector3 viewerPos = boardPoint + Vector3.back;
        if (sceneSetup != null
            && sceneSetup.TryGetShotPose(ReactionLightIntroSceneSetup.ShotId.BoardOverview, out Vector3 shotPos, out _))
        {
            viewerPos = shotPos;
        }
        else if (cinemaCam != null)
        {
            viewerPos = cinemaCam.transform.position;
        }
        else if (hmdCamera != null)
        {
            viewerPos = hmdCamera.transform.position;
        }
        else if (Camera.main != null)
        {
            viewerPos = Camera.main.transform.position;
        }

        Vector3 toCam = viewerPos - boardPoint;
        toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.001f)
            return toCam.normalized;

        // Console faces world -Z; default viewer side is -Z.
        return Vector3.back;
    }

    static Vector3 ViewerSide(Vector3 towardViewer)
    {
        Vector3 side = Vector3.Cross(Vector3.up, towardViewer);
        if (side.sqrMagnitude < 0.001f)
            side = Vector3.right;
        return side.normalized;
    }

    static void StripHand(GameObject root)
    {
        // Same as BallDropIntroDirector: Destroy (not DestroyImmediate) — Immediate crashes the Editor in Play Mode.
        HandPoseDriver[] poses = root.GetComponentsInChildren<HandPoseDriver>(true);
        for (int i = 0; i < poses.Length; i++)
        {
            poses[i].enabled = false;
            Object.Destroy(poses[i]);
        }
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = true;
            Object.Destroy(bodies[i]);
        }

        // Lighter GPU load during cinema (helps avoid D3D TDR with Scene+Game views open).
        SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            skins[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skins[i].receiveShadows = false;
            skins[i].updateWhenOffscreen = false;
        }
        MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshes[i].receiveShadows = false;
        }
    }

    static DemoIndexPokePose AttachDemoPose(GameObject root)
    {
        Animator anim = root.GetComponentInChildren<Animator>(true);
        GameObject host = anim != null ? anim.gameObject : root;
        DemoIndexPokePose pose = host.GetComponent<DemoIndexPokePose>();
        if (pose == null)
            pose = host.AddComponent<DemoIndexPokePose>();
        pose.Configure();
        return pose;
    }

    void SetPokePose(bool left, bool poking)
    {
        DemoIndexPokePose pose = left ? demoLeftPose : demoRightPose;
        if (pose != null)
        {
            pose.SetPoking(poking);
            return;
        }

        // Fallback: animator params only (index bone open may be missing).
        Animator anim = left ? demoLeftAnim : demoRightAnim;
        if (anim == null)
            return;
        if (HasParam(anim, "Grip"))
            anim.SetFloat("Grip", poking ? 0.7f : 0f);
        if (HasParam(anim, "Trigger"))
            anim.SetFloat("Trigger", poking ? 0f : 0f);
    }

    static bool HasParam(Animator anim, string name)
    {
        if (anim == null)
            return false;
        for (int i = 0; i < anim.parameterCount; i++)
        {
            if (anim.GetParameter(i).name == name)
                return true;
        }
        return false;
    }

    static Transform FindIndex3(Transform hand)
    {
        if (hand == null)
            return null;
        Transform tip = null;
        Transform[] children = hand.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string boneName = children[i].name;
            if (boneName.Contains("index_ignore"))
                continue;
            if (boneName.Contains("index3"))
                tip = children[i];
        }
        return tip;
    }

    /// <summary>Hand root world position that places index3 at desiredTipWorld (current rotation/pose).</summary>
    static Vector3 HandRootForTip(Transform hand, Vector3 desiredTipWorld)
    {
        Transform tip = FindIndex3(hand);
        if (tip == null)
            return desiredTipWorld;
        Vector3 localTip = hand.InverseTransformPoint(tip.position);
        return desiredTipWorld - hand.rotation * localTip;
    }

    static IEnumerator MoveHandSoTipReaches(Transform hand, Vector3 desiredTipWorld, float duration)
    {
        if (hand == null)
            yield break;

        Vector3 start = hand.position;
        Vector3 end = HandRootForTip(hand, desiredTipWorld);
        float d = Mathf.Max(0.05f, duration);
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            // Recalculate as index bones settle under poke pose.
            end = HandRootForTip(hand, desiredTipWorld);
            hand.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / d));
            yield return null;
        }

        Transform tip = FindIndex3(hand);
        if (tip != null)
            hand.position += desiredTipWorld - tip.position;
        else
            hand.position = end;
    }

    static void FaceHandToward(Transform hand, Vector3 target)
    {
        if (hand == null)
            return;
        Vector3 flat = target - hand.position;
        if (flat.sqrMagnitude < 0.0001f)
            return;
        hand.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(-15f, 0f, 0f);
    }

    void ResolveHandPrefabs()
    {
        if (leftHandPrefab != null && rightHandPrefab != null)
            return;
        PlayerHands hands = Resources.Load<PlayerHands>("PlayerHands");
        if (hands == null)
            return;
        if (leftHandPrefab == null)
            leftHandPrefab = hands.leftHandPrefab;
        if (rightHandPrefab == null)
            rightHandPrefab = hands.rightHandPrefab;
    }

    void ClearDemoProps()
    {
        for (int i = 0; i < demoProps.Count; i++)
        {
            if (demoProps[i] != null)
                Object.Destroy(demoProps[i]);
        }
        demoProps.Clear();
        demoLeftHand = null;
        demoRightHand = null;
        demoLeftAnim = null;
        demoRightAnim = null;
        demoLeftPose = null;
        demoRightPose = null;
    }

    void StopTalk()
    {
        if (rivera != null)
            rivera.SetTalking(false);
    }

    // --- Dialogue pad ---

    void SetupDialoguePad()
    {
        dialogueZone = GameObject.Find("Dialogue circle");
        if (dialogueZone == null)
        {
            Debug.LogError("ReactionLightIntroDirector: 'Dialogue circle' not found.");
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

        ReactionLightDialogueTrigger trigger = dialogueZone.GetComponent<ReactionLightDialogueTrigger>();
        if (trigger == null)
            trigger = dialogueZone.AddComponent<ReactionLightDialogueTrigger>();
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

    // --- Rivera talk / presenter placement ---

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

    void ResolveConsole()
    {
        if (consoleRoot != null)
            return;
        GameObject go = GameObject.Find("ReactionLightGeneratorConsole");
        if (go != null)
            consoleRoot = go.transform;
        else
        {
            ReactionLightBoard board = Object.FindFirstObjectByType<ReactionLightBoard>();
            if (board != null)
                consoleRoot = board.transform;
        }
    }

    void PlaceRiveraBesideConsole()
    {
        if (riveraRoot == null)
            return;

        ResolveConsole();
        if (!riveraPoseSaved)
        {
            riveraSavedPos = riveraRoot.position;
            riveraSavedRot = riveraRoot.rotation;
            riveraPoseSaved = true;
        }

        // Prefer scene-authored mark when present.
        if (sceneSetup != null && sceneSetup.riveraPresenterMark != null)
        {
            Transform mark = sceneSetup.riveraPresenterMark;
            riveraRoot.SetPositionAndRotation(mark.position, mark.rotation);
            return;
        }

        if (consoleRoot == null)
            return;

        Vector3 focus = GetBoardFocus();
        Vector3 consolePos = consoleRoot.position;
        Vector3 toFocus = focus - consolePos;
        toFocus.y = 0f;
        if (toFocus.sqrMagnitude < 0.001f)
            toFocus = ViewerApproachFrom(focus);
        toFocus.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, toFocus).normalized;
        // Viewer-left of console so he stays in BoardOverview with the lights.
        Vector3 stand = consolePos + side * riveraConsoleSideOffset + toFocus * riveraConsoleForwardOffset;
        stand.y = riveraSavedPos.y;

        Vector3 look = focus - stand;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f)
            look = toFocus;
        riveraRoot.SetPositionAndRotation(stand, Quaternion.LookRotation(look.normalized, Vector3.up));
    }

    void RestoreRivera()
    {
        if (riveraRoot == null || !riveraPoseSaved)
            return;
        riveraRoot.SetPositionAndRotation(riveraSavedPos, riveraSavedRot);
        riveraPoseSaved = false;
    }

    Vector3 GetBoardFocus()
    {
        if (sceneSetup != null && sceneSetup.boardFocus != null)
            return sceneSetup.boardFocus.position;

        Transform light = round != null ? round.GetLightTransform(demoGreenLightIndex) : null;
        if (light != null)
            return light.position;

        if (consoleRoot != null)
            return consoleRoot.position + Vector3.up * 1.35f;

        return new Vector3(0.41f, 1.35f, 2.2f);
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

    void SnapBoardShot()
    {
        Vector3 focus = GetBoardFocus();
        Vector3 lookTarget = focus;
        if (riveraRoot != null && riveraPoseSaved)
        {
            Vector3 riveraChest = riveraRoot.position + Vector3.up * 1.35f;
            lookTarget = Vector3.Lerp(focus, riveraChest, 0.38f);
        }

        Vector3 pos;
        Quaternion rot;
        if (sceneSetup != null && sceneSetup.TryGetShotPose(ReactionLightIntroSceneSetup.ShotId.BoardOverview, out pos, out rot))
        {
            // Pull back / re-aim so console lights and Rivera both stay in frame.
            Vector3 back = -(rot * Vector3.forward);
            back.y = 0f;
            if (back.sqrMagnitude < 0.001f)
                back = Vector3.back;
            back.Normalize();
            pos += back * 0.4f + Vector3.up * 0.1f;

            if (riveraRoot != null)
            {
                Vector3 toRivera = riveraRoot.position - focus;
                toRivera.y = 0f;
                if (toRivera.sqrMagnitude > 0.01f)
                    pos -= toRivera.normalized * 0.28f;
            }

            Vector3 aim = lookTarget - pos;
            if (aim.sqrMagnitude > 0.0001f)
                rot = Quaternion.LookRotation(aim.normalized, Vector3.up);
            ApplyCinemaPose(pos, rot);
            return;
        }

        Vector3 towardViewer = ViewerApproachFrom(focus);
        pos = focus + towardViewer * 1.85f + Vector3.up * 0.35f;
        if (riveraRoot != null)
        {
            Vector3 toRivera = riveraRoot.position - focus;
            toRivera.y = 0f;
            if (toRivera.sqrMagnitude > 0.01f)
                pos -= toRivera.normalized * 0.3f;
        }
        rot = Quaternion.LookRotation((lookTarget - pos).normalized, Vector3.up);
        ApplyCinemaPose(pos, rot);
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

    // --- Effects ---

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
        }

        effectsCanvas = canvasGo.GetComponent<Canvas>();
        if (effectsCanvas == null)
            effectsCanvas = canvasGo.AddComponent<Canvas>();
        effectsCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        effectsCanvas.worldCamera = hmdCamera;
        effectsCanvas.planeDistance = 0.45f;
        effectsCanvas.sortingOrder = 5000;

        if (canvasGo.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler s = canvasGo.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(2048, 1536);
        }

        if (fadeImage == null)
        {
            Transform t = canvasGo.transform.Find("Fade");
            if (t != null)
                fadeImage = t.GetComponent<Image>();
        }
        if (letterboxTop == null)
        {
            Transform t = canvasGo.transform.Find("LetterboxTop");
            if (t != null)
                letterboxTop = t.GetComponent<Image>();
        }
        if (letterboxBottom == null)
        {
            Transform t = canvasGo.transform.Find("LetterboxBottom");
            if (t != null)
                letterboxBottom = t.GetComponent<Image>();
        }

        if (sceneSetup != null && sceneSetup.guidePanel != null)
        {
            guidePanel = sceneSetup.guidePanel;
            guideText = sceneSetup.guideText;
        }
        else
            EnsureGuidePanel(canvasGo.transform);

        if (guideGroup == null && guidePanel != null)
            guideGroup = guidePanel.GetComponent<CanvasGroup>();
        SetGuideVisible(false);
    }

    void EnsureGuidePanel(Transform canvas)
    {
        Transform existing = canvas.Find("GuidePanel");
        GameObject panelGo = existing != null ? existing.gameObject : new GameObject("GuidePanel");
        if (existing == null)
            panelGo.transform.SetParent(canvas, false);
        guidePanel = panelGo.GetComponent<RectTransform>();
        if (guidePanel == null)
            guidePanel = panelGo.AddComponent<RectTransform>();
        guidePanel.anchorMin = new Vector2(0.12f, 0.1f);
        guidePanel.anchorMax = new Vector2(0.88f, 0.24f);
        guidePanel.offsetMin = Vector2.zero;
        guidePanel.offsetMax = Vector2.zero;
        guideGroup = panelGo.GetComponent<CanvasGroup>();
        if (guideGroup == null)
            guideGroup = panelGo.AddComponent<CanvasGroup>();
        Image plate = panelGo.GetComponent<Image>();
        if (plate == null)
            plate = panelGo.AddComponent<Image>();
        plate.color = new Color(0, 0, 0, 0.7f);

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
        EnsureEffectsUi();
        if (fadeImage == null)
            yield break;
        Color c = fadeImage.color;
        float start = c.a;
        float t = 0f;
        float d = Mathf.Max(0.05f, fadeSeconds);
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

    static IEnumerator MoveTransform(Transform target, Vector3 end, float duration)
    {
        if (target == null)
            yield break;
        Vector3 start = target.position;
        float d = Mathf.Max(0.05f, duration);
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            target.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / d));
            yield return null;
        }
        target.position = end;
    }
}

/// <summary>
/// Cutscene poke pose matching gameplay HandPoseDriver:
/// Grip high + Trigger low for fist blend, then force index bones to the cached open/straight pose.
/// </summary>
public class DemoIndexPokePose : MonoBehaviour
{
    // Same poke window as ReactionLightRound.IsPoking (grip >= 0.55, trigger <= 0.35).
    const float PokeGrip = 0.7f;
    const float PokeTrigger = 0f;
    const float RestGrip = 0f;
    const float RestTrigger = 0f;

    Animator animator;
    Transform[] indexBones;
    Quaternion[] openIndex;
    bool poking;
    bool configured;

    public void Configure()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Sample true open hand before caching — avoids caching a curled blend pose.
            ApplyAnimatorPose(RestGrip, RestTrigger);
            animator.Update(0f);
        }

        CacheOpenIndex();
        configured = true;
        SetPoking(false);
    }

    public void SetPoking(bool value)
    {
        poking = value;
        if (value)
            ApplyAnimatorPose(PokeGrip, PokeTrigger);
        else
            ApplyAnimatorPose(RestGrip, RestTrigger);
    }

    void ApplyAnimatorPose(float grip, float trigger)
    {
        if (animator == null)
            return;
        if (HasParam(animator, "Grip"))
            animator.SetFloat("Grip", grip);
        if (HasParam(animator, "Trigger"))
            animator.SetFloat("Trigger", trigger);
    }

    void LateUpdate()
    {
        if (!configured || !poking || indexBones == null || openIndex == null)
            return;

        // After animator blend (fist), force index fully straight — same intent as HandPoseDriver.
        for (int i = 0; i < indexBones.Length; i++)
        {
            if (indexBones[i] == null)
                continue;
            indexBones[i].localRotation = openIndex[i];
        }
    }

    void CacheOpenIndex()
    {
        List<Transform> bones = new List<Transform>();
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string boneName = children[i].name;
            if (boneName.Contains("index_ignore"))
                continue;
            if (boneName.Contains("index1") || boneName.Contains("index2") || boneName.Contains("index3"))
                bones.Add(children[i]);
        }

        indexBones = bones.ToArray();
        openIndex = new Quaternion[indexBones.Length];
        for (int i = 0; i < indexBones.Length; i++)
            openIndex[i] = indexBones[i].localRotation;
    }

    static bool HasParam(Animator anim, string name)
    {
        if (anim == null)
            return false;
        for (int i = 0; i < anim.parameterCount; i++)
        {
            if (anim.GetParameter(i).name == name)
                return true;
        }
        return false;
    }
}

public class ReactionLightDialogueTrigger : MonoBehaviour
{
    ReactionLightIntroDirector director;
    bool fired;

    public void Bind(ReactionLightIntroDirector host)
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
        if (other.GetComponent<CharacterController>() != null)
            return true;
        if (other.GetComponentInParent<CharacterController>() != null)
            return true;
        return other.CompareTag("Player");
    }
}
