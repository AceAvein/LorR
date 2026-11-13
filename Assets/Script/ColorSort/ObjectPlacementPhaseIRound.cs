using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Object Placement Phase I: 3 sets × 10 stylized groceries from conveyor.
/// Fruits (yellow/Grip) → Yellow Box; Vegetables (red/Trigger) → Red Box.
/// Replaces ColorSortRound when an ObjectPlacementStation is present.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementPhaseIRound : MonoBehaviour
{
    const float SpawnInterval = 1.35f;
    const float BeltItemSpeed = 0.38f;

    ObjectPlacementStation station;
    ObjectPlacementConveyorController conveyor;
    Transform yellowBox;
    Transform redBox;
    Collider yellowCol;
    Collider redCol;

    GameObject objectiveScreen;
    GameObject countdownScreen;
    TMP_Text countdownText;
    GameObject resultsScreen;

    readonly List<string> fruitPool = new List<string>();
    readonly List<string> vegPool = new List<string>();
    readonly List<ObjectPlacementGroceryItem> live = new List<ObjectPlacementGroceryItem>();
    readonly Queue<SpawnSpec> spawnQueue = new Queue<SpawnSpec>();

    struct SpawnSpec
    {
        public string id;
        public ObjectPlacementGroceryKind kind;
    }

    bool countingDown;
    bool running;
    bool setOpen;
    int setIndex;
    int setCorrect;
    int setWrong;
    int placedTotal;
    int placedCorrect;
    float spawnTimer;
    int spawnedThisSet;
    float wrongControlFlashUntil;

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

        ObjectPlacementStation station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        if (station == null)
            return;

        if (Object.FindAnyObjectByType<ObjectPlacementPhaseIRound>() != null)
            return;

        ColorSortRound legacy = Object.FindAnyObjectByType<ColorSortRound>();
        if (legacy != null)
            Object.Destroy(legacy.gameObject);

        GameObject host = new GameObject("ObjectPlacementPhaseIRound");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ObjectPlacementPhaseIRound>();
    }

    void Awake()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
        {
            enabled = false;
            return;
        }

        station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        if (station == null)
        {
            enabled = false;
            return;
        }

        conveyor = station.GetComponent<ObjectPlacementConveyorController>();
        CacheBoxes();
        StripLegacyProps();
        EnsureFlapHinges();

        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        if (resultsScreen != null)
            resultsScreen.SetActive(false);

        EnsureCountdown();
        WireStart();
        ShowObjective();
        station.SetScreenText("PHASE I");
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen || !running || !setOpen)
            return;

        TickSpawn();
    }

    void CacheBoxes()
    {
        yellowBox = station.yellowBox;
        redBox = station.redBox;
        if (yellowBox == null)
        {
            GameObject go = FindNamed("Yellow Box");
            if (go != null)
                yellowBox = go.transform;
        }

        if (redBox == null)
        {
            GameObject go = FindNamed("Red Box");
            if (go != null)
                redBox = go.transform;
        }

        // Prefer station boxes when duplicates exist.
        if (station.boxesRoot != null)
        {
            Transform y = station.boxesRoot.Find("Yellow Box");
            Transform r = station.boxesRoot.Find("Red Box");
            if (y != null)
                yellowBox = y;
            if (r != null)
                redBox = r;
        }

        yellowCol = yellowBox != null ? yellowBox.GetComponent<Collider>() : null;
        redCol = redBox != null ? redBox.GetComponent<Collider>() : null;
        if (yellowCol == null && yellowBox != null)
            yellowCol = yellowBox.gameObject.AddComponent<BoxCollider>();
        if (redCol == null && redBox != null)
            redCol = redBox.gameObject.AddComponent<BoxCollider>();
        if (yellowCol != null)
            yellowCol.isTrigger = true;
        if (redCol != null)
            redCol.isTrigger = true;

        if (station.boxesRoot != null)
            station.boxesRoot.gameObject.SetActive(true);
    }

    void StripLegacyProps()
    {
        GameObject shapes = FindNamed("Shapes");
        if (shapes != null && (station == null || !shapes.transform.IsChildOf(station.transform)))
            shapes.SetActive(false);

        // Disable root-level legacy Boxes (Father=0), keep station Boxes.
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t.name != "Boxes")
                continue;
            if (station != null && t.IsChildOf(station.transform))
                continue;
            if (t.parent == null)
                t.gameObject.SetActive(false);
        }
    }

    void EnsureFlapHinges()
    {
        if (station.conveyor != null)
            HingeFlap(station.conveyor.Find("ExitTunnel"), openTowardBelt: true);
        if (station.conveyorLeft != null)
            HingeFlap(station.conveyorLeft.Find("ExitTunnel"), openTowardBelt: false);
    }

    static void HingeFlap(Transform tunnel, bool openTowardBelt)
    {
        if (tunnel == null)
            return;

        Transform flap = tunnel.Find("RubberFlap");
        if (flap == null)
            return;
        if (flap.parent != null && flap.parent.name == "RubberFlapHinge")
            return;

        GameObject hinge = new GameObject("RubberFlapHinge");
        hinge.transform.SetParent(tunnel, false);
        // Pivot at top edge of flap; opposite belts swing opposite ways.
        Vector3 flapPos = flap.localPosition;
        Vector3 flapScale = flap.localScale;
        hinge.transform.localPosition = new Vector3(flapPos.x, flapPos.y + Mathf.Abs(flapScale.y) * 0.5f, flapPos.z);
        hinge.transform.localRotation = Quaternion.Euler(0f, 0f, openTowardBelt ? -8f : 8f);

        flap.SetParent(hinge.transform, true);
        flap.localPosition = new Vector3(0f, -Mathf.Abs(flapScale.y) * 0.5f, 0f);
    }

    void WireStart()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name != "Btn_Start")
                continue;
            buttons[i].onClick.RemoveListener(OnStartPressed);
            buttons[i].onClick.AddListener(OnStartPressed);
        }
    }

    void ShowObjective()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(true);
    }

    void OnStartPressed()
    {
        if (countingDown || running)
            return;

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        StartCoroutine(CountdownThenStart());
    }

    IEnumerator CountdownThenStart()
    {
        countingDown = true;
        EnsureCountdown();
        GamePauseMenu.PlaceInFront(countdownScreen);
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
        placedTotal = 0;
        placedCorrect = 0;
        running = true;
        RefillPools();
        if (conveyor != null)
            conveyor.StartConveyors();
        station.SetScreenText("SET 1 / 3");
        BeginSet();
    }

    void RefillPools()
    {
        fruitPool.Clear();
        vegPool.Clear();
        fruitPool.AddRange(ObjectPlacementGroceryCatalog.FruitIds);
        vegPool.AddRange(ObjectPlacementGroceryCatalog.VegetableIds);
        Shuffle(fruitPool);
        Shuffle(vegPool);
    }

    void BeginSet()
    {
        ClearLive();
        spawnQueue.Clear();
        setCorrect = 0;
        setWrong = 0;
        spawnedThisSet = 0;
        spawnTimer = 0.25f;
        setOpen = true;

        // 5 fruit + 5 veg per set, shuffled order.
        List<SpawnSpec> batch = new List<SpawnSpec>(ObjectPlacementGroceryCatalog.PhaseIItemsPerSet);
        for (int i = 0; i < 5; i++)
        {
            batch.Add(new SpawnSpec { id = TakeNext(fruitPool, ObjectPlacementGroceryCatalog.FruitIds), kind = ObjectPlacementGroceryKind.Fruit });
            batch.Add(new SpawnSpec { id = TakeNext(vegPool, ObjectPlacementGroceryCatalog.VegetableIds), kind = ObjectPlacementGroceryKind.Vegetable });
        }

        Shuffle(batch);
        for (int i = 0; i < batch.Count; i++)
            spawnQueue.Enqueue(batch[i]);

        station.SetScreenText("SET " + (setIndex + 1) + " / 3");
    }

    static string TakeNext(List<string> pool, string[] source)
    {
        if (pool.Count == 0)
        {
            pool.AddRange(source);
            Shuffle(pool);
        }

        string id = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        return id;
    }

    void TickSpawn()
    {
        if (spawnQueue.Count == 0)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = SpawnInterval;
        SpawnSpec spec = spawnQueue.Dequeue();
        SpawnItem(spec);
        spawnedThisSet++;
    }

    void SpawnItem(SpawnSpec spec)
    {
        GameObject go = ObjectPlacementGroceryFactory.Create(spec.id, spec.kind);
        ObjectPlacementGroceryItem item = go.GetComponent<ObjectPlacementGroceryItem>();
        item.Bind(this);

        Transform spawn = station.grocerySpawnPoint;
        Transform belt = station.beltSurface != null ? station.beltSurface : station.conveyor;
        // Alternate belts lightly so both conveyors get use.
        float dir = 1f;
        if (spawnedThisSet % 3 == 2 && station.grocerySpawnPointLeft != null)
        {
            spawn = station.grocerySpawnPointLeft;
            belt = station.beltSurfaceLeft != null ? station.beltSurfaceLeft : station.conveyorLeft;
            dir = -1f; // ConveyorLeft scrolls reverse
        }

        item.PlaceOnBelt(spawn, belt, dir, BeltItemSpeed);
        live.Add(item);
    }

    public void OnWrongControl(ObjectPlacementGroceryItem item)
    {
        wrongControlFlashUntil = Time.time + 0.8f;
        station.SetScreenText(item.Kind == ObjectPlacementGroceryKind.Fruit ? "USE GRIP" : "USE TRIGGER");
    }

    public bool TryScoreItem(ObjectPlacementGroceryItem item, out bool correct)
    {
        correct = false;
        if (item == null)
            return false;

        Bounds itemBounds = GetWorldBounds(item.transform);
        bool inYellow = yellowCol != null && yellowCol.bounds.Intersects(itemBounds);
        bool inRed = redCol != null && redCol.bounds.Intersects(itemBounds);
        if (!inYellow && !inRed)
            return false;

        if (item.Kind == ObjectPlacementGroceryKind.Fruit)
            correct = inYellow && !inRed;
        else
            correct = inRed && !inYellow;

        return true;
    }

    public void OnItemResolved(ObjectPlacementGroceryItem item, bool correct)
    {
        if (!setOpen)
            return;

        placedTotal++;
        if (correct)
        {
            setCorrect++;
            placedCorrect++;
            BallDropSounds.PlayCatch();
        }
        else
        {
            setWrong++;
            BallDropSounds.PlayMiss();
        }

        if (Time.time > wrongControlFlashUntil)
            station.SetScreenText("SET " + (setIndex + 1) + "  " + setCorrect + "/10");

        int remainingLive = 0;
        for (int i = 0; i < live.Count; i++)
        {
            if (live[i] != null && !live[i].Resolved)
                remainingLive++;
        }

        if (spawnQueue.Count == 0 && remainingLive == 0)
            FinishSet();
    }

    void FinishSet()
    {
        if (!setOpen)
            return;

        setOpen = false;
        ClearLive();

        if (setCorrect >= 8)
            ShowWord("Great");
        else if (setCorrect >= 5)
            ShowWord("Good");
        else
            ShowWord("Fair");

        setIndex++;
        if (setIndex >= ObjectPlacementGroceryCatalog.PhaseISetCount)
        {
            EndRound();
            return;
        }

        BeginSet();
    }

    void EndRound()
    {
        running = false;
        setOpen = false;
        ClearLive();
        if (conveyor != null)
            conveyor.StopConveyors();
        station.SetScreenText("DONE");
        ShowResults();
    }

    void ShowResults()
    {
        if (resultsScreen == null)
            resultsScreen = FindNamed("ResultsScreen");
        if (resultsScreen == null)
            return;

        resultsScreen.SetActive(true);
        ColorSortResults results = resultsScreen.GetComponent<ColorSortResults>();
        if (results == null)
            results = resultsScreen.AddComponent<ColorSortResults>();

        float accuracy = placedTotal > 0 ? (100f * placedCorrect / placedTotal) : 0f;
        ColorSortReport report = new ColorSortReport();
        report.setsCleared = ObjectPlacementGroceryCatalog.PhaseISetCount;
        report.setTotal = ObjectPlacementGroceryCatalog.PhaseISetCount;
        report.nonDominantHand = "Phase I";
        report.accuracy = accuracy;
        report.bothCorrect = placedCorrect;
        report.bothShown = placedTotal;
        report.easyCleared = placedCorrect;
        report.totalScore = placedCorrect * 10f;
        results.Show(report);
    }

    void ClearLive()
    {
        for (int i = 0; i < live.Count; i++)
        {
            if (live[i] != null)
            {
                live[i].ForceResolve();
                Destroy(live[i].gameObject);
            }
        }

        live.Clear();
    }

    void ShowWord(string word)
    {
        station.SetScreenText(word.ToUpperInvariant());
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
        countdownText = label.AddComponent<TextMeshProUGUI>();
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 72;
        countdownScreen = screen;
        countdownScreen.SetActive(false);
    }

    static Bounds GetWorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.08f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    static GameObject FindNamed(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }
}
