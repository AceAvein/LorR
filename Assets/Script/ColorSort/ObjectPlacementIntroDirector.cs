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
/// Color Sort intro:
/// Btn_Start → unlock walk → visible Dialogue pad → Rivera welcome
/// → cinematic cam cutscene (player body stays) with letterbox/fade/guides
/// → longer conveyor demos → back to HMD → arm Phase I.
/// </summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class ObjectPlacementIntroDirector : MonoBehaviour
{
    const string WelcomeEnvironment =
        "Welcome to the object sorting station. In this activity, you will pick up a variety of items and place them into their corresponding colored boxes. This task focuses on controlled object handling, visual sorting, and movement coordination using your non-dominant hand.";

    const string WelcomeTraining =
        "Now, let us begin your non-dominant hand training. Check the item's category and the corresponding box color. Use the controller button indicated on the instruction prompt to pick up the item, move it to the correct box, and release it inside.";

    static ObjectPlacementIntroDirector instance;

    public static bool HasCompleted { get; private set; }

    [Header("Timing")]
    [SerializeField] float typeCharsPerSecond = 44f;
    [SerializeField] float pauseBetweenLines = 1.1f;
    [SerializeField] float greetHoldSeconds = 1.25f;
    [SerializeField] float fadeSeconds = 0.65f;
    [SerializeField] float beltRideSeconds = 2.4f;
    [SerializeField] float toBoxSeconds = 1.15f;
    [SerializeField] float beatPause = 2.0f;
    [SerializeField] float camBlendSeconds = 1.1f;
    [SerializeField] float talkFontSize = 0.55f;
    [SerializeField] float pageHoldSeconds = 1.35f;
    [SerializeField] float guideFontSize = 32f;

    ObjectPlacementStation station;
    ObjectPlacementConveyorController conveyor;
    ObjectPlacementPhaseIRound phaseRound;
    DrRiveraAnimator rivera;
    Transform riveraRoot;
    XROrigin xrOrigin;
    Camera hmdCamera;
    GameObject objectiveScreen;
    GameObject talkCanvas;
    TMP_Text talkText;
    GameObject dialogueZone;
    GameObject dialoguePadVisual;

    Image fadeImage;
    Image letterboxTop;
    Image letterboxBottom;
    Image vignetteImage;
    Image guidePlate;
    TMP_Text guideText;
    Canvas effectsCanvas;
    CapsuleCollider dialoguePadCollider;

    Camera cinemaCam;
    Transform cinemaRig;
    readonly List<Behaviour> disabledPoseDrivers = new List<Behaviour>();
    readonly List<Renderer> highlightRenderers = new List<Renderer>();
    readonly List<Color> highlightRestore = new List<Color>();

    Vector3 riveraSavedPos;
    Quaternion riveraSavedRot;
    Vector3 camSavedWorldPos;
    Quaternion camSavedWorldRot;
    bool camPoseSaved;
    bool cinemaActive;

    bool startConsumed;
    bool dialogueStarted;
    bool cutsceneRunning;
    BoxAnimState pendingYellowRestore;
    BoxAnimState pendingRedRestore;
    bool hasPendingBoxRestore;

    struct DemoBeat
    {
        public string id;
        public ObjectPlacementGroceryKind kind;
        public string guide;
        public string tip;
    }

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
        if (!scene.IsValid() || !ModuleScenes.IsColorSort(scene.name) || ModuleScenes.IsGuided(scene.name))
            return;
        if (Object.FindAnyObjectByType<ObjectPlacementStation>() == null)
            return;
        if (Object.FindAnyObjectByType<ObjectPlacementIntroDirector>() != null)
            return;

        HasCompleted = false;
        GameObject host = new GameObject("ObjectPlacementIntroDirector");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ObjectPlacementIntroDirector>();
    }

    public static bool TryConsumeStartButton()
    {
        if (HasCompleted || instance == null)
            return false;
        return instance.HandleStartPressed();
    }

    void Awake()
    {
        instance = this;
        HasCompleted = false;
        ColorSortWalk.MovementLocked = true;

        station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        if (station != null)
            conveyor = station.GetComponent<ObjectPlacementConveyorController>();
        phaseRound = Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>();
        objectiveScreen = GameObject.Find("ObjectiveScreen");
        xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        hmdCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        ResolveRivera();
        ResolveTalkUi();
        ObjectPlacementItemLibrary.EnsureLoaded();
        SetupDialoguePad();
        EnsureEffectsUi();
        EnsureCinemaCamera();
        SetTalkVisible(false);
        // Circle only after Btn_Start unlocks walk.
        SetPadVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
        RestorePoseDrivers();
        RestoreHighlights();
    }

    bool HandleStartPressed()
    {
        if (startConsumed || HasCompleted)
            return false;

        startConsumed = true;
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        ColorSortWalk.MovementLocked = false;
        if (station != null)
            station.SetScreenText("STEP ON THE CIRCLE");
        if (rivera != null)
            rivera.Wave();
        SetPadVisible(true);
        return true;
    }

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

        // Fit Text (TMP) inside its parent Image panel (scene: Talk/Image/Text (TMP)).
        RectTransform textRt = talkText.rectTransform;
        RectTransform panelRt = textRt.parent as RectTransform;
        if (panelRt != null)
        {
            textRt.anchorMin = new Vector2(0.06f, 0.08f);
            textRt.anchorMax = new Vector2(0.94f, 0.92f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = Vector2.zero;
            textRt.offsetMin = new Vector2(0.12f, 0.12f);
            textRt.offsetMax = new Vector2(-0.12f, -0.12f);
        }

        talkText.text = string.Empty;
        talkText.alignment = TextAlignmentOptions.TopLeft;
        talkText.textWrappingMode = TextWrappingModes.Normal;
        talkText.overflowMode = TextOverflowModes.Truncate;
        // Fixed readable size — pagination handles overflow (no auto-shrink).
        talkText.enableAutoSizing = false;
        float fixedSize = talkFontSize > 0.05f ? talkFontSize : 0.55f;
        if (talkText.fontSize > 0.2f && talkText.fontSize < 1.2f)
            fixedSize = talkText.fontSize;
        talkFontSize = fixedSize;
        talkText.fontSize = fixedSize;
        talkText.fontSizeMin = fixedSize;
        talkText.fontSizeMax = fixedSize;
        talkText.margin = Vector4.zero;
    }

    void SetupDialoguePad()
    {
        // ONLY the scene hierarchy object "Dialogue circle" — never invent "Dialogue".
        dialogueZone = GameObject.Find("Dialogue circle");
        if (dialogueZone == null)
        {
            Debug.LogError("ObjectPlacementIntroDirector: required scene object 'Dialogue circle' was not found.");
            return;
        }

        // Clean leftovers from older runtime builds (do not keep alternate trigger systems).
        Transform leftoverProbe = dialogueZone.transform.Find("IntroTrigger");
        if (leftoverProbe != null)
            Destroy(leftoverProbe.gameObject);
        Transform leftoverVisual = dialogueZone.transform.Find("DialoguePadVisual");
        if (leftoverVisual != null)
            Destroy(leftoverVisual.gameObject);
        Transform leftoverLabel = dialogueZone.transform.Find("PadLabel");
        if (leftoverLabel != null)
            Destroy(leftoverLabel.gameObject);

        // Keep authored flat-cylinder transform/mesh from Color Sort.unity.
        MeshRenderer circleMr = dialogueZone.GetComponent<MeshRenderer>();
        if (circleMr != null)
        {
            circleMr.enabled = true;
            circleMr.sharedMaterial = MakePadMaterial();
        }
        dialoguePadVisual = dialogueZone;

        // Reuse the existing CapsuleCollider on Dialogue circle; compensate flat Y scale
        // so world size is walkable (~0.45m radius, ~2m tall).
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
        // Disabled until Btn_Start — prevents load-time / early triggers.
        capsule.enabled = false;

        ObjectPlacementDialogueTrigger trigger = dialogueZone.GetComponent<ObjectPlacementDialogueTrigger>();
        if (trigger == null)
            trigger = dialogueZone.AddComponent<ObjectPlacementDialogueTrigger>();
        trigger.Bind(this);

        if (dialogueZone.GetComponent<ObjectPlacementDialoguePadPulse>() == null)
            dialogueZone.AddComponent<ObjectPlacementDialoguePadPulse>();

        // Hidden until Start unlocks walk.
        dialogueZone.SetActive(false);
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

    void EnsureEffectsUi()
    {
        if (hmdCamera == null)
            hmdCamera = Camera.main;
        if (hmdCamera == null)
            return;

        // Prefer a root-level canvas so Screen Space works in VR + Game View / Device Simulator.
        GameObject canvasGo = GameObject.Find("IntroEffectsCanvas");
        if (canvasGo == null)
        {
            Transform underCam = hmdCamera.transform.Find("IntroEffectsCanvas");
            if (underCam != null)
                canvasGo = underCam.gameObject;
        }

        if (canvasGo == null)
        {
            canvasGo = new GameObject("IntroEffectsCanvas");
            fadeImage = MakeFullImage(canvasGo.transform, "Fade", new Color(0f, 0f, 0f, 0f), 0);
            vignetteImage = MakeFullImage(canvasGo.transform, "Vignette", new Color(0f, 0f, 0f, 0f), 1);
            letterboxTop = MakeBar(canvasGo.transform, "LetterboxTop", true);
            letterboxBottom = MakeBar(canvasGo.transform, "LetterboxBottom", false);
        }

        // Detach from camera local space — Screen Space - Camera handles framing.
        if (canvasGo.transform.parent != null)
            canvasGo.transform.SetParent(null, false);

        effectsCanvas = canvasGo.GetComponent<Canvas>();
        if (effectsCanvas == null)
            effectsCanvas = canvasGo.AddComponent<Canvas>();

        effectsCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        effectsCanvas.worldCamera = hmdCamera;
        effectsCanvas.planeDistance = 0.45f;
        effectsCanvas.overrideSorting = true;
        effectsCanvas.sortingOrder = 5000;
        effectsCanvas.enabled = true;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // iPad mini 4 landscape reference; MatchWidthOrHeight keeps VR + simulator readable.
        scaler.referenceResolution = new Vector2(2048f, 1536f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        if (canvasGo.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            CanvasGroup cg = canvasGo.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        if (canvasRt == null)
            canvasRt = canvasGo.AddComponent<RectTransform>();
        canvasRt.localScale = Vector3.one;
        canvasRt.localRotation = Quaternion.identity;
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.pivot = new Vector2(0.5f, 0.5f);
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        if (fadeImage == null)
            fadeImage = canvasGo.transform.Find("Fade")?.GetComponent<Image>();
        if (vignetteImage == null)
            vignetteImage = canvasGo.transform.Find("Vignette")?.GetComponent<Image>();
        if (letterboxTop == null)
            letterboxTop = canvasGo.transform.Find("LetterboxTop")?.GetComponent<Image>();
        if (letterboxBottom == null)
            letterboxBottom = canvasGo.transform.Find("LetterboxBottom")?.GetComponent<Image>();

        if (fadeImage == null)
            fadeImage = MakeFullImage(canvasGo.transform, "Fade", new Color(0f, 0f, 0f, 0f), 0);
        if (vignetteImage == null)
            vignetteImage = MakeFullImage(canvasGo.transform, "Vignette", new Color(0f, 0f, 0f, 0f), 1);
        if (letterboxTop == null)
            letterboxTop = MakeBar(canvasGo.transform, "LetterboxTop", true);
        if (letterboxBottom == null)
            letterboxBottom = MakeBar(canvasGo.transform, "LetterboxBottom", false);

        // Rebuild lower subtitle band with responsive anchors (not fixed pixels).
        Transform oldGuide = canvasGo.transform.Find("GuidePanel");
        if (oldGuide != null)
            Destroy(oldGuide.gameObject);

        GameObject panel = new GameObject("GuidePanel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.06f, 0.045f);
        prt.anchorMax = new Vector2(0.94f, 0.20f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        guidePlate = panel.AddComponent<Image>();
        guidePlate.color = new Color(0f, 0f, 0f, 0.78f);
        guidePlate.raycastTarget = false;

        GameObject guideGo = new GameObject("GuideText");
        guideGo.transform.SetParent(panel.transform, false);
        RectTransform grt = guideGo.AddComponent<RectTransform>();
        grt.anchorMin = Vector2.zero;
        grt.anchorMax = Vector2.one;
        grt.offsetMin = new Vector2(18f, 10f);
        grt.offsetMax = new Vector2(-18f, -10f);
        guideText = guideGo.AddComponent<TextMeshProUGUI>();
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.enableAutoSizing = false;
        guideText.fontSize = guideFontSize;
        guideText.color = Color.white;
        guideText.alpha = 1f;
        guideText.text = string.Empty;
        guideText.textWrappingMode = TextWrappingModes.Normal;
        guideText.overflowMode = TextOverflowModes.Truncate;
        guideText.fontStyle = FontStyles.Bold;
        guideText.outlineWidth = 0.2f;
        guideText.outlineColor = new Color(0f, 0f, 0f, 0.95f);

        if (letterboxTop != null)
        {
            RectTransform trt = letterboxTop.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.90f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
        if (letterboxBottom != null)
        {
            RectTransform brt = letterboxBottom.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.035f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }

        canvasGo.SetActive(true);
        SetLetterbox(0f);
        SetGuide(string.Empty);
    }

    static Image MakeFullImage(Transform parent, string name, Color color, int sibling)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(sibling);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image MakeBar(Transform parent, string name, bool top)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
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
        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        return img;
    }

    void EnsureCinemaCamera()
    {
        GameObject existing = GameObject.Find("IntroCinemaRig");
        if (existing != null)
        {
            cinemaRig = existing.transform;
            cinemaCam = existing.GetComponentInChildren<Camera>(true);
            if (cinemaCam != null)
                cinemaCam.enabled = false;
            return;
        }

        GameObject rig = new GameObject("IntroCinemaRig");
        cinemaRig = rig.transform;
        GameObject camGo = new GameObject("IntroCinemaCamera");
        camGo.transform.SetParent(rig.transform, false);
        cinemaCam = camGo.AddComponent<Camera>();
        cinemaCam.enabled = false;
        cinemaCam.nearClipPlane = 0.05f;
        cinemaCam.farClipPlane = 80f;
        cinemaCam.fieldOfView = 50f;
        if (hmdCamera != null)
        {
            cinemaCam.clearFlags = hmdCamera.clearFlags;
            cinemaCam.backgroundColor = hmdCamera.backgroundColor;
            cinemaCam.cullingMask = hmdCamera.cullingMask;
        }
        AudioListener listener = camGo.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);
    }

    public void OnPlayerEnteredDialogue()
    {
        if (!startConsumed || dialogueStarted || HasCompleted || cutsceneRunning)
            return;

        dialogueStarted = true;
        ColorSortWalk.MovementLocked = true;
        // Circle was stepped on — remove it (visual + trigger).
        ConsumeDialoguePad();
        StartCoroutine(WelcomeThenCutscene());
    }

    void ConsumeDialoguePad()
    {
        if (dialoguePadCollider != null)
            dialoguePadCollider.enabled = false;

        // Hide the scene Dialogue circle entirely after step-on.
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
        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.Greet();
            yield return new WaitForSeconds(greetHoldSeconds);
            rivera.SetTalking(true);
        }

        SetTalkVisible(true);
        yield return TypeLine(WelcomeEnvironment);
        yield return new WaitForSeconds(pauseBetweenLines);
        yield return TypeLine(WelcomeTraining);
        yield return new WaitForSeconds(pauseBetweenLines);

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.9f);
            rivera.SetTalking(true);
        }

        yield return TypeLine("Watch this demonstration. I will show you how sorting works on the conveyor.");
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
        yield return TypeLine("Your turn. Walk to the station and press the green Start button when you are ready.");
        yield return new WaitForSeconds(1.1f);

        SetTalkVisible(false);
        if (talkText != null)
            talkText.text = string.Empty;

        HasCompleted = true;
        ColorSortWalk.MovementLocked = false;
        SetPadVisible(false);

        if (phaseRound == null)
            phaseRound = Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>();
        if (phaseRound != null)
            phaseRound.ArmTrainingAfterIntro();
        else if (station != null)
            station.SetScreenText("PRESS START");
    }

    IEnumerator RunCinematicDemo()
    {
        cutsceneRunning = true;
        ColorSortWalk.MovementLocked = true;
        SetTalkVisible(false);

        yield return Fade(1f);
        if (station != null)
            station.SetScreenText("DEMO");

        PlaceRiveraAtPresenter();
        FrameCinemaCamera(0f);
        BeginCinemaView();
        yield return null;

        yield return Fade(0f);
        yield return AnimateLetterbox(1f, 0.45f);
        yield return PushInCamera(0.35f, camBlendSeconds);

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.SetTalking(true);
        }

        yield return ShowGuide("This is the sorting station.", beatPause);
        if (conveyor != null)
            conveyor.StartConveyors();
        yield return ShowGuide("Items arrive on the conveyor belt.", 1.5f);

        DemoBeat[] beats =
        {
            new DemoBeat
            {
                id = "Fruit_banana",
                kind = ObjectPlacementGroceryKind.Fruit,
                guide = "YELLOW = FRUITS\nUse GRIP to pick up",
                tip = "Place fruits into the yellow box."
            },
            new DemoBeat
            {
                id = "Fruit_lemon",
                kind = ObjectPlacementGroceryKind.Fruit,
                guide = "Another fruit — same rule.\nGRIP → Yellow box",
                tip = "Watch it ride to the end first."
            },
            new DemoBeat
            {
                id = "Veg_tomato",
                kind = ObjectPlacementGroceryKind.Vegetable,
                guide = "RED = VEGETABLES\nUse TRIGGER to pick up",
                tip = "Place vegetables into the red box."
            },
            new DemoBeat
            {
                id = "Veg_carrot",
                kind = ObjectPlacementGroceryKind.Vegetable,
                guide = "Same for every red item.\nTRIGGER → Red box",
                tip = "Check the category before you place."
            },
        };

        for (int i = 0; i < beats.Length; i++)
            yield return PlayDemoBeat(beats[i]);

        if (conveyor != null)
            conveyor.StopConveyors();

        // Final beat: lift sorted boxes onto the front conveyor, then end.
        yield return PlayBoxDeliveryDemo();

        if (rivera != null)
        {
            rivera.Agree();
            yield return new WaitForSeconds(0.8f);
            rivera.Clap();
        }

        yield return ShowGuide("Great. Now you try.", 1.6f);
        yield return AnimateLetterbox(0f, 0.4f);
        yield return Fade(1f);

        // Restore boxes while screen is black so Phase I starts with crates at home.
        if (hasPendingBoxRestore)
        {
            RestoreBoxState(station != null ? station.yellowBox : null, pendingYellowRestore);
            RestoreBoxState(station != null ? station.redBox : null, pendingRedRestore);
            hasPendingBoxRestore = false;
        }

        EndCinemaView();
        RestoreRivera();
        ClearGuide();
        RestoreHighlights();

        if (station != null)
            station.SetScreenText("PRESS START");

        yield return Fade(0f);
        cutsceneRunning = false;
    }

    IEnumerator PlayBoxDeliveryDemo()
    {
        if (station == null)
            yield break;

        // Scene station refs: Yellow Box / Red Box + front BeltSurface under Conveyor.
        Transform yellow = station.yellowBox;
        Transform red = station.redBox;
        Transform frontBelt = station.beltSurface != null ? station.beltSurface : station.conveyor;
        if (frontBelt == null || (yellow == null && red == null))
            yield break;

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.45f);
            rivera.SetTalking(true);
        }

        yield return ShowGuide("After sorting, lift the boxes\nand place them on the front conveyor.", beatPause);

        BoxAnimState yState = CaptureBoxState(yellow);
        BoxAnimState rState = CaptureBoxState(red);

        // Yellow seat (already correct): SortBox convention local.y = 0.12 on BeltSurface.
        const float YellowAlongLocalX = -0.35f;
        Vector3 yellowSeat = GetFrontBeltSeat(frontBelt, YellowAlongLocalX);

        if (conveyor != null)
            conveyor.StartConveyors();

        if (yellow != null)
        {
            PrepareBoxForAnim(yellow);
            PulseBox(yellow, true);
            DetachBoxPreserveScale(yellow, yState);
            yield return MoveTransformArc(yellow, yellowSeat, toBoxSeconds + 0.25f);
            SeatBoxOnBeltPreserveScale(yellow, frontBelt, yellowSeat, yState);
            PulseBox(yellow, false);
            yield return new WaitForSeconds(0.4f);
        }

        // Red beside yellow — use yellow's final world seat as reference (do not move yellow).
        Vector3 redSeat = yellow != null
            ? GetAdjacentBeltSeat(frontBelt, yellow, yellow.position)
            : GetFrontBeltSeat(frontBelt, YellowAlongLocalX + 0.55f);

        if (red != null)
        {
            PrepareBoxForAnim(red);
            PulseBox(red, true);
            DetachBoxPreserveScale(red, rState);
            yield return MoveTransformArc(red, redSeat, toBoxSeconds + 0.25f);
            SeatBoxOnBeltPreserveScale(red, frontBelt, redSeat, rState);
            PulseBox(red, false);
            yield return new WaitForSeconds(0.55f);
        }

        yield return ShowGuide("Both boxes are on the front conveyor.\nDemo complete.", beatPause);

        if (conveyor != null)
            conveyor.StopConveyors();

        // Keep boxes seated until fade-to-black; restore happens in RunCinematicDemo.
        yield return new WaitForSeconds(0.9f);
        pendingYellowRestore = yState;
        pendingRedRestore = rState;
        hasPendingBoxRestore = true;
    }

    struct BoxAnimState
    {
        public bool valid;
        public Transform parent;
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
        public Vector3 worldPos;
        public Quaternion worldRot;
        public Vector3 lossyScale;
        public Rigidbody body;
        public bool wasKinematic;
        public bool usedGravity;
        public bool detectCollisions;
    }

    static BoxAnimState CaptureBoxState(Transform box)
    {
        BoxAnimState s = new BoxAnimState();
        if (box == null)
            return s;
        s.valid = true;
        s.parent = box.parent;
        s.localPos = box.localPosition;
        s.localRot = box.localRotation;
        s.localScale = box.localScale;
        s.worldPos = box.position;
        s.worldRot = box.rotation;
        s.lossyScale = box.lossyScale;
        s.body = box.GetComponent<Rigidbody>();
        if (s.body != null)
        {
            s.wasKinematic = s.body.isKinematic;
            s.usedGravity = s.body.useGravity;
            s.detectCollisions = s.body.detectCollisions;
        }
        return s;
    }

    static void PrepareBoxForAnim(Transform box)
    {
        if (box == null)
            return;
        Rigidbody body = box.GetComponent<Rigidbody>();
        if (body == null)
            return;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = false;
    }

    /// <summary>
    /// Unparent without inheriting belt non-uniform scale. Keep original world size.
    /// </summary>
    static void DetachBoxPreserveScale(Transform box, BoxAnimState s)
    {
        if (box == null || !s.valid)
            return;
        box.SetParent(null, true);
        // Parent is identity — localScale must equal original lossyScale.
        box.localScale = s.lossyScale;
    }

    static void RestoreBoxState(Transform box, BoxAnimState s)
    {
        if (box == null || !s.valid)
            return;
        box.SetParent(s.parent, false);
        box.localPosition = s.localPos;
        box.localRotation = s.localRot;
        box.localScale = s.localScale;
        if (s.body != null)
        {
            s.body.isKinematic = s.wasKinematic;
            s.body.useGravity = s.usedGravity;
            s.body.detectCollisions = s.detectCollisions;
            s.body.linearVelocity = Vector3.zero;
            s.body.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Seat point on the scene front BeltSurface using the same local.y as SortBox ride seating.
    /// </summary>
    static Vector3 GetFrontBeltSeat(Transform belt, float alongLocalX)
    {
        Vector3 local = new Vector3(alongLocalX, 0.12f, 0f);
        return belt.TransformPoint(local);
    }

    /// <summary>
    /// Place red beside yellow on the belt: same height, offset along belt.right by yellow footprint + gap.
    /// </summary>
    static Vector3 GetAdjacentBeltSeat(Transform belt, Transform yellowBox, Vector3 yellowWorldSeat)
    {
        float footprint = GetBoundsSizeAlong(yellowBox, belt.right);
        if (footprint < 0.15f)
            footprint = 0.45f;
        float gap = 0.08f;
        Vector3 along = belt.right;
        along.y = 0f;
        if (along.sqrMagnitude < 0.0001f)
            along = Vector3.right;
        along.Normalize();

        Vector3 seat = yellowWorldSeat + along * (footprint + gap);
        // Keep on belt top height (SortBox local.y = 0.12).
        Vector3 local = belt.InverseTransformPoint(seat);
        local.y = 0.12f;
        local.z = 0f;
        // Clamp along belt length so the crate stays on the surface.
        float halfLen = 0.95f;
        local.x = Mathf.Clamp(local.x, -halfLen, halfLen);
        return belt.TransformPoint(local);
    }

    static float GetBoundsSizeAlong(Transform target, Vector3 worldAxis)
    {
        if (target == null)
            return 0.45f;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return 0.45f;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        Vector3 axis = worldAxis.normalized;
        Vector3 ext = b.extents;
        // Project AABB extents onto axis (conservative footprint).
        float size = Mathf.Abs(axis.x) * ext.x * 2f
                   + Mathf.Abs(axis.y) * ext.y * 2f
                   + Mathf.Abs(axis.z) * ext.z * 2f;
        return Mathf.Max(0.2f, size);
    }

    /// <summary>
    /// Seat on belt in world space — do NOT parent under BeltSurface (non-uniform scale stretches crates).
    /// </summary>
    static void SeatBoxOnBeltPreserveScale(Transform box, Transform belt, Vector3 worldSeat, BoxAnimState s)
    {
        if (box == null || belt == null)
            return;
        if (box.parent != null)
            box.SetParent(null, true);
        box.position = worldSeat;
        box.rotation = Quaternion.Euler(0f, belt.eulerAngles.y, 0f);
        box.localScale = s.valid ? s.lossyScale : box.localScale;
    }

    IEnumerator PlayDemoBeat(DemoBeat beat)
    {
        Transform box = beat.kind == ObjectPlacementGroceryKind.Fruit
            ? (station != null ? station.yellowBox : null)
            : (station != null ? station.redBox : null);

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.55f);
            rivera.SetTalking(true);
        }

        PulseBox(box, true);
        yield return ShowGuide(beat.guide, beatPause);

        Vector3 spawn = GetBeltSpawn();
        Vector3 stopper = GetBeltStopper();
        Vector3 boxTarget = box != null ? box.position + Vector3.up * 0.14f : stopper + Vector3.up * 0.2f;

        // Slight lateral offset so fruit/veg don't overlap perfectly.
        Vector3 side = beat.kind == ObjectPlacementGroceryKind.Fruit ? Vector3.forward * 0.04f : Vector3.back * 0.04f;
        GameObject prop = SpawnDemoProp(beat.id, beat.kind, spawn + side);

        yield return ShowGuide("Riding the conveyor…", 0.9f);
        yield return MovePropLinear(prop, stopper + side, beltRideSeconds);
        yield return ShowGuide("Stops at the end of the belt.", 1.4f);
        yield return ShowGuide(beat.tip, 1.6f);
        yield return MovePropArc(prop, boxTarget, toBoxSeconds);
        yield return new WaitForSeconds(0.55f);

        if (prop != null)
            Destroy(prop);

        PulseBox(box, false);
        yield return new WaitForSeconds(0.35f);
    }

    Vector3 GetBeltSpawn()
    {
        if (station == null)
            return Vector3.zero;
        Transform spawn = station.grocerySpawnPointLeft != null
            ? station.grocerySpawnPointLeft
            : station.grocerySpawnPoint;
        if (spawn != null)
            return spawn.position + Vector3.up * 0.04f;
        return station.transform.position + Vector3.up * 0.9f;
    }

    Vector3 GetBeltStopper()
    {
        if (station != null && station.beltStopperLeft != null)
            return station.beltStopperLeft.position + Vector3.up * 0.05f;

        // Fallback: end of left belt along -X (left conveyor direction in this project).
        Vector3 spawn = GetBeltSpawn();
        if (station != null && station.beltSurfaceLeft != null)
        {
            Bounds b = new Bounds(station.beltSurfaceLeft.position, Vector3.one * 0.2f);
            Renderer r = station.beltSurfaceLeft.GetComponent<Renderer>();
            if (r != null)
                b = r.bounds;
            return new Vector3(b.min.x + 0.12f, spawn.y, spawn.z);
        }
        return spawn + Vector3.left * 1.4f;
    }

    void PlaceRiveraAtPresenter()
    {
        if (riveraRoot == null || station == null)
            return;

        riveraSavedPos = riveraRoot.position;
        riveraSavedRot = riveraRoot.rotation;

        Vector3 focus = GetDemoFocus();
        Vector3 camPos = GetCinemaStartPos(focus);
        Vector3 side = Vector3.Cross(Vector3.up, (focus - camPos).normalized);
        if (side.sqrMagnitude < 0.01f)
            side = station.transform.right;
        side.Normalize();

        Vector3 stand = focus - (focus - camPos).normalized * 0.15f + side * 0.95f;
        stand.y = station.transform.position.y;
        Vector3 look = focus - stand;
        look.y = 0f;
        if (look.sqrMagnitude < 0.01f)
            look = -side;
        riveraRoot.SetPositionAndRotation(stand, Quaternion.LookRotation(look.normalized, Vector3.up));
    }

    void RestoreRivera()
    {
        if (riveraRoot == null)
            return;
        riveraRoot.SetPositionAndRotation(riveraSavedPos, riveraSavedRot);
    }

    Vector3 GetDemoFocus()
    {
        if (station == null)
            return Vector3.zero;
        Transform y = station.yellowBox;
        Transform r = station.redBox;
        if (y != null && r != null)
            return (y.position + r.position) * 0.5f + Vector3.up * 0.2f;
        if (station.boxesRoot != null)
            return station.boxesRoot.position + Vector3.up * 0.25f;
        return station.transform.position + Vector3.up * 1f;
    }

    Vector3 GetCinemaStartPos(Vector3 focus)
    {
        // 3/4 view: elevated, looking down at boxes + belt + Rivera presenter.
        Vector3 toPlayer = Vector3.back;
        if (xrOrigin != null)
        {
            Transform origin = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
            toPlayer = origin.position - focus;
            toPlayer.y = 0f;
        }
        if (toPlayer.sqrMagnitude < 0.01f && station != null)
            toPlayer = -station.transform.forward;
        if (toPlayer.sqrMagnitude < 0.01f)
            toPlayer = Vector3.back;
        toPlayer.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        return focus + toPlayer * 2.35f + right * 0.85f + Vector3.up * 1.55f;
    }

    void FrameCinemaCamera(float push)
    {
        EnsureCinemaCamera();
        Vector3 focus = GetDemoFocus();
        Vector3 start = GetCinemaStartPos(focus);
        Vector3 end = Vector3.Lerp(start, focus + Vector3.up * 1.1f, 0.22f);
        Vector3 pos = Vector3.Lerp(start, end, Mathf.Clamp01(push));
        if (cinemaCam != null)
        {
            cinemaCam.transform.position = pos;
            cinemaCam.transform.rotation = Quaternion.LookRotation((focus + Vector3.up * 0.1f - pos).normalized, Vector3.up);
        }
    }

    IEnumerator PushInCamera(float pushAmount, float duration)
    {
        Vector3 focus = GetDemoFocus();
        Vector3 start = cinemaCam != null ? cinemaCam.transform.position : GetCinemaStartPos(focus);
        Vector3 end = Vector3.Lerp(start, focus + Vector3.up * 1.05f, pushAmount);
        Quaternion look = Quaternion.LookRotation((focus + Vector3.up * 0.1f - end).normalized, Vector3.up);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration));
            if (cinemaCam != null)
            {
                cinemaCam.transform.position = Vector3.Lerp(start, end, u);
                cinemaCam.transform.rotation = Quaternion.Slerp(cinemaCam.transform.rotation, look, u);
            }
            yield return null;
        }
    }

    void BeginCinemaView()
    {
        if (hmdCamera == null || cinemaCam == null)
            return;

        camSavedWorldPos = hmdCamera.transform.position;
        camSavedWorldRot = hmdCamera.transform.rotation;
        camPoseSaved = true;
        DisablePoseDrivers();
        cinemaActive = true;
        cinemaCam.enabled = false; // we drive the HMD cam instead for VR display
        FrameCinemaCamera(0f);
        // Snap HMD cam to cinema framing (tracking disabled).
        hmdCamera.transform.SetPositionAndRotation(cinemaCam.transform.position, cinemaCam.transform.rotation);

        // Subtitles bind to the active HMD/camera used by Screen Space - Camera.
        EnsureEffectsUi();
        if (effectsCanvas != null)
            effectsCanvas.worldCamera = hmdCamera;
    }

    void EndCinemaView()
    {
        cinemaActive = false;
        if (hmdCamera != null && camPoseSaved)
            hmdCamera.transform.SetPositionAndRotation(camSavedWorldPos, camSavedWorldRot);
        RestorePoseDrivers();
        camPoseSaved = false;
        if (cinemaCam != null)
            cinemaCam.enabled = false;
    }

    void LateUpdate()
    {
        if (!cinemaActive || hmdCamera == null || cinemaCam == null)
            return;
        // Keep HMD camera locked to cinematic pose while tracking is disabled.
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
        // Also disable any XR pose drivers on camera offset.
        MonoBehaviour[] behaviours = hmdCamera.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b == null || !b.enabled)
                continue;
            string n = b.GetType().Name;
            if (n.IndexOf("TrackedPose", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("TrackedPoseDriver", System.StringComparison.OrdinalIgnoreCase) >= 0)
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

    void PulseBox(Transform box, bool on)
    {
        if (!on)
        {
            RestoreHighlights();
            return;
        }
        RestoreHighlights();
        if (box == null)
            return;
        Renderer[] renderers = box.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.material == null)
                continue;
            highlightRenderers.Add(r);
            Color baseCol = r.material.HasProperty("_BaseColor")
                ? r.material.GetColor("_BaseColor")
                : (r.material.HasProperty("_Color") ? r.material.GetColor("_Color") : Color.white);
            highlightRestore.Add(baseCol);
            Color glow = Color.Lerp(baseCol, Color.white, 0.45f);
            glow *= 1.35f;
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", glow);
            if (r.material.HasProperty("_Color"))
                r.material.SetColor("_Color", glow);
            if (r.material.HasProperty("_EmissionColor"))
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", glow * 0.35f);
            }
        }
    }

    void RestoreHighlights()
    {
        for (int i = 0; i < highlightRenderers.Count; i++)
        {
            Renderer r = highlightRenderers[i];
            if (r == null || r.material == null)
                continue;
            Color c = highlightRestore[i];
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", c);
            if (r.material.HasProperty("_Color"))
                r.material.SetColor("_Color", c);
        }
        highlightRenderers.Clear();
        highlightRestore.Clear();
    }

    GameObject SpawnDemoProp(string id, ObjectPlacementGroceryKind kind, Vector3 worldPos)
    {
        ObjectPlacementItemLibrary.EnsureLoaded();
        GameObject template = FindItemTemplate(id, kind);
        GameObject go;
        if (template != null)
            go = ObjectPlacementItemLibrary.Spawn(template, kind, null);
        else
            go = null;

        if (go == null)
        {
            string fallback = id.Replace("Fruit_", string.Empty).Replace("Veg_", string.Empty);
            go = ObjectPlacementGroceryFactory.Create(fallback, kind);
        }

        go.name = "IntroDemo_" + id;
        go.transform.SetParent(null, true);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.identity;

        ObjectPlacementGroceryItem item = go.GetComponent<ObjectPlacementGroceryItem>();
        if (item != null)
            Destroy(item);
        var grab = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            Destroy(grab);

        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        return go;
    }

    static GameObject FindItemTemplate(string id, ObjectPlacementGroceryKind kind)
    {
        GameObject[] pool = kind == ObjectPlacementGroceryKind.Fruit
            ? ObjectPlacementItemLibrary.Fruits
            : ObjectPlacementItemLibrary.Vegetables;
        if (pool == null || pool.Length == 0)
            return null;

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null && pool[i].name == id)
                return pool[i];
        }

        // Partial match (e.g. "banana" inside "Fruit_banana").
        string key = id.Replace("Fruit_", string.Empty).Replace("Veg_", string.Empty);
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null &&
                pool[i].name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return pool[i];
        }

        return pool[0];
    }

    IEnumerator MovePropLinear(GameObject prop, Vector3 end, float duration)
    {
        if (prop == null)
            yield break;
        Vector3 start = prop.transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration));
            prop.transform.position = Vector3.Lerp(start, end, u);
            prop.transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        prop.transform.position = end;
    }

    IEnumerator MovePropArc(GameObject prop, Vector3 end, float duration)
    {
        if (prop == null)
            yield break;
        yield return MoveTransformArc(prop.transform, end, duration, spin: true);
    }

    IEnumerator MoveTransformArc(Transform target, Vector3 end, float duration, bool spin = false)
    {
        if (target == null)
            yield break;
        Vector3 start = target.position;
        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.45f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float e = u * u * (3f - 2f * u);
            Vector3 a = Vector3.Lerp(start, mid, e);
            Vector3 b = Vector3.Lerp(mid, end, e);
            target.position = Vector3.Lerp(a, b, e);
            if (spin)
                target.Rotate(0f, 110f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        target.position = end;
    }

    IEnumerator TypeLine(string line)
    {
        if (talkText == null)
        {
            yield return new WaitForSeconds(Mathf.Max(1.4f, line.Length / Mathf.Max(1f, typeCharsPerSecond)));
            yield break;
        }

        talkText.enableAutoSizing = false;
        talkText.fontSize = talkFontSize;
        talkText.textWrappingMode = TextWrappingModes.Normal;
        talkText.overflowMode = TextOverflowModes.Truncate;

        List<string> pages = BuildDialoguePages(line);
        if (pages.Count == 0)
            pages.Add(string.Empty);

        float delay = 1f / Mathf.Max(8f, typeCharsPerSecond);
        for (int p = 0; p < pages.Count; p++)
        {
            string page = pages[p];
            talkText.text = string.Empty;
            for (int i = 0; i < page.Length; i++)
            {
                talkText.text = page.Substring(0, i + 1);
                yield return new WaitForSeconds(delay);
            }

            // Page hold = existing dialogue continuation beat (no auto-shrink).
            if (p < pages.Count - 1)
                yield return new WaitForSeconds(pageHoldSeconds);
        }
    }

    /// <summary>
    /// Split dialogue into pages that fit the Talk Text (TMP) rect at a fixed font size.
    /// Words are never cut mid-word; overflow becomes the next page.
    /// </summary>
    List<string> BuildDialoguePages(string fullText)
    {
        List<string> pages = new List<string>();
        if (talkText == null || string.IsNullOrEmpty(fullText))
            return pages;

        // Preserve explicit newlines as paragraph breaks.
        string[] paragraphs = fullText.Replace("\r\n", "\n").Split('\n');
        string page = string.Empty;

        for (int p = 0; p < paragraphs.Length; p++)
        {
            string paragraph = paragraphs[p];
            if (p > 0)
            {
                // Prefer keeping paragraph break on current page if it fits.
                string withBreak = string.IsNullOrEmpty(page) ? string.Empty : page + "\n";
                if (!string.IsNullOrEmpty(page) && !TextFitsTalk(withBreak + paragraph.TrimStart()))
                {
                    pages.Add(page.TrimEnd());
                    page = string.Empty;
                }
                else if (!string.IsNullOrEmpty(page))
                    page += "\n";
            }

            string[] words = paragraph.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w];
                string candidate = string.IsNullOrEmpty(page) ? word : page + " " + word;
                if (TextFitsTalk(candidate))
                {
                    page = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(page))
                {
                    pages.Add(page.TrimEnd());
                    page = string.Empty;
                }

                // Single oversized word: hard-wrap by characters into pages (last resort).
                if (!TextFitsTalk(word))
                {
                    foreach (string chunk in SplitOversizedWord(word))
                        pages.Add(chunk);
                    page = string.Empty;
                }
                else
                {
                    page = word;
                }
            }
        }

        if (!string.IsNullOrEmpty(page))
            pages.Add(page.TrimEnd());

        return pages;
    }

    bool TextFitsTalk(string candidate)
    {
        if (talkText == null)
            return true;

        string previous = talkText.text;
        talkText.enableAutoSizing = false;
        talkText.fontSize = talkFontSize;
        talkText.text = candidate ?? string.Empty;
        talkText.ForceMeshUpdate(true);

        bool overflowing = talkText.isTextOverflowing;
        // Also guard by preferred height vs rect when overflow flag is unreliable.
        if (!overflowing)
        {
            Vector2 pref = talkText.GetPreferredValues(candidate, talkText.rectTransform.rect.width, 0f);
            float maxH = talkText.rectTransform.rect.height;
            if (maxH > 0.001f && pref.y > maxH + 0.01f)
                overflowing = true;
        }

        talkText.text = previous;
        talkText.ForceMeshUpdate(true);
        return !overflowing;
    }

    List<string> SplitOversizedWord(string word)
    {
        List<string> chunks = new List<string>();
        if (string.IsNullOrEmpty(word))
            return chunks;

        string current = string.Empty;
        for (int i = 0; i < word.Length; i++)
        {
            string next = current + word[i];
            if (TextFitsTalk(next))
            {
                current = next;
            }
            else
            {
                if (!string.IsNullOrEmpty(current))
                    chunks.Add(current);
                current = word[i].ToString();
            }
        }
        if (!string.IsNullOrEmpty(current))
            chunks.Add(current);
        return chunks;
    }

    IEnumerator ShowGuide(string text, float hold)
    {
        EnsureEffectsUi();
        if (effectsCanvas != null && hmdCamera != null)
            effectsCanvas.worldCamera = hmdCamera;
        SetGuide(text);
        yield return new WaitForSeconds(hold);
    }

    void SetGuide(string text)
    {
        bool has = !string.IsNullOrEmpty(text);
        if (effectsCanvas != null)
        {
            effectsCanvas.enabled = true;
            if (hmdCamera != null)
                effectsCanvas.worldCamera = hmdCamera;
            effectsCanvas.gameObject.SetActive(true);
        }
        if (guidePlate != null)
        {
            guidePlate.enabled = true;
            guidePlate.gameObject.SetActive(has);
            Color c = guidePlate.color;
            c.a = has ? 0.78f : 0f;
            guidePlate.color = c;
        }
        if (guideText != null)
        {
            guideText.gameObject.SetActive(has);
            guideText.enableAutoSizing = false;
            guideText.fontSize = guideFontSize;
            guideText.color = Color.white;
            guideText.alpha = 1f;
            guideText.text = text ?? string.Empty;
        }
    }

    void ClearGuide() => SetGuide(string.Empty);

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
            EnsureEffectsUi();
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;
        float start = c.a;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
            c.a = Mathf.Lerp(start, targetAlpha, u);
            fadeImage.color = c;
            yield return null;
        }
        c.a = targetAlpha;
        fadeImage.color = c;
    }

    IEnumerator AnimateLetterbox(float target, float duration)
    {
        float start = letterboxTop != null ? letterboxTop.color.a : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration));
            SetLetterbox(Mathf.Lerp(start, target, u));
            yield return null;
        }
        SetLetterbox(target);
    }

    void SetLetterbox(float alpha)
    {
        Color c = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        if (letterboxTop != null)
            letterboxTop.color = c;
        if (letterboxBottom != null)
            letterboxBottom.color = c;
        if (vignetteImage != null)
            vignetteImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha) * 0.25f);
    }

    void SetTalkVisible(bool on)
    {
        if (talkCanvas != null)
            talkCanvas.SetActive(on);
    }

    void SetPadVisible(bool on)
    {
        if (dialogueStarted || HasCompleted)
        {
            // Once stepped on / completed, never show again this session.
            ConsumeDialoguePad();
            return;
        }

        if (dialogueZone == null)
            return;

        dialogueZone.SetActive(on);
        if (dialoguePadCollider != null)
            dialoguePadCollider.enabled = on;
    }
}

