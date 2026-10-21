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
        panel.gameObject.SetActive(true);

        ClearValues();
        AddValue(0.795f).text = report.setsCleared + " / " + report.setTotal;
        AddValue(0.760f).text = report.nonDominantHand;
        AddValue(0.726f).text = report.accuracy.ToString("0") + "%";
        AddValue(0.691f).text = Pair(report.nonDominantCorrect, report.nonDominantShown);
        AddValue(0.656f).text = Pair(report.dominantCorrect, report.dominantShown);
        AddValue(0.622f).text = Pair(report.bothCorrect, report.bothShown);
        AddValue(0.587f).text = report.timeAdded + "s";
        AddValue(0.552f).text = report.timeUsed + "s";
        AddValue(0.517f).text = report.timeLeft + "s";
        AddValue(0.418f).text = report.easyCleared + " / 10";
        AddValue(0.382f).text = report.moderateCleared + " / 10";
        AddValue(0.345f).text = report.hardCleared + " / 10";
        AddValue(0.262f).text = report.totalScore.ToString("0.##");
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
        rect.anchorMin = new Vector2(0.56f, y);
        rect.anchorMax = new Vector2(0.94f, y);
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
