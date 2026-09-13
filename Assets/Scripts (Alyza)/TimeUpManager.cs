using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimeUpManager : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI currentBestText;

    [Header("XP")]
    public Slider xpSlider;

    [Header("XP Settings")]
    public float maxXP = 100f;

    void Start()
    {
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int currentBest = PlayerPrefs.GetInt("CurrentBest", 0);
        int xp = PlayerPrefs.GetInt("XP", 0);

        // Progress
        if (progressText != null)
        {
            progressText.text = "PROGRESS: " + finalScore;
        }

        // Current Best
        if (currentBestText != null)
        {
            currentBestText.text = "CURRENT BEST: " + currentBest;
        }

        // XP Bar
        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = maxXP;

            xpSlider.value = Mathf.Clamp(xp, 0, (int)maxXP);

            Debug.Log("XP: " + xp);
            Debug.Log("XP BAR VALUE: " + xpSlider.value);
        }
    }

    public void Next()
    {
        SceneManager.LoadScene("09.4_ReactionLight");
    }
}