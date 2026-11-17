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
/// Ball Drop intro: Objective Start → Dialogue circle → Rivera talk → scripted catch demo → unlock StartButton.
/// Mirrors ObjectPlacementIntroDirector (pad trigger, cinema HMD snap, scene-authored shots).
/// </summary>
[DisallowMultipleComponent]
public class BallDropIntroDirector : MonoBehaviour
{
    public const string WelcomeEnvironment =
        "Welcome to the Ball Drop station. A ball will drop from the designated source, and your task is to intercept it using your non-dominant hand controller. Watch the ball carefully and respond as soon as it begins to fall.";

    public const string WelcomeTraining =
        "The ball will drop from a height of 30 centimeters at a speed of 1.0 meter per second. Complete three sets of ten trials. Focus on observing the ball and moving your controller to intercept it.";

    static BallDropIntroDirector instance;

    [Header("Timing")]
    [SerializeField] float typeCharsPerSecond = 42f;
    [SerializeField] float pauseBetweenLines = 0.55f;
    [SerializeField] float greetHoldSeconds = 1.1f;
    [SerializeField] float fadeSeconds = 0.45f;
    [SerializeField] float beatPause = 1.15f;
    [SerializeField] float catchHoldSeconds = 1.15f;
    [SerializeField] float handReachSeconds = 0.28f;
    [SerializeField] float slightBlendSeconds = 0.55f;
    [SerializeField] float talkFontSize = 0.55f;
    [SerializeField] float guideFontSize = 36f;
    [SerializeField] float cinemaFov = 55f;

    [Header("Gameplay-matched demo")]
    [Tooltip("Same as BallDropSystem easy fall speed.")]
    [SerializeField] float demoFallSpeed = 0.6f;
    [SerializeField] float catchHeight = 1.15f;
    [SerializeField] float handRestHeight = 0.92f;
    [SerializeField] float missFloorY = 0.08f;
    [SerializeField] float preCatchLeadSeconds = 0.32f;

    [Header("Comfort")]
    [Tooltip("Lock cinema to Shot_PoleOverview only (no angle switches).")]
    [SerializeField] bool usePoleOverviewOnly = true;

    [Header("Rivera presenter")]
    [SerializeField] float riveraPoleSideOffset = 0.7f;
    [SerializeField] float riveraPoleForwardOffset = 0.15f;

    [Header("Demo Prefabs (auto from PlayerHands Resources if empty)")]
    [SerializeField] GameObject leftHandPrefab;
    [SerializeField] GameObject rightHandPrefab;
    [SerializeField] string leftBallTemplateName = "Basketball";
    [SerializeField] string rightBallTemplateName = "Volleyball";
    [Tooltip("Cutscene-only hangers (gameplay still uses full set).")]
    [SerializeField] string[] cutsceneHangerNames = { "Hanger_01", "Hanger_02", "Hanger_03", "Hanger_04" };

    public static bool HasCompleted { get; private set; }

    /// <summary>True when intro finished, or no intro director exists (guided / skip).</summary>
    public static bool IsTrainingUnlocked =>
        instance == null || HasCompleted;

    BallDropIntroSceneSetup sceneSetup;
    DrRiveraAnimator rivera;
    Transform riveraRoot;
    GameObject talkCanvas;
    TMP_Text talkText;
    GameObject objectiveScreen;
    GameObject dialogueZone;
    GameObject dialoguePadVisual;
    Collider dialoguePadCollider;
    XROrigin xrOrigin;
    Camera hmdCamera;
    Camera cinemaCam;
    Canvas effectsCanvas;
    Image fadeImage;
    Image vignetteImage;
    Image letterboxTop;
    Image letterboxBottom;
    RectTransform guidePanel;
    Image guidePlate;
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
    readonly List<MonoBehaviour> disabledPoseDrivers = new List<MonoBehaviour>();
    readonly List<GameObject> demoProps = new List<GameObject>();

    Transform demoLeftHand;
    Transform demoRightHand;
    Transform leftCatchPos;
    Transform rightCatchPos;
    Transform spawnLeft;
    Transform spawnRight;
    Transform missEnd;
    Transform poleScreen;
    Transform poleRoot;
    GameObject leftBallTemplate;
    GameObject rightBallTemplate;
    Animator demoLeftAnim;
    Animator demoRightAnim;
    Vector3 riveraSavedPos;
    Quaternion riveraSavedRot;
    bool riveraPoseSaved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    static void TryCreate(Scene scene)
    {
        if (!scene.IsValid() || !ModuleScenes.IsBallDrop(scene.name))
            return;
        if (ModuleScenes.IsGuided(scene.name))
            return;
        if (Object.FindAnyObjectByType<BallDropIntroDirector>() != null)
            return;

        GameObject host = new GameObject("BallDropIntroDirector");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<BallDropIntroDirector>();
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
        BallDropWalk.MovementLocked = true;

        sceneSetup = Object.FindAnyObjectByType<BallDropIntroSceneSetup>();
        objectiveScreen = GameObject.Find("ObjectiveScreen");
        xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        hmdCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        ResolveRivera();
        ResolveTalkUi();
        ResolveDemoAssets();
        SetupDialoguePad();
        EnsureEffectsUi();
        EnsureCinemaCamera();
        EnsureDemoAnchors();
        SetTalkVisible(false);
        SetPadVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
        RestorePoseDrivers();
        RestoreRivera();
        ClearDemoProps();
    }

