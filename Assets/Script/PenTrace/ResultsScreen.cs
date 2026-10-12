using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct FinishedPattern
{
    public int tier;
    public float accuracy;
    public float seconds;
    public int starsGot;
    public int starsOnPattern;
}

public struct RoundReport
{
    public int patternsCompleted;
    public int patternTotal;
    public string difficulty;
    public float accuracy;
    public float averageSeconds;
    public int stars;
    public int timeUsed;
    public int timeLeft;
    public int easyCompleted;
    public int moderateCompleted;
    public int hardCompleted;
    public int easyTotal;
    public int moderateTotal;
    public int hardTotal;
    public float lastTraced;
    public float totalScore;
}

public class ResultsScreen : MonoBehaviour
{
    public Sprite panelSprite;

    TMP_Text patternsCompleted;
    TMP_Text difficultyReached;
    TMP_Text accuracy;
    TMP_Text averageSpeed;
    TMP_Text starsCollected;
    TMP_Text timeUsed;
    TMP_Text timeLeft;
    TMP_Text easyCompleted;
    TMP_Text moderateCompleted;
    TMP_Text hardCompleted;
    TMP_Text lastPatternTraced;
    TMP_Text totalScore;
    bool built;

    void Awake()
    {
        EnsureVisuals();
    }

    public void Show(RoundReport report)
    {
        EnsureVisuals();

        patternsCompleted.text = report.patternsCompleted + " / " + report.patternTotal;
        difficultyReached.text = report.difficulty;
        accuracy.text = report.accuracy.ToString("0") + "%";
        averageSpeed.text = report.averageSeconds.ToString("0.0") + "s";
        starsCollected.text = report.stars.ToString();
        timeUsed.text = report.timeUsed + "s";
        timeLeft.text = report.timeLeft + "s";
        easyCompleted.text = report.easyCompleted + " / " + report.easyTotal;
        moderateCompleted.text = report.moderateCompleted + " / " + report.moderateTotal;
        hardCompleted.text = report.hardCompleted + " / " + report.hardTotal;
        lastPatternTraced.text = report.lastTraced.ToString("0") + "%";
        totalScore.text = report.totalScore.ToString("0.##");
    }

    void EnsureVisuals()
    {
        if (built)
            return;

        built = true;
        HideOldLabels();
        FitPanel();

        patternsCompleted = AddValue("Value_Patterns", 0.785f);
        difficultyReached = AddValue("Value_Difficulty", 0.735f);
        accuracy = AddValue("Value_Accuracy", 0.679f);
        averageSpeed = AddValue("Value_Speed", 0.629f);
        starsCollected = AddValue("Value_Stars", 0.582f);
        timeUsed = AddValue("Value_TimeUsed", 0.531f);
        timeLeft = AddValue("Value_TimeLeft", 0.480f);
        easyCompleted = AddValue("Value_Easy", 0.356f);
        moderateCompleted = AddValue("Value_Moderate", 0.303f);
        hardCompleted = AddValue("Value_Hard", 0.250f);
        lastPatternTraced = AddValue("Value_LastTraced", 0.124f);
        totalScore = AddValue("Value_Total", 0.072f);
    }

    void HideOldLabels()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string name = labels[i].gameObject.name;
            if (name == "Text_TimesUp" || name == "Text_Accuracy")
                labels[i].gameObject.SetActive(false);
        }
    }

    void FitPanel()
    {
        Image image = GetComponent<Image>();
        if (image != null)
        {
            if (panelSprite != null)
                image.sprite = panelSprite;

            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(72f, 96f);
        rect.anchoredPosition = Vector2.zero;
    }

    TMP_Text AddValue(string name, float y)
    {
        GameObject valueObject = new GameObject(name);
        valueObject.transform.SetParent(transform, false);

        RectTransform rect = valueObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.52f, y);
        rect.anchorMax = new Vector2(0.92f, y);
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
