using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PatternManager : MonoBehaviour
{
    public GameObject[] patterns;
    public PathManager pathManager;
    public bool generateRandomPatterns;

    struct Recipe
    {
        public int kind;
        public int count;
        public float amp;
    }

    int currentPattern;
    LineRenderer generatedPath;
    TextMeshPro startLabel;
    TextMeshPro endLabel;
    readonly List<Vector3> generatedPoints = new List<Vector3>();
    readonly List<Recipe> deck = new List<Recipe>();
    int nextRecipe = -1;

    void Start()
    {
        if (generateRandomPatterns)
            ShowGenerated(true);
        else
            ShowPattern(currentPattern);
    }

    public void ShowPattern(int index)
    {
        for (int i = 0; i < patterns.Length; i++)
            patterns[i].SetActive(i == index);

        currentPattern = index;

        Transform pattern = patterns[index].transform;
        LineRenderer newTracePath = pattern.Find("TracePath").GetComponent<LineRenderer>();
        Transform newTraceEnd = pattern.Find("TraceEnd");

        pathManager.tracePath = newTracePath;
        pathManager.traceEnd = newTraceEnd;
        pathManager.ResetPattern();
    }

    public void NextPattern()
    {
        if (generateRandomPatterns)
        {
            ShowGenerated(false);
            return;
        }

        currentPattern++;

        if (currentPattern >= patterns.Length)
            currentPattern = 0;

        ShowPattern(currentPattern);
    }

    public void ResetPatterns()
    {
        if (generateRandomPatterns)
        {
            ShowGenerated(true);
            return;
        }

        currentPattern = 0;
        ShowPattern(currentPattern);
    }

    void ShowGenerated(bool newPlay)
    {
        for (int i = 0; i < patterns.Length; i++)
            patterns[i].SetActive(false);

        EnsureGeneratedGuide();
        FillRecipe(DrawRecipe(newPlay));

        generatedPath.positionCount = generatedPoints.Count;
        generatedPath.SetPositions(generatedPoints.ToArray());

        PlaceLabel(startLabel, generatedPoints[0], -1f);
        PlaceLabel(endLabel, generatedPoints[generatedPoints.Count - 1], 1f);

        pathManager.tracePath = generatedPath;
        pathManager.traceEnd = endLabel.transform;
        pathManager.ResetPattern();
    }

    void EnsureGeneratedGuide()
    {
        if (generatedPath != null)
            return;

        Transform root = patterns[0].transform.parent;
        GameObject guide = new GameObject("GeneratedGuide");
        guide.transform.SetParent(root, false);
        guide.transform.localPosition = new Vector3(0f, 0.63f, 0f);
        guide.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        guide.transform.localScale = Vector3.one;

        generatedPath = guide.AddComponent<LineRenderer>();
        generatedPath.useWorldSpace = false;
        generatedPath.alignment = LineAlignment.TransformZ;
        generatedPath.numCapVertices = 8;
        generatedPath.numCornerVertices = 4;
        generatedPath.textureMode = LineTextureMode.Stretch;
        generatedPath.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        generatedPath.receiveShadows = false;

        startLabel = CreateLabel("Start");
        endLabel = CreateLabel("End");
    }

    static TextMeshPro CreateLabel(string word)
    {
        GameObject labelObject = new GameObject(word + "Label");
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
            font = TMP_Settings.defaultFontAsset;

        label.font = font;
        label.text = word;
        label.fontSize = 1.5f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.outlineWidth = 0.2f;
        label.outlineColor = Color.black;
        label.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);

        Renderer textRenderer = label.renderer;
        if (textRenderer != null)
            textRenderer.sortingOrder = 5;

        return label;
    }

    void PlaceLabel(TextMeshPro label, Vector3 point, float side)
    {
        Transform guide = generatedPath.transform;
        Transform area = guide.parent.parent.parent;
        Vector3 onLine = guide.TransformPoint(point);
        Vector3 towardPlayer = -area.forward;
        Vector3 right = guide.TransformDirection(Vector3.right).normalized;
        Vector3 up = guide.TransformDirection(Vector3.up).normalized;

        label.transform.SetParent(area, false);
        label.transform.localScale = Vector3.one;
        label.transform.position = onLine + towardPlayer * 0.02f + right * (side * 0.24f) + up * 0.1f;
        label.transform.rotation = Quaternion.LookRotation(-towardPlayer, Vector3.up);
    }

    Recipe DrawRecipe(bool newPlay)
    {
        if (deck.Count == 0)
            BuildDeck();

        bool restart = newPlay || nextRecipe < 0 || nextRecipe >= deck.Count;
        Recipe previous = default;
        bool avoidRepeat = false;
        if (restart && nextRecipe > 0 && nextRecipe <= deck.Count)
        {
            previous = deck[nextRecipe - 1];
            avoidRepeat = true;
        }

        if (restart)
        {
            ShuffleDeck();
            if (avoidRepeat && SameRecipe(deck[0], previous))
            {
                Recipe first = deck[0];
                deck[0] = deck[1];
                deck[1] = first;
            }

            nextRecipe = 0;
        }

        return deck[nextRecipe++];
    }

    void BuildDeck()
    {
        const float amp = 0.22f;
        deck.Clear();

        AddPair(0, 2, amp);
        AddPair(0, 3, amp);
        AddPair(1, 3, amp);
        AddPair(1, 4, amp);
        AddPair(1, 5, amp);
        AddPair(2, 1, amp);
        AddPair(3, 2, amp);
        AddPair(3, 3, amp);
        AddPair(4, 2, amp);
        AddPair(5, 0, amp);
    }

    void AddPair(int kind, int count, float amp)
    {
        deck.Add(new Recipe { kind = kind, count = count, amp = amp });
        deck.Add(new Recipe { kind = kind, count = count, amp = -amp });
    }

    void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            Recipe current = deck[i];
            deck[i] = deck[swap];
            deck[swap] = current;
        }
    }

    static bool SameRecipe(Recipe a, Recipe b)
    {
        return a.kind == b.kind && a.count == b.count && Mathf.Approximately(a.amp, b.amp);
    }

    void FillRecipe(Recipe recipe)
    {
        generatedPoints.Clear();

        const float x0 = -0.45f;
        const float x1 = 0.45f;

        switch (recipe.kind)
        {
            case 0:
            case 2:
                AddWave(x0, x1, recipe.amp, recipe.count);
                break;
            case 1:
                AddZigzag(x0, x1, recipe.amp, recipe.count);
                break;
            case 3:
                AddSteps(x0, x1, recipe.amp, recipe.count);
                break;
            case 4:
                AddHills(x0, x1, recipe.amp);
                break;
            default:
                AddCorner(x0, x1, recipe.amp);
                break;
        }
    }

    void AddWave(float x0, float x1, float amp, int humps)
    {
        const int count = 40;
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = Mathf.Lerp(x0, x1, t);
            float y = Mathf.Sin(t * humps * Mathf.PI) * amp;
            generatedPoints.Add(new Vector3(x, y, 0f));
        }
    }

    void AddHills(float x0, float x1, float amp)
    {
        const int count = 40;
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = Mathf.Lerp(x0, x1, t);
            float wave = Mathf.Sin(t * Mathf.PI * 2f);
            float y = (wave * wave) * amp - amp * 0.5f;
            generatedPoints.Add(new Vector3(x, y, 0f));
        }
    }

    void AddZigzag(float x0, float x1, float amp, int corners)
    {
        for (int i = 0; i <= corners; i++)
        {
            float t = i / (float)corners;
            float x = Mathf.Lerp(x0, x1, t);
            float y = i % 2 == 0 ? amp : -amp;
            generatedPoints.Add(new Vector3(x, y, 0f));
        }
    }

    void AddSteps(float x0, float x1, float amp, int rises)
    {
        float yStart = -amp;
        float yEnd = amp;
        generatedPoints.Add(new Vector3(x0, yStart, 0f));

        for (int i = 0; i < rises; i++)
        {
            float x = Mathf.Lerp(x0, x1, (i + 1f) / rises);
            float y = Mathf.Lerp(yStart, yEnd, i / (float)rises);
            float yNext = Mathf.Lerp(yStart, yEnd, (i + 1f) / rises);
            generatedPoints.Add(new Vector3(x, y, 0f));
            generatedPoints.Add(new Vector3(x, yNext, 0f));
        }
    }

    void AddCorner(float x0, float x1, float amp)
    {
        generatedPoints.Add(new Vector3(x0, -amp, 0f));
        generatedPoints.Add(new Vector3(0f, -amp, 0f));
        generatedPoints.Add(new Vector3(0f, amp, 0f));
        generatedPoints.Add(new Vector3(x1, amp, 0f));
    }
}
