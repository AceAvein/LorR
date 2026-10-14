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

    public float timeLimit = 10f;

    private float timeRemaining;
    private float elapsed;
    private bool timerRunning = false;

    void Start()
    {
        ResolveHud();
        timeRemaining = timeLimit;
        UpdateTimerDisplay();
    }

    public void StartTimer()
    {
        gameObject.SetActive(true);
        timeRemaining = timeLimit;
        elapsed = 0f;
        timerRunning = true;

        if (pathManager != null)
            pathManager.BeginPatternClock();
    }

    void Update()
    {
        if (!timerRunning)
            return;

        timeRemaining -= Time.deltaTime;
        elapsed += Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
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

    public void TryAgain()
    {
        timesUpScreen.SetActive(false);

        patternManager.ResetPatterns();

        tracingBoard.SetActive(true);
        tracingArea.SetActive(false);

        Countdown countdown = FindObjectOfType<Countdown>();
        countdown.StartCountdown();
    }
}