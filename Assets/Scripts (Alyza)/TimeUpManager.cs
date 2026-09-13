using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimeUpManager : MonoBehaviour
{
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI currentBestText;
    public TextMeshProUGUI xpText;

    void Start()
    {
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int currentBest = PlayerPrefs.GetInt("CurrentBest", 0);
        int xp = PlayerPrefs.GetInt("XP", 0);

        progressText.text = "PROGRESS: " + finalScore;
        currentBestText.text = "CURRENT BEST: " + currentBest;
        xpText.text = "XP: " + xp;
    }

    public void Next()
    {
        SceneManager.LoadScene("09.4_ReactionLight");
    }
}