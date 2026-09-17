using UnityEngine;
using TMPro;

public class TracingTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject timesUpScreen;
    public GameObject tracingBoard;
    public GameObject tracingArea;
    public PathManager pathManager;

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

            pathManager.averageAccuracyText.text =
            "Average Tracing Accuracy: " + pathManager.GetAccuracy().ToString("F1") + "%";

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

    public void TryAgain()
    {
        timesUpScreen.SetActive(false);
        tracingBoard.SetActive(true);
        tracingArea.SetActive(true);

        StartTimer();
    }
}