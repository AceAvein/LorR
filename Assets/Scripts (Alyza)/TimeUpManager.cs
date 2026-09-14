using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimeUpManager : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI currentBestText;
    public TextMeshProUGUI highestLevelText;

    [Header("XP")]
    public Slider xpSlider;

    void Start()
    {
        int finalScore =
            PlayerPrefs.GetInt("FinalScore", 0);

        int currentBest =
            PlayerPrefs.GetInt("CurrentBest", 0);

        int progress =
            PlayerPrefs.GetInt("Progress", 0);

        int currentLevel =
            PlayerPrefs.GetInt("CurrentLevel", 1);

        // Progress text
        if (progressText != null)
        {
            progressText.text =
                "PROGRESS: " + finalScore + "%";
        }

        // Current best
        if (currentBestText != null)
        {
            currentBestText.text =
                "CURRENT BEST: " + currentBest;
        }

        // Highest Level
        if (highestLevelText != null)
        {
            highestLevelText.text =
                "HIGHEST LEVEL: " + currentLevel;
        }

        // XP BAR
        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 100f;

            xpSlider.value = progress;

            Debug.Log(
                "TIME UP XP PROGRESS: " + progress
            );
        }
        else
        {
            Debug.LogError(
                "XP Slider is NOT assigned!"
            );
        }
    }

    public void Next()
    {
        SceneManager.LoadScene("09.4_ReactionLight");
    }
}