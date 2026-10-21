using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuidedColorSortLesson : MonoBehaviour, IColorSortHost
{
    const int TeachSteps = 10;
    const int PracticeSets = 5;
    const int StepTotal = TeachSteps + PracticeSets;
    const float SqueezeAmount = 0.55f;
    const float OpenAmount = 0.35f;

    enum SortColor
    {
        Red,
        Blue,
        Yellow
    }

    class SortBox
    {
        public Transform transform;
        public Collider collider;
        public SortColor color;
    }

    struct BoxSlot
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    public bool RetryWrong
    {
        get { return true; }
    }

    GuidedLesson lesson;
    GuidedColorSortVoices voices;
    GameObject objectiveScreen;
    GameObject resultsScreen;
    GameObject boxesRoot;
    Transform shapeArea;
    Material redMaterial;
    Material blueMaterial;
    Material yellowMaterial;
    readonly List<ColorSortPiece> shapes = new List<ColorSortPiece>();
    readonly List<ColorSortPiece> active = new List<ColorSortPiece>();
    readonly List<SortBox> boxes = new List<SortBox>();
    readonly List<BoxSlot> boxSlots = new List<BoxSlot>();
    InputAction leftGrip;
    InputAction rightGrip;
    int step;
    bool lessonOpen;
    bool pendingAdvance;
    bool squeezeArmed;
    bool waitingOpen;
    float wordHideAt;
    string lastFail;

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
        if (!scene.IsValid() || scene.name != "Guided Color Sort")
            return;

        if (FindAnyObjectByType<GuidedColorSortLesson>() != null)
            return;

        GameObject host = new GameObject("GuidedLesson");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GuidedColorSortLesson>();
    }

    void Awake()
    {
        lesson = GetComponent<GuidedLesson>();
        if (lesson == null)
            lesson = gameObject.AddComponent<GuidedLesson>();

        voices = GuidedColorSortVoices.Find();
        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        FindShapes();
        FindShapeArea();
        FindBoxes();
        HideShapes();
        if (boxesRoot != null)
            boxesRoot.SetActive(false);

        HideHud();
        SetObjectiveText("HOW TO PLAY:\nFirst squeeze and open GRAB. Then grab objects. Left is red, right is blue, yellow needs both hands. Put each object in the matching box.");
        WireStart();
        ShowIntro();
    }

    void Start()
    {
        HideHud();
        if (!lessonOpen)
        {
            ShowIntro();
            if (voices != null)
                voices.PlayIntro();
        }
    }

    void OnEnable()
    {
        BindSqueeze();
    }

    void OnDisable()
    {
        UnbindSqueeze();
    }

    void Update()
    {
        HideWordIfDue();

        if (GamePauseMenu.IsFrozen)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (!active[i].resolved)
                    active[i].CancelHold();
            }

            return;
        }

        if (!lessonOpen)
            return;

        if (IsPoseStep())
            PollPose(step == 2);

        if (pendingAdvance)
        {
            pendingAdvance = false;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].resolved)
                    active[i].HideResolved();
            }

            Advance();
        }
    }

    void ShowIntro()
    {
        lessonOpen = false;
        pendingAdvance = false;
        step = 0;
        HideShapes();
        GuidedPointer.ClearAll();
        HideWord();
        if (boxesRoot != null)
            boxesRoot.SetActive(false);
        if (lesson != null)
            lesson.Hide();
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
        if (objectiveScreen != null)
            objectiveScreen.SetActive(true);
    }

    public void StartLesson()
    {
        ScoringChecker.Ensure().TryContinue(StartLessonAfterCheck);
    }

    void StartLessonAfterCheck()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (resultsScreen != null)
            resultsScreen.SetActive(false);

        HideHud();
        PlayerHands.ShowBothHands();
        lessonOpen = true;
        step = 1;
        squeezeArmed = false;
        waitingOpen = false;
        ShowCurrentStep();
        BeginStep();
    }

    void ShowCurrentStep()
    {
        if (lesson == null)
            return;

        if (step == 1)
            lesson.ShowStep(1, StepTotal, "Squeeze GRAB on your LEFT controller, then open GRAB to release.", "");
        else if (step == 2)
            lesson.ShowStep(2, StepTotal, "Squeeze GRAB on your RIGHT controller, then open GRAB to release.", "");
        else if (step == 3)
            lesson.ShowStep(3, StepTotal, "Grab this object with your LEFT hand, then open GRAB to release.", "");
        else if (step == 4)
            lesson.ShowStep(4, StepTotal, "Grab this object with your RIGHT hand, then open GRAB to release.", "");
        else if (step == 5)
            lesson.ShowStep(5, StepTotal, "This is RED. Grab it with your LEFT hand, then release.", "Left hand is red.");
        else if (step == 6)
            lesson.ShowStep(6, StepTotal, "This is BLUE. Grab it with your RIGHT hand, then release.", "Right hand is blue.");
        else if (step == 7)
            lesson.ShowStep(7, StepTotal, "This is YELLOW. Use both hands. One hand will not lift it.", "Yellow needs both hands.");
        else if (step == 8)
            lesson.ShowStep(8, StepTotal, "Put the RED object in the RED box. Open GRAB to release it.", "");
        else if (step == 9)
            lesson.ShowStep(9, StepTotal, "Put the BLUE object in the BLUE box.", "");
        else if (step == 10)
            lesson.ShowStep(10, StepTotal, "Put the YELLOW object in the YELLOW box. Use both hands.", "");
        else
        {
            int setNumber = step - TeachSteps;
            lesson.ShowStep(step, StepTotal, "Set " + setNumber + " of " + PracticeSets + ". Grab, then release in the matching box.", "");
        }

        if (voices != null)
            voices.PlayStep(step);
    }

    void BeginStep()
    {
        pendingAdvance = false;
        squeezeArmed = false;
        waitingOpen = false;
        lastFail = "";
        HideShapes();
        GuidedPointer.ClearAll();

        if (IsPoseStep())
        {
            if (boxesRoot != null)
                boxesRoot.SetActive(false);
            MarkPoseHand(step == 2);
            return;
        }

        bool showBoxes = NeedBoxes();
        if (boxesRoot != null)
            boxesRoot.SetActive(showBoxes);
        if (showBoxes)
            RestoreBoxes();

        SpawnForStep();
        PlaceShapes();
        RefreshMarks();
    }

    void SpawnForStep()
    {
        if (IsPractice())
        {
            SpawnPractice();
            return;
        }

        if (step == 3 || step == 5 || step == 8)
            SpawnOne(SortColor.Red, 0);
        else if (step == 4 || step == 6 || step == 9)
            SpawnOne(SortColor.Blue, 1);
        else
            SpawnOne(SortColor.Yellow, 2);
    }

    void SpawnOne(SortColor color, int shapeIndex)
    {
        ColorSortPiece piece = ShapeAt(shapeIndex);
        if (piece == null)
            return;

        piece.PrepareColor((int)color, MaterialFor(color));
        WirePiece(piece);
        active.Add(piece);
    }

    void SpawnPractice()
    {
        int setNumber = step - TeachSteps;
        if (setNumber == 2 || setNumber == 5)
        {
            SpawnOne(SortColor.Red, 0);
            SpawnOne(SortColor.Yellow, 2);
            return;
        }

        if (setNumber == 3)
        {
            SpawnOne(SortColor.Blue, 1);
            SpawnOne(SortColor.Yellow, 2);
            return;
        }

        SpawnOne(SortColor.Red, 0);
        SpawnOne(SortColor.Blue, 1);
    }

    ColorSortPiece ShapeAt(int index)
    {
        if (shapes.Count == 0)
            return null;

        if (index < 0 || index >= shapes.Count)
            index = 0;

        return shapes[index];
    }

    void MarkPoseHand(bool rightHand)
    {
        GuidedPointer.FollowHand(FindHand(!rightHand), "GRAB");
    }

    void PollPose(bool rightHand)
    {
        bool down = ReadSqueeze(rightHand ? rightGrip : leftGrip) >= (waitingOpen ? OpenAmount : SqueezeAmount);
        bool other = ReadSqueeze(rightHand ? leftGrip : rightGrip) >= SqueezeAmount;
        if (other && !down)
            HintUseHand(!rightHand);
        else if (!waitingOpen && lesson != null && string.IsNullOrEmpty(lastFail))
            lesson.SetHint("");

        if (!waitingOpen)
        {
            if (!down)
            {
                squeezeArmed = true;
                return;
            }

            if (!squeezeArmed)
                return;

            squeezeArmed = false;
            waitingOpen = true;
            BallDropSounds.PlayCatch();
            GuidedPointer.ClearAll();
            GuidedPointer.FollowHand(FindHand(!rightHand), "OPEN");
            if (lesson != null)
                lesson.SetHint("Now open GRAB to release.");
            if (voices != null)
                voices.PlayOpen();
            return;
        }

        if (down)
        {
            squeezeArmed = true;
            return;
        }

        if (!squeezeArmed)
            return;

        squeezeArmed = false;
        waitingOpen = false;
        BallDropSounds.PlayCatch();
        Advance();
    }

    void HintUseHand(bool left)
    {
        if (lesson == null)
            return;

        lesson.SetHint(left ? "Use your LEFT controller." : "Use your RIGHT controller.");
        if (voices != null)
            voices.PlayUseHand(left);
    }

    void Advance()
    {
        if (step < StepTotal)
        {
            step++;
            ShowCurrentStep();
            BeginStep();
            return;
        }

        Finish();
    }

    void WirePiece(ColorSortPiece piece)
    {
        piece.Grabbed = OnPieceGrabbed;
        piece.YellowNeedsBoth = OnYellowNeedsBoth;
        piece.WrongDropped = OnWrongDropped;
        piece.MissedBox = OnMissedBox;
    }

    void OnPieceGrabbed(ColorSortPiece piece, bool leftHand)
    {
        if (!lessonOpen || piece == null || lesson == null)
            return;

        if (step == 3 && !leftHand)
        {
            lesson.SetHint("Use your LEFT hand.");
            if (voices != null)
                voices.PlayUseHand(true);
        }
        else if (step == 4 && leftHand)
        {
            lesson.SetHint("Use your RIGHT hand.");
            if (voices != null)
                voices.PlayUseHand(false);
        }
        else if (step == 5 && !leftHand)
        {
            lesson.SetHint("RED uses the LEFT hand.");
            if (voices != null)
                voices.PlayUseHand(true);
        }
        else if (step == 6 && leftHand)
        {
            lesson.SetHint("BLUE uses the RIGHT hand.");
            if (voices != null)
                voices.PlayUseHand(false);
        }
        else if (piece.ColorIndex == (int)SortColor.Yellow)
        {
            lesson.SetHint("Yellow needs both hands. Open GRAB together" + (NeedBoxes() ? " over the YELLOW box." : " to release."));
            if (voices != null)
                voices.PlayYellowBoth();
        }
        else if (NeedBoxes())
        {
            lesson.SetHint("Open GRAB to release over the matching box.");
            if (voices != null)
                voices.PlayOverBox();
        }
        else
        {
            lesson.SetHint("Open GRAB to release.");
            if (voices != null)
                voices.PlayOpen();
        }

        RefreshMarks();
    }

    void OnYellowNeedsBoth(ColorSortPiece piece)
    {
        if (!lessonOpen || lesson == null)
            return;

        lesson.SetHint("Yellow will not grab with one hand. Use both hands.");
        if (voices != null)
            voices.PlayYellowBoth();
        RefreshMarks();
    }

    void OnWrongDropped(ColorSortPiece piece)
    {
        if (!lessonOpen)
            return;

        ShowWord("Wrong");
        if (lesson != null)
            lesson.SetHint(string.IsNullOrEmpty(lastFail) ? "Try again." : lastFail);
        PlayWrongVoice();
        RefreshMarks();
    }

    void OnMissedBox(ColorSortPiece piece)
    {
        if (!lessonOpen || lesson == null)
            return;

        if (NeedBoxes())
        {
            lesson.SetHint("Release over the matching box.");
            if (voices != null)
                voices.PlayOverBox();
        }
        else
        {
            lesson.SetHint("Open GRAB to release.");
            if (voices != null)
                voices.PlayOpen();
        }

        RefreshMarks();
    }

    public bool ScoreDrop(Bounds bounds, int colorIndex, bool leftHand, bool bothHands, out bool correct)
    {
        correct = false;
        lastFail = "";

        if (IsPoseStep())
            return false;

        if (!NeedBoxes())
        {
            correct = TeachDropOk((SortColor)colorIndex, leftHand, bothHands);
            if (!correct)
                lastFail = TeachFailReason((SortColor)colorIndex, leftHand, bothHands);
            return true;
        }

        SortBox box = BoxUnder(bounds);
        if (box == null)
            return false;

        correct = CorrectDrop((SortColor)colorIndex, leftHand, bothHands, box);
        if (!correct)
            lastFail = FailReason((SortColor)colorIndex, leftHand, bothHands, box);
        return true;
    }

    bool TeachDropOk(SortColor color, bool leftHand, bool bothHands)
    {
        if (step == 3)
            return leftHand;
        if (step == 4)
            return !leftHand;
        if (step == 5)
            return leftHand && color == SortColor.Red;
        if (step == 6)
            return !leftHand && color == SortColor.Blue;
        if (step == 7)
            return bothHands && color == SortColor.Yellow;
        return false;
    }

    void PlayWrongVoice()
    {
        if (voices == null)
            return;

        if (!string.IsNullOrEmpty(lastFail) && lastFail.Contains("LEFT"))
            voices.PlayUseHand(true);
        else if (!string.IsNullOrEmpty(lastFail) && lastFail.Contains("RIGHT"))
            voices.PlayUseHand(false);
        else if (!string.IsNullOrEmpty(lastFail) && lastFail.Contains("Yellow"))
            voices.PlayYellowBoth();
        else
            voices.PlayWrongBox();
    }

    string TeachFailReason(SortColor color, bool leftHand, bool bothHands)
    {
        if (step == 3 || (step == 5 && color == SortColor.Red))
            return "Use your LEFT hand.";
        if (step == 4 || (step == 6 && color == SortColor.Blue))
            return "Use your RIGHT hand.";
        if (step == 7 || color == SortColor.Yellow)
            return "Yellow needs both hands.";
        if (!leftHand)
            return "Use your LEFT hand.";
        return "Use your RIGHT hand.";
    }

    public void OnPieceResolved(int colorIndex, bool correct)
    {
        if (!lessonOpen)
            return;

        if (correct)
            ShowWord("Great");

        if (IsPractice())
        {
            int remaining = 0;
            for (int i = 0; i < active.Count; i++)
            {
                if (!active[i].resolved)
                    remaining++;
            }

            if (remaining == 0)
                pendingAdvance = true;
            else
            {
                if (lesson != null)
                    lesson.SetHint("Now the other object.");
                if (voices != null)
                    voices.PlayOther();
            }

            RefreshMarks();
            return;
        }

        pendingAdvance = true;
        RefreshMarks();
    }

    void Finish()
    {
        lessonOpen = false;
        pendingAdvance = false;
        HideShapes();
        GuidedPointer.ClearAll();
        HideWord();
        if (boxesRoot != null)
            boxesRoot.SetActive(false);
        if (lesson != null)
            lesson.Hide();
        ShowDone();
    }

    void ShowDone()
    {
        if (resultsScreen == null)
            return;

        ColorSortResults sim = resultsScreen.GetComponent<ColorSortResults>();
        if (sim != null)
            sim.enabled = false;

        resultsScreen.SetActive(true);
        if (voices != null)
            voices.PlayDone();
        Transform panel = resultsScreen.transform.Find("ResultsPanel");
        if (panel == null)
            return;

        panel.gameObject.SetActive(true);
        UseGuidedCard(panel);
        EnsureDoneText(panel);
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child.name == "Value" || child.name.StartsWith("Value_"))
                child.gameObject.SetActive(false);
        }

        TMP_Text[] labels = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string name = labels[i].gameObject.name;
            bool keep = name == "Text_Done" || IsButtonLabel(labels[i].transform);
            labels[i].gameObject.SetActive(keep);
        }
    }

    static void UseGuidedCard(Transform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image == null)
            return;

        Transform stepCanvas = FindNamedTransform("GuidedStepCanvas");
        if (stepCanvas != null)
        {
            Image card = stepCanvas.GetComponentInChildren<Image>(true);
            if (card != null && card.sprite != null)
                image.sprite = card.sprite;
        }

        image.color = Color.white;
        image.preserveAspect = true;
    }

    static bool IsButtonLabel(Transform label)
    {
        Transform current = label;
        while (current != null)
        {
            if (current.GetComponent<Button>() != null)
                return true;
            current = current.parent;
        }

        return false;
    }

    static void EnsureDoneText(Transform panel)
    {
        if (FindChildText(panel, "Text_Done") != null)
            return;

        GameObject go = new GameObject("Text_Done", typeof(RectTransform));
        go.transform.SetParent(panel, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.35f);
        rect.anchorMax = new Vector2(0.9f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        text.fontSize = 6.5f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.text = "You did it!\nThat's how you play.";
    }

    void RefreshMarks()
    {
        GuidedPointer.ClearAll();
        if (!lessonOpen)
            return;

        if (IsPoseStep())
        {
            GuidedPointer.FollowHand(FindHand(step == 1), waitingOpen ? "OPEN" : "GRAB");
            return;
        }

        ColorSortPiece held = HeldPiece();
        bool left = GuideLeftHand();
        if (held != null)
        {
            if (held.ColorIndex == (int)SortColor.Yellow)
            {
                GuidedPointer.FollowHand(FindHand(true), "OPEN");
                GuidedPointer.FollowHand(FindHand(false), "OPEN");
            }
            else
            {
                GuidedPointer.FollowHand(FindHand(left), "OPEN");
            }

            GuidedPointer.FollowMesh(held.transform, "");
            return;
        }

        if (step == 7 || HasYellowActive())
        {
            GuidedPointer.FollowHand(FindHand(true), "BOTH");
            GuidedPointer.FollowHand(FindHand(false), "");
        }
        else
        {
            GuidedPointer.FollowHand(FindHand(left), "GRAB");
        }

        for (int i = 0; i < active.Count; i++)
        {
            ColorSortPiece piece = active[i];
            if (piece == null || piece.resolved)
                continue;

            GuidedPointer.FollowMesh(piece.transform, "");
        }
    }

    bool GuideLeftHand()
    {
        if (step == 4 || step == 6 || step == 9)
            return false;

        ColorSortPiece held = HeldPiece();
        if (held != null)
            return held.ColorIndex != (int)SortColor.Blue;

        if (IsPractice())
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null && !active[i].resolved)
                    return active[i].ColorIndex != (int)SortColor.Blue;
            }
        }

        return true;
    }

    bool HasYellowActive()
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null && !active[i].resolved && active[i].ColorIndex == (int)SortColor.Yellow)
                return true;
        }

        return false;
    }

    ColorSortPiece HeldPiece()
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i] != null && !active[i].resolved && active[i].IsHeld)
                return active[i];
        }

        return null;
    }

    bool IsPoseStep()
    {
        return step == 1 || step == 2;
    }

    bool NeedBoxes()
    {
        return step >= 8;
    }

    bool IsPractice()
    {
        return step > TeachSteps;
    }

    void FindShapes()
    {
        GameObject root = FindNamed("Shapes");
        if (root == null)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>(true) == null)
                continue;

            ColorSortPiece piece = child.GetComponent<ColorSortPiece>();
            if (piece == null)
                piece = child.gameObject.AddComponent<ColorSortPiece>();

            piece.Bind(this);
            WirePiece(piece);
            shapes.Add(piece);
        }
    }

    void FindShapeArea()
    {
        GameObject area = FindNamed("AREA");
        if (area == null)
            return;

        shapeArea = area.transform;
        Renderer areaRenderer = area.GetComponent<Renderer>();
        if (areaRenderer != null)
            areaRenderer.enabled = false;
    }

    void FindBoxes()
    {
        boxesRoot = FindNamed("Boxes");
        if (boxesRoot == null)
            return;

        AddBox("Red Box", SortColor.Red);
        AddBox("Blue Box", SortColor.Blue);
        AddBox("Yellow Box", SortColor.Yellow);
    }

    void AddBox(string objectName, SortColor color)
    {
        GameObject boxObject = FindNamed(objectName);
        if (boxObject == null)
            return;

        Renderer renderer = boxObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (color == SortColor.Red)
                redMaterial = renderer.sharedMaterial;
            else if (color == SortColor.Blue)
                blueMaterial = renderer.sharedMaterial;
            else
                yellowMaterial = renderer.sharedMaterial;
        }

        Collider collider = boxObject.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider created = boxObject.AddComponent<BoxCollider>();
            if (renderer is MeshRenderer meshRenderer)
            {
                created.center = meshRenderer.localBounds.center;
                created.size = meshRenderer.localBounds.size;
            }

            created.isTrigger = true;
            collider = created;
        }

        SortBox box = new SortBox();
        box.transform = boxObject.transform;
        box.collider = collider;
        box.color = color;
        boxes.Add(box);

        BoxSlot slot = new BoxSlot();
        slot.localPosition = boxObject.transform.localPosition;
        slot.localRotation = boxObject.transform.localRotation;
        boxSlots.Add(slot);
    }

    Material MaterialFor(SortColor color)
    {
        if (color == SortColor.Red)
            return redMaterial;
        if (color == SortColor.Blue)
            return blueMaterial;
        return yellowMaterial;
    }

    void HideShapes()
    {
        for (int i = 0; i < shapes.Count; i++)
            shapes[i].Remove();
        active.Clear();
    }

    void RestoreBoxes()
    {
        if (boxes.Count == 0 || boxSlots.Count != boxes.Count)
            return;

        for (int i = 0; i < boxes.Count; i++)
        {
            boxes[i].transform.localPosition = boxSlots[i].localPosition;
            boxes[i].transform.localRotation = boxSlots[i].localRotation;
        }
    }

    void PlaceShapes()
    {
        if (shapeArea == null)
            return;

        Vector3 forward = shapeArea.forward;
        forward.y = 0f;
        Camera headset = Camera.main;
        if (headset != null)
        {
            forward = headset.transform.position - shapeArea.position;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 axis = shapeArea.right;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.001f)
            axis = Vector3.right;
        axis.Normalize();

        Vector3 center = shapeArea.position;
        Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up);
        float spacing = 0.16f;
        for (int i = 0; i < active.Count; i++)
        {
            float x = (i - (active.Count - 1) * 0.5f) * spacing;
            active[i].Place(center + axis * x, rotation);
        }
    }

    SortBox BoxUnder(Bounds bounds)
    {
        SortBox closest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < boxes.Count; i++)
        {
            Collider collider = boxes[i].collider;
            if (collider == null)
                continue;

            Bounds area = collider.bounds;
            area.Expand(0.08f);
            float distance = Vector3.Distance(area.center, bounds.center);
            if (!area.Intersects(bounds) && distance > 0.25f)
                continue;

            distance = distance * distance;
            if (distance < best)
            {
                best = distance;
                closest = boxes[i];
            }
        }

        return closest;
    }

    static bool CorrectDrop(SortColor color, bool leftHand, bool bothHands, SortBox box)
    {
        if (box == null)
            return false;

        if (color == SortColor.Yellow)
            return bothHands && box.color == SortColor.Yellow;

        if (color == SortColor.Red)
            return leftHand && box.color == SortColor.Red;

        return !leftHand && box.color == SortColor.Blue;
    }

    static string FailReason(SortColor color, bool leftHand, bool bothHands, SortBox box)
    {
        if (color == SortColor.Red && !leftHand)
            return "Use your LEFT hand for red.";
        if (color == SortColor.Blue && leftHand)
            return "Use your RIGHT hand for blue.";
        if (color == SortColor.Yellow && !bothHands)
            return "Yellow needs both hands.";
        if (box != null && box.color != color)
            return "Wrong box. Try again.";
        return "Wrong box. Try again.";
    }

    void HideHud()
    {
        SetNamedActive("CountdownScreen", false);
        SetNamedActive("Select hand", false);
        SetNamedActive("SelectHand", false);
        SetNamedActive("Canvas Guide", false);

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == "Hand Canvas")
                transforms[i].gameObject.SetActive(false);
        }

        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.HideGameplay();
    }

    void SetObjectiveText(string body)
    {
        if (objectiveScreen == null)
            return;

        TMP_Text label = FindChildText(objectiveScreen.transform, "Text_Objective");
        if (label != null)
            label.text = body;
    }

    void WireStart()
    {
        if (objectiveScreen == null)
            return;

        Transform buttonTransform = objectiveScreen.transform.Find("Objective_Panel/Btn_Start");
        if (buttonTransform == null)
            return;

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
            return;

        int persistentCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(StartLesson);
    }

    void BindSqueeze()
    {
        UnbindSqueeze();
        leftGrip = MakeSqueeze("{LeftHand}/grip");
        rightGrip = MakeSqueeze("{RightHand}/grip");
    }

    void UnbindSqueeze()
    {
        DisposeSqueeze(ref leftGrip);
        DisposeSqueeze(ref rightGrip);
    }

    static InputAction MakeSqueeze(string path)
    {
        InputAction action = new InputAction(type: InputActionType.Value, binding: "<XRController>" + path);
        action.Enable();
        return action;
    }

    static void DisposeSqueeze(ref InputAction action)
    {
        if (action == null)
            return;

        action.Disable();
        action.Dispose();
        action = null;
    }

    static float ReadSqueeze(InputAction action)
    {
        return action != null ? action.ReadValue<float>() : 0f;
    }

    void ShowWord(string objectName)
    {
        HideWord();
        SetNamedActive(objectName, true);
        wordHideAt = Time.time + 1f;
    }

    void HideWordIfDue()
    {
        if (wordHideAt > 0f && Time.time >= wordHideAt)
            HideWord();
    }

    void HideWord()
    {
        wordHideAt = 0f;
        SetNamedActive("Great", false);
        SetNamedActive("Good", false);
        SetNamedActive("Fair", false);
        SetNamedActive("Wrong", false);
        SetNamedActive("Oops", false);
    }

    static Transform FindHand(bool left)
    {
        Transform controller = PlayerHands.WritingController(!left);
        if (controller == null)
            controller = FindNamedTransform(left ? "Left Controller" : "Right Controller");

        Transform model = FindHandModel(controller);
        if (model != null)
            return model;

        Transform named = FindNamedTransform(left ? "Left Hand Model" : "Right Hand Model");
        if (named != null)
            return named;

        return controller;
    }

    static Transform FindHandModel(Transform controller)
    {
        if (controller == null)
            return null;

        for (int i = 0; i < controller.childCount; i++)
        {
            Transform child = controller.GetChild(i);
            if (child.name.Contains("Hand Model"))
                return child;
        }

        return null;
    }

    static GameObject FindNamed(string objectName)
    {
        Transform found = FindNamedTransform(objectName);
        return found != null ? found.gameObject : null;
    }

    static Transform FindNamedTransform(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    static void SetNamedActive(string objectName, bool active)
    {
        GameObject go = FindNamed(objectName);
        if (go != null)
            go.SetActive(active);
    }

    static TMP_Text FindChildText(Transform root, string objectName)
    {
        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }
}
