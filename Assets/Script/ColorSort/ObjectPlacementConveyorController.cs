using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Index-finger poke on Btn_Start_World (either hand) → press anim → both belts run.
/// Conveyor scrolls +X; ConveyorLeft scrolls reverse (-X).
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementConveyorController : MonoBehaviour
{
    [Header("Motion")]
    public float beltScrollSpeed = 0.65f;
    public float flowMarkSpeed = 0.38f;
    public float pokeRadius = 0.055f;
    [Tooltip("Evenly spaced marks generated from a single FlowMark template per belt.")]
    public int flowMarkCount = 6;

    [Header("Button")]
    public float buttonPressDepth = 0.028f;
    public float buttonPressSeconds = 0.07f;
    public float buttonReleaseSeconds = 0.14f;

    [Header("Audio")]
    public float motorVolume = 0.72f;
    public float clickVolume = 0.7f;

    ObjectPlacementStation station;
    ObjectPlacementPhaseIRound phaseRound;
    Transform button;
    Vector3 buttonRestLocal;
    bool pressing;
    bool running;
    bool tipWasInsideLeft;
    bool tipWasInsideRight;

    readonly List<BeltRig> belts = new List<BeltRig>();
    AudioSource motorSource;
    AudioSource clickSource;
    Coroutine pressRoutine;
    static Texture2D sharedStripeTex;

    sealed class BeltRig
    {
        public string name;
        public Material beltMat;
        public readonly List<Transform> flowMarks = new List<Transform>();
        public float uvOffset;
        public float markMinX;
        public float markMaxX;
        public float direction = 1f;
    }

    void Awake()
    {
        station = GetComponent<ObjectPlacementStation>();
        CacheRigs();
        WireButton();
        EnsureAudio();
    }

    void OnDisable()
    {
        StopMotor();
    }

    void Update()
    {
        PollIndexFingerPoke();

        if (!running)
            return;

        if (phaseRound == null)
            phaseRound = Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>();
        bool pauseLeft = phaseRound != null && phaseRound.LeftBeltMotionPaused;

        float dt = Time.deltaTime;
        for (int i = 0; i < belts.Count; i++)
        {
            BeltRig rig = belts[i];
            // Left / outbound item belt stops while the stopper queue is full.
            if (pauseLeft && IsLeftBelt(rig))
                continue;
            TickBelt(rig, dt);
        }
    }

    static bool IsLeftBelt(BeltRig rig)
    {
        if (rig == null)
            return false;
        if (rig.direction < 0f)
            return true;
        return !string.IsNullOrEmpty(rig.name) &&
               rig.name.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void CacheRigs()
    {
        belts.Clear();

        Transform right = station != null ? station.conveyor : null;
        Transform left = station != null ? station.conveyorLeft : null;
        if (right == null)
            right = transform.Find("Conveyor");
        if (left == null)
            left = transform.Find("ConveyorLeft");

        if (right != null)
            belts.Add(BuildRig(right, station != null ? station.beltSurface : null, 1f));
        if (left != null)
            belts.Add(BuildRig(left, station != null ? station.beltSurfaceLeft : null, -1f));
    }

    BeltRig BuildRig(Transform root, Transform beltSurfaceOverride, float direction)
    {
        BeltRig rig = new BeltRig();
        rig.name = root.name;
        rig.direction = direction;

        Transform beltTf = beltSurfaceOverride;
        if (beltTf == null)
            beltTf = root.Find("BeltSurface");

        if (beltTf != null)
        {
            Renderer beltRenderer = beltTf.GetComponent<Renderer>();
            if (beltRenderer != null)
            {
                rig.beltMat = beltRenderer.material;
                ApplyStripeTexture(rig.beltMat);
            }

            // Straight path along full BeltSurface local X (supports lengthened belts).
            float half = Mathf.Abs(beltTf.localScale.x) * 0.5f;
            float center = beltTf.localPosition.x;
            float pad = 0.05f;
            rig.markMinX = center - half + pad;
            rig.markMaxX = center + half - pad;
        }
        else
        {
            rig.markMinX = -0.85f;
            rig.markMaxX = 0.55f;
        }

        BuildEvenFlowMarks(rig, root, beltTf);
        return rig;
    }

    /// <summary>
    /// Uses one FlowMark template; hides extras; spawns evenly spaced clones on a straight belt path.
    /// Keep a single FlowMark (e.g. FlowMark_0) under each conveyor — code generates the rest.
    /// </summary>
    void BuildEvenFlowMarks(BeltRig rig, Transform root, Transform beltTf)
    {
        Transform template = null;
        List<Transform> existing = new List<Transform>();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == root)
                continue;
            if (!child.name.StartsWith("FlowMark"))
                continue;
            existing.Add(child);
            if (child.name == "FlowMark_0" || child.name == "FlowMark")
                template = child;
        }

        if (template == null && existing.Count > 0)
            template = existing[0];

        // Clear prior generated marks from a previous play session / domain reload.
        for (int i = 0; i < existing.Count; i++)
        {
            Transform mark = existing[i];
            if (mark == null)
                continue;
            if (mark.name.StartsWith("FlowMark_Gen_"))
            {
                Destroy(mark.gameObject);
                continue;
            }

            if (mark != template)
                mark.gameObject.SetActive(false);
        }

        if (template == null)
            return;

        int count = Mathf.Max(1, flowMarkCount);
        Vector3 templateLocal = root.InverseTransformPoint(template.position);
        float y = templateLocal.y;
        float z = templateLocal.z;
        if (beltTf != null)
        {
            // Straight centerline on top of the belt.
            y = beltTf.localPosition.y + Mathf.Abs(beltTf.localScale.y) * 0.5f + 0.012f;
            z = beltTf.localPosition.z;
        }

        Vector3 templateScale = template.localScale;
        Quaternion templateRot = template.localRotation;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float x = Mathf.Lerp(rig.markMinX, rig.markMaxX, t);

            GameObject clone = Instantiate(template.gameObject, root, false);
            clone.name = "FlowMark_Gen_" + i;
            clone.SetActive(true);
            clone.transform.localPosition = new Vector3(x, y, z);
            clone.transform.localRotation = templateRot;
            clone.transform.localScale = templateScale;
            rig.flowMarks.Add(clone.transform);
        }

        template.gameObject.SetActive(false);
    }

    static void ApplyStripeTexture(Material mat)
    {
        if (mat == null)
            return;

        if (sharedStripeTex == null)
        {
            sharedStripeTex = new Texture2D(64, 8, TextureFormat.RGBA32, false);
            sharedStripeTex.wrapMode = TextureWrapMode.Repeat;
            sharedStripeTex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < 64; x++)
            {
                bool dark = ((x / 8) % 2) == 0;
                Color c = dark ? new Color(0.08f, 0.08f, 0.09f) : new Color(0.18f, 0.18f, 0.2f);
                for (int y = 0; y < 8; y++)
                    sharedStripeTex.SetPixel(x, y, c);
            }

            sharedStripeTex.Apply(false, false);
        }

        mat.mainTexture = sharedStripeTex;
        mat.mainTextureScale = new Vector2(4f, 1f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", sharedStripeTex);
            mat.SetTextureScale("_BaseMap", new Vector2(4f, 1f));
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
    }

    void WireButton()
    {
        if (station != null && station.startButton != null)
            button = station.startButton.Find("Btn_Start_World");

        if (button == null)
        {
            Transform startRoot = transform.Find("StartConveyorButton");
            if (startRoot != null)
                button = startRoot.Find("Btn_Start_World");
        }

        if (button == null)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == "Btn_Start_World")
                {
                    button = all[i];
                    break;
                }
            }
        }

        if (button == null)
            return;

        buttonRestLocal = button.localPosition;

        // Visual collider only — poke uses index tip distance, not grab select.
        Collider col = button.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = button.gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = 0.55f;
            capsule.height = 1.2f;
            capsule.direction = 1;
        }

        // Strip grab/select interactable so VR grip cannot "grab" the button.
        var simple = button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (simple != null)
            Destroy(simple);

        var grab = button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            Destroy(grab);

        ObjectPlacementStartButtonProxy proxy = button.GetComponent<ObjectPlacementStartButtonProxy>();
        if (proxy == null)
            proxy = button.gameObject.AddComponent<ObjectPlacementStartButtonProxy>();
        proxy.Bind(this);
    }

    void EnsureAudio()
    {
        // Mostly 2D so motor is always audible in headset / editor.
        motorSource = gameObject.AddComponent<AudioSource>();
        motorSource.playOnAwake = false;
        motorSource.loop = true;
        motorSource.spatialBlend = 0.15f;
        motorSource.rolloffMode = AudioRolloffMode.Linear;
        motorSource.minDistance = 1f;
        motorSource.maxDistance = 20f;
        motorSource.priority = 64;
        motorSource.volume = motorVolume;
        motorSource.clip = ObjectPlacementConveyorSounds.MotorLoop();
        motorSource.dopplerLevel = 0f;

        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.loop = false;
        clickSource.spatialBlend = 0.1f;
        clickSource.priority = 32;
        clickSource.volume = clickVolume;
        clickSource.dopplerLevel = 0f;
    }

    void PollIndexFingerPoke()
    {
        if (pressing || button == null)
            return;

        tipWasInsideRight = CheckTipEdge(FindIndexTip(true), tipWasInsideRight);
        tipWasInsideLeft = CheckTipEdge(FindIndexTip(false), tipWasInsideLeft);
    }

    bool CheckTipEdge(Transform tip, bool wasInside)
    {
        if (tip == null)
            return false;

        float dist = Vector3.Distance(tip.position, button.position);
        bool inside = dist <= pokeRadius;
        if (inside && !wasInside)
            TryPress();
        return inside;
    }

    static Transform FindIndexTip(bool rightHand)
    {
        Transform controller = PlayerHands.WritingController(rightHand);
        if (controller == null)
        {
            string want = rightHand ? "Right Controller" : "Left Controller";
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == want)
                {
                    controller = all[i];
                    break;
                }
            }
        }

        if (controller == null)
            return null;

        Transform tip = null;
        Transform[] children = controller.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string n = children[i].name;
            if (n.Contains("index3") || n.Contains("Index Distal") || n.Contains("INDEX_DISTAL"))
                tip = children[i];
        }

        return tip != null ? tip : controller;
    }

    public void TryPress()
    {
        if (pressing)
            return;

        if (pressRoutine != null)
            StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(PressRoutine());
    }

    IEnumerator PressRoutine()
    {
        pressing = true;

        if (clickSource != null)
        {
            clickSource.spatialBlend = 0.1f;
            clickSource.PlayOneShot(ObjectPlacementConveyorSounds.ButtonClick(), clickVolume);
        }

        if (button != null)
        {
            Vector3 down = buttonRestLocal + Vector3.down * buttonPressDepth;
            float t = 0f;
            while (t < buttonPressSeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.01f, buttonPressSeconds));
                button.localPosition = Vector3.Lerp(buttonRestLocal, down, u);
                yield return null;
            }

            button.localPosition = down;
            t = 0f;
            while (t < buttonReleaseSeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.01f, buttonReleaseSeconds));
                float e = 1f - (1f - u) * (1f - u);
                button.localPosition = Vector3.Lerp(down, buttonRestLocal, e);
                yield return null;
            }

            button.localPosition = buttonRestLocal;
        }

        StartConveyors();
        pressing = false;
        pressRoutine = null;
    }

    public bool IsRunning => running;

    public float BeltFlowSpeed => flowMarkSpeed;

    public void StartConveyors()
    {
        if (running)
            return;

        running = true;
        if (motorSource != null)
        {
            motorSource.clip = ObjectPlacementConveyorSounds.MotorLoop();
            motorSource.volume = motorVolume;
            motorSource.spatialBlend = 0.15f;
            if (!motorSource.isPlaying)
                motorSource.Play();
        }

        // Screen text is owned by ObjectPlacementPhaseIRound during training.
        if (station != null && Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>() == null)
            station.SetScreenText("CONVEYOR ON");
    }

    public void StopConveyors()
    {
        running = false;
        StopMotor();
        if (station != null && Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>() == null)
            station.SetScreenText("READY!");
    }

    void StopMotor()
    {
        if (motorSource != null && motorSource.isPlaying)
            motorSource.Stop();
    }

    void TickBelt(BeltRig rig, float dt)
    {
        if (rig == null)
            return;

        float dir = Mathf.Sign(rig.direction);
        if (Mathf.Approximately(dir, 0f))
            dir = 1f;

        if (rig.beltMat != null)
        {
            rig.uvOffset += beltScrollSpeed * dt * dir;
            if (rig.uvOffset > 20f)
                rig.uvOffset -= 20f;
            if (rig.uvOffset < -20f)
                rig.uvOffset += 20f;

            Vector2 offset = new Vector2(rig.uvOffset, 0f);
            if (rig.beltMat.HasProperty("_BaseMap"))
                rig.beltMat.SetTextureOffset("_BaseMap", offset);
            if (rig.beltMat.HasProperty("_MainTex"))
                rig.beltMat.SetTextureOffset("_MainTex", offset);
            rig.beltMat.mainTextureOffset = offset;
        }

        float span = rig.markMaxX - rig.markMinX;
        if (span < 0.05f || rig.flowMarks.Count == 0)
            return;

        for (int i = 0; i < rig.flowMarks.Count; i++)
        {
            Transform mark = rig.flowMarks[i];
            if (mark == null)
                continue;

            Vector3 p = mark.localPosition;
            p.x += flowMarkSpeed * dt * dir;
            if (dir > 0f)
            {
                while (p.x > rig.markMaxX)
                    p.x -= span;
            }
            else
            {
                while (p.x < rig.markMinX)
                    p.x += span;
            }

            mark.localPosition = p;
        }
    }
}

/// <summary>
/// Editor click fallback on Btn_Start_World. VR uses index-finger tip poke on the controller.
/// </summary>
public class ObjectPlacementStartButtonProxy : MonoBehaviour
{
    ObjectPlacementConveyorController owner;

    public void Bind(ObjectPlacementConveyorController controller)
    {
        owner = controller;
    }

    void OnMouseDown()
    {
        if (owner != null)
            owner.TryPress();
    }
}