/// <summary>Emission pulse on Dialogue circle — does not scale (keeps trigger world size stable).</summary>
public class ObjectPlacementDialoguePadPulse : MonoBehaviour
{
    Material mat;
    Color baseColor;
    bool ready;

    void Awake()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            return;
        mat = mr.material;
        if (mat.HasProperty("_EmissionColor"))
            baseColor = mat.GetColor("_EmissionColor");
        else if (mat.HasProperty("_BaseColor"))
            baseColor = mat.GetColor("_BaseColor");
        else
            return;
        ready = true;
    }

    void Update()
    {
        if (!ready || mat == null)
            return;
        float e = 1.35f + Mathf.Sin(Time.time * 2.6f) * 0.4f;
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", baseColor * e);
    }
}

/// <summary>Trigger on the scene Dialogue circle CapsuleCollider.</summary>
public class ObjectPlacementDialogueTrigger : MonoBehaviour
{
    ObjectPlacementIntroDirector director;
    bool fired;

    public void Bind(ObjectPlacementIntroDirector host)
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
        // Primary: CharacterController used by ColorSortWalk on XR Origin.
        if (other.GetComponent<CharacterController>() != null)
            return true;
        if (other.GetComponentInParent<CharacterController>() != null)
            return true;
        if (other.CompareTag("Player"))
            return true;
        return false;
    }
}
