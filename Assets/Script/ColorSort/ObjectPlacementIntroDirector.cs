using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Color Sort / Object Placement intro:
/// Btn_Start → unlock walk → Dialogue trigger → Rivera welcome (Talk canvas + Talking)
/// → VR in-world cutscene (head free, move locked) → arm Phase I training.
/// </summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class ObjectPlacementIntroDirector : MonoBehaviour
{
    const string WelcomeEnvironment =
        "Welcome to the object sorting station. In this activity, you will pick up a variety of items and place them into their corresponding colored boxes. This task focuses on controlled object handling, visual sorting, and movement coordination using your non-dominant hand.";

    const string WelcomeTraining =
        "Now, let us begin your non-dominant hand training. Check the item's category and the corresponding box color. Use the controller button indicated on the instruction prompt to pick up the item, move it to the correct box, and release it inside.";

    const string CutsceneLine =
        "Watch closely — yellow fruits use Grip into the yellow box. Red vegetables use Trigger into the red box.";

    static ObjectPlacementIntroDirector instance;

    public static bool HasCompleted { get; private set; }

    [Header("Timing")]
    [SerializeField] float typeCharsPerSecond = 42f;
    [SerializeField] float pauseBetweenLines = 1.15f;
    [SerializeField] float greetHoldSeconds = 1.35f;
    [SerializeField] float fadeSeconds = 0.55f;
    [SerializeField] float demoMoveSeconds = 1.35f;
    [SerializeField] float demoHoldSeconds = 0.85f;

    ObjectPlacementStation station;
    ObjectPlacementConveyorController conveyor;
    ObjectPlacementPhaseIRound phaseRound;
    DrRiveraAnimator rivera;
    GameObject objectiveScreen;
    GameObject talkCanvas;
    TMP_Text talkText;
    GameObject dialogueZone;
    Image fadeImage;

    bool startConsumed;
    bool dialogueStarted;
    bool cutsceneRunning;

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

    /// <summary>Phase I Btn_Start routes here until intro finishes.</summary>
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

        ResolveRivera();
        ResolveTalkUi();
        SetupDialogueZone();
        EnsureFadeOverlay();
        SetTalkVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
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
            station.SetScreenText("MEET DR. RIVERA");

        if (rivera != null)
            rivera.Wave();

        return true;
    }

    void ResolveRivera()
    {
        GameObject riveraGo = GameObject.Find("Dr. Rivera");
        if (riveraGo == null)
            return;

        rivera = riveraGo.GetComponent<DrRiveraAnimator>();
        if (rivera == null)
            rivera = riveraGo.AddComponent<DrRiveraAnimator>();

        Animator anim = riveraGo.GetComponent<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;
    }

    void ResolveTalkUi()
    {
        if (rivera == null)
            return;

        Transform talk = rivera.transform.Find("Talk");
        if (talk == null)
        {
            Transform[] kids = rivera.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i].name == "Talk")
                {
                    talk = kids[i];
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

    void SetupDialogueZone()
    {
        dialogueZone = GameObject.Find("Dialogue");
        if (dialogueZone == null && rivera != null)
        {
            dialogueZone = new GameObject("Dialogue");
            dialogueZone.transform.position = rivera.transform.position + rivera.transform.forward * 0.85f + Vector3.up * 0.9f;
        }

        if (dialogueZone == null)
            return;

        // Soften the marker so it reads as a pad, not a solid prop.
        MeshRenderer mr = dialogueZone.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        // Avoid tiny/skewed parent scale — put an unscaled world trigger child.
        CapsuleCollider legacy = dialogueZone.GetComponent<CapsuleCollider>();
        if (legacy != null)
            legacy.enabled = false;

        Transform probe = dialogueZone.transform.Find("IntroTrigger");
        GameObject probeGo = probe != null ? probe.gameObject : new GameObject("IntroTrigger");
        if (probe == null)
            probeGo.transform.SetParent(dialogueZone.transform, false);

        probeGo.transform.localPosition = Vector3.zero;
        probeGo.transform.localRotation = Quaternion.identity;
        // Counter parent scale so radius/height stay in meters.
        Vector3 parentScale = dialogueZone.transform.lossyScale;
        probeGo.transform.localScale = new Vector3(
            parentScale.x != 0f ? 1f / parentScale.x : 1f,
            parentScale.y != 0f ? 1f / parentScale.y : 1f,
            parentScale.z != 0f ? 1f / parentScale.z : 1f);

        CapsuleCollider col = probeGo.GetComponent<CapsuleCollider>();
        if (col == null)
            col = probeGo.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.direction = 1;
        col.radius = 1.25f;
        col.height = 2.6f;
        col.center = new Vector3(0f, 1.0f, 0f);

        ObjectPlacementDialogueTrigger trigger = probeGo.GetComponent<ObjectPlacementDialogueTrigger>();
        if (trigger == null)
            trigger = probeGo.AddComponent<ObjectPlacementDialogueTrigger>();
        trigger.Bind(this);
    }

    void EnsureFadeOverlay()
    {
        XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
        Camera cam = origin != null && origin.Camera != null
            ? origin.Camera
            : Camera.main;
        if (cam == null)
            return;

        Transform existing = cam.transform.Find("IntroFadeCanvas");
        GameObject canvasGo;
        if (existing != null)
        {
            canvasGo = existing.gameObject;
        }
        else
        {
            canvasGo = new GameObject("IntroFadeCanvas");
            canvasGo.transform.SetParent(cam.transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;
            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 2f);
            rt.localPosition = new Vector3(0f, 0f, 0.35f);
            rt.localScale = Vector3.one * 0.25f;

            GameObject imageGo = new GameObject("Fade");
            imageGo.transform.SetParent(canvasGo.transform, false);
            RectTransform irt = imageGo.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            fadeImage = imageGo.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
            return;
        }

        fadeImage = canvasGo.GetComponentInChildren<Image>(true);
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    public void OnPlayerEnteredDialogue()
    {
        if (!startConsumed || dialogueStarted || HasCompleted || cutsceneRunning)
            return;

        dialogueStarted = true;
        ColorSortWalk.MovementLocked = true;
        StartCoroutine(WelcomeThenCutscene());
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
        yield return new WaitForSeconds(pauseBetweenLines * 0.75f);

        if (rivera != null)
        {
            rivera.Point();
            yield return new WaitForSeconds(0.85f);
            rivera.SetTalking(true);
        }

        yield return TypeLine(CutsceneLine);
        yield return new WaitForSeconds(0.55f);

        yield return RunStationDemoCutscene();

        if (rivera != null)
        {
            rivera.ReturnToIdle();
            rivera.PlayObserve();
        }

        SetTalkVisible(false);
        if (talkText != null)
            talkText.text = string.Empty;

        HasCompleted = true;
        ColorSortWalk.MovementLocked = false;

        if (phaseRound == null)
            phaseRound = Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>();
        if (phaseRound != null)
            phaseRound.ArmTrainingAfterIntro();
        else if (station != null)
            station.SetScreenText("PRESS START");
    }

    IEnumerator RunStationDemoCutscene()
    {
        cutsceneRunning = true;
        ColorSortWalk.MovementLocked = true;

        yield return Fade(1f);
        if (station != null)
            station.SetScreenText("DEMO");
        yield return Fade(0f);

        if (rivera != null)
            rivera.Point();

        if (conveyor != null)
            conveyor.StartConveyors();

        Transform yellow = station != null ? station.yellowBox : null;
        Transform red = station != null ? station.redBox : null;
        Transform spawn = station != null
            ? (station.grocerySpawnPointLeft != null ? station.grocerySpawnPointLeft : station.grocerySpawnPoint)
            : null;

        Vector3 spawnPos = spawn != null
            ? spawn.position + Vector3.up * 0.05f
            : (yellow != null ? yellow.position + Vector3.up * 0.35f + Vector3.back * 0.4f : Vector3.zero);

        GameObject fruit = SpawnDemoProp("banana", ObjectPlacementGroceryKind.Fruit, spawnPos + Vector3.left * 0.08f);
        GameObject veg = SpawnDemoProp("tomato", ObjectPlacementGroceryKind.Vegetable, spawnPos + Vector3.right * 0.08f);

        yield return new WaitForSeconds(0.35f);

        Vector3 yellowTarget = yellow != null
            ? yellow.position + Vector3.up * 0.12f
            : spawnPos + Vector3.forward * 0.5f;
        Vector3 redTarget = red != null
            ? red.position + Vector3.up * 0.12f
            : spawnPos + Vector3.forward * 0.5f + Vector3.right * 0.25f;

        yield return MovePropArc(fruit, yellowTarget, demoMoveSeconds);
        yield return new WaitForSeconds(0.2f);
        yield return MovePropArc(veg, redTarget, demoMoveSeconds);
        yield return new WaitForSeconds(demoHoldSeconds);

        if (conveyor != null)
            conveyor.StopConveyors();

        if (rivera != null)
            rivera.Agree();

        yield return new WaitForSeconds(0.7f);
        yield return Fade(1f);

        if (fruit != null)
            Destroy(fruit);
        if (veg != null)
            Destroy(veg);

        if (station != null)
            station.SetScreenText("PRESS START");

        yield return Fade(0f);
        cutsceneRunning = false;
    }

    GameObject SpawnDemoProp(string id, ObjectPlacementGroceryKind kind, Vector3 worldPos)
    {
        GameObject go = ObjectPlacementGroceryFactory.Create(id, kind);
        go.name = "IntroDemo_" + id;
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.identity;

        ObjectPlacementGroceryItem item = go.GetComponent<ObjectPlacementGroceryItem>();
        if (item != null)
            Destroy(item);

        var grab = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            Destroy(grab);

        Collider[] cols = go.GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        return go;
    }

    IEnumerator MovePropArc(GameObject prop, Vector3 end, float duration)
    {
        if (prop == null)
            yield break;

        Vector3 start = prop.transform.position;
        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            float e = u * u * (3f - 2f * u);
            Vector3 a = Vector3.Lerp(start, mid, e);
            Vector3 b = Vector3.Lerp(mid, end, e);
            prop.transform.position = Vector3.Lerp(a, b, e);
            prop.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        prop.transform.position = end;
    }

    IEnumerator TypeLine(string line)
    {
        if (talkText == null)
        {
            yield return new WaitForSeconds(Mathf.Max(1.5f, line.Length / Mathf.Max(1f, typeCharsPerSecond)));
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

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
            EnsureFadeOverlay();
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

    void SetTalkVisible(bool on)
    {
        if (talkCanvas != null)
            talkCanvas.SetActive(on);
    }
}

/// <summary>Proximity trigger on the Dialogue pad in front of Dr. Rivera.</summary>
public class ObjectPlacementDialogueTrigger : MonoBehaviour
{
    ObjectPlacementIntroDirector director;

    public void Bind(ObjectPlacementIntroDirector host)
    {
        director = host;
    }

    void OnTriggerEnter(Collider other)
    {
        if (director == null || other == null)
            return;

        if (!IsPlayer(other))
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

        Camera cam = Camera.main;
        if (cam != null && other.transform.IsChildOf(cam.transform.root))
            return true;

        return other.name.IndexOf("XR", System.StringComparison.OrdinalIgnoreCase) >= 0
               || other.name.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
