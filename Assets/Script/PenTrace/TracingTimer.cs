using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TracingTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject timesUpScreen;
    public GameObject tracingBoard;
    public GameObject tracingArea;
    public PathManager pathManager;
    public PatternManager patternManager;

    readonly List<TMP_Text> levelValues = new List<TMP_Text>();
    readonly List<TMP_Text> timerValues = new List<TMP_Text>();
    readonly List<TMP_Text> patternValues = new List<TMP_Text>();
    GameObject leftWristHud;
    GameObject rightWristHud;
    readonly List<GameObject> fairImages = new List<GameObject>();
    readonly List<GameObject> goodImages = new List<GameObject>();
    readonly List<GameObject> greatImages = new List<GameObject>();
    bool feedbackResolved;

    public float timeLimit = 10f;

    private float timeRemaining;
    private float elapsed;
    private float feedbackHideAt;
    private bool timerRunning = false;
    private bool feedbackShowing;

    public bool IsRunning
    {
        get { return timerRunning; }
    }

    void Start()
    {
        ResolveHud();
        ResolveWristHuds();
        ResolveFeedback();
        HideFeedback();
        HideResults();
        timeRemaining = timeLimit;
        UpdateTimerDisplay();
    }

    public void StartTimer()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        gameObject.SetActive(true);
        timeRemaining = timeLimit;
        elapsed = 0f;
        timerRunning = true;
        HideFeedback();
        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.ShowGameplay();
        ShowWristHud();
        LevelVoices.Reset();
        if (patternManager != null && patternManager.HasDeck)
        {
            LevelVoices.PlayTier(patternManager.CurrentTier());
            BallDropSky.ApplyCurrent(patternManager.CurrentTier());
        }

        if (pathManager != null)
            pathManager.BeginPatternClock();
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (feedbackShowing && Time.time >= feedbackHideAt)
            HideFeedback();

        if (!timerRunning)
            return;

        timeRemaining -= Time.deltaTime;
        elapsed += Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            HideFeedback();
            gameObject.SetActive(false);

            if (pathManager.finishedScreen != null)
                pathManager.finishedScreen.SetActive(false);
            ShowResults(false);
            tracingBoard.SetActive(false);
            tracingArea.SetActive(false);
        }

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));

        if (timerText != null)
            timerText.text = seconds.ToString();

        ResolveHud();

        SetTexts(timerValues, seconds.ToString());

        if (patternManager == null)
            return;

        int total = patternManager.PatternTotal();
        if (total <= 0)
            total = 30;

        int number = patternManager.CurrentPatternIndex + 1;
        if (number < 1)
            number = 1;
        if (number > total)
            number = total;

        SetTexts(patternValues, number + "/" + total);
        SetTexts(levelValues, patternManager.PatternTotal() > 0 ? TierName(patternManager.CurrentTier()) : "Easy");
    }

    public void RefreshHud()
    {
        UpdateTimerDisplay();
    }

    void ResolveHud()
    {
        if (levelValues.Count == 0)
            FindTexts("Level Value", levelValues);
        if (timerValues.Count == 0)
            FindTexts("Timer Value", timerValues);
        if (patternValues.Count == 0)
            FindTexts("Patterns Value", patternValues);
    }

    public void HideWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(false);
        if (rightWristHud != null)
            rightWristHud.SetActive(false);
    }

    public void ShowWristHud()
    {
        ResolveWristHuds();
        PenSelect select = FindFirstObjectByType<PenSelect>(FindObjectsInactive.Include);
        bool rightHand = select != null && select.WritingWithRightHand;
        if (leftWristHud != null)
            leftWristHud.SetActive(!rightHand);
        if (rightWristHud != null)
            rightWristHud.SetActive(rightHand);
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

            if (canvasName == "Left Hand Canvas" || IsUnder(transforms[i], "Left Controller"))
                leftWristHud = transforms[i].gameObject;
            else if (canvasName == "Right Hand Canvas" || IsUnder(transforms[i], "Right Controller"))
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

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void AddSeconds(float amount)
    {
        if (!timerRunning)
            return;

        timeRemaining += amount;
        UpdateTimerDisplay();
    }

    public void CompleteGame()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        if (!timerRunning)
            return;

        timerRunning = false;
        HideFeedback();
        if (pathManager.finishedScreen != null)
            pathManager.finishedScreen.SetActive(false);
        ShowResults(true);
        tracingBoard.SetActive(false);
        tracingArea.SetActive(false);
    }

    void ShowResults(bool deckCleared)
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        if (pathManager == null)
            return;

        GameObject screen = FindResultsScreen();
        if (screen == null)
            screen = timesUpScreen;
        if (screen == null)
            return;

        if (timesUpScreen != null && timesUpScreen != screen)
            timesUpScreen.SetActive(false);

        screen.SetActive(true);
        ResultsScreen results = screen.GetComponent<ResultsScreen>();
        if (results == null)
            results = screen.AddComponent<ResultsScreen>();

        results.Show(pathManager.BuildReport(elapsed, timeRemaining, deckCleared));
    }

    void HideResults()
    {
        GameObject screen = FindResultsScreen();
        if (screen != null)
            screen.SetActive(false);
        if (timesUpScreen != null)
            timesUpScreen.SetActive(false);
    }

    static GameObject FindResultsScreen()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == "ResultsScreen")
                return transforms[i].gameObject;
        }

        return null;
    }

    public void ShowPatternRating(float accuracy)
    {
        ResolveFeedback();

        ShowGroup(fairImages, accuracy < 60f);
        ShowGroup(goodImages, accuracy >= 60f && accuracy < 85f);
        ShowGroup(greatImages, accuracy >= 85f);

        feedbackHideAt = Time.time + 1f;
        feedbackShowing = true;
    }

    static void ShowGroup(List<GameObject> images, bool show)
    {
        for (int i = 0; i < images.Count; i++)
            ShowOne(images[i], show);
    }

    static void ShowOne(GameObject image, bool show)
    {
        if (image == null)
            return;

        if (!show)
        {
            image.SetActive(false);
            return;
        }

        PatternRatingPopup popup = image.GetComponent<PatternRatingPopup>();
        if (popup == null)
            popup = image.AddComponent<PatternRatingPopup>();

        image.SetActive(true);
        popup.Restart();
    }

    void ResolveFeedback()
    {
        if (feedbackResolved)
            return;

        feedbackResolved = true;
        FindNamed("Fair", fairImages);
        FindNamed("Good", goodImages);
        FindNamed("Great", greatImages);
    }

    static void FindNamed(string objectName, List<GameObject> results)
    {
        results.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                results.Add(transforms[i].gameObject);
        }
    }

    public void HideFeedback()
    {
        feedbackShowing = false;
        ResolveFeedback();
        ShowGroup(fairImages, false);
        ShowGroup(goodImages, false);
        ShowGroup(greatImages, false);
    }

    public void TryAgain()
    {
        HideFeedback();
        HideWristHud();
        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.HideGameplay();
        HideResults();

        patternManager.ResetPatterns();

        tracingBoard.SetActive(true);
        tracingArea.SetActive(false);

        Countdown countdown = FindObjectOfType<Countdown>();
        countdown.StartCountdown();
    }
}