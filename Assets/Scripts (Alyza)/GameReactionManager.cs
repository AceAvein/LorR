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
    public TextMeshProUGUI levelText;

    [Header("Game Settings")]
    public float gameTime = 90f;

    private int currentLevel = 1;

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
        UpdateLevelText();

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

        score += 1;

        UpdateScore();

        CheckLevelUp();

        if (niceText != null)
            StartCoroutine(ShowMessage(niceText));
    }

    public void WrongAnswer()
    {
        if (!gameRunning)
            return;

        wrongAnswers++;

        score -= 1;

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

        score -= 1;

        if (score < 0)
            score = 0;

        UpdateScore();

        if (tooSlowText != null)
            StartCoroutine(ShowMessage(tooSlowText));
    }

    void CheckLevelUp()
    {
        int newLevel = Mathf.Clamp(
            (correctAnswers / 20) + 1,
            1,
            5
        );

        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;

            UpdateLevelText();

            if (LightManager.Instance != null)
            {
                LightManager.Instance.UpdateDifficulty();
            }

            Debug.Log("LEVEL UP! Current Level: " + currentLevel);
        }
    }

    void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "LEVEL: " + currentLevel;
        }
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
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
        int totalAttempts =
            correctAnswers +
            wrongAnswers +
            tooSlowAnswers;

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

        int progress = Mathf.Clamp(xp, 0, 100);

        int currentBest =
            PlayerPrefs.GetInt("CurrentBest", 0);

        if (score > currentBest)
        {
            currentBest = score;
        }

        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.SetInt("Accuracy", accuracy);
        PlayerPrefs.SetInt("XP", xp);
        PlayerPrefs.SetInt("Progress", progress);

        PlayerPrefs.SetInt("Correct", correctAnswers);
        PlayerPrefs.SetInt("Wrong", wrongAnswers);
        PlayerPrefs.SetInt("TooSlow", tooSlowAnswers);

        PlayerPrefs.SetInt("CurrentBest", currentBest);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);

        PlayerPrefs.Save();

        Debug.Log("========== GAME FINISHED ==========");
        Debug.Log("Final Score: " + score);
        Debug.Log("Accuracy: " + accuracy + "%");
        Debug.Log("XP: " + xp);
        Debug.Log("XP Progress: " + progress + "%");
        Debug.Log("Current Best: " + currentBest);
        Debug.Log("Highest Level: " + currentLevel);
        Debug.Log("===================================");

        SceneManager.LoadScene("09.3_ReactionLight");
    }

    IEnumerator ShowMessage(TextMeshProUGUI message)
    {
        message.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        message.gameObject.SetActive(false);
    }
}