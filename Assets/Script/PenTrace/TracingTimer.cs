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
        timerRunning = true;
    }

    void Update()
    {
        if (!timerRunning)
            return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            gameObject.SetActive(false);

            pathManager.finishedScreen.SetActive(false);

            pathManager.averageAccuracyText.text =
            "Average Tracing Accuracy: " + pathManager.GetAccuracy().ToString("F1") + "%";

            SetEndTitle("Time's Up!");
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
        pathManager.averageAccuracyText.text =
            "Average Tracing Accuracy: " + pathManager.GetAccuracy().ToString("F1") + "%";

        SetEndTitle("Game Complete!");
        timesUpScreen.SetActive(true);
        tracingBoard.SetActive(false);
        tracingArea.SetActive(false);
    }

    void SetEndTitle(string message)
    {
        if (timesUpScreen == null)
            return;

        TMP_Text[] labels = timesUpScreen.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].text == "Time's Up!" || labels[i].text == "Game Complete!")
            {
                labels[i].text = message;
                return;
            }
        }
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