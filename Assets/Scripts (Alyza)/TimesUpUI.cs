using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimesUpUI : MonoBehaviour
{
    public TMP_Text progressText;
    public TMP_Text currentBestText;
    public Slider xpSlider;

    public void Populate(float completionPercent, float bestAccuracySoFar, float xpProgress01)
    {
        progressText.text = $"PROGRESS: {completionPercent:0}%";
        currentBestText.text = $"CURRENT BEST: {bestAccuracySoFar:0}%";
        xpSlider.value = xpProgress01; // 0 to 1
    }
}