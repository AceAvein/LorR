using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ProgressionManager : MonoBehaviour
{
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI reactionSpeedText;
    public TextMeshProUGUI performanceText;

    void Start()
    {
        int accuracy = PlayerPrefs.GetInt("Accuracy", 0);
        int correct = PlayerPrefs.GetInt("Correct", 0);
        int wrong = PlayerPrefs.GetInt("Wrong", 0);
        int tooSlow = PlayerPrefs.GetInt("TooSlow", 0);

        accuracyText.text = "ACCURACY:\n" + accuracy + "%";

        rankText.text = "RANK:\n" + GetRank(accuracy);

        reactionSpeedText.text =
            "REACTION SPEED:\n" + GetReactionSpeed(accuracy, tooSlow);

        performanceText.text =
            "PERFORMANCE:\n" + GetPerformance(accuracy, correct, wrong);
    }

    string GetRank(int accuracy)
    {
        if (accuracy >= 90)
            return "S";

        if (accuracy >= 80)
            return "A";

        if (accuracy >= 70)
            return "B";

        if (accuracy >= 60)
            return "C";

        return "D";
    }

    string GetReactionSpeed(int accuracy, int tooSlow)
    {
        if (tooSlow <= 2 && accuracy >= 80)
            return "EXCELLENT";

        if (tooSlow <= 5 && accuracy >= 60)
            return "GOOD";

        return "NEEDS PRACTICE";
    }

    string GetPerformance(int accuracy, int correct, int wrong)
    {
        if (accuracy >= 90)
            return "EXCELLENT";

        if (accuracy >= 75)
            return "GOOD";

        if (accuracy >= 60)
            return "AVERAGE";

        return "NEEDS PRACTICE";
    }

    public void TryAgain()
    {
        SceneManager.LoadScene("09.1_ReactionLight");
    }

    public void Back()
    {
        SceneManager.LoadScene("03_ModeSelection");
    }
}