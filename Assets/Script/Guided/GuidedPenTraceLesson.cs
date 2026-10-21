using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuidedPenTraceLesson : MonoBehaviour
{
    const int StepTotal = 5;
    const float SqueezeAmount = 0.55f;
    const float GreatScore = 85f;

    GuidedLesson lesson;
    GuidedPenTraceVoices voices;
    PenSelect penSelect;
    PatternManager patterns;
    PathManager pathManager;
    GameObject objectiveScreen;
    GameObject resultsScreen;
    GameObject tracingArea;
    InputAction leftTrigger;
    InputAction rightTrigger;
    int step;
    bool lessonOpen;
    bool squeezeArmed;
    bool drawingStep;
    bool waiting;
    bool wrongVoiceArmed;

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
        if (!scene.IsValid() || scene.name != "Guided Pen Control Tracing")
            return;

        if (FindAnyObjectByType<GuidedPenTraceLesson>() != null)
            return;

        GameObject host = new GameObject("GuidedLesson");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GuidedPenTraceLesson>();
    }

    void Awake()
    {
        lesson = GetComponent<GuidedLesson>();
        if (lesson == null)
            lesson = gameObject.AddComponent<GuidedLesson>();

        voices = GuidedPenTraceVoices.Find();
        penSelect = FindFirstObjectByType<PenSelect>(FindObjectsInactive.Include);
        patterns = FindFirstObjectByType<PatternManager>(FindObjectsInactive.Include);
        pathManager = FindFirstObjectByType<PathManager>(FindObjectsInactive.Include);
        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        tracingArea = FindNamed("PenTracingArea");
        HideHud();
        SetObjectiveText("HOW TO PLAY:\nUse the pencil. Hold TRIGGER on the same controller as the pencil to draw. You will try left and right.");
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
        PathManager.PatternReachedEnd += OnPathDone;
        BindTriggers();
    }

    void OnDisable()
    {
        PathManager.PatternReachedEnd -= OnPathDone;
        UnbindTriggers();
    }

    void Update()
    {
        if (!lessonOpen || GamePauseMenu.IsFrozen)
            return;

        if (step == 1)
            PollTrigger(false);
        else if (step == 2 || step == 4)
            PollTrigger(true);
        else if (drawingStep)
            WatchWrongTrigger();
    }

    void ShowIntro()
    {
        lessonOpen = false;
        step = 0;
        drawingStep = false;
        waiting = false;
        StopPassWait();
        GuidedPointer.ClearAll();
        if (lesson != null)
            lesson.Hide();
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
        if (tracingArea != null)
            tracingArea.SetActive(false);
        if (patterns != null)
            patterns.HideGuidedPath();
        if (penSelect != null)
            penSelect.HideGuidedPencil();
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
        PlaceTracingArea();
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
            lesson.ShowStep(1, StepTotal, "The pencil is on your LEFT hand. Squeeze TRIGGER on the LEFT controller.", "");
        else if (step == 2)
            lesson.ShowStep(2, StepTotal, "Now the pencil is on your RIGHT hand. Squeeze TRIGGER on the RIGHT controller.", "");
        else if (step == 3)
            lesson.ShowStep(3, StepTotal, "Keep holding trigger. Stay on the line and go through every star with your LEFT hand.", "");
        else if (step == 4)
            lesson.ShowStep(4, StepTotal, "The pencil is on your RIGHT hand. Squeeze TRIGGER on the RIGHT controller.", "");
        else
            lesson.ShowStep(5, StepTotal, "Keep holding trigger. Stay on the line and go through every star with your RIGHT hand.", "");

        if (voices != null)
            voices.PlayStep(step);
    }

    void BeginStep()
    {
        GuidedPointer.ClearAll();
        squeezeArmed = false;
        waiting = false;
        wrongVoiceArmed = false;
        drawingStep = step == 3 || step == 5;

        bool rightHand = step == 2 || step == 4 || step == 5;
        if (penSelect != null)
            penSelect.EquipGuidedPencil(rightHand);

        if (drawingStep)
        {
            if (patterns != null)
                patterns.ShowGuidedPath();
            PointAtPencil();
            return;
        }

        if (patterns != null)
            patterns.HideGuidedPath();

        PointAtPencil();
    }

    void PollTrigger(bool rightHand)
    {
        bool down = ReadTrigger(rightHand);
        bool other = ReadTrigger(!rightHand);
        if (other && !down)
            HintWrongHand();
        else if (!other)
            wrongVoiceArmed = true;

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

    void WatchWrongTrigger()
    {
        bool rightHand = step == 5;
        if (ReadTrigger(!rightHand) && !ReadTrigger(rightHand))
            HintWrongHand();
        else
            wrongVoiceArmed = true;
    }

    void HintWrongHand()
    {
        if (lesson != null)
            lesson.SetHint("Use the hand with the pencil.");

        if (!wrongVoiceArmed)
            return;

        wrongVoiceArmed = false;
        if (voices != null)
            voices.PlayWrongHand();
    }

    void OnPathDone()
    {
        if (!lessonOpen || !drawingStep || waiting)
            return;

        float score = pathManager != null ? pathManager.ScoreCurrentPattern() : 0f;
        int starsGot = patterns != null ? patterns.StarsCollected() : 0;
        int starsNeed = patterns != null ? patterns.StarsOnPattern() : 0;
        bool aligned = score >= GreatScore && (starsNeed == 0 || starsGot >= starsNeed);

        TracingTimer timer = FindFirstObjectByType<TracingTimer>(FindObjectsInactive.Include);
        PenSounds.PlayRating(aligned ? 100f : score);
        if (timer != null)
            timer.ShowPatternRating(aligned ? 100f : score);

        if (!aligned)
        {
            if (lesson != null)
                lesson.SetHint("Stay on the line. Pass through every star.");
            if (voices != null)
                voices.PlayRetry();
            if (patterns != null)
                patterns.ShowGuidedPath();
            PointAtPencil();
            return;
        }

        StartCoroutine(PassDraw());
    }

    IEnumerator PassDraw()
    {
        waiting = true;
        float waited = 0f;
        while (waited < 1.15f)
        {
            if (!GamePauseMenu.IsFrozen)
                waited += Time.deltaTime;
            yield return null;
        }

        TracingTimer timer = FindFirstObjectByType<TracingTimer>(FindObjectsInactive.Include);
        if (timer != null)
            timer.HideFeedback();

        waiting = false;
        Advance();
    }

    void StopPassWait()
    {
        StopAllCoroutines();
        waiting = false;
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

    void Finish()
    {
        lessonOpen = false;
        drawingStep = false;
        waiting = false;
        StopPassWait();
        GuidedPointer.ClearAll();
        if (lesson != null)
            lesson.Hide();
        if (patterns != null)
            patterns.HideGuidedPath();
        if (tracingArea != null)
            tracingArea.SetActive(false);
        if (penSelect != null)
            penSelect.HideGuidedPencil();
        ShowDone();
    }

    void ShowDone()
    {
        SetNamedActive("TimesUpScreen", false);
        if (resultsScreen == null)
            return;

        ResultsScreen sim = resultsScreen.GetComponent<ResultsScreen>();
        if (sim != null)
            sim.enabled = false;

        resultsScreen.SetActive(true);
        if (voices != null)
            voices.PlayDone();

        Transform panel = resultsScreen.transform.Find("ResultsPanel");
        if (panel == null)
            return;

        panel.gameObject.SetActive(true);

        TMP_Text[] labels = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string name = labels[i].gameObject.name;
            if (name.StartsWith("Value_") || name == "Text_TimesUp" || name == "Text_Accuracy")
                labels[i].gameObject.SetActive(false);
            else if (name == "Text_Done")
                labels[i].gameObject.SetActive(true);
        }
    }

    void PointAtPencil()
    {
        if (penSelect != null && penSelect.pencil != null && penSelect.pencil.activeInHierarchy)
        {
            GuidedPointer.FollowHand(penSelect.pencil.transform, "TRIGGER");
            return;
        }

        bool rightHand = step == 2 || step == 4 || step == 5;
        GuidedPointer.FollowHand(FindHand(rightHand), "TRIGGER");
    }

    void PlaceTracingArea()
    {
        if (tracingArea == null)
            return;

        Camera headset = Camera.main;
        if (headset != null)
        {
            Vector3 forward = headset.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 position = headset.transform.position + forward * 0.45f;
            position.y = headset.transform.position.y - 0.3f;
            tracingArea.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        tracingArea.SetActive(true);

        Transform board = tracingArea.transform.Find("TracingBoard");
        if (board != null && board.GetComponent<TracingBoardSurface>() == null)
            board.gameObject.AddComponent<TracingBoardSurface>();
    }

    void HideHud()
    {
        SetNamedActive("CountdownScreen", false);
        SetNamedActive("SelectPenCanvas", false);
        SetNamedActive("selectPenCanvas", false);
        if (penSelect != null)
        {
            if (penSelect.selectPenCanvas != null)
                penSelect.selectPenCanvas.SetActive(false);
            if (penSelect.selectHandCanvas != null)
                penSelect.selectHandCanvas.SetActive(false);
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == "Hand Canvas")
                transforms[i].gameObject.SetActive(false);
        }

        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.HideGameplay();

        TracingTimer timer = FindFirstObjectByType<TracingTimer>(FindObjectsInactive.Include);
        if (timer != null)
            timer.HideWristHud();

        SetNamedActive("TimesUpScreen", false);
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

    void BindTriggers()
    {
        UnbindTriggers();
        leftTrigger = MakeTrigger("{LeftHand}");
        rightTrigger = MakeTrigger("{RightHand}");
    }

    void UnbindTriggers()
    {
        DisposeAction(ref leftTrigger);
        DisposeAction(ref rightTrigger);
    }

    static InputAction MakeTrigger(string hand)
    {
        InputAction action = new InputAction(type: InputActionType.Button, binding: "<XRController>" + hand + "/{TriggerButton}");
        action.Enable();
        return action;
    }

    static void DisposeAction(ref InputAction action)
    {
        if (action == null)
            return;

        action.Disable();
        action.Dispose();
        action = null;
    }

    bool ReadTrigger(bool rightHand)
    {
        InputAction action = rightHand ? rightTrigger : leftTrigger;
        if (action != null && action.IsPressed())
            return true;

        if (action != null)
            return action.ReadValue<float>() >= SqueezeAmount;

        return false;
    }

    static Transform FindHand(bool rightHand)
    {
        Transform controller = PlayerHands.WritingController(rightHand);
        if (controller == null)
            controller = FindNamedTransform(rightHand ? "Right Controller" : "Left Controller");

        Transform model = FindHandModel(controller);
        if (model != null)
            return model;

        Transform named = FindNamedTransform(rightHand ? "Right Hand Model" : "Left Hand Model");
        return named != null ? named : controller;
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
