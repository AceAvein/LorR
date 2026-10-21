using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuidedBallDropLesson : MonoBehaviour
{
    const int StepTotal = 5;
    const float FallSpeed = 0.35f;
    const float SqueezeAmount = 0.55f;

    GuidedLesson lesson;
    BallDropSystem dropSystem;
    GameObject objectiveScreen;
    GameObject resultsScreen;
    GameObject leftTemplate;
    GameObject rightTemplate;
    DropBall currentBall;
    InputAction leftGrip;
    InputAction leftTrigger;
    InputAction rightGrip;
    InputAction rightTrigger;
    int step;
    bool waiting;
    bool lessonOpen;
    bool squeezeArmed;

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
        if (!scene.IsValid() || scene.name != "Guided Ball Drop")
            return;

        if (FindAnyObjectByType<GuidedBallDropLesson>() != null)
            return;

        GameObject host = new GameObject("GuidedLesson");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GuidedBallDropLesson>();
    }

    void Awake()
    {
        lesson = GetComponent<GuidedLesson>();
        if (lesson == null)
            lesson = gameObject.AddComponent<GuidedLesson>();

        dropSystem = FindFirstObjectByType<BallDropSystem>(FindObjectsInactive.Include);
        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        FindTemplates();
        HideHud();
        SetObjectiveText("HOW TO PLAY:\nFirst squeeze Grab on each controller, then catch the falling ball with the matching hand.");
        WireStart();
        ShowIntro();
    }

    void Start()
    {
        HideHud();
        if (!lessonOpen)
            ShowIntro();
    }

    void OnEnable()
    {
        DropBall.Grabbed += OnGrabbed;
        DropBall.Missed += OnMissed;
        BindSqueeze();
    }

    void OnDisable()
    {
        DropBall.Grabbed -= OnGrabbed;
        DropBall.Missed -= OnMissed;
        UnbindSqueeze();
    }

    void Update()
    {
        if (!lessonOpen || waiting || GamePauseMenu.IsFrozen)
            return;

        if (step == 1)
            PollSqueeze(true);
        else if (step == 2)
            PollSqueeze(false);
    }

    void ShowIntro()
    {
        lessonOpen = false;
        waiting = false;
        step = 0;
        ClearBall();
        GuidedPointer.ClearAll();
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
        lessonOpen = true;
        step = 1;
        squeezeArmed = false;
        ShowCurrentStep();
        BeginStep();
    }

    void ShowCurrentStep()
    {
        if (lesson == null)
            return;

        if (step == 1)
            lesson.ShowStep(1, StepTotal, "Squeeze GRAB on your LEFT controller.", "");
        else if (step == 2)
            lesson.ShowStep(2, StepTotal, "Now squeeze GRAB on your RIGHT controller.", "");
        else if (step == 3)
            lesson.ShowStep(3, StepTotal, "Catch this ball with your LEFT hand.", "");
        else if (step == 4)
            lesson.ShowStep(4, StepTotal, "Catch this ball with your RIGHT hand.", "");
        else
            lesson.ShowStep(5, StepTotal, "Wrong hand does not count. Use the matching hand.", "");
    }

    void BeginStep()
    {
        ClearBall();
        GuidedPointer.ClearAll();
        squeezeArmed = false;

        if (step <= 2)
        {
            MarkGrabHand(step == 1);
            return;
        }

        SpawnForStep();
    }

    void SpawnForStep()
    {
        if (!lessonOpen || dropSystem == null)
            return;

        bool forLeft = step != 4;
        GameObject template = forLeft ? leftTemplate : rightTemplate;
        if (template == null)
            template = leftTemplate != null ? leftTemplate : rightTemplate;

        GameObject spawned = dropSystem.SpawnOne(template, forLeft, FallSpeed);
        if (spawned == null)
            return;

        currentBall = spawned.GetComponent<DropBall>();
        MarkCatch(spawned.transform, forLeft);
    }

    void MarkGrabHand(bool left)
    {
        Transform hand = FindHand(left);
        GuidedPointer.Follow(hand, "GRAB", 0.07f, 0.12f);
    }

    void MarkCatch(Transform ball, bool forLeft)
    {
        GuidedPointer.Follow(ball, forLeft ? "LEFT" : "RIGHT", 0.16f, 0.08f);
        GuidedPointer.Follow(FindHand(forLeft), "", 0.07f, 0.12f);
    }

    void PollSqueeze(bool left)
    {
        float grip = ReadSqueeze(left ? leftGrip : rightGrip);
        float trigger = ReadSqueeze(left ? leftTrigger : rightTrigger);
        bool down = grip >= SqueezeAmount || trigger >= SqueezeAmount;
        if (!down)
        {
            squeezeArmed = true;
            return;
        }

        if (!squeezeArmed)
            return;

        squeezeArmed = false;
        BallDropSounds.PlayCatch();
        Advance();
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

    void OnGrabbed(DropBall ball, bool correct)
    {
        if (!lessonOpen || waiting || step < 3 || ball == null || ball != currentBall)
            return;

        if (!correct)
        {
            BallDropSounds.PlayMiss();
            string handWord = ball.forLeftHand ? "LEFT" : "RIGHT";
            if (lesson != null)
                lesson.SetHint("Wrong hand. Use the " + handWord + " hand.");
            return;
        }

        BallDropSounds.PlayCatch();
        currentBall = null;
        GuidedPointer.ClearAll();
        if (lesson != null)
            lesson.SetHint("");

        Advance();
    }

    void OnMissed(DropBall ball)
    {
        if (!lessonOpen || waiting || ball == null || ball != currentBall)
            return;

        BallDropSounds.PlayMiss();
        currentBall = null;
        if (lesson != null)
            lesson.SetHint("Catch it before it falls.");

        StartCoroutine(RespawnSoon());
    }

    IEnumerator RespawnSoon()
    {
        waiting = true;
        float waited = 0f;
        while (waited < 0.7f)
        {
            if (!GamePauseMenu.IsFrozen)
                waited += Time.deltaTime;
            yield return null;
        }

        waiting = false;
        if (lessonOpen && currentBall == null && step >= 3)
            SpawnForStep();
    }

    void Finish()
    {
        lessonOpen = false;
        waiting = false;
        ClearBall();
        GuidedPointer.ClearAll();
        if (lesson != null)
            lesson.Hide();

        ShowDone();
    }

    void ShowDone()
    {
        if (resultsScreen == null)
            return;

        resultsScreen.SetActive(true);
        Transform panel = resultsScreen.transform.Find("ResultsPanel");
        if (panel != null)
        {
            Image sheet = panel.GetComponent<Image>();
            if (sheet != null)
            {
                sheet.enabled = true;
                sheet.sprite = null;
                sheet.color = new Color(0.07f, 0.1f, 0.16f, 0.94f);
            }

            TMP_Text done = FindChildText(panel, "Text_Done");
            if (done == null)
                done = CreateDoneLabel(panel);
            done.text = "You did it!\nThat's how you play.";
            done.gameObject.SetActive(true);
        }
    }

    static TMP_Text CreateDoneLabel(Transform panel)
    {
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
        return text;
    }

    void FindTemplates()
    {
        string[] names = { "Basketball", "Volleyball", "Soccerball", "Football", "Tennisball", "Baseball", "Bowlingball" };
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].parent != null || transforms[i].name != names[n])
                    continue;

                transforms[i].gameObject.SetActive(false);
                if (leftTemplate == null)
                    leftTemplate = transforms[i].gameObject;
                else if (rightTemplate == null && transforms[i].gameObject != leftTemplate)
                    rightTemplate = transforms[i].gameObject;
                break;
            }
        }

        if (rightTemplate == null)
            rightTemplate = leftTemplate;
    }

    void HideHud()
    {
        SetNamedActive("Ball assign canvas", false);
        SetNamedActive("CountdownScreen", false);
        SetNamedActive("Canvas Guide", false);
        SetNamedActive("GameCanvas", false);

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

    void ClearBall()
    {
        if (currentBall == null)
            return;

        DropBall ball = currentBall;
        currentBall = null;
        if (ball != null)
            Destroy(ball.gameObject);
    }

    void BindSqueeze()
    {
        UnbindSqueeze();
        leftGrip = MakeSqueeze("{LeftHand}/grip");
        leftTrigger = MakeSqueeze("{LeftHand}/trigger");
        rightGrip = MakeSqueeze("{RightHand}/grip");
        rightTrigger = MakeSqueeze("{RightHand}/trigger");
    }

    void UnbindSqueeze()
    {
        DisposeSqueeze(ref leftGrip);
        DisposeSqueeze(ref leftTrigger);
        DisposeSqueeze(ref rightGrip);
        DisposeSqueeze(ref rightTrigger);
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

    static Transform FindHand(bool left)
    {
        Transform controller = PlayerHands.WritingController(!left);
        if (controller != null)
            return controller;

        Transform named = FindNamedTransform(left ? "Left Controller" : "Right Controller");
        if (named != null)
            return named;

        return FindNamedTransform(left ? "Left Hand Model" : "Right Hand Model");
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

    static GameObject FindNamed(string objectName)
    {
        Transform found = FindNamedTransform(objectName);
        return found != null ? found.gameObject : null;
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
