using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct SetRecord
{
    public int tier;
    public bool cleared;
    public float reaction;
}

public struct ReactionLightReport
{
    public int setsCleared;
    public int setTotal;
    public int setsPlayed;
    public string hand;
    public float accuracy;
    public float averageReaction;
    public int greenHits;
    public int yellowPresses;
    public int redPresses;
    public int timeAdded;
    public int timeUsed;
    public int timeLeft;
    public int easyCleared;
    public int moderateCleared;
    public int hardCleared;
    public int tierTotal;
    public string lastSet;
    public int totalScore;
}

public class ReactionLightResults : MonoBehaviour
{
    public void Show(ReactionLightReport report)
    {
        Transform panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = CreatePanel();
        panel.gameObject.SetActive(true);

        ClearValues(panel);
        AddValue(panel, report.setsCleared + " / " + report.setTotal, 0.800f);
        AddValue(panel, report.hand, 0.767f);
        AddValue(panel, report.accuracy.ToString("0") + "%", 0.734f);
        AddValue(panel, report.averageReaction.ToString("0.0") + "s", 0.701f);
        AddValue(panel, report.greenHits.ToString(), 0.668f);
        AddValue(panel, report.yellowPresses.ToString(), 0.635f);
        AddValue(panel, report.redPresses.ToString(), 0.602f);
        AddValue(panel, report.timeAdded + "s", 0.569f);
        AddValue(panel, report.timeUsed + "s", 0.536f);
        AddValue(panel, report.timeLeft + "s", 0.503f);
        AddValue(panel, report.easyCleared + " / " + report.tierTotal, 0.403f);
        AddValue(panel, report.moderateCleared + " / " + report.tierTotal, 0.368f);
        AddValue(panel, report.hardCleared + " / " + report.tierTotal, 0.333f);
        AddValue(panel, report.lastSet, 0.253f);
        AddValue(panel, report.totalScore.ToString(), 0.215f);
    }

    static void ClearValues(Transform panel)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child.name == "Value")
                Destroy(child.gameObject);
        }
    }

    Transform CreatePanel()
    {
        GameObject panelObject = new GameObject("ResultsPanel");
        panelObject.transform.SetParent(transform, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(72f, 96f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("ReactionLight/ResultsPanel");
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return panelObject.transform;
    }

    void AddValue(Transform panel, string value, float y)
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
        text.text = value;
    }
}
