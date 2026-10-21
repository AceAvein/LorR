using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuidedLesson : MonoBehaviour
{
    TMP_Text stepLabel;
    TMP_Text instructionLabel;
    TMP_Text hintLabel;
    GameObject canvas;

    public static GuidedLesson Ensure()
    {
        GuidedLesson existing = FindFirstObjectByType<GuidedLesson>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject host = new GameObject("GuidedLesson");
        return host.AddComponent<GuidedLesson>();
    }

    void Awake()
    {
        BuildCanvas();
        Hide();
    }

    public void ShowStep(int number, int total, string instruction, string hint)
    {
        BuildCanvas();
        if (canvas != null)
            canvas.SetActive(true);

        if (stepLabel != null)
            stepLabel.text = "Step " + number + " of " + total;

        if (instructionLabel != null)
            instructionLabel.text = instruction;

        SetHint(hint);
    }

    public void SetHint(string hint)
    {
        if (hintLabel == null)
            return;

        hintLabel.text = string.IsNullOrEmpty(hint) ? "" : hint;
        hintLabel.gameObject.SetActive(!string.IsNullOrEmpty(hint));
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.SetActive(false);
    }

    void BuildCanvas()
    {
        if (canvas != null)
            return;

        canvas = GameObject.Find("GuidedStepCanvas");
        if (canvas == null)
            canvas = CreateCanvas();

        stepLabel = FindChildText(canvas.transform, "Text_Step");
        instructionLabel = FindChildText(canvas.transform, "Text_Instruction");
        hintLabel = FindChildText(canvas.transform, "Text_Hint");
    }

    GameObject CreateCanvas()
    {
        GameObject root = new GameObject("GuidedStepCanvas", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        CopyObjectivePose(rect);

        Canvas canvasComponent = root.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.worldCamera = Camera.main;
        canvasComponent.sortingOrder = 20;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        root.AddComponent<GraphicRaycaster>();

        Image panel = CreateUi(root.transform, "Panel", new Vector2(92f, 36f), Vector2.zero).AddComponent<Image>();
        panel.color = new Color(0.07f, 0.1f, 0.16f, 0.92f);
        panel.raycastTarget = false;

        stepLabel = CreateLabel(panel.transform, "Text_Step", new Vector2(84f, 8f), new Vector2(0f, 11f), 5.2f, new Color(1f, 0.85f, 0.2f));
        instructionLabel = CreateLabel(panel.transform, "Text_Instruction", new Vector2(84f, 16f), new Vector2(0f, 1f), 6.4f, Color.white);
        hintLabel = CreateLabel(panel.transform, "Text_Hint", new Vector2(84f, 8f), new Vector2(0f, -11f), 5f, new Color(1f, 0.55f, 0.35f));
        instructionLabel.enableWordWrapping = true;
        hintLabel.gameObject.SetActive(false);

        return root;
    }

    static void CopyObjectivePose(RectTransform rect)
    {
        Transform objective = FindNamed("ObjectiveScreen");
        if (objective != null)
        {
            rect.SetPositionAndRotation(objective.position, objective.rotation);
            rect.localScale = objective.lossyScale;
        }
        else
        {
            rect.position = new Vector3(0f, 1.43f, 1.14f);
            rect.localScale = Vector3.one * 0.009f;
        }

        rect.sizeDelta = new Vector2(100f, 100f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static GameObject CreateUi(Transform parent, string objectName, Vector2 size, Vector2 anchored)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchored;
        return go;
    }

    static TMP_Text CreateLabel(Transform parent, string objectName, Vector2 size, Vector2 anchored, float fontSize, Color color)
    {
        GameObject go = CreateUi(parent, objectName, size, anchored);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static TMP_Text FindChildText(Transform root, string objectName)
    {
        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }

    static Transform FindNamed(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }
}
