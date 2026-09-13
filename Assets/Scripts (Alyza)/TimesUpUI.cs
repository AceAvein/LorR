using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimesUpUI : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text progressText;
    public TMP_Text currentBestText;

    [Header("XP")]
    public Slider xpSlider;

    public void Populate(
        float completionPercent,
        float bestAccuracySoFar,
        float xpProgress01)
    {
        if (progressText != null)
        {
            progressText.text =
                $"PROGRESS: {completionPercent:0}%";
        }

        if (currentBestText != null)
        {
            currentBestText.text =
                $"CURRENT BEST: {bestAccuracySoFar:0}%";
        }

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;

            xpSlider.value = Mathf.Clamp01(xpProgress01);
        }
    }
}