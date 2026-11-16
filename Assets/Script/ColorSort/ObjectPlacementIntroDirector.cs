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
        SetPadVisible(true);
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
        if (talkText != null)
            talkText.text = string.Empty;
    }

    void SetupDialoguePad()
    {
        dialogueZone = GameObject.Find("Dialogue");
        if (dialogueZone == null && riveraRoot != null)
        {
            dialogueZone = new GameObject("Dialogue");
            Vector3 flatFwd = riveraRoot.forward;
            flatFwd.y = 0f;
            if (flatFwd.sqrMagnitude < 0.01f)
                flatFwd = Vector3.forward;
            flatFwd.Normalize();
            dialogueZone.transform.position = riveraRoot.position + flatFwd * 1.05f;
        }
        if (dialogueZone == null)
            return;

        // Flatten marker to floor — this object IS the step circle.
        Vector3 p = dialogueZone.transform.position;
        p.y = 0.02f;
        dialogueZone.transform.position = p;
        dialogueZone.transform.rotation = Quaternion.identity;
        dialogueZone.transform.localScale = Vector3.one;

        // Remove old mesh / extra triggers — circle only.
        MeshRenderer oldMr = dialogueZone.GetComponent<MeshRenderer>();
        if (oldMr != null)
            oldMr.enabled = false;
        MeshFilter oldMf = dialogueZone.GetComponent<MeshFilter>();
        if (oldMf != null)
            Destroy(oldMf);
        CapsuleCollider[] oldCaps = dialogueZone.GetComponents<CapsuleCollider>();
        for (int i = 0; i < oldCaps.Length; i++)
            Destroy(oldCaps[i]);
        Transform oldProbe = dialogueZone.transform.Find("IntroTrigger");
        if (oldProbe != null)
            Destroy(oldProbe.gameObject);

        Transform visual = dialogueZone.transform.Find("DialoguePadVisual");
        if (visual == null)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DialoguePadVisual";
            ring.transform.SetParent(dialogueZone.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(1.4f, 0.025f, 1.4f);
            // Collider stays on the circle mesh — convert to trigger on parent instead.
            Collider ringCol = ring.GetComponent<Collider>();
            if (ringCol != null)
                Destroy(ringCol);
            MeshRenderer mr = ring.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MakePadMaterial();
            dialoguePadVisual = ring;

            GameObject labelGo = new GameObject("PadLabel");
            labelGo.transform.SetParent(ring.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelGo.transform.localScale = Vector3.one * 0.14f;
            TextMeshPro tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = "STEP HERE";
            tmp.fontSize = 9f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.05f, 0.12f, 0.22f, 1f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else
        {
            dialoguePadVisual = visual.gameObject;
            Collider ringCol = dialoguePadVisual.GetComponent<Collider>();
            if (ringCol != null)
                Destroy(ringCol);
        }

        // Single trigger on the Dialogue circle root (matches the visible pad).
        dialoguePadCollider = dialogueZone.GetComponent<CapsuleCollider>();
        if (dialoguePadCollider == null)
            dialoguePadCollider = dialogueZone.AddComponent<CapsuleCollider>();
        dialoguePadCollider.isTrigger = true;
        dialoguePadCollider.direction = 1;
        dialoguePadCollider.radius = 0.75f;
        dialoguePadCollider.height = 2.0f;
        dialoguePadCollider.center = new Vector3(0f, 1.0f, 0f);

        ObjectPlacementDialogueTrigger trigger = dialogueZone.GetComponent<ObjectPlacementDialogueTrigger>();
        if (trigger == null)
            trigger = dialogueZone.AddComponent<ObjectPlacementDialogueTrigger>();
        trigger.Bind(this);

        if (dialogueZone.GetComponent<ObjectPlacementDialoguePadPulse>() == null)
            dialogueZone.AddComponent<ObjectPlacementDialoguePadPulse>();
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
            return;

        Transform existing = hmdCamera.transform.Find("IntroEffectsCanvas");
        GameObject canvasGo;
        if (existing != null)
        {
            canvasGo = existing.gameObject;
            effectsCanvas = canvasGo.GetComponent<Canvas>();
        }
        else
        {
            canvasGo = new GameObject("IntroEffectsCanvas");
            canvasGo.transform.SetParent(hmdCamera.transform, false);
            effectsCanvas = canvasGo.AddComponent<Canvas>();
            effectsCanvas.renderMode = RenderMode.WorldSpace;
            effectsCanvas.sortingOrder = 1000;

            fadeImage = MakeFullImage(canvasGo.transform, "Fade", new Color(0f, 0f, 0f, 0f), 0);
            vignetteImage = MakeFullImage(canvasGo.transform, "Vignette", new Color(0f, 0f, 0f, 0f), 1);
            letterboxTop = MakeBar(canvasGo.transform, "LetterboxTop", true);
            letterboxBottom = MakeBar(canvasGo.transform, "LetterboxBottom", false);
        }

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        if (canvasRt == null)
            canvasRt = canvasGo.AddComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(2f, 2f);
        canvasRt.localPosition = new Vector3(0f, 0f, 0.28f);
        canvasRt.localScale = Vector3.one * 0.55f;

        if (fadeImage == null)
            fadeImage = canvasGo.transform.Find("Fade")?.GetComponent<Image>();
        if (letterboxTop == null)
            letterboxTop = canvasGo.transform.Find("LetterboxTop")?.GetComponent<Image>();
        if (letterboxBottom == null)
            letterboxBottom = canvasGo.transform.Find("LetterboxBottom")?.GetComponent<Image>();

        // Always rebuild guide plate so text stays readable (old tiny text gets replaced).
        Transform oldGuide = canvasGo.transform.Find("GuidePanel");
        if (oldGuide != null)
            Destroy(oldGuide.gameObject);
        Transform oldGuideText = canvasGo.transform.Find("GuideText");
        if (oldGuideText != null)
            Destroy(oldGuideText.gameObject);

        GameObject panel = new GameObject("GuidePanel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        // Centered, above letterbox — large readable band.
        prt.anchorMin = new Vector2(0.06f, 0.30f);
        prt.anchorMax = new Vector2(0.94f, 0.52f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        guidePlate = panel.AddComponent<Image>();
        guidePlate.color = new Color(0f, 0f, 0f, 0.72f);
        guidePlate.raycastTarget = false;

        GameObject guideGo = new GameObject("GuideText");
        guideGo.transform.SetParent(panel.transform, false);
        RectTransform grt = guideGo.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.04f, 0.08f);
        grt.anchorMax = new Vector2(0.96f, 0.92f);
        grt.offsetMin = Vector2.zero;
        grt.offsetMax = Vector2.zero;
        guideText = guideGo.AddComponent<TextMeshProUGUI>();
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.fontSize = 0.32f;
        guideText.fontSizeMin = 0.22f;
        guideText.fontSizeMax = 0.42f;
        guideText.enableAutoSizing = true;
        guideText.color = Color.white;
        guideText.text = string.Empty;
        guideText.textWrappingMode = TextWrappingModes.Normal;
        guideText.fontStyle = FontStyles.Bold;
        Outline outline = guideGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Slimmer letterbox so guide plate has room.
        if (letterboxTop != null)
        {
            RectTransform trt = letterboxTop.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.88f);
            trt.anchorMax = new Vector2(1f, 1f);
        }
        if (letterboxBottom != null)
        {
            RectTransform brt = letterboxBottom.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.12f);
        }

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
        if (dialoguePadVisual != null)
            dialoguePadVisual.SetActive(false);
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

        if (rivera != null)
        {
            rivera.Agree();
            yield return new WaitForSeconds(0.8f);
            rivera.Clap();
        }

        yield return ShowGuide("Great. Now you try.", 1.6f);
        yield return AnimateLetterbox(0f, 0.4f);
        yield return Fade(1f);

        EndCinemaView();
        RestoreRivera();
        ClearGuide();
        RestoreHighlights();

        if (station != null)
            station.SetScreenText("PRESS START");

        yield return Fade(0f);
        cutsceneRunning = false;
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
        Vector3 start = prop.transform.position;
        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float e = u * u * (3f - 2f * u);
            Vector3 a = Vector3.Lerp(start, mid, e);
            Vector3 b = Vector3.Lerp(mid, end, e);
            prop.transform.position = Vector3.Lerp(a, b, e);
            prop.transform.Rotate(0f, 110f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        prop.transform.position = end;
    }

    IEnumerator TypeLine(string line)
    {
        if (talkText == null)
        {
            yield return new WaitForSeconds(Mathf.Max(1.4f, line.Length / Mathf.Max(1f, typeCharsPerSecond)));
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

    IEnumerator ShowGuide(string text, float hold)
    {
        SetGuide(text);
        yield return new WaitForSeconds(hold);
    }

    void SetGuide(string text)
    {
        bool has = !string.IsNullOrEmpty(text);
        if (guidePlate != null)
            guidePlate.enabled = has;
        if (guideText != null)
            guideText.text = text ?? string.Empty;
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
        if (dialogueStarted && !on)
        {
            ConsumeDialoguePad();
            return;
        }

        if (dialogueZone != null)
            dialogueZone.SetActive(on);
        if (dialoguePadVisual != null)
            dialoguePadVisual.SetActive(on);
        if (dialoguePadCollider != null)
            dialoguePadCollider.enabled = on;
    }
}

/// <summary>Soft pulse on the STEP HERE pad.</summary>
public class ObjectPlacementDialoguePadPulse : MonoBehaviour
{
    Transform visual;
    Vector3 baseScale;
    Material mat;
    Color baseColor;

    void Awake()
    {
        Transform v = transform.Find("DialoguePadVisual");
        if (v == null)
            return;
        visual = v;
        baseScale = visual.localScale;
        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mat = mr.material;
            if (mat.HasProperty("_EmissionColor"))
                baseColor = mat.GetColor("_EmissionColor");
            else if (mat.HasProperty("_BaseColor"))
                baseColor = mat.GetColor("_BaseColor");
        }
    }

    void Update()
    {
        if (visual == null)
            return;
        float w = 1f + Mathf.Sin(Time.time * 2.6f) * 0.045f;
        visual.localScale = new Vector3(baseScale.x * w, baseScale.y, baseScale.z * w);
        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            float e = 1.4f + Mathf.Sin(Time.time * 2.6f) * 0.45f;
            mat.SetColor("_EmissionColor", baseColor * e);
        }
    }
}

/// <summary>Proximity trigger on the Dialogue pad.</summary>
public class ObjectPlacementDialogueTrigger : MonoBehaviour
{
    ObjectPlacementIntroDirector director;

    public void Bind(ObjectPlacementIntroDirector host) => director = host;

    void OnTriggerEnter(Collider other)
    {
        if (director == null || other == null || !IsPlayer(other))
            return;
        director.OnPlayerEnteredDialogue();
    }

    static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;
        if (other.GetComponentInParent<XROrigin>() != null)
            return true;
        if (other.GetComponentInParent<CharacterController>() != null)
            return true;
        return other.name.IndexOf("XR", System.StringComparison.OrdinalIgnoreCase) >= 0
               || other.name.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
