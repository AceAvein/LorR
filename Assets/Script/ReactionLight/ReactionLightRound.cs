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

    GameObject objectiveScreen;
    GameObject selectHand;
    GameObject countdownScreen;
    TMP_Text countdownText;
    TextMeshPro timerLabel;
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
        WireButtons();
        ShowObjective();
    }

    void Start()
    {
        StartCoroutine(PlaceWhenReady());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        grip?.Dispose();
        trigger?.Dispose();
    }

    IEnumerator PlaceWhenReady()
    {
        yield return null;
        PlaceBalls();
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

        RefreshTimer();
        ReadPokes();

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
    }

    public void ShowHandSelect()
    {
        if (running || countingDown)
            return;

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (selectHand != null)
        {
            PlaceInFront(selectHand, 0.75f);
            selectHand.SetActive(true);
        }

        PlaceBalls();
    }

    public void ChooseLeftHand()
    {
        BeginCountdown(false);
    }

    public void ChooseRightHand()
    {
        BeginCountdown(true);
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
        PlaceBalls();
        StartCoroutine(CountdownThenPlay());
    }

    IEnumerator CountdownThenPlay()
    {
        countingDown = true;
        if (countdownScreen != null)
        {
            PlaceInFront(countdownScreen, 0.45f);
            countdownScreen.SetActive(true);
        }

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
        greenHits = 0;
        yellowHits = 0;
        redHits = 0;
        setsCleared = 0;
        setIndex = 0;
        announcedTier = -1;
        timeRemaining = RoundSeconds;
        running = true;
        touching.Clear();
        LevelVoices.Reset();
        EnsureTimer();
        BeginSet();
        RefreshTimer();
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
        setEndsAt = Time.time + duration;
    }

    void FinishSet(bool cleared)
    {
        if (!running)
            return;

        setEndsAt = float.PositiveInfinity;
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
        timeLeft = Mathf.Max(0, Mathf.RoundToInt(timeRemaining));
        timeRemaining = timeLeft;
        PaintAll(LightKind.Red);
        touching.Clear();
        RefreshTimer();
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
            return;
        }

        redHits++;
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

        if (timerLabel != null)
            timerLabel.transform.position = origin + Vector3.up * 0.42f;
    }

    void EnsureTimer()
    {
        if (timerLabel != null)
            return;

        GameObject labelObject = new GameObject("ReactionTimer");
        timerLabel = labelObject.AddComponent<TextMeshPro>();
        timerLabel.font = TMP_Settings.defaultFontAsset;
        timerLabel.fontSize = 8f;
        timerLabel.alignment = TextAlignmentOptions.Center;
        timerLabel.color = Color.white;
        timerLabel.rectTransform.sizeDelta = new Vector2(4f, 2f);
        labelObject.transform.localScale = Vector3.one * 0.12f;
        timerLabel.text = Mathf.CeilToInt(RoundSeconds).ToString();
        PlaceBalls();
    }

    void RefreshTimer()
    {
        if (timerLabel == null)
            return;

        timerLabel.text = Mathf.Max(0, Mathf.CeilToInt(timeRemaining)).ToString();
        Camera headset = Camera.main;
        if (headset == null)
            return;

        Vector3 toPlayer = headset.transform.position - timerLabel.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
            timerLabel.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
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
        return 1f;
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
                countdownScreen = transforms[i].gameObject;
        }

        if (countdownScreen != null)
            countdownText = countdownScreen.GetComponentInChildren<TMP_Text>(true);
    }

    void WireButtons()
    {
        Wire("Btn_Start", ShowHandSelect);
        Wire("Left Hand", ChooseLeftHand);
        Wire("Right Hand", ChooseRightHand);
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
