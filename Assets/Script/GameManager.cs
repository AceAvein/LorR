using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject objectiveScreen, countdownScreen, hudScreen, timesUpScreen, progressionScreen;

    [Header("Screen Scripts")]
    public CountdownUItrace countdownUI;
    public TimesUpUItrace timesUpUI;
    public ProgressionUItrace progressionUI;

    [Header("Tracing")]
    public TracingController tracingController;
    public TracingPath[] levelPatterns;
    public GameObject tracingSurface; // the 3D panel + pen + path — only shown during actual tracing

    private int currentPatternIndex = 0;
    private float bestAccuracySoFar = 0f;

    void Start()
    {
        ShowOnly(objectiveScreen);
        if (tracingSurface != null) tracingSurface.SetActive(false);
    }

    public void OnStartPressed()
    {
        ShowOnly(countdownScreen);
        countdownUI.OnCountdownFinished = BeginTracing;
        countdownUI.StartCountdown();
    }

    void BeginTracing()
    {
        if (tracingSurface != null) tracingSurface.SetActive(true);
        ShowOnly(hudScreen);
        tracingController.OnPatternComplete = OnPatternFinished;
        tracingController.OnTimeUp = OnPatternFinished;
        tracingController.BeginPattern(levelPatterns[currentPatternIndex]);
    }

    void OnPatternFinished()
    {
        if (tracingSurface != null) tracingSurface.SetActive(false);

        float completion = tracingController.GetCompletionPercent();
        float accuracy = tracingController.accuracyScore;
        if (accuracy > bestAccuracySoFar) bestAccuracySoFar = accuracy;

        ShowOnly(timesUpScreen);
        timesUpUI.Populate(completion, bestAccuracySoFar, accuracy / 100f);
    }

    public void OnContinuePressed()
    {
        currentPatternIndex++;
        if (currentPatternIndex < levelPatterns.Length)
        {
            ShowOnly(countdownScreen);
            countdownUI.OnCountdownFinished = BeginTracing;
            countdownUI.StartCountdown();
        }
        else
        {
            ShowOnly(progressionScreen);
            progressionUI.Populate(tracingController.accuracyScore);
        }
    }

    public void OnRetryPressed()
    {
        currentPatternIndex = 0;
        bestAccuracySoFar = 0f;
        if (tracingSurface != null) tracingSurface.SetActive(false);
        ShowOnly(objectiveScreen);
    }

    public void OnBackToMainMenu()
    {
        SceneManager.LoadScene("02_MainMenu");
    }

    void ShowOnly(GameObject active)
    {
        objectiveScreen.SetActive(active == objectiveScreen);
        countdownScreen.SetActive(active == countdownScreen);
        hudScreen.SetActive(active == hudScreen);
        timesUpScreen.SetActive(active == timesUpScreen);
        progressionScreen.SetActive(active == progressionScreen);
    }
}