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

    public float timeLimit = 10f;

    private float timeRemaining;
    private float elapsed;
    private bool timerRunning = false;

    void Start()
    {
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
        timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
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