using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionUI : MonoBehaviour
{
    [Header("Existing UI")]
    public TMP_Text accuracyText;
    public TMP_Text rankText;

    [Header("XP UI")]
    public Slider xpSlider;

    [Header("XP Settings")]
    public float maxXP = 500f;

    public void Populate(float accuracy)
    {
        // Accuracy
        accuracyText.text = $"ACCURACY: {accuracy:0}%";

        // Rank
        rankText.text = $"RANK: {RankCalculator.GetRank(accuracy)}";

        // Calculate XP
        float xp = (accuracy / 100f) * maxXP;

        // Update XP Bar
        if (xpSlider != null)
        {
            xpSlider.maxValue = maxXP;
            xpSlider.value = xp;
        }
    }
}