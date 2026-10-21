using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuidedReactionLightLesson : MonoBehaviour
{
    const int StepTotal = 8;
    const int Columns = 4;
    const int Rows = 3;
    const float GripAmount = 0.55f;
    const float TriggerMax = 0.35f;
    const float TrapSoundGap = 0.9f;

    enum LightKind
    {
        Red,
        Green,
        Yellow
    }

    class Target
    {
        public Transform transform;
        public Material material;
        public LightKind kind;
        public float radius;
    }

    static readonly Color Red = new Color(0.85f, 0.08f, 0.08f);
    static readonly Color Green = new Color(0.1f, 0.9f, 0.2f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);
    static readonly int[] YellowSlots = { 1, 4, 10 };

    GuidedLesson lesson;
    GuidedReactionLightVoices voices;
    GameObject objectiveScreen;
    GameObject resultsScreen;
    readonly List<Target> balls = new List<Target>();
    readonly HashSet<Target> touchingLeft = new HashSet<Target>();
    readonly HashSet<Target> touchingRight = new HashSet<Target>();
    InputAction leftGrip;
    InputAction leftTrigger;
    InputAction rightGrip;
    InputAction rightTrigger;
    int step;
    int greensGot;
    int greenIndex = -1;
    bool lessonOpen;
    bool squeezeArmed;
    bool hintVoiceArmed;
    float wordHideAt;
    float trapSoundAt;

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
        if (!scene.IsValid() || scene.name != "Guided Reaction Light")
            return;

        if (FindAnyObjectByType<GuidedReactionLightLesson>() != null)
            return;

        GameObject host = new GameObject("GuidedLesson");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GuidedReactionLightLesson>();
    }

    void Awake()
    {
        lesson = GetComponent<GuidedLesson>();
        if (lesson == null)
            lesson = gameObject.AddComponent<GuidedLesson>();

        voices = GuidedReactionLightVoices.Find();
        objectiveScreen = FindNamed("ObjectiveScreen");
        resultsScreen = FindNamed("ResultsScreen");
        FindBalls();
        HideHud();
        SetObjectiveText("HOW TO PLAY:\nSqueeze GRAB and leave TRIGGER. Poke GREEN only. Avoid yellow and red. Practice with both hands.");
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
        BindHands();
    }

    void OnDisable()
    {
        UnbindHands();
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        HideWordIfDue();
        if (!lessonOpen)
            return;

        if (IsPoseStep())
            PollPose(UseRightHand());
        else
            PollPokes(UseRightHand());
    }

    void ShowIntro()
    {
        lessonOpen = false;
        step = 0;
        greensGot = 0;
        GuidedPointer.ClearAll();
        HideWord();
        SetBallsVisible(false);
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

        string hand = UseRightHand() ? "RIGHT" : "LEFT";
        if (IsPoseStep())
        {
            lesson.ShowStep(step, StepTotal, "Squeeze GRAB on your " + hand + " controller. Do not squeeze TRIGGER.", "");
            return;
        }

        int need = GreensNeeded();
        int next = greensGot + 1;
        if (next > need)
            next = need;

        if (HasYellows() && TeachesRed())
            lesson.ShowStep(step, StepTotal, "Avoid RED and yellow. Poke GREEN with your " + hand + " finger. " + next + " of " + need + ".", "Only green.");
        else if (HasYellows())
            lesson.ShowStep(step, StepTotal, "Yellow lights are traps. Avoid yellow. Poke GREEN with your " + hand + " finger. " + next + " of " + need + ".", "Do not poke yellow.");
        else
            lesson.ShowStep(step, StepTotal, "Poke the GREEN light with your " + hand + " finger. " + next + " of " + need + ".", "");
    }

    void BeginStep()
    {
        GuidedPointer.ClearAll();
        squeezeArmed = false;
        hintVoiceArmed = false;
        greensGot = 0;
        greenIndex = -1;
        touchingLeft.Clear();
        touchingRight.Clear();

        if (IsPoseStep())
        {
            SetBallsVisible(false);
            GuidedPointer.FollowHand(FindHand(UseRightHand()), "GRAB");
            if (voices != null)
                voices.PlayStep(step);
            return;
        }

        bool boardOut = balls.Count > 0 && balls[0].transform != null && balls[0].transform.gameObject.activeSelf;
        if (!boardOut)
            PlaceBalls();

        SetBallsVisible(true);
        PaintBoard();
        PointAtGreen();
        MarkCurrentHits();
        if (voices != null)
            voices.PlayStep(step);
    }

    void PaintBoard()
    {
        PaintAll(LightKind.Red);
        if (HasYellows())
        {
            for (int i = 0; i < YellowSlots.Length; i++)
            {
                Target yellow = BallAt(YellowSlots[i]);
                if (yellow != null)
                    Paint(yellow, LightKind.Yellow);
            }
        }

        MoveGreen();
    }

    void MoveGreen()
    {
        Target old = GreenBall();
        if (old != null)
            Paint(old, IsYellowSlot(IndexOf(old)) && HasYellows() ? LightKind.Yellow : LightKind.Red);

        int next = NextGreenIndex();
        Target green = BallAt(next);
        if (green == null)
            return;

        greenIndex = next;
        Paint(green, LightKind.Green);
        PointAtGreen();
        MarkCurrentHits();
    }

    int NextGreenIndex()
    {
        int count = balls.Count;
        if (count <= 0)
            return 0;

        int start = greenIndex < 0 ? 0 : greenIndex + 1;
        for (int n = 0; n < count; n++)
        {
            int index = (start + n) % count;
            if (index == greenIndex)
                continue;
            if (HasYellows() && IsYellowSlot(index))
                continue;
            return index;
        }

        return 0;
    }

    void PointAtGreen()
    {
        GuidedPointer.ClearAll();
        Target green = GreenBall();
        if (green != null)
            GuidedPointer.Follow(green.transform, "GREEN", 0.12f);
    }

    void PollPose(bool rightHand)
    {
        bool posed = IsPoking(rightHand);
        bool other = IsPoking(!rightHand);
        if (other && !posed)
            HintUseHand(rightHand);
        else if (!other)
            hintVoiceArmed = true;

        if (!posed)
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

    void PollPokes(bool rightHand)
    {
        ReadPokeHand(rightHand, true);
        ReadPokeHand(!rightHand, false);
        HintIfTouchingWithoutPoke(rightHand);
    }

    void HintIfTouchingWithoutPoke(bool rightHand)
    {
        if (lesson == null)
            return;

        if (IsPoking(rightHand) || !NearAnyBall(rightHand))
        {
            if (!NearAnyBall(rightHand))
                hintVoiceArmed = true;
            return;
        }

        lesson.SetHint("Poke. Squeeze GRAB and leave TRIGGER.");
        if (!hintVoiceArmed)
            return;

        hintVoiceArmed = false;
        if (voices != null)
            voices.PlayNeedPoke();
    }

    bool NearAnyBall(bool rightHand)
    {
        Transform tip = FindFingerTip(rightHand);
        Transform hand = FindHand(rightHand);
        for (int i = 0; i < balls.Count; i++)
        {
            Target ball = balls[i];
            if (ball.transform == null)
                continue;

            float reach = ball.radius + 0.06f;
            if (tip != null && Vector3.Distance(tip.position, ball.transform.position) <= reach)
                return true;
            if (hand != null && Vector3.Distance(hand.position, ball.transform.position) <= reach)
                return true;
        }

        return false;
    }

    void ReadPokeHand(bool rightHand, bool allowed)
    {
        HashSet<Target> touching = rightHand ? touchingRight : touchingLeft;
        Transform tip = FindFingerTip(rightHand);
        bool poking = tip != null && IsPoking(rightHand);
        Vector3 point = poking ? tip.position : Vector3.zero;

        for (int i = 0; i < balls.Count; i++)
        {
            Target ball = balls[i];
            bool hit = poking && Vector3.Distance(point, ball.transform.position) <= ball.radius + 0.025f;
            if (!hit)
            {
                touching.Remove(ball);
                continue;
            }

            if (!touching.Add(ball))
                continue;

            OnPoke(ball, allowed, rightHand);
        }
    }

    void MarkCurrentHits()
    {
        MarkHitsForHand(false);
        MarkHitsForHand(true);
    }

    void MarkHitsForHand(bool rightHand)
    {
        HashSet<Target> touching = rightHand ? touchingRight : touchingLeft;
        Transform tip = FindFingerTip(rightHand);
        bool poking = tip != null && IsPoking(rightHand);
        Vector3 point = poking ? tip.position : Vector3.zero;

        for (int i = 0; i < balls.Count; i++)
        {
            Target ball = balls[i];
            bool hit = poking && Vector3.Distance(point, ball.transform.position) <= ball.radius + 0.025f;
            if (hit)
                touching.Add(ball);
            else
                touching.Remove(ball);
        }
    }

    void OnPoke(Target ball, bool allowed, bool rightHand)
    {
        if (ball.kind == LightKind.Yellow)
        {
            PlayTrap("Oops", "Avoid yellow. Poke only GREEN.");
            return;
        }

        if (ball.kind == LightKind.Red)
        {
            PlayTrap("Wrong", "Avoid red. Poke only GREEN.");
            return;
        }

        if (!allowed)
        {
            HintUseHand(!rightHand);
            return;
        }

        ShowWord("Great");
        BallDropSounds.PlayCatch();
        touchingLeft.Add(ball);
        touchingRight.Add(ball);
        greensGot++;
        if (greensGot >= GreensNeeded())
        {
            Advance();
            return;
        }

        ShowCurrentStep();
        MoveGreen();
    }

    void PlayTrap(string word, string hint)
    {
        ShowWord(word);
        if (lesson != null)
            lesson.SetHint(hint);

        if (Time.unscaledTime < trapSoundAt)
            return;

        trapSoundAt = Time.unscaledTime + TrapSoundGap;
        if (word == "Oops")
        {
            ReactionLightSounds.PlayOops();
            if (voices != null)
                voices.PlayAvoidYellow();
        }
        else
        {
            BallDropSounds.PlayMiss();
            if (voices != null)
                voices.PlayAvoidRed();
        }
    }

    void HintUseHand(bool rightHand)
    {
        if (lesson != null)
            lesson.SetHint("Use your " + (rightHand ? "RIGHT" : "LEFT") + " hand.");

        if (!hintVoiceArmed)
            return;

        hintVoiceArmed = false;
        if (voices != null)
            voices.PlayUseHand(rightHand);
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
        GuidedPointer.ClearAll();
        HideWord();
        SetBallsVisible(false);
        if (lesson != null)
            lesson.Hide();
        ShowDone();
    }

    void ShowDone()
    {
        if (resultsScreen == null)
            return;

        ReactionLightResults sim = resultsScreen.GetComponent<ReactionLightResults>();
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
            if (name.StartsWith("Value_"))
                labels[i].gameObject.SetActive(false);
            else if (name == "Text_Done")
                labels[i].gameObject.SetActive(true);
        }
    }

    bool IsPoseStep()
    {
        return step == 1 || step == 5;
    }

    bool UseRightHand()
    {
        return step >= 5;
    }

    bool HasYellows()
    {
        return step == 3 || step == 4 || step == 7 || step == 8;
    }

    bool TeachesRed()
    {
        return step == 4 || step == 8;
    }

    int GreensNeeded()
    {
        return HasYellows() ? 2 : 3;
    }

    static bool IsYellowSlot(int index)
    {
        for (int i = 0; i < YellowSlots.Length; i++)
        {
            if (YellowSlots[i] == index)
                return true;
        }

        return false;
    }

    int IndexOf(Target ball)
    {
        return balls.IndexOf(ball);
    }

    void HideHud()
    {
        SetNamedActive("CountdownScreen", false);
        SetNamedActive("SelectHand", false);
        SetNamedActive("selectHandCanvas", false);
        SetNamedActive("TimesUpScreen", false);

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

    void BindHands()
    {
        UnbindHands();
        leftGrip = MakeAxis("{LeftHand}/grip");
        leftTrigger = MakeAxis("{LeftHand}/trigger");
        rightGrip = MakeAxis("{RightHand}/grip");
        rightTrigger = MakeAxis("{RightHand}/trigger");
    }

    void UnbindHands()
    {
        DisposeAction(ref leftGrip);
        DisposeAction(ref leftTrigger);
        DisposeAction(ref rightGrip);
        DisposeAction(ref rightTrigger);
    }

    static InputAction MakeAxis(string path)
    {
        InputAction action = new InputAction(type: InputActionType.Value, binding: "<XRController>" + path);
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

    bool IsPoking(bool rightHand)
    {
        InputAction grip = rightHand ? rightGrip : leftGrip;
        InputAction trigger = rightHand ? rightTrigger : leftTrigger;
        if (grip == null || trigger == null)
            return false;

        return grip.ReadValue<float>() >= GripAmount && trigger.ReadValue<float>() <= TriggerMax;
    }

    void FindBalls()
    {
        balls.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

    Target GreenBall()
    {
        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i].kind == LightKind.Green)
                return balls[i];
        }

        return null;
    }

    Target BallAt(int index)
    {
        if (balls.Count == 0)
            return null;

        if (index < 0 || index >= balls.Count)
            return balls[0];

        return balls[index];
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

    static Transform FindFingerTip(bool rightHand)
    {
        Transform controller = PlayerHands.WritingController(rightHand);
        if (controller == null)
            controller = FindNamedTransform(rightHand ? "Right Controller" : "Left Controller");
        if (controller == null)
            return null;

        Transform tip = null;
        Transform[] children = controller.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Contains("index3"))
                tip = children[i];
        }

        return tip != null ? tip : FindHand(rightHand);
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
