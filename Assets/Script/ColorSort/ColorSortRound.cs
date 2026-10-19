using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ColorSortRound : MonoBehaviour
{
    const int SetTotal = 30;
    const float RoundSeconds = 180f;
    const float SetBonus = 2f;

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

    readonly List<ColorSortPiece> shapes = new List<ColorSortPiece>();
    readonly List<SortBox> boxes = new List<SortBox>();
    readonly List<BoxSlot> boxSlots = new List<BoxSlot>();
    readonly List<ColorSortPiece> active = new List<ColorSortPiece>();
    readonly List<TMP_Text> levelValues = new List<TMP_Text>();
    readonly List<TMP_Text> timerValues = new List<TMP_Text>();
    readonly List<TMP_Text> setValues = new List<TMP_Text>();

    GameObject objectiveScreen;
    GameObject boxesRoot;
    GameObject countdownScreen;
    TMP_Text countdownText;
    GameObject leftWristHud;
    GameObject rightWristHud;
    GameObject selectHand;
    GameObject resultsScreen;
    Material redMaterial;
    Material blueMaterial;
    Material yellowMaterial;
    bool countingDown;
    bool choosingHand;
    bool running;
    bool setOpen;
    bool pendingAdvance;
    bool nonDominantIsLeft = true;
    int setCorrect;
    int setWrong;
    int[] shownByColor = new int[3];
    int[] correctByColor = new int[3];
    int placedTotal;
    int placedCorrect;
    int greatSets;
    int easyGreat;
    int moderateGreat;
    int hardGreat;
    int timeAdded;
    float elapsed;
    float score;
    int setIndex;
    int announcedTier = -1;
    float timeRemaining;
    float wordHideAt;
    Transform shapeArea;

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
        if (!scene.IsValid() || scene.name != "ColorSort")
            return;

        if (FindAnyObjectByType<ColorSortRound>() != null)
            return;

        GameObject host = new GameObject("ColorSortRound");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ColorSortRound>();
    }

    void Awake()
    {
        FindShapes();
        FindShapeArea();
        FindBoxes();
        HideShapes();
        if (boxesRoot != null)
            boxesRoot.SetActive(false);

        objectiveScreen = FindNamed("ObjectiveScreen");
        selectHand = FindNamed("Select hand");
        resultsScreen = FindNamed("ResultsScreen");
        if (selectHand != null)
            selectHand.SetActive(false);
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
        EnsureCountdown();
        WireStart();
        HideWord();
        ShowObjective();
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

        if (!running)
            return;

        elapsed += Time.deltaTime;
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            if (setOpen)
                FinishSet();
            else
                EndRound();
            return;
        }

        if (pendingAdvance)
        {
            pendingAdvance = false;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].resolved)
                    active[i].HideResolved();
            }

            if (setOpen)
                FinishSet();
        }

        RefreshHud();
    }

    void ShowObjective()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(true);
        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        HideGameplayHud();
        HideWristHud();
        if (selectHand != null)
            selectHand.SetActive(false);
    }

    public void BeginFromStart()
    {
        if (running || countingDown || choosingHand)
            return;

        ScoringChecker.Ensure().TryContinue(BeginFromStartAfterCheck);
    }

    void BeginFromStartAfterCheck()
    {
        if (running || countingDown || choosingHand)
            return;

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
        if (selectHand != null)
        {
            choosingHand = true;
            PlaceInFront(selectHand, 0.75f);
            selectHand.SetActive(true);
            return;
        }

        StartCoroutine(CountdownThenPlay());
    }

    public void ChooseLeftHand()
    {
        BeginCountdown(true);
    }

    public void ChooseRightHand()
    {
        BeginCountdown(false);
    }

    void BeginCountdown(bool leftIsNonDominant)
    {
        if (running || countingDown)
            return;

        nonDominantIsLeft = leftIsNonDominant;
        choosingHand = false;
        if (selectHand != null)
            selectHand.SetActive(false);
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        StartCoroutine(CountdownThenPlay());
    }

    IEnumerator CountdownThenPlay()
    {
        countingDown = true;
        EnsureCountdown();
        PlaceInFront(countdownScreen, 0.75f);
        if (countdownScreen != null)
            countdownScreen.SetActive(true);

        int[] steps = { 3, 2, 1 };
        for (int i = 0; i < steps.Length; i++)
        {
            if (countdownText != null)
                countdownText.text = steps[i].ToString();

            LevelVoices.PlayCount(steps[i]);
            yield return new WaitForSeconds(1f);
        }

        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        countingDown = false;
        StartRound();
    }

    void StartRound()
    {
        setIndex = 0;
        announcedTier = -1;
        timeRemaining = RoundSeconds;
        elapsed = 0f;
        score = 0f;
        timeAdded = 0;
        greatSets = 0;
        easyGreat = 0;
        moderateGreat = 0;
        hardGreat = 0;
        placedTotal = 0;
        placedCorrect = 0;
        for (int i = 0; i < 3; i++)
        {
            shownByColor[i] = 0;
            correctByColor[i] = 0;
        }
        running = true;
        LevelVoices.Reset();
        if (boxesRoot != null)
            boxesRoot.SetActive(true);

        ShowGameplayHud();
        ShowWristHud();
        BeginSet();
        RefreshHud();
    }

    void BeginSet()
    {
        if (!running)
            return;

        if (setIndex >= SetTotal)
        {
            EndRound();
            return;
        }

        int tier = TierFor(setIndex);
        if (tier != announcedTier)
        {
            announcedTier = tier;
            LevelVoices.PlayTier(tier);
            BallDropSky.ApplyCurrent(tier);
        }

        setCorrect = 0;
        setWrong = 0;
        setOpen = true;
        ShuffleBoxes();
        active.Clear();
        for (int i = 0; i < shapes.Count; i++)
            shapes[i].Remove();

        int count = CountFor(setIndex);
        List<ColorSortPiece> pool = new List<ColorSortPiece>(shapes);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int pick = Random.Range(0, pool.Count);
            ColorSortPiece piece = pool[pick];
            pool.RemoveAt(pick);
            SortColor color = (SortColor)Random.Range(0, 3);
            piece.PrepareColor((int)color, MaterialFor(color));
            shownByColor[(int)color]++;
            active.Add(piece);
        }

        PlaceShapes();
        RefreshHud();
    }

    public void OnPieceResolved(int colorIndex, bool correct)
    {
        if (!setOpen)
            return;

        placedTotal++;
        if (!correct)
            setWrong++;
        else
        {
            setCorrect++;
            placedCorrect++;
            if (colorIndex >= 0 && colorIndex < correctByColor.Length)
                correctByColor[colorIndex]++;
        }

        int remaining = 0;
        for (int i = 0; i < active.Count; i++)
        {
            if (!active[i].resolved)
                remaining++;
        }

        if (remaining == 0)
            pendingAdvance = true;
    }

    void FinishSet()
    {
        if (!setOpen)
            return;

        setOpen = false;
        for (int i = 0; i < active.Count; i++)
        {
            if (!active[i].resolved)
                active[i].Remove();
        }

        bool perfect = active.Count > 0 && setCorrect == active.Count && setWrong == 0;
        int tier = TierFor(setIndex);
        float points = tier <= 0 ? 10f : tier == 1 ? 15f : 20f;
        if (perfect)
        {
            timeRemaining += SetBonus;
            timeAdded += (int)SetBonus;
            greatSets++;
            if (tier <= 0)
                easyGreat++;
            else if (tier == 1)
                moderateGreat++;
            else
                hardGreat++;
            score += points;
            ShowWord("Great");
            BallDropSounds.PlayCatch();
        }
        else if (setCorrect > 0)
        {
            score += points * 0.5f;
            ShowWord("Good");
            BallDropSounds.PlayCatch();
        }
        else
        {
            ShowWord("Fair");
            BallDropSounds.PlayMiss();
        }

        setIndex++;
        if (setIndex >= SetTotal)
            score += Mathf.Max(0f, timeRemaining) * 2f;
        if (setIndex >= SetTotal || timeRemaining <= 0f)
        {
            EndRound();
            return;
        }

        BeginSet();
    }

    void EndRound()
    {
        if (!running)
            return;

        running = false;
        setOpen = false;
        pendingAdvance = false;
        timeRemaining = Mathf.Max(0f, timeRemaining);
        HideShapes();
        if (boxesRoot != null)
            boxesRoot.SetActive(false);

        HideGameplayHud();
        HideWristHud();
        RefreshHud();
        ShowResults();
    }

    void ShowResults()
    {
        if (resultsScreen == null)
            resultsScreen = FindNamed("ResultsScreen");
        if (resultsScreen == null)
            return;

        resultsScreen.SetActive(true);
        PlaceInFront(resultsScreen, 0.7f);
        ColorSortResults results = resultsScreen.GetComponent<ColorSortResults>();
        if (results == null)
            results = resultsScreen.AddComponent<ColorSortResults>();
        results.Show(BuildReport());
    }

    ColorSortReport BuildReport()
    {
        int weak = nonDominantIsLeft ? 0 : 1;
        int strong = nonDominantIsLeft ? 1 : 0;
        ColorSortReport report = new ColorSortReport();
        report.setsCleared = greatSets;
        report.setTotal = SetTotal;
        report.nonDominantHand = nonDominantIsLeft ? "Left" : "Right";
        report.accuracy = placedTotal == 0 ? 0f : placedCorrect * 100f / placedTotal;
        report.nonDominantCorrect = correctByColor[weak];
        report.nonDominantShown = shownByColor[weak];
        report.dominantCorrect = correctByColor[strong];
        report.dominantShown = shownByColor[strong];
        report.bothCorrect = correctByColor[2];
        report.bothShown = shownByColor[2];
        report.timeAdded = timeAdded;
        report.timeUsed = Mathf.Max(0, Mathf.RoundToInt(elapsed));
        report.timeLeft = Mathf.Max(0, Mathf.RoundToInt(timeRemaining));
        report.easyCleared = easyGreat;
        report.moderateCleared = moderateGreat;
        report.hardCleared = hardGreat;
        report.totalScore = score;
        return report;
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
    }

    void ShuffleBoxes()
    {
        if (boxes.Count == 0 || boxSlots.Count != boxes.Count)
            return;

        int[] order = new int[boxes.Count];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;

        for (int i = 0; i < order.Length; i++)
        {
            int swap = Random.Range(i, order.Length);
            int held = order[i];
            order[i] = order[swap];
            order[swap] = held;
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            BoxSlot slot = boxSlots[order[i]];
            boxes[i].transform.localPosition = slot.localPosition;
            boxes[i].transform.localRotation = slot.localRotation;
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
        float span = Mathf.Abs(shapeArea.lossyScale.x);
        float depth = Mathf.Abs(shapeArea.lossyScale.z);
        if (depth > span)
        {
            axis = shapeArea.forward;
            span = depth;
        }

        axis.y = 0f;
        if (axis.sqrMagnitude < 0.001f)
            axis = Vector3.right;
        axis.Normalize();

        Vector3 center = shapeArea.position;
        Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up);
        float widest = 0.14f;
        for (int i = 0; i < active.Count; i++)
        {
            active[i].Place(center, rotation);
            widest = Mathf.Max(widest, active[i].HorizontalSize());
        }

        float spacing = widest + 0.08f;
        if (active.Count > 1)
        {
            float spread = span * 0.9f / (active.Count - 1);
            spacing = Mathf.Max(spacing, spread);
        }

        for (int i = 0; i < active.Count; i++)
        {
            float x = (i - (active.Count - 1) * 0.5f) * spacing;
            active[i].Place(center + axis * x, rotation);
        }
    }

    public bool ScoreDrop(Bounds bounds, int colorIndex, bool leftHand, bool bothHands, out bool correct)
    {
        correct = false;
        SortBox box = BoxUnder(bounds);
        if (box == null)
            return false;

        correct = CorrectDrop((SortColor)colorIndex, leftHand, bothHands, box);
        return true;
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

    bool CorrectDrop(SortColor color, bool leftHand, bool bothHands, SortBox box)
    {
        if (box == null)
            return false;

        if (color == SortColor.Yellow)
            return bothHands && box.color == SortColor.Yellow;

        if (color == SortColor.Red)
            return leftHand && box.color == SortColor.Red;

        return !leftHand && box.color == SortColor.Blue;
    }

    void EnsureCountdown()
    {
        if (countdownScreen != null && countdownText != null)
            return;

        countdownScreen = FindNamed("CountdownScreen");
        if (countdownScreen != null)
            countdownText = countdownScreen.GetComponentInChildren<TMP_Text>(true);

        if (countdownScreen != null && countdownText != null)
            return;

        GameObject screen = new GameObject("CountdownScreen", typeof(RectTransform));
        Canvas canvas = screen.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = screen.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100f, 100f);
        rect.localScale = Vector3.one * 0.009f;

        GameObject label = new GameObject("CountdownText", typeof(RectTransform));
        label.transform.SetParent(screen.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(50f, 50f);
        labelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI number = label.AddComponent<TextMeshProUGUI>();
        number.font = TMP_Settings.defaultFontAsset;
        if (number.font == null)
            number.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        number.fontSize = 36f;
        number.alignment = TextAlignmentOptions.Center;
        number.color = Color.white;
        number.raycastTarget = false;
        number.text = "3";

        countdownScreen = screen;
        countdownText = number;
        screen.SetActive(false);
    }

    public void ShowWord(string objectName)
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

    void ShowGameplayHud()
    {
        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.ShowGameplay();
    }

    void HideGameplayHud()
    {
        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.HideGameplay();
    }

    void ShowWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(nonDominantIsLeft);
        if (rightWristHud != null)
            rightWristHud.SetActive(!nonDominantIsLeft);
    }

    void HideWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(false);
        if (rightWristHud != null)
            rightWristHud.SetActive(false);
    }

    void ResolveWristHuds()
    {
        if (leftWristHud != null && rightWristHud != null)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != "Hand Canvas")
                continue;

            if (IsUnder(transforms[i], "Left Controller") || IsUnder(transforms[i], "Left Hand Model"))
                leftWristHud = transforms[i].gameObject;
            else if (IsUnder(transforms[i], "Right Controller") || IsUnder(transforms[i], "Right Hand Model"))
                rightWristHud = transforms[i].gameObject;
        }
    }

    void RefreshHud()
    {
        if (levelValues.Count == 0)
            FindTexts("Level Value", levelValues);
        if (timerValues.Count == 0)
            FindTexts("Timer Value", timerValues);
        if (setValues.Count == 0)
            FindTexts("Sets Value", setValues);

        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
        int shownSet = setIndex + 1;
        if (shownSet > SetTotal)
            shownSet = SetTotal;

        SetTexts(levelValues, TierName(TierFor(setIndex >= SetTotal ? SetTotal - 1 : setIndex)));
        SetTexts(timerValues, seconds.ToString());
        SetTexts(setValues, shownSet + "/" + SetTotal);
    }

    void WireStart()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == "Btn_Start")
                buttons[i].onClick.AddListener(BeginFromStart);
            else if (buttons[i].gameObject.name == "Left Hand")
                buttons[i].onClick.AddListener(ChooseLeftHand);
            else if (buttons[i].gameObject.name == "Right Hand")
                buttons[i].onClick.AddListener(ChooseRightHand);
        }
    }

    static int TierFor(int index)
    {
        int tier = index / 10;
        if (tier > 2)
            tier = 2;
        if (tier < 0)
            tier = 0;
        return tier;
    }

    static int CountFor(int index)
    {
        int tier = TierFor(index);
        if (tier <= 0)
            return 2;
        if (tier == 1)
            return 3;
        return 4;
    }

    static string TierName(int tier)
    {
        if (tier <= 0)
            return "Easy";
        if (tier == 1)
            return "Moderate";
        return "Hard";
    }

    static void FindTexts(string objectName, List<TMP_Text> list)
    {
        list.Clear();
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                list.Add(labels[i]);
        }
    }

    static void SetTexts(List<TMP_Text> labels, string value)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
                labels[i].text = value;
        }
    }

    static void SetNamedActive(string objectName, bool active)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                transforms[i].gameObject.SetActive(active);
        }
    }

    static GameObject FindNamed(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }

    static bool IsUnder(Transform child, string ancestorName)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name == ancestorName)
                return true;

            current = current.parent;
        }

        return false;
    }

    static void PlaceInFront(GameObject panel, float distance)
    {
        Camera headset = Camera.main;
        if (headset == null || panel == null)
            return;

        Vector3 forward = headset.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = headset.transform.position + forward * distance;
        position.y = headset.transform.position.y;
        panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }
}

