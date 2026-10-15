using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct ColorSortReport
{
    public int setsCleared;
    public int setTotal;
    public string nonDominantHand;
    public float accuracy;
    public int nonDominantCorrect;
    public int nonDominantShown;
    public int dominantCorrect;
    public int dominantShown;
    public int bothCorrect;
    public int bothShown;
    public int timeAdded;
    public int timeUsed;
    public int timeLeft;
    public int easyCleared;
    public int moderateCleared;
    public int hardCleared;
    public float totalScore;
}

public class ColorSortResults : MonoBehaviour
{
    Transform panel;

    public void Show(ColorSortReport report)
    {
        panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = transform;

        ClearValues();
        AddValue(0.802f).text = report.setsCleared + " / " + report.setTotal;
        AddValue(0.752f).text = report.nonDominantHand;
        AddValue(0.701f).text = report.accuracy.ToString("0") + "%";
        AddValue(0.651f).text = Pair(report.nonDominantCorrect, report.nonDominantShown);
        AddValue(0.601f).text = Pair(report.dominantCorrect, report.dominantShown);
        AddValue(0.550f).text = Pair(report.bothCorrect, report.bothShown);
        AddValue(0.500f).text = report.timeAdded + "s";
        AddValue(0.450f).text = report.timeUsed + "s";
        AddValue(0.399f).text = report.timeLeft + "s";
        AddValue(0.295f).text = report.easyCleared + " / 10";
        AddValue(0.245f).text = report.moderateCleared + " / 10";
        AddValue(0.194f).text = report.hardCleared + " / 10";
        AddValue(0.097f).text = report.totalScore.ToString("0.##");
    }

    static string Pair(int done, int total)
    {
        return done + " / " + total;
    }

    void ClearValues()
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child.name == "Value")
                Destroy(child.gameObject);
        }
    }

    TMP_Text AddValue(float y)
    {
        GameObject valueObject = new GameObject("Value");
        valueObject.transform.SetParent(panel, false);

        RectTransform rect = valueObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.52f, y);
        rect.anchorMax = new Vector2(0.92f, y);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 3.2f);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = valueObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        text.fontSize = 3.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.black;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