    bool HandleStartPressed()
    {
        if (startConsumed || HasCompleted)
            return false;

        startConsumed = true;
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        BallDropWalk.MovementLocked = false;
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
        BallDropWalk.MovementLocked = true;
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
        else if (dialoguePadVisual != null)
        {
            dialoguePadVisual.SetActive(false);
        }
    }

    IEnumerator WelcomeThenCutscene()
    {
        // First dialogue stays at Rivera's original position — move only for cutscene.
        EnsureDemoAnchors();

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.Greet();
            yield return new WaitForSeconds(greetHoldSeconds);
            rivera.SetTalking(true);
        }

        SetTalkVisible(true);
        yield return TypeLine(WelcomeEnvironment);
        StopRiveraTalking();
        yield return new WaitForSeconds(pauseBetweenLines);

        if (rivera != null)
            rivera.SetTalking(true);
        yield return TypeLine(WelcomeTraining);
        StopRiveraTalking();
        yield return new WaitForSeconds(pauseBetweenLines);

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.9f);
            rivera.SetTalking(true);
        }

        yield return TypeLine("Watch this demonstration. I will show you how to catch with the correct hand.");
        StopRiveraTalking();
        yield return new WaitForSeconds(0.7f);

        yield return RunCinematicDemo();

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.PlayObserve();
        }

        SetTalkVisible(true);
        if (rivera != null)
            rivera.SetTalking(true);
        yield return TypeLine("Your turn. Walk to the pole and poke the Start button when you are ready.");
        StopRiveraTalking();
        yield return new WaitForSeconds(1.1f);

        SetTalkVisible(false);
        if (talkText != null)
            talkText.text = string.Empty;

        HasCompleted = true;
        BallDropWalk.MovementLocked = false;
        SetPadVisible(false);
        // Rivera already restored at end of cinematic demo; safety restore if demo skipped.
        if (riveraPoseSaved)
            RestoreRivera();
    }

    IEnumerator RunCinematicDemo()
    {
        cutsceneRunning = true;
        BallDropWalk.MovementLocked = true;
        SetTalkVisible(false);
        ClearDemoProps();
        EnsureDemoAnchors();
        if (!riveraPoseSaved)
            PlaceRiveraBesidePole();
        SpawnDemoHands();
        ApplyPoleScreenIcons();

        yield return Fade(1f);
        // Single comfort shot — no angle switches.
        SnapCinemaShot(BallDropIntroSceneSetup.ShotId.PoleOverview);
        BeginCinemaView();
        yield return null;
        yield return Fade(0f);
        yield return AnimateLetterbox(1f, 0.45f);

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.SetTalking(true);
        }

        yield return ShowGuide("Balls drop from the overhead dispensers — same as training.\nPole Screen shows Left / Right hand assignments.", beatPause);
        StopRiveraTalking();
        if (rivera != null)
            rivera.PlayObserve();

        // A. Left catch
        yield return ShowGuide("LEFT HAND CATCH\nBall falls — left hand intercepts.", 1.0f);
        yield return ScriptedCatch(true, true, "CATCH — Left hand");

        // B. Right catch
        yield return ShowGuide("RIGHT HAND CATCH\nBall falls — right hand intercepts.", 1.0f);
        yield return ScriptedCatch(false, false, "CATCH — Right hand");

        // C. Wrong hand then correct (left ball)
        yield return ShowGuide("WRONG HAND\nLeft-hand ball — right hand tries first.", 1.2f);
        yield return ScriptedWrongThenCorrect(true);

        // D. Wrong hand then correct (right ball)
        yield return ShowGuide("WRONG HAND\nRight-hand ball — left hand tries first.", 1.2f);
        yield return ScriptedWrongThenCorrect(false);

        // E. Miss
        yield return ShowGuide("MISSED BALL\nNo catch — ball keeps falling.", 1.2f);
        yield return ScriptedMiss(true);

        yield return ShowGuide("Demo complete.\nUse the correct hand for each ball.", 1.3f);

        yield return Fade(1f);
        yield return AnimateLetterbox(0f, 0.35f);
        EndCinemaView();
        ClearDemoProps();
        RestoreRivera();
        yield return Fade(0f);

        cutsceneRunning = false;
    }

    IEnumerator ScriptedCatch(bool ballForLeft, bool useLeftHand, string successGuide)
    {
        GameObject ball = SpawnDemoBall(ballForLeft);
        Transform hand = useLeftHand ? demoLeftHand : demoRightHand;
        Transform spawn = ballForLeft ? spawnLeft : spawnRight;
        Vector3 catchAt = CatchPoint(spawn);
        if (ball == null || hand == null || spawn == null)
            yield break;

        ball.transform.position = spawn.position;
        SetHandPose(useLeftHand, 0f);
        Vector3 handRest = HandRestPose(spawn, useLeftHand);
        hand.position = handRest;
        FaceHandUp(hand, catchAt);

        // Fall like gameplay; hand rises to meet the ball near catch height.
        yield return FallAndMeet(ball.transform, hand, catchAt, handRest, true);

        if (!BallDropDemoCatchLogic.TryCatch(ball, hand, ballForLeft, useLeftHand))
        {
            yield return ShowGuide("CATCH FAILED", 0.8f);
            yield break;
        }

        SetHandPose(useLeftHand, 1f);
        yield return SettleCaughtHand(hand, catchAt);
        yield return ShowGuide(successGuide, catchHoldSeconds);
        DestroyDemo(ball);
        hand.position = handRest;
        SetHandPose(useLeftHand, 0f);
    }

    IEnumerator ScriptedWrongThenCorrect(bool ballForLeft)
    {
        GameObject ball = SpawnDemoBall(ballForLeft);
        Transform wrongHand = ballForLeft ? demoRightHand : demoLeftHand;
        Transform correctHand = ballForLeft ? demoLeftHand : demoRightHand;
        Transform spawn = ballForLeft ? spawnLeft : spawnRight;
        Transform wrongSpawn = ballForLeft ? spawnRight : spawnLeft;
        if (ball == null || wrongHand == null || correctHand == null || spawn == null)
            yield break;

        Vector3 catchAt = CatchPoint(spawn);
        Vector3 wrongRest = HandRestPose(wrongSpawn != null ? wrongSpawn : spawn, !ballForLeft);
        Vector3 correctRest = HandRestPose(spawn, ballForLeft);
        wrongHand.position = wrongRest;
        correctHand.position = correctRest;
        ball.transform.position = spawn.position;
        SetHandPose(true, 0f);
        SetHandPose(false, 0f);

        // Fall toward catch height; wrong hand reaches across and fails validation.
        yield return FallToHeight(ball.transform, catchAt.y + 0.12f);
        FaceHandUp(wrongHand, catchAt);
        yield return MoveTransform(wrongHand, catchAt + Vector3.down * 0.02f, handReachSeconds);

        if (BallDropDemoCatchLogic.TryCatch(ball, wrongHand, ballForLeft, !ballForLeft))
            ball.transform.SetParent(null, true);

        yield return ShowGuide("WRONG HAND — catch blocked", 0.9f);
        yield return MoveTransform(wrongHand, wrongRest, handReachSeconds);

        // Ball keeps falling; correct hand rises and catches.
        FaceHandUp(correctHand, catchAt);
        bool caught = false;
        float speed = Mathf.Max(0.15f, demoFallSpeed);
        while (ball != null && ball.transform.position.y > catchAt.y)
        {
            ball.transform.position += Vector3.down * speed * Time.deltaTime;
            float dist = ball.transform.position.y - catchAt.y;
            if (dist <= speed * preCatchLeadSeconds + 0.05f && !caught)
            {
                correctHand.position = Vector3.MoveTowards(
                    correctHand.position, catchAt, (catchAt - correctRest).magnitude / Mathf.Max(0.05f, handReachSeconds) * Time.deltaTime);
            }

            if (!caught && ball.transform.position.y <= catchAt.y + 0.04f)
            {
                correctHand.position = catchAt;
                if (BallDropDemoCatchLogic.TryCatch(ball, correctHand, ballForLeft, ballForLeft))
                {
                    SetHandPose(ballForLeft, 1f);
                    caught = true;
                    break;
                }
            }

            yield return null;
        }

        if (!caught)
        {
            correctHand.position = catchAt;
            caught = BallDropDemoCatchLogic.TryCatch(ball, correctHand, ballForLeft, ballForLeft);
            if (caught)
                SetHandPose(ballForLeft, 1f);
        }

        if (!caught)
        {
            yield return ShowGuide("Expected correct catch failed", 0.8f);
            DestroyDemo(ball);
            correctHand.position = correctRest;
            yield break;
        }

        yield return SettleCaughtHand(correctHand, catchAt);
        yield return ShowGuide(ballForLeft ? "CORRECT — Left hand catches" : "CORRECT — Right hand catches", catchHoldSeconds);
        DestroyDemo(ball);
        correctHand.position = correctRest;
        SetHandPose(ballForLeft, 0f);
    }

    IEnumerator ScriptedMiss(bool ballForLeft)
    {
        GameObject ball = SpawnDemoBall(ballForLeft);
        Transform hand = ballForLeft ? demoLeftHand : demoRightHand;
        Transform spawn = ballForLeft ? spawnLeft : spawnRight;
        if (ball == null || hand == null || spawn == null)
            yield break;

        Vector3 catchAt = CatchPoint(spawn);
        Vector3 handRest = HandRestPose(spawn, ballForLeft);
        hand.position = handRest;
        ball.transform.position = spawn.position;
        SetHandPose(ballForLeft, 0f);

        // Hand stays put; ball falls past catch height to the floor (gameplay miss).
        yield return FallToHeight(ball.transform, catchAt.y + 0.05f);
        yield return ShowGuide("Hand misses…", 0.55f);
        yield return FallToHeight(ball.transform, missFloorY);
        yield return ShowGuide("MISS — ball continues past the hand", catchHoldSeconds);
        DestroyDemo(ball);
        hand.position = handRest;
    }

    IEnumerator FallAndMeet(Transform ball, Transform hand, Vector3 catchAt, Vector3 handRest, bool raiseHand)
    {
        if (ball == null)
            yield break;

        float speed = Mathf.Max(0.15f, demoFallSpeed);
        bool reaching = false;
        while (ball.position.y > catchAt.y)
        {
            ball.position += Vector3.down * speed * Time.deltaTime;

            if (raiseHand && hand != null)
            {
                float dist = ball.position.y - catchAt.y;
                if (dist <= speed * preCatchLeadSeconds + 0.08f)
                {
                    if (!reaching)
                    {
                        FaceHandUp(hand, catchAt);
                        reaching = true;
                    }

                    hand.position = Vector3.MoveTowards(
                        hand.position,
                        catchAt,
                        Vector3.Distance(handRest, catchAt) / Mathf.Max(0.05f, handReachSeconds) * Time.deltaTime);
                }
            }

            yield return null;
        }

        ball.position = new Vector3(catchAt.x, catchAt.y, catchAt.z);
        if (hand != null)
            hand.position = catchAt;
    }

    IEnumerator FallToHeight(Transform ball, float targetY)
    {
        if (ball == null)
            yield break;

        float speed = Mathf.Max(0.15f, demoFallSpeed);
        while (ball.position.y > targetY)
        {
            ball.position += Vector3.down * speed * Time.deltaTime;
            yield return null;
        }

        Vector3 p = ball.position;
        p.y = targetY;
        ball.position = p;
    }

    IEnumerator SettleCaughtHand(Transform hand, Vector3 catchAt)
    {
        if (hand == null)
            yield break;

        Vector3 held = catchAt + Vector3.down * 0.04f + Vector3.back * 0.02f;
        yield return MoveTransform(hand, held, 0.18f);
    }

    static void FaceHandUp(Transform hand, Vector3 target)
    {
        if (hand == null)
            return;
        Vector3 flat = target - hand.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;
        hand.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up)
                        * Quaternion.Euler(-25f, 0f, 0f);
    }

    Vector3 CatchPoint(Transform spawn)
    {
        if (spawn == null)
            return new Vector3(0f, catchHeight, 1.7f);
        return new Vector3(spawn.position.x, catchHeight, spawn.position.z);
    }

    Vector3 HandRestPose(Transform spawn, bool leftHand)
    {
        Vector3 catchAt = CatchPoint(spawn);
        float side = leftHand ? -0.04f : 0.04f;
        return new Vector3(catchAt.x + side, handRestHeight, catchAt.z + 0.05f);
    }

    // --- Demo assets / anchors ---

    void ResolveDemoAssets()
    {
        if (leftHandPrefab == null || rightHandPrefab == null)
        {
            PlayerHands hands = Resources.Load<PlayerHands>("PlayerHands");
            if (hands != null)
            {
                if (leftHandPrefab == null)
                    leftHandPrefab = hands.leftHandPrefab;
                if (rightHandPrefab == null)
                    rightHandPrefab = hands.rightHandPrefab;
            }
        }

        leftBallTemplate = FindRootNamed(leftBallTemplateName);
        rightBallTemplate = FindRootNamed(rightBallTemplateName);
    }

    void EnsureDemoAnchors()
    {
        if (sceneSetup != null)
        {
            poleScreen = sceneSetup.poleScreen;
            leftCatchPos = sceneSetup.leftCatchAnchor;
            rightCatchPos = sceneSetup.rightCatchAnchor;
            missEnd = sceneSetup.missEnd;
        }

        if (poleScreen == null)
        {
            GameObject ps = GameObject.Find("PoleScreen");
            if (ps != null)
                poleScreen = ps.transform;
        }

        // Cutscene-only: Hanger_01..04 (centered pair for left/right).
        spawnLeft = null;
        spawnRight = null;
        ResolveCutsceneHangers();

        Transform anchors = transform.Find("DemoAnchors");
        if (anchors == null)
        {
            GameObject go = new GameObject("DemoAnchors");
            go.transform.SetParent(transform, false);
            anchors = go.transform;
        }

        if (spawnLeft == null || spawnRight == null)
        {
            Vector3 stance = new Vector3(-0.72f, 2.4f, 1.87f);
            spawnLeft = EnsureAnchor(anchors, "SpawnLeft", spawnLeft, stance + Vector3.left * 0.2f);
            spawnRight = EnsureAnchor(anchors, "SpawnRight", spawnRight, stance + Vector3.right * 0.2f);
        }

        leftCatchPos = EnsureAnchor(anchors, "LeftCatch", leftCatchPos, CatchPoint(spawnLeft));
        rightCatchPos = EnsureAnchor(anchors, "RightCatch", rightCatchPos, CatchPoint(spawnRight));
        missEnd = EnsureAnchor(anchors, "MissEnd", missEnd,
            new Vector3(spawnLeft.position.x, missFloorY, spawnLeft.position.z));

        leftCatchPos.position = CatchPoint(spawnLeft);
        rightCatchPos.position = CatchPoint(spawnRight);
        missEnd.position = new Vector3(spawnLeft.position.x, missFloorY, spawnLeft.position.z);

        ResolvePoleRoot();
    }

    void ResolvePoleRoot()
    {
        if (poleScreen != null)
        {
            poleRoot = poleScreen.parent != null ? poleScreen.parent : poleScreen;
            return;
        }

        GameObject arc = GameObject.Find("BallDropOverheadArc");
        if (arc != null)
            poleRoot = arc.transform;
    }

    /// <summary>
    /// Cutscene drops only from Hanger_01..04. Uses the two center hangers (by X) for L/R
    /// so hands + balls stay framed in Shot_PoleOverview.
    /// </summary>
    void ResolveCutsceneHangers()
    {
        string[] names = cutsceneHangerNames != null && cutsceneHangerNames.Length > 0
            ? cutsceneHangerNames
            : new[] { "Hanger_01", "Hanger_02", "Hanger_03", "Hanger_04" };

        List<Transform> hangers = new List<Transform>();
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < names.Length; i++)
        {
            Transform hanger = FindNamedTransform(all, names[i]);
            if (hanger == null)
                continue;
            hangers.Add(GetSpawnFromHanger(hanger));
        }

        if (hangers.Count == 0)
            return;

        hangers.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        // Center the action: pick middle pair when 4 hangers, else ends.
        if (hangers.Count >= 4)
        {
            spawnLeft = hangers[1];
            spawnRight = hangers[2];
        }
        else if (hangers.Count >= 2)
        {
            spawnLeft = hangers[0];
            spawnRight = hangers[hangers.Count - 1];
        }
        else
        {
            spawnLeft = hangers[0];
            spawnRight = hangers[0];
        }
    }

    static Transform FindNamedTransform(Transform[] all, string name)
    {
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name)
                return all[i];
        }
        return null;
    }

    static Transform GetSpawnFromHanger(Transform hanger)
    {
        if (hanger == null)
            return null;

        for (int i = 0; i < hanger.childCount; i++)
        {
            Transform c = hanger.GetChild(i);
            if (c != null && (c.name == "SpawnPoint" || c.name.StartsWith("SpawnPoint")))
                return c;
        }

        return hanger;
    }

    void StopRiveraTalking()
    {
        if (rivera == null)
            return;
        rivera.SetTalking(false);
    }

    void PlaceRiveraBesidePole()
    {
        if (riveraRoot == null)
            return;

        ResolvePoleRoot();
        riveraSavedPos = riveraRoot.position;
        riveraSavedRot = riveraRoot.rotation;
        riveraPoseSaved = true;

        Transform pole = poleScreen != null ? poleScreen : poleRoot;
        if (pole == null)
            return;

        Vector3 focus = GetDropFocus();
        Vector3 polePos = pole.position;
        Vector3 toFocus = focus - polePos;
        toFocus.y = 0f;
        if (toFocus.sqrMagnitude < 0.001f)
            toFocus = -pole.forward;
        toFocus.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, toFocus).normalized;
        // Stand to the viewer's left of the pole so he stays in PoleOverview frame.
        Vector3 stand = polePos + side * riveraPoleSideOffset + toFocus * riveraPoleForwardOffset;
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

    Vector3 GetDropFocus()
    {
        Vector3 left = spawnLeft != null ? spawnLeft.position : new Vector3(-0.25f, 2.4f, 1.9f);
        Vector3 right = spawnRight != null ? spawnRight.position : new Vector3(0.25f, 2.4f, 1.9f);
        Vector3 focus = (left + right) * 0.5f;
        focus.y = catchHeight;
        return focus;
    }

    static Transform EnsureAnchor(Transform parent, string name, Transform existing, Vector3 worldPos)
    {
        if (existing != null)
            return existing;

        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }

        t.position = worldPos;
        return t;
    }

    void SpawnDemoHands()
    {
        if (demoLeftHand != null || demoRightHand != null)
            ClearDemoProps();

        if (leftHandPrefab != null)
        {
            GameObject left = Instantiate(leftHandPrefab);
            left.name = "Demo_LeftHand";
            StripGameplayComponents(left);
            Vector3 rest = HandRestPose(spawnLeft, true);
            left.transform.position = rest;
            FaceHandUp(left.transform, CatchPoint(spawnLeft));
            demoLeftHand = left.transform;
            demoLeftAnim = left.GetComponentInChildren<Animator>();
            demoProps.Add(left);
        }

        if (rightHandPrefab != null)
        {
            GameObject right = Instantiate(rightHandPrefab);
            right.name = "Demo_RightHand";
            StripGameplayComponents(right);
            Vector3 rest = HandRestPose(spawnRight, false);
            right.transform.position = rest;
            FaceHandUp(right.transform, CatchPoint(spawnRight));
            demoRightHand = right.transform;
            demoRightAnim = right.GetComponentInChildren<Animator>();
            demoProps.Add(right);
        }
    }

    static void StripGameplayComponents(GameObject root)
    {
        // Demo-only visuals — remove XR / pose drivers that fight scripted motion.
        HandPoseDriver[] poses = root.GetComponentsInChildren<HandPoseDriver>(true);
        for (int i = 0; i < poses.Length; i++)
            Object.Destroy(poses[i]);

        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
            Object.Destroy(bodies[i]);
    }

    GameObject SpawnDemoBall(bool forLeft)
    {
        GameObject template = forLeft ? leftBallTemplate : rightBallTemplate;
        if (template == null)
            template = forLeft ? rightBallTemplate : leftBallTemplate;
        if (template == null)
            return CreateFallbackBall(forLeft);

        // Match BallDropSystem.SpawnWrapped: root + fitted visual at gameplay diameter.
        float diameter = ResolveGameplayBallDiameter();
        GameObject root = new GameObject(forLeft ? "DemoBall_Left" : "DemoBall_Right");

        bool wasActive = template.activeSelf;
        template.SetActive(true);
        GameObject visual = Instantiate(template);
        template.SetActive(wasActive);

        visual.name = template.name;
        visual.SetActive(true);
        StripBallGameplay(visual);
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        FitBallVisual fit = visual.GetComponent<FitBallVisual>();
        if (fit == null)
            fit = visual.AddComponent<FitBallVisual>();
        fit.targetDiameter = diameter;
        fit.Fit();

        DropBall drop = root.GetComponent<DropBall>();
        if (drop == null)
            drop = root.AddComponent<DropBall>();
        drop.forLeftHand = forLeft;

        BallDropDemoBallMarker marker = root.GetComponent<BallDropDemoBallMarker>();
        if (marker == null)
            marker = root.AddComponent<BallDropDemoBallMarker>();
        marker.forLeftHand = forLeft;

        demoProps.Add(root);
        return root;
    }

    float ResolveGameplayBallDiameter()
    {
        BallDropSystem system = Object.FindFirstObjectByType<BallDropSystem>(FindObjectsInactive.Include);
        if (system != null && system.targetDiameter > 0.001f)
            return system.targetDiameter;
        return 0.15f;
    }

    GameObject CreateFallbackBall(bool forLeft)
    {
        float diameter = ResolveGameplayBallDiameter();
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = forLeft ? "DemoBall_Left" : "DemoBall_Right";
        ball.transform.localScale = Vector3.one * diameter;
        Renderer r = ball.GetComponent<Renderer>();
        if (r != null)
            r.material.color = forLeft ? new Color(0.9f, 0.35f, 0.15f) : new Color(0.2f, 0.55f, 0.95f);
        Collider c = ball.GetComponent<Collider>();
        if (c != null)
            Object.Destroy(c);
        BallDropDemoBallMarker marker = ball.AddComponent<BallDropDemoBallMarker>();
        marker.forLeftHand = forLeft;
        demoProps.Add(ball);
        return ball;
    }

    static void StripBallGameplay(GameObject ball)
    {
        BallFall fall = ball.GetComponent<BallFall>();
        if (fall != null)
            Object.Destroy(fall);
        BallCollision col = ball.GetComponent<BallCollision>();
        if (col != null)
            Object.Destroy(col);

        var grab = ball.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            Object.Destroy(grab);

        Rigidbody body = ball.GetComponent<Rigidbody>();
        if (body != null)
            Object.Destroy(body);

        Collider[] cols = ball.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            Object.Destroy(cols[i]);
    }

    void ApplyPoleScreenIcons()
    {
        BallAssignController assign = Object.FindFirstObjectByType<BallAssignController>(FindObjectsInactive.Include);
        if (assign == null)
            return;

        // Deterministic icons for demo: left/right templates' sprites if available.
        if (assign.leftHandImage != null && assign.sportsBalls != null)
        {
            Sprite leftIcon = FindIcon(assign, leftBallTemplateName);
            if (leftIcon != null)
            {
                assign.leftHandImage.sprite = leftIcon;
                assign.leftHandImage.preserveAspect = true;
                assign.leftHandImage.enabled = true;
            }
        }

        if (assign.rightHandImage != null && assign.sportsBalls != null)
        {
            Sprite rightIcon = FindIcon(assign, rightBallTemplateName);
            if (rightIcon != null)
            {
                assign.rightHandImage.sprite = rightIcon;
                assign.rightHandImage.preserveAspect = true;
                assign.rightHandImage.enabled = true;
            }
        }
    }

    static Sprite FindIcon(BallAssignController assign, string ballName)
    {
        if (assign.sportsBalls == null)
            return null;
        for (int i = 0; i < assign.sportsBalls.Length; i++)
        {
            if (assign.sportsBalls[i] != null && assign.sportsBalls[i].ballName == ballName)
                return assign.sportsBalls[i].icon;
        }
        return null;
    }

    void SetHandPose(bool left, float grip)
    {
        Animator anim = left ? demoLeftAnim : demoRightAnim;
        if (anim == null)
            return;
        if (HasParam(anim, "Grip"))
            anim.SetFloat("Grip", grip);
        if (HasParam(anim, "Trigger"))
            anim.SetFloat("Trigger", 0f);
    }

    static bool HasParam(Animator anim, string name)
    {
        for (int i = 0; i < anim.parameterCount; i++)
        {
            if (anim.GetParameter(i).name == name)
                return true;
        }
        return false;
    }

    void DestroyDemo(GameObject go)
    {
        if (go == null)
            return;
        demoProps.Remove(go);
        Object.Destroy(go);
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
    }

    static GameObject FindRootNamed(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].parent == null && transforms[i].name == objectName)
                return transforms[i].gameObject;
        }
        return null;
    }

    // --- Dialogue pad (same architecture as Color Sort) ---

    void SetupDialoguePad()
    {
        dialogueZone = GameObject.Find("Dialogue circle");
        if (dialogueZone == null)
        {
            Debug.LogError("BallDropIntroDirector: required scene object 'Dialogue circle' was not found.");
            return;
        }

        Transform leftoverProbe = dialogueZone.transform.Find("IntroTrigger");
        if (leftoverProbe != null)
            Destroy(leftoverProbe.gameObject);

        MeshRenderer circleMr = dialogueZone.GetComponent<MeshRenderer>();
        if (circleMr != null)
        {
            circleMr.enabled = true;
            circleMr.sharedMaterial = MakePadMaterial();
        }
        dialoguePadVisual = dialogueZone;

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

        BallDropDialogueTrigger trigger = dialogueZone.GetComponent<BallDropDialogueTrigger>();
        if (trigger == null)
            trigger = dialogueZone.AddComponent<BallDropDialogueTrigger>();
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

    // --- Rivera talk UI ---

    void ResolveRivera()
    {
        GameObject riveraGo = GameObject.Find("Dr. Rivera");
        if (riveraGo == null)
            return;
        riveraRoot = riveraGo.transform;
        rivera = riveraGo.GetComponent<DrRiveraAnimator>();
        if (rivera == null)
            rivera = riveraGo.AddComponent<DrRiveraAnimator>();
        Animator anim = riveraGo.GetComponent<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;
    }

    void ResolveTalkUi()
    {
        if (riveraRoot == null)
            return;
        Transform talk = riveraRoot.Find("Talk");
        if (talk == null)
        {
            foreach (Transform t in riveraRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Talk")
                {
                    talk = t;
                    break;
                }
            }
        }
        if (talk == null)
            return;

        talkCanvas = talk.gameObject;
        talkText = talk.GetComponentInChildren<TMP_Text>(true);
        if (talkText == null)
            return;

        talkText.text = string.Empty;
        talkText.alignment = TextAlignmentOptions.TopLeft;
        talkText.enableAutoSizing = false;
        float fixedSize = talkFontSize > 0.05f ? talkFontSize : 0.55f;
        talkText.fontSize = fixedSize;
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

    // --- Cinema (HMD snap — same as Color Sort; does not move XR Origin) ---

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
        cinemaCam.nearClipPlane = 0.05f;
        cinemaCam.fieldOfView = cinemaFov;
        AudioListener listener = camGo.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);
    }

    void SnapCinemaShot(BallDropIntroSceneSetup.ShotId shotId)
    {
        if (usePoleOverviewOnly)
            shotId = BallDropIntroSceneSetup.ShotId.PoleOverview;

        Vector3 pos;
        Quaternion rot;
        ResolveShotPose(shotId, out pos, out rot);
        ApplyCinemaPose(pos, rot);
    }

    IEnumerator SlightBlend(BallDropIntroSceneSetup.ShotId shotId)
    {
        // Locked to PoleOverview — no switches.
        if (usePoleOverviewOnly)
            yield break;

        Vector3 to;
        Quaternion toRot;
        ResolveShotPose(shotId, out to, out toRot);
        yield return BlendPose(to, toRot, slightBlendSeconds);
    }

    IEnumerator BlendPose(Vector3 endPos, Quaternion endRot, float duration)
    {
        EnsureCinemaCamera();
        if (cinemaCam == null)
            yield break;

        Vector3 startPos = cinemaCam.transform.position;
        Quaternion startRot = cinemaCam.transform.rotation;
        float d = Mathf.Max(0.05f, duration);
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / d);
            ApplyCinemaPose(Vector3.Lerp(startPos, endPos, u), Quaternion.Slerp(startRot, endRot, u));
            yield return null;
        }
        ApplyCinemaPose(endPos, endRot);
    }

    void ResolveShotPose(BallDropIntroSceneSetup.ShotId shotId, out Vector3 pos, out Quaternion rot)
    {
        if (sceneSetup == null)
            sceneSetup = Object.FindAnyObjectByType<BallDropIntroSceneSetup>();

        if (usePoleOverviewOnly)
            shotId = BallDropIntroSceneSetup.ShotId.PoleOverview;

        // Use authored Shot_PoleOverview position AND rotation as-is (Scene View editable).
        if (sceneSetup != null && sceneSetup.TryGetShotPose(shotId, out pos, out rot))
            return;

        // Fallback only when IntroCutsceneControls / shot marker is missing.
        Vector3 focus = GetDropFocus();
        focus.y = catchHeight + 0.2f;
        Vector3 stance = new Vector3(-0.72f, catchHeight, 1.87f);
        Vector3 view = focus - stance;
        view.y = 0f;
        if (view.sqrMagnitude < 0.001f)
            view = Vector3.forward;
        view.Normalize();

        Vector3 camForward = -view;
        pos = focus + camForward * 1.7f + Vector3.up * 0.3f;
        rot = Quaternion.LookRotation((focus - pos).normalized, Vector3.up);
    }

    void ApplyCinemaPose(Vector3 pos, Quaternion rot)
    {
        EnsureCinemaCamera();
        if (cinemaCam == null)
            return;
        cinemaCam.transform.SetPositionAndRotation(pos, rot);
        if (cinemaFov > 10f)
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
        if (cinemaFov > 10f)
        {
            cinemaCam.fieldOfView = cinemaFov;
            hmdCamera.fieldOfView = cinemaFov;
        }
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
            string n = b.GetType().Name;
            if (n.IndexOf("TrackedPose", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!disabledPoseDrivers.Contains(b))
                {
                    b.enabled = false;
                    disabledPoseDrivers.Add(b);
                }
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

    // --- Effects / guides ---

    void EnsureEffectsUi()
    {
        if (hmdCamera == null)
            hmdCamera = Camera.main;
        if (hmdCamera == null)
            return;

        GameObject canvasGo = null;
        if (sceneSetup != null && sceneSetup.effectsCanvas != null)
            canvasGo = sceneSetup.effectsCanvas.gameObject;
        if (canvasGo == null)
            canvasGo = GameObject.Find("IntroEffectsCanvas");

        if (canvasGo == null)
        {
            canvasGo = new GameObject("IntroEffectsCanvas");
            fadeImage = MakeFullImage(canvasGo.transform, "Fade", new Color(0f, 0f, 0f, 0f), 0);
            vignetteImage = MakeFullImage(canvasGo.transform, "Vignette", new Color(0f, 0f, 0f, 0f), 1);
            letterboxTop = MakeBar(canvasGo.transform, "LetterboxTop", true);
            letterboxBottom = MakeBar(canvasGo.transform, "LetterboxBottom", false);
        }

        effectsCanvas = canvasGo.GetComponent<Canvas>();
        if (effectsCanvas == null)
            effectsCanvas = canvasGo.AddComponent<Canvas>();
        effectsCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        effectsCanvas.worldCamera = hmdCamera;
        effectsCanvas.planeDistance = 0.45f;
        effectsCanvas.overrideSorting = true;
        effectsCanvas.sortingOrder = 5000;

        if (canvasGo.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048f, 1536f);
        }

        if (fadeImage == null)
        {
            Transform fadeT = canvasGo.transform.Find("Fade");
            if (fadeT != null)
                fadeImage = fadeT.GetComponent<Image>();
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
            guidePlate = sceneSetup.guidePlate;
            guideText = sceneSetup.guideText;
        }
        else
        {
            EnsureGuidePanel(canvasGo.transform);
        }

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
        guidePanel.anchorMin = new Vector2(0.18f, 0.08f);
        guidePanel.anchorMax = new Vector2(0.82f, 0.22f);
        guidePanel.offsetMin = Vector2.zero;
        guidePanel.offsetMax = Vector2.zero;

        guideGroup = panelGo.GetComponent<CanvasGroup>();
        if (guideGroup == null)
            guideGroup = panelGo.AddComponent<CanvasGroup>();

        Transform plateT = panelGo.transform.Find("GuidePlate");
        GameObject plateGo = plateT != null ? plateT.gameObject : new GameObject("GuidePlate");
        if (plateT == null)
            plateGo.transform.SetParent(panelGo.transform, false);
        guidePlate = plateGo.GetComponent<Image>();
        if (guidePlate == null)
            guidePlate = plateGo.AddComponent<Image>();
        guidePlate.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform plateRt = guidePlate.rectTransform;
        plateRt.anchorMin = Vector2.zero;
        plateRt.anchorMax = Vector2.one;
        plateRt.offsetMin = Vector2.zero;
        plateRt.offsetMax = Vector2.zero;

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
        RectTransform textRt = guideText.rectTransform;
        textRt.anchorMin = new Vector2(0.04f, 0.1f);
        textRt.anchorMax = new Vector2(0.96f, 0.9f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
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
        img.color = new Color(0f, 0f, 0f, 0f);
        RectTransform rt = img.rectTransform;
        if (top)
        {
            rt.anchorMin = new Vector2(0f, 0.88f);
            rt.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.12f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    IEnumerator Fade(float targetAlpha)
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
            float u = Mathf.Clamp01(t / d);
            c.a = Mathf.Lerp(start, targetAlpha, u);
            fadeImage.color = c;
            yield return null;
        }
        c.a = targetAlpha;
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
            float a = Mathf.Lerp(startTop, target, u);
            if (letterboxTop != null)
            {
                Color c = letterboxTop.color;
                c.a = a;
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
            float u = Mathf.SmoothStep(0f, 1f, t / d);
            target.position = Vector3.Lerp(start, end, u);
            yield return null;
        }
        target.position = end;
    }
}

/// <summary>Left/right validation for scripted demo catches (mirrors BallCollision hand check).</summary>
public static class BallDropDemoCatchLogic
{
    public static bool IsCorrectHand(bool ballForLeft, bool handIsLeft)
    {
        return ballForLeft == handIsLeft;
    }

    public static bool TryCatch(GameObject ball, Transform hand, bool ballForLeft, bool handIsLeft)
    {
        if (ball == null || hand == null)
            return false;

        if (!IsCorrectHand(ballForLeft, handIsLeft))
            return false;

        ball.transform.SetParent(hand, true);
        // Palm-ish hold offset so the catch reads clearly.
        ball.transform.localPosition = new Vector3(0f, 0.03f, 0.08f);
        ball.transform.localRotation = Quaternion.identity;
        return true;
    }
}

public class BallDropDemoBallMarker : MonoBehaviour
{
    public bool forLeftHand;
}

/// <summary>Trigger on the scene Dialogue circle CapsuleCollider.</summary>
public class BallDropDialogueTrigger : MonoBehaviour
{
    BallDropIntroDirector director;
    bool fired;

    public void Bind(BallDropIntroDirector host)
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
        if (other.CompareTag("Player"))
            return true;
        return false;
    }
}
