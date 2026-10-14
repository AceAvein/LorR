using UnityEngine;
using TMPro;

public class TracingTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject timesUpScreen;
    public GameObject tracingBoard;
    public GameObject tracingArea;
    public PathManager pathManager;
    public PatternManager patternManager;

    TMP_Text levelValue;
    TMP_Text timerValue;
    TMP_Text patternsValue;
    GameObject fairImage;
    GameObject goodImage;
    GameObject greatImage;

    public float timeLimit = 10f;

    private float timeRemaining;
    private float elapsed;
    private float feedbackHideAt;
    private bool timerRunning = false;
    private bool feedbackShowing;

    void Start()
    {
        ResolveHud();
        ResolveFeedback();
        HideFeedback();
        timeRemaining = timeLimit;
        UpdateTimerDisplay();
    }

    public void StartTimer()
    {
        gameObject.SetActive(true);
        timeRemaining = timeLimit;
        elapsed = 0f;
        timerRunning = true;
        HideFeedback();

        if (pathManager != null)
            pathManager.BeginPatternClock();
    }

    void Update()
    {
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

            pathManager.finishedScreen.SetActive(false);
            ShowResults(false);
            timesUpScreen.SetActive(true);
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

        if (timerValue != null)
            timerValue.text = seconds.ToString();

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

        if (patternsValue != null)
            patternsValue.text = number + "/" + total;

        if (levelValue != null)
            levelValue.text = patternManager.PatternTotal() > 0 ? TierName(patternManager.CurrentTier()) : "Easy";
    }

    public void RefreshHud()
    {
        UpdateTimerDisplay();
    }

    void ResolveHud()
    {
        if (levelValue == null)
            levelValue = FindText("Level Value");
        if (timerValue == null)
            timerValue = FindText("Timer Value");
        if (patternsValue == null)
            patternsValue = FindText("Patterns Value");
    }

    static string TierName(int tier)
    {
        if (tier <= 0)
            return "Easy";
        if (tier == 1)
            return "Moderate";
        return "Hard";
    }

    static TMP_Text FindText(string objectName)
    {
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
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
        if (!timerRunning)
            return;

        timerRunning = false;
        HideFeedback();
        pathManager.finishedScreen.SetActive(false);
        ShowResults(true);
        timesUpScreen.SetActive(true);
        tracingBoard.SetActive(false);
        tracingArea.SetActive(false);
    }

    void ShowResults(bool deckCleared)
    {
        if (timesUpScreen == null || pathManager == null)
            return;

        ResultsScreen results = timesUpScreen.GetComponent<ResultsScreen>();
        if (results == null)
            results = timesUpScreen.AddComponent<ResultsScreen>();

        results.Show(pathManager.BuildReport(elapsed, timeRemaining, deckCleared));
    }

    public void ShowPatternRating(float accuracy)
    {
        ResolveFeedback();

        ShowOne(fairImage, accuracy < 60f);
        ShowOne(goodImage, accuracy >= 60f && accuracy < 85f);
        ShowOne(greatImage, accuracy >= 85f);

        feedbackHideAt = Time.time + 1f;
        feedbackShowing = true;
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
        if (fairImage == null)
            fairImage = FindNamed("Fair");
        if (goodImage == null)
            goodImage = FindNamed("Good");
        if (greatImage == null)
            greatImage = FindNamed("Great");
    }

    void HideFeedback()
    {
        feedbackShowing = false;
        if (fairImage != null)
            fairImage.SetActive(false);
        if (goodImage != null)
            goodImage.SetActive(false);
        if (greatImage != null)
            greatImage.SetActive(false);
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

    public void TryAgain()
    {
        HideFeedback();
        timesUpScreen.SetActive(false);

        patternManager.ResetPatterns();

        tracingBoard.SetActive(true);
        tracingArea.SetActive(false);

        Countdown countdown = FindObjectOfType<Countdown>();
        countdown.StartCountdown();
    }
}