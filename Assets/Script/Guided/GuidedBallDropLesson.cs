using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuidedBallDropLesson : MonoBehaviour
{
    const int StepTotal = 3;
    const float FallSpeed = 0.35f;

    GuidedLesson lesson;
    BallDropSystem dropSystem;
    GameObject objectiveScreen;
    GameObject resultsScreen;
    GameObject leftTemplate;
    GameObject rightTemplate;
    DropBall currentBall;
    int step;
    bool waiting;
    bool lessonOpen;

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
        SetObjectiveText("HOW TO PLAY:\nCatch the falling ball with the matching hand.");
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
    }

    void OnDisable()
    {
        DropBall.Grabbed -= OnGrabbed;
        DropBall.Missed -= OnMissed;
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
        ShowCurrentStep();
        SpawnForStep();
    }

    void ShowCurrentStep()
    {
        if (lesson == null)
            return;

        if (step == 1)
            lesson.ShowStep(1, StepTotal, "Grab this ball with your LEFT hand.", "");
        else if (step == 2)
            lesson.ShowStep(2, StepTotal, "Now grab this ball with your RIGHT hand.", "");
        else
            lesson.ShowStep(3, StepTotal, "Wrong hand does not count. Use the matching hand.", "");
    }

    void SpawnForStep()
    {
        if (!lessonOpen || dropSystem == null)
            return;

        ClearBall();
        bool forLeft = step != 2;
        GameObject template = forLeft ? leftTemplate : rightTemplate;
        if (template == null)
            template = leftTemplate != null ? leftTemplate : rightTemplate;

        GameObject spawned = dropSystem.SpawnOne(template, forLeft, FallSpeed);
        if (spawned == null)
            return;

        currentBall = spawned.GetComponent<DropBall>();
        MarkTargets(spawned.transform, forLeft);
    }

    void MarkTargets(Transform ball, bool forLeft)
    {
        GuidedPointer.ClearAll();
        GuidedPointer.Attach(ball, forLeft ? "LEFT" : "RIGHT", new Vector3(0f, 0.12f, 0f));

        Transform hand = FindHand(forLeft);
        if (hand != null)
            GuidedPointer.Attach(hand, "THIS HAND", new Vector3(0f, 0.08f, 0f));
    }

    void OnGrabbed(DropBall ball, bool correct)
    {
        if (!lessonOpen || waiting || ball == null || ball != currentBall)
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

        if (step < StepTotal)
        {
            step++;
            ShowCurrentStep();
            SpawnForStep();
            return;
        }

        Finish();
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
        if (lessonOpen && currentBall == null)
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

    static Transform FindHand(bool left)
    {
        string key = left ? "Left Hand Model" : "Right Hand Model";
        Transform named = FindNamedTransform(key);
        if (named != null)
            return named;

        return FindNamedTransform(left ? "Left Controller" : "Right Controller");
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
