using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameReactionManager : MonoBehaviour
{
    public static GameReactionManager Instance;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI niceText;
    public TextMeshProUGUI missText;
    public TextMeshProUGUI tooSlowText;

    public float gameTime = 90f;

    private int score = 0;
    private int correctAnswers = 0;
    private int wrongAnswers = 0;
    private int tooSlowAnswers = 0;

    private bool gameRunning = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateScore();

        niceText.gameObject.SetActive(false);
        missText.gameObject.SetActive(false);
        tooSlowText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameRunning)
            return;

        gameTime -= Time.deltaTime;

        if (gameTime <= 0)
        {
            gameTime = 0;
            gameRunning = false;

            UpdateTimer();
            EndGame();
            return;
        }

        UpdateTimer();
    }

    void UpdateTimer()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);

        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }

    public void CorrectAnswer()
    {
        if (!gameRunning)
            return;

        correctAnswers++;
        score += 5;

        UpdateScore();
        StartCoroutine(ShowMessage(niceText));
    }

    public void WrongAnswer()
    {
        if (!gameRunning)
            return;

        wrongAnswers++;
        score -= 5;

        if (score < 0)
            score = 0;

        UpdateScore();
        StartCoroutine(ShowMessage(missText));
    }

    public void TooSlow()
    {
        if (!gameRunning)
            return;

        tooSlowAnswers++;
        score -= 5;

        if (score < 0)
            score = 0;

        UpdateScore();
        StartCoroutine(ShowMessage(tooSlowText));
    }

    void UpdateScore()
    {
        scoreText.text = "SCORE: " + score;
    }

    void EndGame()
    {
        int totalAttempts = correctAnswers + wrongAnswers + tooSlowAnswers;

        int accuracy = 0;

        if (totalAttempts > 0)
        {
            accuracy = Mathf.RoundToInt(
                ((float)correctAnswers / totalAttempts) * 100f
            );
        }

        int xp = score;

        if (xp < 0)
            xp = 0;

        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.SetInt("Accuracy", accuracy);
        PlayerPrefs.SetInt("XP", xp);
        PlayerPrefs.SetInt("Correct", correctAnswers);
        PlayerPrefs.SetInt("Wrong", wrongAnswers);
        PlayerPrefs.SetInt("TooSlow", tooSlowAnswers);

        // Save best score
        int currentBest = PlayerPrefs.GetInt("CurrentBest", 0);

        if (score > currentBest)
        {
            currentBest = score;
            PlayerPrefs.SetInt("CurrentBest", currentBest);
        }

        // Progress toward 100 XP
        int progress = xp;

        if (progress > 100)
            progress = 100;

        PlayerPrefs.SetInt("Progress", progress);

        PlayerPrefs.Save();

        SceneManager.LoadScene("09.3_ReactionLight");
    }

    IEnumerator ShowMessage(TextMeshProUGUI message)
    {
        message.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        message.gameObject.SetActive(false);
    }
}