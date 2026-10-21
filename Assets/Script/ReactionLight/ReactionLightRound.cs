using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReactionLightRound : MonoBehaviour
{
    public static ReactionLightRound Instance { get; private set; }

    public int greenHits { get; private set; }
    public int yellowHits { get; private set; }
    public int redHits { get; private set; }
    public int setsCleared { get; private set; }
    public int timeLeft { get; private set; }

    const int SetTotal = 30;
    const float RoundSeconds = 30f;
    const float SetBonus = 2f;
    const float RedPenalty = 2f;
    const int Columns = 4;
    const int Rows = 3;

    enum LightKind
    {
        Red,
        Green,
        Yellow
    }

    class Target
    {
        public Transform transform;
        public Renderer renderer;
        public Material material;
        public LightKind kind;
        public float radius;
    }

    readonly List<Target> balls = new List<Target>();
    readonly HashSet<Target> touching = new HashSet<Target>();
    readonly List<Target> greenBalls = new List<Target>();
    readonly List<SetRecord> records = new List<SetRecord>();

    GameObject objectiveScreen;
    GameObject selectHand;
    GameObject countdownScreen;
    TMP_Text countdownText;
    readonly List<TMP_Text> levelValues = new List<TMP_Text>();
    readonly List<TMP_Text> timerValues = new List<TMP_Text>();
    readonly List<TMP_Text> setValues = new List<TMP_Text>();
    GameObject leftWristHud;
    GameObject rightWristHud;
    InputAction grip;
    InputAction trigger;
    Transform fingerTip;
    bool useRightHand;
    bool countingDown;
    bool running;
    int setIndex;
    int greensLeft;
    int announcedTier = -1;
    float timeRemaining;
    float setEndsAt;
    float setStartedAt;
    float roundStartedAt;
    bool setOpen;
    float wordHideAt;

    static readonly Color Red = new Color(0.85f, 0.08f, 0.08f);
    static readonly Color Green = new Color(0.1f, 0.9f, 0.2f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);

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
        if (!scene.IsValid() || scene.name != "ReactionLight")
            return;

        if (Instance != null)
            return;

        GameObject host = new GameObject("ReactionLightRound");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ReactionLightRound>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindBalls();
        FindScreens();
        PaintAll(LightKind.Red);
        SetBallsVisible(false);
        WireButtons();
        HideWord();
        SetNamedActive("ResultsScreen", false);
        ShowObjective();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        grip?.Dispose();
        trigger?.Dispose();
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (!running)
            return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndRound();
            return;
        }

        RefreshHud();
        ReadPokes();
        HideWordIfDue();

        if (running && Time.time >= setEndsAt)
            FinishSet(false);
    }

    void ShowObjective()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(true);
        if (selectHand != null)
            selectHand.SetActive(false);
        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        HideGameplayHud();
        HideWristHud();
    }

    public void ShowHandSelect()
    {
        StartFromObjective();
    }

    public void StartFromObjective()
    {
        if (running || countingDown)
            return;

        ScoringChecker.Ensure().TryContinue(StartFromObjectiveAfterCheck);
    }

    void StartFromObjectiveAfterCheck()
    {
        if (running || countingDown)
            return;

        ProfileManager.TryGetNonDominantLeft(out bool leftHand);
        BeginCountdown(!leftHand);
    }

    public void ChooseLeftHand()
    {
        StartFromObjective();
    }

    public void ChooseRightHand()
    {
        StartFromObjective();
    }

    void BeginCountdown(bool rightHand)
    {
        if (running || countingDown)
            return;

        useRightHand = rightHand;
        if (selectHand != null)
            selectHand.SetActive(false);
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);

        BindHand();
        StartCoroutine(CountdownThenPlay());
    }

    IEnumerator CountdownThenPlay()
    {
        countingDown = true;
        PrepareWorldCountdown();
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

    void PrepareWorldCountdown()
    {
        if (countdownScreen == null)
            countdownScreen = FindNamed("CountdownScreen");
        if (countdownScreen == null)
            return;

        if (countdownText == null)
            countdownText = countdownScreen.GetComponentInChildren<TMP_Text>(true);

        if (countdownText != null)
        {
            RectTransform rect = countdownText.rectTransform;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(50f, 50f);
            countdownText.fontSize = 36f;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color = Color.white;
        }

        GamePauseMenu.PlaceInFront(countdownScreen);
    }

    void StartRound()
    {
        greenHits = 0;
        yellowHits = 0;
        redHits = 0;
        setsCleared = 0;
        setIndex = 0;
        announcedTier = -1;
        timeRemaining = RoundSeconds;
        roundStartedAt = Time.time;
        setOpen = false;
        records.Clear();
        running = true;
        touching.Clear();
        LevelVoices.Reset();
        SetBallsVisible(true);
        PlaceBalls();
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

        PaintAll(LightKind.Red);
        greenBalls.Clear();

        int greens = GreensForSet(setIndex);
        int yellows = YellowsForSet(setIndex);
        float duration = DurationForSet(setIndex);
        int tier = setIndex / 10;
        if (tier != announcedTier)
        {
            announcedTier = tier;
            LevelVoices.PlayTier(tier);
            BallDropSky.ApplyCurrent(tier);
        }

        List<Target> pool = new List<Target>(balls);
        for (int i = 0; i < greens + yellows && pool.Count > 0; i++)
        {
            int pick = Random.Range(0, pool.Count);
            Target ball = pool[pick];
            pool.RemoveAt(pick);

            if (i < greens)
            {
                Paint(ball, LightKind.Green);
                greenBalls.Add(ball);
            }
            else
            {
                Paint(ball, LightKind.Yellow);
            }
        }

        greensLeft = greenBalls.Count;
        setStartedAt = Time.time;
        setOpen = true;
        setEndsAt = Time.time + duration;
    }

    void FinishSet(bool cleared)
    {
        if (!running)
            return;

        setEndsAt = float.PositiveInfinity;
        RecordSet(cleared);
        if (cleared)
        {
            setsCleared++;
            timeRemaining += SetBonus;
        }

        setIndex++;
        if (setIndex >= SetTotal)
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
        setEndsAt = float.PositiveInfinity;
        RecordSet(false);
        timeLeft = Mathf.Max(0, Mathf.RoundToInt(timeRemaining));
        timeRemaining = timeLeft;
        PaintAll(LightKind.Red);
        touching.Clear();
        SetBallsVisible(false);
        HideGameplayHud();
        HideWristHud();
        HideWord();
        RefreshHud();
        ShowResults();
    }

    void RecordSet(bool cleared)
    {
        if (!setOpen)
            return;

        setOpen = false;
        SetRecord record = new SetRecord();
        record.tier = setIndex / 10;
        if (record.tier > 2)
            record.tier = 2;
        record.cleared = cleared;
        record.reaction = cleared ? Mathf.Max(0f, Time.time - setStartedAt) : 0f;
        records.Add(record);
    }

    void ShowResults()
    {
        GameObject screen = FindNamed("ResultsScreen");
        if (screen == null)
            return;

        screen.SetActive(true);
        ReactionLightResults results = screen.GetComponent<ReactionLightResults>();
        if (results == null)
            results = screen.AddComponent<ReactionLightResults>();
        results.Show(BuildReport());
    }

    ReactionLightReport BuildReport()
    {
        int easyCleared = 0;
        int moderateCleared = 0;
        int hardCleared = 0;
        float reactionSum = 0f;
        int reactionCount = 0;
        int score = 0;

        for (int i = 0; i < records.Count; i++)
        {
            SetRecord record = records[i];
            if (!record.cleared)
                continue;

            reactionSum += record.reaction;
            reactionCount++;
            if (record.tier <= 0)
            {
                easyCleared++;
                score += 10;
            }
            else if (record.tier == 1)
            {
                moderateCleared++;
                score += 20;
            }
            else
            {
                hardCleared++;
                score += 30;
            }
        }

        ReactionLightReport report = new ReactionLightReport();
        report.setsCleared = setsCleared;
        report.setTotal = SetTotal;
        report.setsPlayed = records.Count;
        report.hand = useRightHand ? "Right" : "Left";
        report.accuracy = records.Count == 0 ? 0f : setsCleared * 100f / records.Count;
        report.averageReaction = reactionCount == 0 ? 0f : reactionSum / reactionCount;
        report.greenHits = greenHits;
        report.yellowPresses = yellowHits;
        report.redPresses = redHits;
        report.timeAdded = Mathf.RoundToInt(setsCleared * SetBonus);
        report.timeUsed = Mathf.Max(0, Mathf.RoundToInt(Time.time - roundStartedAt));
        report.timeLeft = timeLeft;
        report.easyCleared = easyCleared;
        report.moderateCleared = moderateCleared;
        report.hardCleared = hardCleared;
        report.tierTotal = 10;
        report.lastSet = LastSetText();
        report.totalScore = score;
        return report;
    }

    string LastSetText()
    {
        if (records.Count == 0)
            return "None";

        SetRecord record = records[records.Count - 1];
        string outcome = record.cleared ? "Cleared" : "Missed";
        return outcome + " · " + TierName(record.tier);
    }

    void ReadPokes()
    {
        if (fingerTip == null)
            BindHand();

        bool poking = fingerTip != null && IsPoking();
        Vector3 tip = poking ? fingerTip.position : Vector3.zero;

        for (int i = 0; i < balls.Count; i++)
        {
            Target ball = balls[i];
            bool hit = poking && Vector3.Distance(tip, ball.transform.position) <= ball.radius + 0.025f;
            if (hit)
            {
                if (touching.Add(ball))
                    Press(ball);
            }
            else
            {
                touching.Remove(ball);
            }
        }
    }

    void Press(Target ball)
    {
        if (!running)
            return;

        if (ball.kind == LightKind.Green)
        {
            greenHits++;
            ShowWord("Great");
            BallDropSounds.PlayCatch();
            Paint(ball, LightKind.Red);
            greenBalls.Remove(ball);
            greensLeft--;
            if (greensLeft <= 0)
                FinishSet(true);
            return;
        }

        if (ball.kind == LightKind.Yellow)
        {
            yellowHits++;
            ShowWord("Oops");
            ReactionLightSounds.PlayOops();
            return;
        }

        redHits++;
        ShowWord("Wrong");
        BallDropSounds.PlayMiss();
        timeRemaining -= RedPenalty;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndRound();
        }
    }

    void BindHand()
    {
        grip?.Dispose();
        trigger?.Dispose();

        string hand = useRightHand ? "{RightHand}" : "{LeftHand}";
        grip = new InputAction(type: InputActionType.Value, binding: "<XRController>" + hand + "/grip");
        trigger = new InputAction(type: InputActionType.Value, binding: "<XRController>" + hand + "/trigger");
        grip.Enable();
        trigger.Enable();
        fingerTip = FindFingerTip();
    }

    bool IsPoking()
    {
        if (grip == null || trigger == null)
            return false;

        float gripAmount = grip.ReadValue<float>();
        float triggerAmount = trigger.ReadValue<float>();
        return gripAmount >= 0.55f && triggerAmount <= 0.35f;
    }

    Transform FindFingerTip()
    {
        Transform controller = PlayerHands.WritingController(useRightHand);
        if (controller == null)
            return null;

        Transform tip = null;
        Transform[] children = controller.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string boneName = children[i].name;
            if (boneName.Contains("index3"))
                tip = children[i];
        }

        return tip;
    }

    void FindBalls()
    {
        balls.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            string objectName = transforms[i].name;
            if (objectName != "Ball" && !objectName.StartsWith("Ball ("))
                continue;

            Renderer renderer = transforms[i].GetComponent<Renderer>();
            if (renderer == null)
                continue;

            Target ball = new Target();
            ball.transform = transforms[i];
            ball.renderer = renderer;
            ball.material = renderer.material;
            ball.kind = LightKind.Red;
            ball.radius = transforms[i].lossyScale.x * 0.5f;
            balls.Add(ball);
        }
    }

    void PlaceBalls()
    {
        if (balls.Count == 0)
            return;

        Camera headset = Camera.main;
        Vector3 forward = Vector3.forward;
        Vector3 eye = new Vector3(0f, 1.5f, 0f);
        if (headset != null)
        {
            eye = headset.transform.position;
            forward = headset.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 origin = eye + forward * 0.55f;
        origin.y = eye.y - 0.05f;
        float spacing = 0.2f;

        for (int i = 0; i < balls.Count; i++)
        {
            int column = i % Columns;
            int row = i / Columns;
            if (row >= Rows)
                row = Rows - 1;

            float x = (column - (Columns - 1) * 0.5f) * spacing;
            float y = ((Rows - 1) * 0.5f - row) * spacing;
            balls[i].transform.position = origin + right * x + Vector3.up * y;
            balls[i].radius = balls[i].transform.lossyScale.x * 0.5f;
        }
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
        SetNamedActive("Wrong", false);
        SetNamedActive("Oops", false);
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
        if (shownSet < 1)
            shownSet = 1;

        int tier = setIndex / 10;
        if (tier > 2)
            tier = 2;

        SetTexts(levelValues, TierName(tier));
        SetTexts(timerValues, seconds.ToString());
        SetTexts(setValues, shownSet + "/" + SetTotal);
    }

    void HideWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(false);
        if (rightWristHud != null)
            rightWristHud.SetActive(false);
    }

    void ShowWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(!useRightHand);
        if (rightWristHud != null)
            rightWristHud.SetActive(useRightHand);
    }

    void ResolveWristHuds()
    {
        if (leftWristHud != null && rightWristHud != null)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            string canvasName = transforms[i].name;
            if (canvasName != "Hand Canvas" && canvasName != "Left Hand Canvas" && canvasName != "Right Hand Canvas")
                continue;

            if (canvasName == "Left Hand Canvas" || IsUnder(transforms[i], "Left Controller") || IsUnder(transforms[i], "Left Hand Model"))
                leftWristHud = transforms[i].gameObject;
            else if (canvasName == "Right Hand Canvas" || IsUnder(transforms[i], "Right Controller") || IsUnder(transforms[i], "Right Hand Model"))
                rightWristHud = transforms[i].gameObject;
        }
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

    static void SetTexts(List<TMP_Text> labels, string value)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
                labels[i].text = value;
        }
    }

    static void FindTexts(string objectName, List<TMP_Text> results)
    {
        results.Clear();
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                results.Add(labels[i]);
        }
    }

    static string TierName(int tier)
    {
        if (tier <= 0)
            return "Easy";
        if (tier == 1)
            return "Moderate";
        return "Hard";
    }

    void SetBallsVisible(bool visible)
    {
        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i].transform != null)
                balls[i].transform.gameObject.SetActive(visible);
        }
    }

    void PaintAll(LightKind kind)
    {
        for (int i = 0; i < balls.Count; i++)
            Paint(balls[i], kind);
    }

    static void Paint(Target ball, LightKind kind)
    {
        ball.kind = kind;
        if (ball.material == null)
            return;

        Color color = Red;
        if (kind == LightKind.Green)
            color = Green;
        else if (kind == LightKind.Yellow)
            color = Yellow;

        if (ball.material.HasProperty("_BaseColor"))
            ball.material.SetColor("_BaseColor", color);
        if (ball.material.HasProperty("_Color"))
            ball.material.SetColor("_Color", color);

        ball.material.EnableKeyword("_EMISSION");
        if (ball.material.HasProperty("_EmissionColor"))
            ball.material.SetColor("_EmissionColor", color * 1.4f);
    }

    static int GreensForSet(int index)
    {
        int tier = index / 10;
        if (tier <= 0)
            return 1;
        if (tier == 1)
            return 2;
        return 3;
    }

    static int YellowsForSet(int index)
    {
        int tier = index / 10;
        if (tier <= 0)
            return 0;
        if (tier == 1)
            return 1;
        return 2;
    }

    static float DurationForSet(int index)
    {
        int tier = index / 10;
        if (tier <= 0)
            return 2f;
        if (tier == 1)
            return 1.5f;
        return 1.3f;
    }

    void FindScreens()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (objectiveScreen == null && transforms[i].name == "ObjectiveScreen")
                objectiveScreen = transforms[i].gameObject;
            if (selectHand == null && transforms[i].name == "Select hand")
                selectHand = transforms[i].gameObject;
            if (countdownScreen == null && transforms[i].name == "CountdownScreen")
            {
                countdownScreen = transforms[i].gameObject;
                countdownText = countdownScreen.GetComponentInChildren<TMP_Text>(true);
            }
        }
    }

    void WireButtons()
    {
        Wire("Btn_Start", StartFromObjective);
    }

    static void Wire(string objectName, UnityEngine.Events.UnityAction action)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name != objectName)
                continue;

            buttons[i].onClick.AddListener(action);
        }
    }

}