public class ColorSortPiece : MonoBehaviour
{
    enum SortColor
    {
        Red,
        Blue,
        Yellow
    }

    ColorSortRound round;
    XRGrabInteractable grab;
    Renderer[] renderers;
    Collider body;
    SortColor color;
    Vector3 homePosition;
    Quaternion homeRotation;
    bool forcing;
    public bool resolved { get; private set; }

    public void Bind(ColorSortRound owner)
    {
        round = owner;
        renderers = GetComponentsInChildren<Renderer>(true);
        EnsureBody();
    }

    public void PrepareColor(int colorIndex, Material material)
    {
        color = (SortColor)colorIndex;
        resolved = false;
        gameObject.SetActive(true);

        if (material != null)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = material;
        }

        if (grab != null)
        {
            grab.enabled = true;
            grab.selectMode = color == SortColor.Yellow
                ? InteractableSelectMode.Multiple
                : InteractableSelectMode.Single;
            grab.trackPosition = color != SortColor.Yellow;
            grab.trackRotation = color != SortColor.Yellow;
        }
    }

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        Renderer mesh = FirstRenderer();
        if (mesh != null)
        {
            Vector3 shift = position - mesh.bounds.center;
            if (shift.sqrMagnitude > 0.000001f)
                transform.position += shift;
        }

        homePosition = transform.position;
        homeRotation = transform.rotation;
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.position = transform.position;
            rigidbody.rotation = transform.rotation;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public float HorizontalSize()
    {
        Renderer mesh = FirstRenderer();
        if (mesh == null)
            return 0.14f;

        Vector3 size = mesh.bounds.size;
        return Mathf.Max(size.x, size.z);
    }

    Renderer FirstRenderer()
    {
        if (renderers == null)
            return null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                return renderers[i];
        }

        return null;
    }

    public void CancelHold()
    {
        if (resolved)
            return;

        ForceRelease();
        ReturnHome();
    }

    public void Remove()
    {
        resolved = true;
        ForceRelease();
        gameObject.SetActive(false);
    }

    public void HideResolved()
    {
        if (!resolved)
            return;

        ForceRelease();
        gameObject.SetActive(false);
    }

    void EnsureBody()
    {
        body = GetComponent<Collider>();
        if (body == null)
        {
            BoxCollider created = gameObject.AddComponent<BoxCollider>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (meshRenderer != null && meshRenderer.transform == transform)
            {
                created.center = meshRenderer.localBounds.center;
                created.size = meshRenderer.localBounds.size;
            }
            else if (meshRenderer != null)
            {
                created.center = transform.InverseTransformPoint(meshRenderer.bounds.center);
                Vector3 size = meshRenderer.bounds.size;
                Vector3 lossy = transform.lossyScale;
                created.size = new Vector3(
                    size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                    size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                    size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
            }

            body = created;
        }

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.useDynamicAttach = true;
        grab.throwOnDetach = false;
        grab.attachEaseInTime = 0f;
        grab.colliders.Clear();
        grab.colliders.Add(body);
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void LateUpdate()
    {
        if (resolved)
        {
            HideResolved();
            return;
        }

        if (grab == null || color != SortColor.Yellow)
            return;

        if (GamePauseMenu.IsFrozen)
        {
            ForceRelease();
            ReturnHome();
            return;
        }

        if (grab.interactorsSelecting.Count == 1)
            ReturnHome();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (resolved)
            return;

        if (GamePauseMenu.IsFrozen)
        {
            ForceRelease();
            return;
        }

        if (color != SortColor.Yellow)
            return;

        bool heldByBoth = grab.interactorsSelecting.Count >= 2;
        grab.trackPosition = heldByBoth;
        grab.trackRotation = heldByBoth;
        if (!heldByBoth)
            ReturnHome();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (forcing || resolved)
            return;

        if (GamePauseMenu.IsFrozen)
        {
            ForceRelease();
            ReturnHome();
            return;
        }

        if (color == SortColor.Yellow && grab.interactorsSelecting.Count > 0)
        {
            bool scored = round != null && round.ScoreDrop(body.bounds, (int)color, false, true, out _);
            if (scored)
                Finish(args);
            else
            {
                ForceRelease();
                ReturnHome();
            }

            return;
        }

        if (grab.isSelected)
            return;

        Finish(args);
    }

    void Finish(SelectExitEventArgs args)
    {
        bool leftHand = IsLeft(args.interactorObject);
        bool bothHands = color == SortColor.Yellow;
        bool correct;
        if (round == null || !round.ScoreDrop(body.bounds, (int)color, leftHand, bothHands, out correct))
        {
            ReturnHome();
            return;
        }

        resolved = true;

        if (correct)
            BallDropSounds.PlayCatch();
        else
            BallDropSounds.PlayMiss();

        round.OnPieceResolved((int)color, correct);
        ForceRelease();
    }

    void ReturnHome()
    {
        transform.SetPositionAndRotation(homePosition, homeRotation);
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.position = homePosition;
            rigidbody.rotation = homeRotation;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        if (grab != null && color == SortColor.Yellow)
        {
            grab.trackPosition = false;
            grab.trackRotation = false;
        }
    }

    void ForceRelease()
    {
        if (grab == null || grab.interactionManager == null || forcing)
            return;

        forcing = true;
        List<IXRSelectInteractor> held = new List<IXRSelectInteractor>(grab.interactorsSelecting);
        for (int i = 0; i < held.Count; i++)
            grab.interactionManager.SelectExit(held[i], grab);
        forcing = false;
    }

    static bool IsLeft(IXRInteractor interactor)
    {
        if (interactor == null)
            return false;

        Transform current = interactor.transform;
        while (current != null)
        {
            if (current.name.Contains("Left"))
                return true;
            if (current.name.Contains("Right"))
                return false;

            current = current.parent;
        }

        return false;
    }

}
