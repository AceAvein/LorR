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
        public float span;
        public int passes;
        public int stars;
    }

    class GuideStar
    {
        public GameObject root;
        public MeshRenderer fill;
        public bool collected;
        public Vector3 worldPoint;
    }

    const float StarReach = 0.045f;
    const int StarPool = 3;
    const float LetterStroke = 6.3f / 59f;

    int currentPattern;
    LineRenderer generatedPath;
    TextMeshPro startLabel;
    TextMeshPro endLabel;
    readonly List<Vector3> generatedPoints = new List<Vector3>();
    readonly List<Vector3> worldPoints = new List<Vector3>();
    readonly List<Recipe> deck = new List<Recipe>();
    int easyCount;
    int moderateCount;
    int hardCount;
    readonly List<GuideStar> stars = new List<GuideStar>();
    readonly List<float> starDistances = new List<float>();
    int nextRecipe = -1;
    static Mesh starMesh;
    static Material starMaterial;

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
        RefreshTracingHud();
    }

    public void NextPattern()
    {
        if (generateRandomPatterns)
        {
            if (deck.Count > 0 && nextRecipe >= deck.Count)
            {
                TracingTimer timer = FindFirstObjectByType<TracingTimer>();
                if (timer != null)
                    timer.CompleteGame();
                return;
            }

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

    public void CheckStars(Vector3 penPosition)
    {
        TracingTimer timer = null;

        for (int i = 0; i < stars.Count; i++)
        {
            GuideStar star = stars[i];
            if (!star.root.activeSelf || star.collected)
                continue;

            if (Vector3.Distance(penPosition, star.worldPoint) > StarReach)
                continue;

            star.collected = true;
            star.fill.enabled = true;

            if (timer == null)
                timer = FindFirstObjectByType<TracingTimer>();

            if (timer != null)
                timer.AddSeconds(2f);

            PenSounds.PlayStar();
        }
    }

    void ShowGenerated(bool newPlay)
    {
        for (int i = 0; i < patterns.Length; i++)
            patterns[i].SetActive(false);

        EnsureGeneratedGuide();
        Recipe recipe = DrawRecipe(newPlay);
        FillRecipe(recipe);

        generatedPath.positionCount = generatedPoints.Count;
        generatedPath.SetPositions(generatedPoints.ToArray());

        PlaceLabel(startLabel, 0);
        PlaceLabel(endLabel, generatedPoints.Count - 1);
        PlaceStars(recipe.stars);

        pathManager.guideWorldWidth = LetterThickness(startLabel);
        pathManager.tracePath = generatedPath;
        pathManager.traceEnd = endLabel.transform;
        pathManager.ResetPattern();
        RefreshTracingHud();
    }

    void RefreshTracingHud()
    {
        TracingTimer timer = FindFirstObjectByType<TracingTimer>(FindObjectsInactive.Include);
        if (timer != null)
            timer.RefreshHud();
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
        generatedPath.alignment = LineAlignment.View;
        generatedPath.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        generatedPath.widthMultiplier = 0.012f;
        generatedPath.sortingOrder = 0;
        generatedPath.numCapVertices = 8;
        generatedPath.numCornerVertices = 4;
        generatedPath.textureMode = LineTextureMode.Stretch;
        generatedPath.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        generatedPath.receiveShadows = false;

        startLabel = CreateLabel("Start");
        endLabel = CreateLabel("End");

        for (int i = 0; i < StarPool; i++)
            stars.Add(CreateStar());
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

    GuideStar CreateStar()
    {
        EnsureStarAssets();

        GameObject root = new GameObject("GuideStar");
        root.transform.localScale = Vector3.one * 0.08f;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(root.transform, false);
        MeshFilter filter = fillObject.AddComponent<MeshFilter>();
        filter.sharedMesh = starMesh;
        MeshRenderer fill = fillObject.AddComponent<MeshRenderer>();
        fill.sharedMaterial = starMaterial;
        fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill.receiveShadows = false;
        fill.sortingOrder = 21;
        fill.enabled = false;

        LineRenderer outline = root.AddComponent<LineRenderer>();
        outline.useWorldSpace = false;
        outline.loop = false;
        outline.alignment = LineAlignment.TransformZ;
        outline.widthMultiplier = 0.008f;
        outline.numCornerVertices = 2;
        outline.numCapVertices = 2;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outline.receiveShadows = false;
        outline.material = starMaterial;
        outline.sortingOrder = 20;
        outline.startColor = new Color(1f, 0.82f, 0.15f);
        outline.endColor = outline.startColor;
        Vector3[] outlinePoints = StarPoints(true);
        outline.positionCount = outlinePoints.Length;
        outline.SetPositions(outlinePoints);

        root.SetActive(false);

        return new GuideStar
        {
            root = root,
            fill = fill,
            collected = false,
            worldPoint = Vector3.zero
        };
    }

    static void EnsureStarAssets()
    {
        if (starMesh == null)
        {
            Vector3[] points = StarPoints(false);
            Vector3[] vertices = new Vector3[points.Length + 1];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
                vertices[i + 1] = points[i];

            int[] triangles = new int[points.Length * 3];
            for (int i = 0; i < points.Length; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % points.Length + 1;
            }

            starMesh = new Mesh();
            starMesh.name = "GuideStar";
            starMesh.vertices = vertices;
            starMesh.triangles = triangles;
            starMesh.RecalculateNormals();
            starMesh.RecalculateBounds();
        }

        if (starMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            starMaterial = new Material(shader);
            starMaterial.color = new Color(1f, 0.82f, 0.15f);
        }
    }

    static Vector3[] StarPoints(bool closeLoop)
    {
        const int tips = 5;
        int count = tips * 2;
        Vector3[] points = new Vector3[closeLoop ? count + 1 : count];

        for (int i = 0; i < points.Length; i++)
        {
            int step = i % count;
            float angle = step * Mathf.PI / tips + Mathf.PI / 2f;
            float radius = step % 2 == 0 ? 0.5f : 0.22f;
            points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        }

        return points;
    }

    void PlaceLabel(TextMeshPro label, int pointIndex)
    {
        Transform guide = generatedPath.transform;
        Transform area = guide.parent.parent.parent;
        Vector3 onLine = guide.TransformPoint(generatedPoints[pointIndex]);
        Vector3 towardPlayer = -area.forward;

        int neighbor = pointIndex == 0 ? 1 : pointIndex - 1;
        Vector3 outward = onLine - guide.TransformPoint(generatedPoints[neighbor]);
        if (outward.sqrMagnitude < 0.000001f)
            outward = guide.TransformDirection(Vector3.right);
        outward.Normalize();

        label.transform.SetParent(area, false);
        label.transform.localScale = Vector3.one;
        label.ForceMeshUpdate();
        label.transform.rotation = Quaternion.LookRotation(-towardPlayer, Vector3.up);

        Vector3 halfWidth = label.transform.TransformVector(new Vector3(label.textBounds.extents.x, 0f, 0f));
        float reach = halfWidth.magnitude + LetterThickness(label) * 0.5f;
        label.transform.position = onLine + towardPlayer.normalized * 0.004f + outward * reach;
    }

    float LetterThickness(TextMeshPro label)
    {
        label.ForceMeshUpdate();
        float height = label.transform.TransformVector(new Vector3(0f, label.textBounds.size.y, 0f)).magnitude;
        if (height < 0.0001f)
            return pathManager.guideWorldWidth;

        return height * LetterStroke;
    }

    void PlaceStars(int count)
    {
        BuildWorldPath();
        PickStarDistances(count);

        Transform guide = generatedPath.transform;
        Transform area = guide.parent.parent.parent;
        Vector3 towardPlayer = -area.forward;
        Quaternion rotation = Quaternion.LookRotation(-towardPlayer, Vector3.up);

        for (int i = 0; i < stars.Count; i++)
        {
            GuideStar star = stars[i];
            if (i >= starDistances.Count)
            {
                star.root.SetActive(false);
                continue;
            }

            Vector3 onLine = PointAlong(starDistances[i]);
            star.collected = false;
            star.fill.enabled = false;
            star.worldPoint = onLine;
            star.root.SetActive(true);
            star.root.transform.SetParent(area, false);
            star.root.transform.localScale = Vector3.one * 0.08f;
            star.root.transform.SetPositionAndRotation(onLine + towardPlayer * 0.006f, rotation);
        }
    }

    void BuildWorldPath()
    {
        worldPoints.Clear();
        Transform guide = generatedPath.transform;

        for (int i = 0; i < generatedPoints.Count; i++)
            worldPoints.Add(guide.TransformPoint(generatedPoints[i]));
    }

    void PickStarDistances(int count)
    {
        starDistances.Clear();
        float total = PathLength();
        if (total < 0.05f || count <= 0)
            return;

        float margin = Mathf.Min(total * 0.15f, 0.08f);
        float minGap = 0.12f;
        int guard = 0;

        while (starDistances.Count < count && guard < 50)
        {
            float distance = Random.Range(margin, Mathf.Max(margin + 0.01f, total - margin));
            bool clear = true;

            for (int i = 0; i < starDistances.Count; i++)
            {
                if (Mathf.Abs(starDistances[i] - distance) < minGap)
                    clear = false;
            }

            if (clear)
                starDistances.Add(distance);

            guard++;
        }

        if (starDistances.Count < count)
        {
            starDistances.Clear();
            for (int i = 0; i < count; i++)
            {
                float step = (i + 1f) / (count + 1f);
                starDistances.Add(Mathf.Lerp(margin, total - margin, step));
            }
        }
    }

    float PathLength()
    {
        float length = 0f;

        for (int i = 1; i < worldPoints.Count; i++)
            length += Vector3.Distance(worldPoints[i - 1], worldPoints[i]);

        return length;
    }

    Vector3 PointAlong(float distance)
    {
        float walked = 0f;

        for (int i = 1; i < worldPoints.Count; i++)
        {
            float segment = Vector3.Distance(worldPoints[i - 1], worldPoints[i]);
            if (walked + segment >= distance)
            {
                float t = segment <= 0.0001f ? 0f : (distance - walked) / segment;
                return Vector3.Lerp(worldPoints[i - 1], worldPoints[i], t);
            }

            walked += segment;
        }

        return worldPoints[worldPoints.Count - 1];
    }

    Recipe DrawRecipe(bool newPlay)
    {
        if (newPlay || deck.Count == 0)
        {
            BuildDeck();
            nextRecipe = 0;
        }

        return deck[nextRecipe++];
    }

    void BuildDeck()
    {
        List<Recipe> easy = new List<Recipe>();
        List<Recipe> moderate = new List<Recipe>();
        List<Recipe> hard = new List<Recipe>();

        AddGuide(easy, 0, 1, 0.1f, 0.2f, 1, 3);
        AddGuide(easy, 0, 2, 0.09f, 0.22f, 1, 3);
        AddGuide(easy, 0, 3, 0.07f, 0.22f, 1, 3);
        AddGuide(easy, 1, 2, 0.1f, 0.2f, 1, 3);
        AddGuide(easy, 1, 3, 0.08f, 0.22f, 1, 3);
        AddGuide(easy, 3, 1, 0.1f, 0.2f, 1, 3);
        AddGuide(easy, 3, 2, 0.08f, 0.22f, 1, 3);
        AddGuide(easy, 5, 0, 0.1f, 0.2f, 1, 3);
        AddGuide(easy, 6, 0, 0.1f, 0.22f, 1, 3);
        AddGuide(easy, 4, 1, 0.08f, 0.22f, 1, 3);

        AddGuide(moderate, 0, 4, 0.16f, 0.4f, 1, 2);
        AddGuide(moderate, 0, 5, 0.14f, 0.42f, 1, 2);
        AddGuide(moderate, 7, 1, 0.16f, 0.42f, 1, 2);
        AddGuide(moderate, 1, 4, 0.16f, 0.4f, 1, 2);
        AddGuide(moderate, 1, 5, 0.14f, 0.42f, 1, 2);
        AddGuide(moderate, 1, 6, 0.12f, 0.42f, 1, 2);
        AddGuide(moderate, 3, 3, 0.14f, 0.4f, 1, 2);
        AddGuide(moderate, 3, 4, 0.12f, 0.42f, 1, 2);
        AddGuide(moderate, 4, 2, 0.16f, 0.42f, 1, 2);
        AddGuide(moderate, 5, 1, 0.16f, 0.38f, 1, 2);

        AddGuide(hard, 0, 1, 0.04f, 0.36f, 3, 1);
        AddGuide(hard, 0, 2, 0.035f, 0.36f, 3, 1);
        AddGuide(hard, 0, 3, 0.03f, 0.36f, 3, 1);
        AddGuide(hard, 1, 2, 0.04f, 0.36f, 3, 1);
        AddGuide(hard, 1, 3, 0.035f, 0.36f, 3, 1);
        AddGuide(hard, 1, 4, 0.03f, 0.36f, 3, 1);
        AddGuide(hard, 3, 2, 0.035f, 0.36f, 3, 1);
        AddGuide(hard, 3, 3, 0.03f, 0.36f, 3, 1);
        AddGuide(hard, 4, 2, 0.04f, 0.36f, 3, 1);
        AddGuide(hard, 7, 2, 0.035f, 0.36f, 3, 1);

        Shuffle(easy);
        Shuffle(moderate);
        Shuffle(hard);

        easyCount = easy.Count;
        moderateCount = moderate.Count;
        hardCount = hard.Count;

        deck.Clear();
        deck.AddRange(easy);
        deck.AddRange(moderate);
        deck.AddRange(hard);
    }

    public int CurrentPatternIndex
    {
        get { return Mathf.Max(0, nextRecipe - 1); }
    }

    public int CurrentTier()
    {
        return TierOf(CurrentPatternIndex);
    }

    public int TierOf(int index)
    {
        if (index < easyCount)
            return 0;

        if (index < easyCount + moderateCount)
            return 1;

        return 2;
    }

    public int TierCount(int tier)
    {
        if (tier == 0)
            return easyCount;

        if (tier == 1)
            return moderateCount;

        return hardCount;
    }

    public int PatternTotal()
    {
        return easyCount + moderateCount + hardCount;
    }

    public int StarsCollected()
    {
        int count = 0;

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i].root.activeSelf && stars[i].collected)
                count++;
        }

        return count;
    }

    public int StarsOnPattern()
    {
        int count = 0;

        for (int i = 0; i < stars.Count; i++)
        {
            if (stars[i].root.activeSelf)
                count++;
        }

        return count;
    }

    static void AddGuide(List<Recipe> group, int kind, int count, float amp, float span, int passes, int starCount)
    {
        group.Add(new Recipe { kind = kind, count = count, amp = amp, span = span, passes = passes, stars = starCount });
    }

    static void Shuffle(List<Recipe> group)
    {
        for (int i = group.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            Recipe current = group[i];
            group[i] = group[swap];
            group[swap] = current;
        }
    }

    void FillRecipe(Recipe recipe)
    {
        generatedPoints.Clear();
        float gap = Mathf.Abs(recipe.amp) * 2f + 0.1f;

        for (int pass = 0; pass < recipe.passes; pass++)
        {
            float yBase = (pass - (recipe.passes - 1) * 0.5f) * gap;
            bool reverse = pass % 2 == 1;
            float xStart = reverse ? recipe.span : -recipe.span;
            float xEnd = reverse ? -recipe.span : recipe.span;
            AppendShape(recipe, xStart, xEnd, yBase);
        }
    }

    void AppendShape(Recipe recipe, float xStart, float xEnd, float yBase)
    {
        switch (recipe.kind)
        {
            case 1:
                AddZigzag(xStart, xEnd, yBase, recipe.amp, recipe.count);
                break;
            case 3:
                AddSteps(xStart, xEnd, yBase, recipe.amp, recipe.count);
                break;
            case 4:
                AddHills(xStart, xEnd, yBase, recipe.amp);
                break;
            case 5:
                AddCorner(xStart, xEnd, yBase, recipe.amp, recipe.count);
                break;
            case 6:
                AddSlope(xStart, xEnd, yBase, recipe.amp);
                break;
            case 7:
                AddScurve(xStart, xEnd, yBase, recipe.amp);
                break;
            default:
                AddWave(xStart, xEnd, yBase, recipe.amp, recipe.count);
                break;
        }
    }

    void AddWave(float xStart, float xEnd, float yBase, float amp, int humps)
    {
        const int count = 40;
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = Mathf.Lerp(xStart, xEnd, t);
            float y = yBase + Mathf.Sin(t * humps * Mathf.PI) * amp;
            AddPoint(new Vector3(x, y, 0f));
        }
    }

    void AddHills(float xStart, float xEnd, float yBase, float amp)
    {
        const int count = 40;
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = Mathf.Lerp(xStart, xEnd, t);
            float wave = Mathf.Sin(t * Mathf.PI * 2f);
            float y = yBase + (wave * wave) * amp - amp * 0.5f;
            AddPoint(new Vector3(x, y, 0f));
        }
    }

    void AddZigzag(float xStart, float xEnd, float yBase, float amp, int corners)
    {
        for (int i = 0; i <= corners; i++)
        {
            float t = i / (float)corners;
            float x = Mathf.Lerp(xStart, xEnd, t);
            float y = yBase + (i % 2 == 0 ? amp : -amp);
            AddPoint(new Vector3(x, y, 0f));
        }
    }

    void AddSteps(float xStart, float xEnd, float yBase, float amp, int rises)
    {
        float yStart = yBase - amp;
        float yEnd = yBase + amp;
        AddPoint(new Vector3(xStart, yStart, 0f));

        for (int i = 0; i < rises; i++)
        {
            float x = Mathf.Lerp(xStart, xEnd, (i + 1f) / rises);
            float y = Mathf.Lerp(yStart, yEnd, i / (float)rises);
            float yNext = Mathf.Lerp(yStart, yEnd, (i + 1f) / rises);
            AddPoint(new Vector3(x, y, 0f));
            AddPoint(new Vector3(x, yNext, 0f));
        }
    }

    void AddCorner(float xStart, float xEnd, float yBase, float amp, int count)
    {
        float bend = Mathf.Lerp(xStart, xEnd, count == 0 ? 0.35f : 0.65f);
        AddPoint(new Vector3(xStart, yBase - amp, 0f));
        AddPoint(new Vector3(bend, yBase - amp, 0f));
        AddPoint(new Vector3(bend, yBase + amp, 0f));
        AddPoint(new Vector3(xEnd, yBase + amp, 0f));
    }

    void AddSlope(float xStart, float xEnd, float yBase, float amp)
    {
        AddPoint(new Vector3(xStart, yBase - amp, 0f));
        AddPoint(new Vector3(xEnd, yBase + amp, 0f));
    }

    void AddScurve(float xStart, float xEnd, float yBase, float amp)
    {
        const int count = 40;
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            float x = Mathf.Lerp(xStart, xEnd, t);
            float y = yBase + Mathf.Sin(t * Mathf.PI * 2f) * amp;
            AddPoint(new Vector3(x, y, 0f));
        }
    }

    void AddPoint(Vector3 point)
    {
        if (generatedPoints.Count > 0 && Vector3.Distance(generatedPoints[generatedPoints.Count - 1], point) < 0.004f)
            return;

        generatedPoints.Add(point);
    }
}
