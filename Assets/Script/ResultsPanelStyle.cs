using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared results-panel sizing/fonts so every module canvas matches.
/// Sized for up to 5 bible metric rows (Reaction Light max).
/// </summary>
public static class ResultsPanelStyle
{
    // World/UI size — same for every module (image is 900×560).
    public const float PanelWidth = 72f;
    public const float PanelHeight = 44.8f;
    public const float ValueFontSize = 3.2f;
    public const float ValueHeight = 3.4f;
    public const float ValueLeft = 0.56f;
    public const float ValueRight = 0.94f;

    // Matches drawn label rows on the shared panel art (max 5).
    public static readonly float[] RowY =
    {
        0.670f,
        0.545f,
        0.420f,
        0.295f,
        0.170f
    };

    public static void FitPanel(Transform panel, string resourcePath, Sprite overrideSprite = null)
    {
        if (panel == null)
            return;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null)
            rect = panel.gameObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        if (image == null)
            return;

        if (!string.IsNullOrEmpty(resourcePath))
        {
            Sprite loaded = Resources.Load<Sprite>(resourcePath);
            if (loaded != null)
                image.sprite = loaded;
        }
        if (overrideSprite != null)
            image.sprite = overrideSprite;

        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    public static TMP_Text AddValue(Transform panel, string name, int row)
    {
        float y = RowY[Mathf.Clamp(row, 0, RowY.Length - 1)];

        GameObject valueObject = new GameObject(name);
        valueObject.transform.SetParent(panel, false);

        RectTransform rect = valueObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(ValueLeft, y);
        rect.anchorMax = new Vector2(ValueRight, y);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(0f, ValueHeight);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = valueObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        text.fontSize = ValueFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.black;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    public static void ClearValues(Transform panel)
    {
        if (panel == null)
            return;

        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child.name == "Value" || child.name.StartsWith("Value_"))
                Object.Destroy(child.gameObject);
        }
    }
}
