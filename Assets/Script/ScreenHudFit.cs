using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class ScreenHudFit : MonoBehaviour
{
    const float LabelMin = 32f;
    const float LabelMax = 55f;
    const float ItemGap = 28f;
    const float SideMargin = 24f;
    const float TopMargin = 12f;
    const float BarHeight = 80f;

    CanvasScaler scaler;
    RectTransform safeArea;
    TMP_Text[] labels;
    string[] cachedText;
    float[] usedEm;
    int screenWidth;
    int screenHeight;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        BuildSafeArea();
        CollectLabels();
        Apply();
    }

    void LateUpdate()
    {
        if (labels == null)
            return;

        if (Screen.width == screenWidth && Screen.height == screenHeight && !TextChanged())
            return;

        Apply();
        FlowLabels();
    }

    bool TextChanged()
    {
        if (cachedText == null || cachedText.Length != labels.Length)
            return true;

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null)
                continue;

            if (cachedText[i] != labels[i].text)
                return true;
        }

        return false;
    }

    void BuildSafeArea()
    {
        Transform existing = transform.Find("SafeArea");
        if (existing == null)
        {
            GameObject panel = new GameObject("SafeArea", typeof(RectTransform));
            safeArea = panel.GetComponent<RectTransform>();
            safeArea.SetParent(transform, false);
        }
        else
        {
            safeArea = existing as RectTransform;
        }

        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        safeArea.pivot = new Vector2(0.5f, 0.5f);
        safeArea.localScale = Vector3.one;
        safeArea.localRotation = Quaternion.identity;

        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == safeArea || child.GetComponent<TMP_Text>() == null)
                continue;

            children.Add(child);
        }

        for (int i = 0; i < children.Count; i++)
            children[i].SetParent(safeArea, false);
    }

    void CollectLabels()
    {
        if (safeArea == null)
            return;

        TMP_Text[] found = safeArea.GetComponentsInChildren<TMP_Text>(true);
        List<TMP_Text> ordered = new List<TMP_Text>(found);
        ordered.Sort(CompareLeftToRight);

        labels = ordered.ToArray();
        cachedText = new string[labels.Length];
        usedEm = new float[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.BaselineLeft;
            label.margin = Vector4.zero;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        HideGameplay();
    }

    public void ShowGameplay()
    {
        SetGameplayVisible(true);
        FlowLabels();
    }

    public void HideGameplay()
    {
        SetGameplayVisible(false);
        FlowLabels();
    }

    void SetGameplayVisible(bool visible)
    {
        if (labels == null)
            return;

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || IsModule(label))
                continue;

            label.gameObject.SetActive(visible);
        }
    }

    static bool IsModule(TMP_Text label)
    {
        return label.gameObject.name.StartsWith("Module");
    }

    static int CompareLeftToRight(TMP_Text a, TMP_Text b)
    {
        float ax = a.rectTransform.anchorMin.x;
        float bx = b.rectTransform.anchorMin.x;
        int byAnchor = ax.CompareTo(bx);
        if (byAnchor != 0)
            return byAnchor;

        return a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x);
    }

    void FlowLabels()
    {
        if (labels == null || safeArea == null)
            return;

        float barWidth = safeArea.rect.width - SideMargin * 2f;
        if (barWidth < 8f)
            return;

        float size = SharedSize(barWidth);
        float x = SideMargin;

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || !label.gameObject.activeSelf)
                continue;

            cachedText[i] = label.text;
            label.fontSize = size;

            if (string.IsNullOrEmpty(label.text))
                continue;

            label.ForceMeshUpdate();
            float width = SlotWidth(i, label.preferredWidth, size);

            RectTransform rect = label.rectTransform;
            rect.sizeDelta = new Vector2(width, BarHeight);
            rect.anchoredPosition = new Vector2(x, -TopMargin);
            x += width + ItemGap;
        }
    }

    float SharedSize(float barWidth)
    {
        if (RowWidth(LabelMax) <= barWidth)
            return LabelMax;

        float low = LabelMin;
        float high = LabelMax;

        for (int step = 0; step < 8; step++)
        {
            float mid = (low + high) * 0.5f;
            if (RowWidth(mid) <= barWidth)
                low = mid;
            else
                high = mid;
        }

        return low;
    }

    float RowWidth(float size)
    {
        float sum = 0f;
        int shown = 0;

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || !label.gameObject.activeSelf || string.IsNullOrEmpty(label.text))
                continue;

            label.fontSize = size;
            label.ForceMeshUpdate();
            sum += SlotWidth(i, label.preferredWidth, size);
            shown++;
        }

        if (shown > 1)
            sum += ItemGap * (shown - 1);

        return sum;
    }

    float SlotWidth(int index, float preferred, float size)
    {
        float em = (preferred + 6f) / Mathf.Max(1f, size);
        if (em > usedEm[index])
            usedEm[index] = em;

        return usedEm[index] * size;
    }

    void Apply()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            float screenAspect = screenWidth / Mathf.Max(1f, screenHeight);
            float referenceAspect = scaler.referenceResolution.x / Mathf.Max(1f, scaler.referenceResolution.y);
            scaler.matchWidthOrHeight = screenAspect > referenceAspect ? 1f : 0f;
        }

        if (safeArea == null)
            return;

        Rect area = Screen.safeArea;
        float width = Mathf.Max(1, screenWidth);
        float height = Mathf.Max(1, screenHeight);
        safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
        safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }
}
