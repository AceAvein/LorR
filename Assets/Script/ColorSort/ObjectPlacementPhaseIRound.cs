using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Object Placement Phase I (bible): 3×10 fruits/veg from left conveyor.
/// Yellow + Red boxes only. Fruit=Grip→Yellow, Veg=Trigger→Red.
/// Items stay in boxes across all 3 sets. After set 3, two-hand-carry boxes
/// onto the facing conveyor to finish Phase I (storyboard).
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementPhaseIRound : MonoBehaviour
{
    const float SpawnInterval = 1.6f;
    const float ScoreDelay = 0.18f;
    public const int MaxBeltQueue = 4;
    const float QueueSpacing = 0.17f;

    ObjectPlacementStation station;
    ObjectPlacementConveyorController conveyor;
    Transform yellowBox;
    Transform redBox;
    Transform blueBox;
    Collider yellowCol;
    Collider redCol;
    Transform beltStopper;
    ObjectPlacementSortBox yellowSortBox;
    ObjectPlacementSortBox redSortBox;
    Transform frontDropZone;
    readonly List<ObjectPlacementGroceryItem> beltQueue = new List<ObjectPlacementGroceryItem>();

    GameObject objectiveScreen;
    GameObject countdownScreen;
    TMP_Text countdownText;
    GameObject resultsScreen;

    readonly List<GameObject> fruitDeck = new List<GameObject>();
    readonly List<GameObject> vegDeck = new List<GameObject>();
    readonly List<ObjectPlacementGroceryItem> live = new List<ObjectPlacementGroceryItem>();
    readonly List<ObjectPlacementGroceryItem> placed = new List<ObjectPlacementGroceryItem>();
    readonly Queue<SpawnSpec> spawnQueue = new Queue<SpawnSpec>();
    readonly List<ScorePending> scorePending = new List<ScorePending>();

    struct SpawnSpec
    {
        public GameObject template;
        public ObjectPlacementGroceryKind kind;
    }

    struct ScorePending
    {
        public ObjectPlacementGroceryItem item;
        public float at;
    }

    bool countingDown;
    bool running;
    bool setOpen;
    bool boxDeliveryPhase;
    bool waitingForConveyorStart;
    int setIndex;
    int setCorrect;
    int setWrong;
    int placedTotal;
    int placedCorrect;
    int boxesDelivered;
    float spawnTimer;
    float wrongControlFlashUntil;

    public bool BeltIsRunning => conveyor != null && conveyor.IsRunning;
    public float BeltFlowSpeed => conveyor != null ? conveyor.BeltFlowSpeed : 0.38f;

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
        ApplyPhaseIBoxVisibility();
        CacheStopper();
        SetupSortBoxes();
        StripLegacyProps();
        ObjectPlacementItemLibrary.EnsureLoaded();

        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        if (resultsScreen != null)
            resultsScreen.SetActive(false);

        EnsureCountdown();
        WireStart();
        ObjectPlacementFarGrabBlocker.Install();
        ShowObjective();
        station.SetScreenText("PHASE I");
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        TickScorePending();

        if (!running || !setOpen)
            return;

        if (!BeltIsRunning)
        {
            waitingForConveyorStart = true;
            if (Time.time > wrongControlFlashUntil)
                station.SetScreenText("PRESS START");
            return;
        }

        if (waitingForConveyorStart)
        {
            waitingForConveyorStart = false;
            station.SetScreenText("SET " + (setIndex + 1));
        }

        // Keep item belt speed locked to FlowMarks (only while conveyor is on).
        float speed = BeltFlowSpeed;
        for (int i = 0; i < live.Count; i++)
        {
            if (live[i] != null && !live[i].Resolved)
                live[i].SetBeltSpeed(speed);
        }

        TickSpawn();
    }

    void CacheBoxes()
    {
        if (station.boxesRoot != null)
        {
            yellowBox = station.boxesRoot.Find("Yellow Box");
            redBox = station.boxesRoot.Find("Red Box");
            blueBox = station.boxesRoot.Find("Blue Box");
        }

        if (yellowBox == null)
            yellowBox = station.yellowBox;
        if (redBox == null)
            redBox = station.redBox;
        if (blueBox == null)
            blueBox = station.blueBox;

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

        yellowCol = EnsureTriggerCollider(yellowBox);
        redCol = EnsureTriggerCollider(redBox);

        if (station.boxesRoot != null)
            station.boxesRoot.gameObject.SetActive(true);
    }

    void SetupSortBoxes()
    {
        EnsureFrontDropZone();
        yellowSortBox = ConfigureSortBox(yellowBox, ObjectPlacementSortBox.BoxId.Yellow);
        redSortBox = ConfigureSortBox(redBox, ObjectPlacementSortBox.BoxId.Red);
        SetSortBoxesGrabEnabled(false);
    }

    ObjectPlacementSortBox ConfigureSortBox(Transform box, ObjectPlacementSortBox.BoxId id)
    {
        if (box == null)
            return null;

        ObjectPlacementSortBox sort = box.GetComponent<ObjectPlacementSortBox>();
        if (sort == null)
            sort = box.gameObject.AddComponent<ObjectPlacementSortBox>();

        Transform belt;
        float dir;
        GetFrontConveyor(out belt, out dir);
        sort.Configure(this, id, belt, frontDropZone, dir);
        return sort;
    }

    /// <summary>
    /// Storyboard: front belt = sorted boxes transport (station Conveyor / BeltSurface).
    /// Ride direction = toward ExitTunnel (into the mouth).
    /// </summary>
    void GetFrontConveyor(out Transform belt, out float direction)
    {
        belt = station.beltSurface != null ? station.beltSurface : station.conveyor;
        direction = 1f;

        Transform tunnel = station.supplyBox;
        if (tunnel == null && station.conveyor != null)
            tunnel = station.conveyor.Find("ExitTunnel");

        if (belt != null && tunnel != null)
        {
            Vector3 toTunnel = tunnel.position - belt.position;
            direction = Vector3.Dot(toTunnel, belt.right) >= 0f ? 1f : -1f;
        }
    }

    void EnsureFrontDropZone()
    {
        Transform belt;
        float dir;
        GetFrontConveyor(out belt, out dir);
        if (belt == null)
            return;

        Transform existing = belt.Find("BoxDropZone");
        if (existing == null && belt.parent != null)
            existing = belt.parent.Find("BoxDropZone");

        if (existing != null)
        {
            frontDropZone = existing;
            return;
        }

        GameObject zone = new GameObject("BoxDropZone");
        Transform parent = belt.parent != null ? belt.parent : belt;
        zone.transform.SetParent(parent, false);

        // Sit above the open run of the front belt so crates are easy to land.
        Vector3 local = belt.localPosition + new Vector3(0f, 0.18f, 0f);
        if (belt.parent == parent)
            zone.transform.localPosition = local;
        else
            zone.transform.position = belt.position + Vector3.up * 0.18f;

        zone.transform.localRotation = Quaternion.identity;
        BoxCollider col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        // Wide landing pad covering most of the front belt top.
        float len = Mathf.Max(1.0f, Mathf.Abs(belt.lossyScale.x) * 1.1f);
        float depth = Mathf.Max(0.45f, Mathf.Abs(belt.lossyScale.z) * 1.4f);
        col.size = new Vector3(len, 0.45f, depth);
        col.center = Vector3.zero;
        frontDropZone = zone.transform;
    }

    void SetSortBoxesGrabEnabled(bool enabled)
    {
        if (yellowSortBox != null)
            yellowSortBox.SetGrabEnabled(enabled);
        if (redSortBox != null)
            redSortBox.SetGrabEnabled(enabled);

        // Scoring volumes stay triggers while sorting; grab mode flips collider in SortBox.
        if (!enabled)
        {
            if (yellowCol != null)
                yellowCol.isTrigger = true;
            if (redCol != null)
                redCol.isTrigger = true;
        }
    }

    void ApplyPhaseIBoxVisibility()
    {
        if (yellowBox != null)
            yellowBox.gameObject.SetActive(true);
        if (redBox != null)
            redBox.gameObject.SetActive(true);
        if (blueBox != null)
            blueBox.gameObject.SetActive(false);

        // Hide any green/brown bins if present under station.
        if (station.boxesRoot != null)
        {
            for (int i = 0; i < station.boxesRoot.childCount; i++)
            {
                Transform child = station.boxesRoot.GetChild(i);
                string n = child.name;
                if (n.IndexOf("Green", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Brown", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Blue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    static Collider EnsureTriggerCollider(Transform box)
    {
        if (box == null)
            return null;

        Collider col = box.GetComponent<Collider>();
        if (col == null)
            col = box.gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        return col;
    }

    void CacheStopper()
    {
        beltStopper = station.beltStopperLeft;
        if (beltStopper != null)
            return;

        Transform root = station.conveyorLeft != null ? station.conveyorLeft : station.transform;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n.IndexOf("stop", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                beltStopper = all[i];
                station.beltStopperLeft = beltStopper;
                return;
            }
        }

        // Fallback: create a thin stopper at the open end of the left belt.
        if (station.beltSurfaceLeft != null)
        {
            GameObject stop = new GameObject("BeltStopper");
            stop.transform.SetParent(station.conveyorLeft != null ? station.conveyorLeft : station.beltSurfaceLeft, false);
            Transform belt = station.beltSurfaceLeft;
            float half = Mathf.Abs(belt.localScale.x) * 0.5f;
            // ConveyorLeft flows -X in world along belt; stopper at the outgoing end.
            stop.transform.position = belt.TransformPoint(new Vector3(-half + 0.05f, 0.06f, 0f));
            BoxCollider col = stop.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.06f, 0.16f, Mathf.Max(0.2f, Mathf.Abs(belt.localScale.z)));
            beltStopper = stop.transform;
            station.beltStopperLeft = beltStopper;
        }
    }

    void StripLegacyProps()
    {
        GameObject shapes = FindNamed("Shapes");
        if (shapes != null && (station == null || !shapes.transform.IsChildOf(station.transform)))
            shapes.SetActive(false);

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
        boxesDelivered = 0;
        boxDeliveryPhase = false;
        running = true;
        ClearPlacedInBoxes();
        RefillDecks();
        SetSortBoxesGrabEnabled(false);
        BeginSet();
        // Conveyor starts only from Btn_Start_World poke — not from UI start.
        waitingForConveyorStart = !BeltIsRunning;
        if (waitingForConveyorStart)
            station.SetScreenText("PRESS START");
    }

    void RefillDecks()
    {
        fruitDeck.Clear();
        vegDeck.Clear();
        fruitDeck.AddRange(ObjectPlacementItemLibrary.Fruits);
        vegDeck.AddRange(ObjectPlacementItemLibrary.Vegetables);
        Shuffle(fruitDeck);
        Shuffle(vegDeck);
    }

    void BeginSet()
    {
        ClearUnresolvedLive();
        // Keep sorted items in boxes across all 3 sets (clear only on new round).
        beltQueue.Clear();
        spawnQueue.Clear();
        scorePending.Clear();
        setCorrect = 0;
        setWrong = 0;
        spawnTimer = 0.35f;
        setOpen = true;
        boxDeliveryPhase = false;
        SetSortBoxesGrabEnabled(false);

        List<SpawnSpec> batch = new List<SpawnSpec>(ObjectPlacementGroceryCatalog.PhaseIItemsPerSet);
        for (int i = 0; i < 5; i++)
        {
            batch.Add(new SpawnSpec { template = TakeNext(fruitDeck, ObjectPlacementItemLibrary.Fruits), kind = ObjectPlacementGroceryKind.Fruit });
            batch.Add(new SpawnSpec { template = TakeNext(vegDeck, ObjectPlacementItemLibrary.Vegetables), kind = ObjectPlacementGroceryKind.Vegetable });
        }

        Shuffle(batch);
        for (int i = 0; i < batch.Count; i++)
        {
            if (batch[i].template != null)
                spawnQueue.Enqueue(batch[i]);
        }

        station.SetScreenText("SET " + (setIndex + 1));
    }

    static GameObject TakeNext(List<GameObject> deck, GameObject[] source)
    {
        if (deck.Count == 0)
        {
            if (source == null || source.Length == 0)
                return null;
            deck.AddRange(source);
            Shuffle(deck);
        }

        GameObject go = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        return go;
    }

    void TickSpawn()
    {
        if (spawnQueue.Count == 0)
            return;

        // Wait for Btn_Start_World before anything leaves the spawn.
        if (!BeltIsRunning)
            return;

        // Queue full or still filling toward capacity — nothing new leaves the spawn.
        if (!BeltQueueHasRoom || CountItemsOnBelt() >= MaxBeltQueue)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = SpawnInterval;
        SpawnSpec spec = spawnQueue.Dequeue();
        SpawnItem(spec);
    }

    int CountItemsOnBelt()
    {
        int n = 0;
        for (int i = 0; i < live.Count; i++)
        {
            ObjectPlacementGroceryItem item = live[i];
            if (item == null || item.Resolved || item.IsHeld)
                continue;
            n++;
        }

        return n;
    }

    /// <summary>Join end-of-belt queue. Slot 0 is front (grab first).</summary>
    public int JoinBeltQueue(ObjectPlacementGroceryItem item)
    {
        if (item == null)
            return -1;

        int existing = beltQueue.IndexOf(item);
        if (existing >= 0)
            return existing;

        if (beltQueue.Count >= MaxBeltQueue)
            return -1;

        beltQueue.Add(item);
        return beltQueue.Count - 1;
    }

    /// <summary>
    /// Missed drop: always put item at the back of this set's line (may overflow MaxBeltQueue).
    /// Spaced slots avoid stacking on the stopper.
    /// </summary>
    public int RequeueAtEnd(ObjectPlacementGroceryItem item)
    {
        if (item == null)
            return -1;

        CompactBeltQueue();
        beltQueue.Remove(item);
        beltQueue.Add(item);
        return beltQueue.Count - 1;
    }

    public bool IsInBeltQueue(ObjectPlacementGroceryItem item)
    {
        return item != null && beltQueue.IndexOf(item) >= 0;
    }

    public void LeaveBeltQueue(ObjectPlacementGroceryItem item)
    {
        if (item == null)
            return;
        if (beltQueue.Remove(item))
            CompactBeltQueue();
    }

    void CompactBeltQueue()
    {
        for (int i = beltQueue.Count - 1; i >= 0; i--)
        {
            if (beltQueue[i] == null || beltQueue[i].Resolved)
                beltQueue.RemoveAt(i);
        }
    }

    public Vector3 GetBeltQueueWorldPosition(ObjectPlacementGroceryItem item)
    {
        int index = beltQueue.IndexOf(item);
        if (index < 0)
            index = Mathf.Max(0, beltQueue.Count);

        return GetBeltQueueSlotWorld(index);
    }

    public Vector3 GetBeltQueueSlotWorld(int index)
    {
        Transform belt = station != null
            ? (station.beltSurfaceLeft != null ? station.beltSurfaceLeft : station.beltSurface)
            : null;
        if (belt == null && station != null)
            belt = station.conveyorLeft != null ? station.conveyorLeft : station.conveyor;

        float dir = -1f;
        if (station != null && station.conveyorLeft != null && belt != null &&
            (belt == station.beltSurfaceLeft || belt.IsChildOf(station.conveyorLeft) || belt == station.conveyorLeft))
            dir = -1f;
        else
            dir = 1f;

        Vector3 anchor = beltStopper != null
            ? beltStopper.position
            : (belt != null ? belt.position : Vector3.zero);

        if (belt != null)
        {
            Vector3 local = belt.InverseTransformPoint(anchor);
            // Park just upstream of stopper; each slot further upstream.
            local.x -= dir * (0.07f + index * QueueSpacing);
            Vector3 world = belt.TransformPoint(local);
            world.y = anchor.y + 0.02f;
            if (beltStopper != null)
                world.y = Mathf.Max(world.y, beltStopper.position.y + 0.01f);
            return world;
        }

        Vector3 right = Vector3.right;
        return anchor - right * dir * (0.07f + index * QueueSpacing) + Vector3.up * 0.02f;
    }

    public bool BeltQueueHasRoom => beltQueue.Count < MaxBeltQueue;

    void SpawnItem(SpawnSpec spec)
    {
        if (spec.template == null)
            return;

        Transform spawn = station.grocerySpawnPointLeft != null ? station.grocerySpawnPointLeft : station.grocerySpawnPoint;
        Transform belt = station.beltSurfaceLeft != null ? station.beltSurfaceLeft : station.beltSurface;
        if (belt == null)
            belt = station.conveyorLeft != null ? station.conveyorLeft : station.conveyor;

        GameObject go = ObjectPlacementItemLibrary.Spawn(spec.template, spec.kind, null);
        if (go == null)
            return;

        ObjectPlacementGroceryItem item = go.GetComponent<ObjectPlacementGroceryItem>();
        item.Bind(this);

        float speed = conveyor != null ? conveyor.BeltFlowSpeed : 0.38f;
        // ConveyorLeft scrolls reverse (-X).
        float dir = station.conveyorLeft != null && belt != null &&
                    (belt == station.beltSurfaceLeft || belt.IsChildOf(station.conveyorLeft) || belt == station.conveyorLeft)
            ? -1f
            : 1f;

        item.PlaceOnBelt(spawn, belt, dir, speed, beltStopper);
        live.Add(item);
    }

    public void OnWrongControl(ObjectPlacementGroceryItem item)
    {
        wrongControlFlashUntil = Time.time + 0.85f;
        station.SetScreenText(item != null && item.Kind == ObjectPlacementGroceryKind.Fruit ? "USE GRIP" : "USE TRIGGER");
    }

    public void RequestScoreCheck(ObjectPlacementGroceryItem item)
    {
        if (item == null || item.Resolved)
            return;

        ScorePending pending = new ScorePending();
        pending.item = item;
        pending.at = Time.time + ScoreDelay;
        scorePending.Add(pending);
    }

    void TickScorePending()
    {
        for (int i = scorePending.Count - 1; i >= 0; i--)
        {
            ScorePending pending = scorePending[i];
            if (pending.item == null || pending.item.Resolved)
            {
                scorePending.RemoveAt(i);
                continue;
            }

            if (Time.time < pending.at)
                continue;

            scorePending.RemoveAt(i);
            TryResolveDrop(pending.item);
        }
    }

    void TryResolveDrop(ObjectPlacementGroceryItem item)
    {
        if (item == null || item.Resolved || !setOpen)
            return;

        Transform box;
        bool correct;
        if (!TryScoreItem(item, out correct, out box))
        {
            // Missed both boxes — return to conveyor at the end of this set's queue.
            item.ReturnToStopper();
            return;
        }

        item.SettleInBox(box, correct);
        if (!placed.Contains(item))
            placed.Add(item);
    }

    public bool TryScoreItem(ObjectPlacementGroceryItem item, out bool correct, out Transform box)
    {
        correct = false;
        box = null;
        if (item == null)
            return false;

        Bounds itemBounds = GetWorldBounds(item.transform);
        bool inYellow = yellowCol != null && Overlaps(yellowCol.bounds, itemBounds);
        bool inRed = redCol != null && Overlaps(redCol.bounds, itemBounds);
        if (!inYellow && !inRed)
            return false;

        if (inYellow && inRed)
        {
            // Pick closer box center.
            float dy = Vector3.Distance(itemBounds.center, yellowCol.bounds.center);
            float dr = Vector3.Distance(itemBounds.center, redCol.bounds.center);
            inYellow = dy <= dr;
            inRed = !inYellow;
        }

        if (item.Kind == ObjectPlacementGroceryKind.Fruit)
        {
            box = inYellow ? yellowBox : redBox;
            correct = inYellow;
        }
        else
        {
            box = inRed ? redBox : yellowBox;
            correct = inRed;
        }

        return box != null;
    }

    static bool Overlaps(Bounds a, Bounds b)
    {
        Bounds expanded = a;
        expanded.Expand(0.12f);
        return expanded.Intersects(b);
    }

    public void OnItemResolved(ObjectPlacementGroceryItem item, bool correct)
    {
        LeaveBeltQueue(item);
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

        int remaining = 0;
        for (int i = 0; i < live.Count; i++)
        {
            if (live[i] != null && !live[i].Resolved)
                remaining++;
        }

        if (spawnQueue.Count == 0 && remaining == 0)
            FinishSet();
    }

    void FinishSet()
    {
        if (!setOpen)
            return;

        setOpen = false;

        if (setCorrect >= 8)
            station.SetScreenText("GREAT");
        else if (setCorrect >= 5)
            station.SetScreenText("GOOD");
        else
            station.SetScreenText("FAIR");

        setIndex++;
        if (setIndex >= ObjectPlacementGroceryCatalog.PhaseISetCount)
        {
            StartCoroutine(BeginBoxDeliveryAfterBeat());
            return;
        }

        StartCoroutine(NextSetAfterBeat());
    }

    IEnumerator NextSetAfterBeat()
    {
        yield return new WaitForSeconds(1.1f);
        BeginSet();
    }

    IEnumerator BeginBoxDeliveryAfterBeat()
    {
        yield return new WaitForSeconds(1.1f);
        BeginBoxDeliveryPhase();
    }

    void BeginBoxDeliveryPhase()
    {
        running = false;
        setOpen = false;
        boxDeliveryPhase = true;
        ClearUnresolvedLive();
        SetSortBoxesGrabEnabled(true);
        if (conveyor != null && !conveyor.IsRunning)
            conveyor.StartConveyors();
        station.SetScreenText("BOTH HANDS → FRONT CONVEYOR");
    }

    public void OnSortBoxMissed(ObjectPlacementSortBox box)
    {
        if (!boxDeliveryPhase)
            return;
        station.SetScreenText("DROP ON FRONT CONVEYOR");
    }

    public void OnSortBoxAccepted(ObjectPlacementSortBox box)
    {
        if (!boxDeliveryPhase)
            return;
        station.SetScreenText("BOX LOADING…");
    }

    public void OnSortBoxDelivered(ObjectPlacementSortBox box)
    {
        if (!boxDeliveryPhase)
            return;

        boxesDelivered++;
        int remaining = 0;
        if (yellowSortBox != null && !yellowSortBox.IsDelivered)
            remaining++;
        if (redSortBox != null && !redSortBox.IsDelivered)
            remaining++;

        if (remaining > 0)
        {
            station.SetScreenText("PUT NEXT BOX ON CONVEYOR");
            return;
        }

        EndRound();
    }

    void EndRound()
    {
        running = false;
        setOpen = false;
        boxDeliveryPhase = false;
        SetSortBoxesGrabEnabled(false);
        ClearUnresolvedLive();
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

    void ClearUnresolvedLive()
    {
        beltQueue.Clear();
        for (int i = 0; i < live.Count; i++)
        {
            ObjectPlacementGroceryItem item = live[i];
            if (item == null)
                continue;
            if (item.Resolved)
                continue;
            item.ForceResolve();
            Destroy(item.gameObject);
        }

        live.Clear();
    }

    void ClearPlacedInBoxes()
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (placed[i] != null)
                Destroy(placed[i].gameObject);
        }

        placed.Clear();
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
