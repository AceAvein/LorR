using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallDropResults : MonoBehaviour
{
    TMP_Text ballsCaught;
    TMP_Text nonDominantHand;
    TMP_Text accuracy;
    TMP_Text averageReaction;
    TMP_Text timeAdded;
    TMP_Text timeUsed;
    TMP_Text timeLeft;
    TMP_Text dominantCaught;
    TMP_Text dominantAccuracy;
    TMP_Text nonDominantCaught;
    TMP_Text nonDominantAccuracy;
    TMP_Text handDifference;
    TMP_Text wrongGrabs;
    TMP_Text lastBall;
    TMP_Text totalScore;
    bool built;

    public void Show(BallDropReport report)
    {
        EnsureVisuals();

        ballsCaught.text = report.caught + " / " + report.spawned;
        nonDominantHand.text = report.nonDominantHand;
        accuracy.text = report.accuracy.ToString("0") + "%";
        averageReaction.text = report.averageReaction.ToString("0.0") + "s";
        timeAdded.text = report.timeAdded + "s";
        timeUsed.text = report.timeUsed + "s";
        timeLeft.text = report.timeLeft + "s";
        dominantCaught.text = report.dominantCaught + " / " + report.dominantSpawned;
        dominantAccuracy.text = report.dominantAccuracy.ToString("0") + "%";
        nonDominantCaught.text = report.nonDominantCaught + " / " + report.nonDominantSpawned;
        nonDominantAccuracy.text = report.nonDominantAccuracy.ToString("0") + "%";
        handDifference.text = (report.handDifference > 0 ? "+" : "") + report.handDifference;
        wrongGrabs.text = report.wrongGrabs.ToString();
        lastBall.text = report.lastBall;
        totalScore.text = report.totalScore.ToString("0.##");
    }

    void EnsureVisuals()
    {
        if (built)
            return;

        built = true;
        HideOldLabel();

        Transform panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = CreatePanel();
        panel.gameObject.SetActive(true);

        ballsCaught = AddValue(panel, 0.783f);
        nonDominantHand = AddValue(panel, 0.745f);
        accuracy = AddValue(panel, 0.707f);
        averageReaction = AddValue(panel, 0.668f);
        timeAdded = AddValue(panel, 0.630f);
        timeUsed = AddValue(panel, 0.592f);
        timeLeft = AddValue(panel, 0.554f);
        dominantCaught = AddValue(panel, 0.460f);
        dominantAccuracy = AddValue(panel, 0.422f);
        nonDominantCaught = AddValue(panel, 0.384f);
        nonDominantAccuracy = AddValue(panel, 0.346f);
        handDifference = AddValue(panel, 0.307f);
        wrongGrabs = AddValue(panel, 0.269f);
        lastBall = AddValue(panel, 0.172f);
        totalScore = AddValue(panel, 0.125f);
    }

    void HideOldLabel()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == "Text_Results")
                labels[i].gameObject.SetActive(false);
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
        image.sprite = Resources.Load<Sprite>("BallDrop/ResultsPanel");
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return panelObject.transform;
    }

    TMP_Text AddValue(Transform panel, float y)
    {
        GameObject valueObject = new GameObject("Value");
        valueObject.transform.SetParent(panel, false);

        RectTransform rect = valueObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.56f, y);
        rect.anchorMax = new Vector2(0.94f, y);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 3.6f);
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
        text.text = "";
        return text;
    }
}
