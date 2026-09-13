using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameReactionManager : MonoBehaviour
{
    public static GameReactionManager Instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI niceText;
    public TextMeshProUGUI missText;
    public TextMeshProUGUI tooSlowText;

    [Header("Game Settings")]
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

        if (niceText != null)
            niceText.gameObject.SetActive(false);

        if (missText != null)
            missText.gameObject.SetActive(false);

        if (tooSlowText != null)
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

        if (timerText != null)
        {
            timerText.text =
                string.Format("{0}:{1:00}", minutes, seconds);
        }
    }

    public void CorrectAnswer()
    {
        if (!gameRunning)
            return;

        correctAnswers++;

        // Correct answer = +5 score / XP
        score += 5;

        UpdateScore();

        if (niceText != null)
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

        if (missText != null)
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

        if (tooSlowText != null)
            StartCoroutine(ShowMessage(tooSlowText));
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score;
        }
    }

    void EndGame()
    {
        // Calculate total attempts
        int totalAttempts =
            correctAnswers +
            wrongAnswers +
            tooSlowAnswers;

        // Calculate accuracy
        int accuracy = 0;

        if (totalAttempts > 0)
        {
            accuracy = Mathf.RoundToInt(
                ((float)correctAnswers / totalAttempts) * 100f
            );
        }

        // XP is based on the final score
        int xp = score;

        if (xp < 0)
            xp = 0;

        // XP progress is limited to 100
        int progress = Mathf.Clamp(xp, 0, 100);

        // Get previous best
        int currentBest =
            PlayerPrefs.GetInt("CurrentBest", 0);

        // Update best score
        if (score > currentBest)
        {
            currentBest = score;
        }

        // SAVE ALL GAME RESULTS
        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.SetInt("Accuracy", accuracy);

        // Raw XP
        PlayerPrefs.SetInt("XP", xp);

        // XP BAR VALUE: 0-100
        PlayerPrefs.SetInt("Progress", progress);

        PlayerPrefs.SetInt("Correct", correctAnswers);
        PlayerPrefs.SetInt("Wrong", wrongAnswers);
        PlayerPrefs.SetInt("TooSlow", tooSlowAnswers);

        PlayerPrefs.SetInt("CurrentBest", currentBest);

        // Actually save PlayerPrefs
        PlayerPrefs.Save();

        // DEBUG
        Debug.Log("========== GAME FINISHED ==========");
        Debug.Log("Final Score: " + score);
        Debug.Log("Accuracy: " + accuracy + "%");
        Debug.Log("XP: " + xp);
        Debug.Log("XP Progress: " + progress + "%");
        Debug.Log("Current Best: " + currentBest);
        Debug.Log("===================================");

        // Go to Times Up scene
        SceneManager.LoadScene("09.3_ReactionLight");
    }

    IEnumerator ShowMessage(TextMeshProUGUI message)
    {
        message.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        message.gameObject.SetActive(false);
    }
}