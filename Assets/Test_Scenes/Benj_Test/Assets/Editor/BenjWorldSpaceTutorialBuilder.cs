using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;

/// <summary>
/// Editor-only builder. Generates the full two-panel World Space Tutorial UI
/// with one click, so nothing has to be built by hand in the Editor.
///
/// Menu: Benj UI World Space -> Build Reference Tutorial
///
/// Layout notes (per your latest simplification):
/// - Root "Benj_Tutorial_WorldSpace" sits at local position (0,0,0).
/// - LEFT canvas local rotation Y = -5 degrees.
/// - RIGHT canvas local rotation Y = +5 degrees.
/// - Pressing a row button on the LEFT panel shows its guideline image on the
///   RIGHT panel (idle state shows a centered stopwatch icon).
/// </summary>
public static class BenjWorldSpaceTutorialBuilder
{
    // ---------- Colors ----------
    static readonly Color32 DarkGreen    = new Color32(0x0F, 0x32, 0x14, 0xFF);
    static readonly Color32 MediumGreen  = new Color32(0x1E, 0x58, 0x28, 0xFF);
    static readonly Color32 Lime         = new Color32(0xA6, 0xE2, 0x5D, 0xFF);
    static readonly Color32 PastelLime   = new Color32(0xDB, 0xF1, 0xA8, 0xFF);
    static readonly Color32 BorderGreen  = new Color32(0x93, 0xC0, 0x65, 0xFF);
    static readonly Color32 DarkText     = new Color32(0x1C, 0x28, 0x0D, 0xFF);
    static readonly Color32 RightTop     = new Color32(0x20, 0x70, 0x28, 0xFF);
    static readonly Color32 RightBottom  = new Color32(0x0D, 0x3E, 0x14, 0xFF);
    static readonly Color White = Color.white;

    // ---------- World-space sizing ----------
    // Canvas contents are authored in "UI units" (like normal pixels), then the
    // whole canvas GameObject is scaled down so the panel ends up the right
    // physical size in the VR world. Keeping one shared scale keeps both
    // panels visually consistent.
    const float UI_SCALE = 0.004f;

    const float LEFT_W = 390f, LEFT_H = 500f;
    const float RIGHT_W = 490f, RIGHT_H = 500f;
    const float GAP_WORLD = 0.22f;

    static Font _font;
    static Font UIFont
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }
    }

    [MenuItem("Benj UI World Space/Build Reference Tutorial")]
    public static void BuildTutorial()
    {
        // ---- Root ----
        GameObject existingRoot = GameObject.Find("Benj_Tutorial_WorldSpace");
        if (existingRoot != null)
        {
            if (!EditorUtility.DisplayDialog("Benj Tutorial UI",
                "'Benj_Tutorial_WorldSpace' already exists in the scene. Delete and rebuild it?",
                "Rebuild", "Cancel"))
                return;
            Object.DestroyImmediate(existingRoot);
        }

        GameObject root = new GameObject("Benj_Tutorial_WorldSpace");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        EnsureEventSystem();

        TutorialUIController controller = root.AddComponent<TutorialUIController>();

        // ---- LEFT canvas ----
        float leftHalfW = (LEFT_W * UI_SCALE) * 0.5f;
        float rightHalfW = (RIGHT_W * UI_SCALE) * 0.5f;
        float leftX = -(leftHalfW + GAP_WORLD * 0.5f);
        float rightX = (rightHalfW + GAP_WORLD * 0.5f);

        Canvas leftCanvas = CreateWorldCanvas(
            "LEFT WINDOW — Navigation", root.transform,
            new Vector3(leftX, 0f, 0f), -5f, new Vector2(LEFT_W, LEFT_H));

        BuildLeftPanel(leftCanvas.transform, controller);

        // ---- RIGHT canvas ----
        Canvas rightCanvas = CreateWorldCanvas(
            "RIGHT WINDOW — Guideline Preview", root.transform,
            new Vector3(rightX, 0f, 0f), 5f, new Vector2(RIGHT_W, RIGHT_H));

        BuildRightPanel(rightCanvas.transform, controller);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("[Benj UI World Space] Tutorial UI built. Root: Benj_Tutorial_WorldSpace " +
                  "(currently at local position 0,0,0 — drag it in front of the player in the Inspector).");
    }

    // ------------------------------------------------------------------
    // Canvas / EventSystem setup
    // ------------------------------------------------------------------

    static Canvas CreateWorldCanvas(string name, Transform parent, Vector3 localPos, float yRotDeg, Vector2 sizeUiUnits)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yRotDeg, 0f);
        go.transform.localScale = Vector3.one * UI_SCALE;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = sizeUiUnits;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        // Standalone input module lets you test with a mouse in the Editor.
        // For an actual VR build, see the setup notes in the chat response —
        // you'll add an XR UI Input Module (from XR Interaction Toolkit)
        // alongside/instead of this one.
        es.AddComponent<StandaloneInputModule>();
    }

    // ------------------------------------------------------------------
    // LEFT PANEL
    // ------------------------------------------------------------------

    static void BuildLeftPanel(Transform canvasT, TutorialUIController controller)
    {
        // Border (slightly larger, sits behind the surface)
        CreateImage("Border", canvasT, MakeRoundedSprite((int)LEFT_W + 12, (int)LEFT_H + 12, 20, BorderGreen),
            TopLeft(-6, -6), new Vector2(LEFT_W + 12, LEFT_H + 12));

        // Panel surface (gradient fill)
        CreateImage("Left Window Surface", canvasT, MakeRoundedGradientSprite((int)LEFT_W, (int)LEFT_H, 16, MediumGreen, DarkGreen),
            TopLeft(0, 0), new Vector2(LEFT_W, LEFT_H));

        // Simple physical thickness so the panel doesn't read as paper-flat.
        AddDepthSlab(canvasT, LEFT_W, LEFT_H, DarkGreen);

        // Title
        CreateText("Tutorial", canvasT, "Tutorial", 34, White, FontStyle.Bold, TextAnchor.UpperLeft,
            TopLeft(24, 20), new Vector2(220, 50));

        // Back button (protrudes past the top-right edge)
        Button back = CreateButton("Back Button", canvasT,
            MakeRoundedSprite(90, 40, 16, PastelLime),
            TopLeft(LEFT_W - 70, 8), new Vector2(90, 40));
        CreateText("Back", back.transform, "Back", 18, DarkText, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, new Vector2(90, 40));
        UnityEventTools.AddPersistentListener(back.onClick, controller.ResetToIdle);

        // Basic Controls row
        CreateRow("Basic Controls Row", canvasT, TopLeft(24, 90), "Basic Controls Tutorial",
            18, controller, "Basic Controls");

        // Training Modes banner (tight, does not span full width)
        CreateBanner(canvasT, TopLeft(24, 150));

        // Training mode rows
        CreateRow("Ball Drop Reflex Drill Row", canvasT, TopLeft(24, 202), "Ball Drop Reflex Drill",
            16, controller, "Ball Drop Reflex Drill");
        CreateRow("Color Sort Row", canvasT, TopLeft(24, 252), "Color Sort",
            16, controller, "Object Placement Drill"); // TODO: swap to "Color Sort" once that art exists
        CreateRow("Reaction Light Burst Row", canvasT, TopLeft(24, 302), "Reaction light burst\ndrill",
            16, controller, "Reaction Light Burst Drill", rowHeight: 60);
        CreateRow("Pen Tracing Row", canvasT, TopLeft(24, 372), "Pen Tracing",
            16, controller, "Pen Control Tracing");
    }

    static void CreateRow(string name, Transform parent, Vector2 pos, string label, int fontSize,
        TutorialUIController controller, string resourceName, float rowHeight = 46f)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        row.transform.SetParent(parent, false);
        RectTransform rt = row.GetComponent<RectTransform>();
        SetTopLeft(rt, pos.x, pos.y, new Vector2(320, rowHeight));

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0f); // invisible, just for raycasting the whole row

        CreateImage("Lime Square", row.transform, MakeRoundedSprite(32, 32, 8, Lime),
            new Vector2(0, -(rowHeight - 32) / 2f), new Vector2(32, 32));

        CreateText("Label", row.transform, label, fontSize, White, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Vector2(44, 0), new Vector2(260, rowHeight));

        Button button = row.GetComponent<Button>();
        UnityEventTools.AddStringPersistentListener(button.onClick, controller.ShowGuideline, resourceName);
    }

    static void CreateBanner(Transform parent, Vector2 pos)
    {
        // Tight banner that hugs the "Training Modes" text.
        Vector2 size = new Vector2(178, 36);
        CreateImage("Training Modes Banner Border", parent, MakeRoundedSprite((int)size.x + 6, (int)size.y + 6, 14, BorderGreen),
            new Vector2(pos.x - 3, pos.y - 3), size + new Vector2(6, 6));
        GameObject banner = CreateImage("Training Modes Banner", parent, MakeRoundedSprite((int)size.x, (int)size.y, 12, DarkGreen),
            pos, size);
        CreateText("Training Modes", banner.transform, "Training Modes", 16, White, FontStyle.Bold,
            TextAnchor.MiddleCenter, Vector2.zero, size);
    }

    // ------------------------------------------------------------------
    // RIGHT PANEL
    // ------------------------------------------------------------------

    static void BuildRightPanel(Transform canvasT, TutorialUIController controller)
    {
        CreateImage("Border", canvasT, MakeRoundedSprite((int)RIGHT_W + 12, (int)RIGHT_H + 12, 20, BorderGreen),
            TopLeft(-6, -6), new Vector2(RIGHT_W + 12, RIGHT_H + 12));

        CreateImage("Right Window Surface", canvasT, MakeRoundedGradientSprite((int)RIGHT_W, (int)RIGHT_H, 16, RightTop, RightBottom),
            TopLeft(0, 0), new Vector2(RIGHT_W, RIGHT_H));

        AddDepthSlab(canvasT, RIGHT_W, RIGHT_H, RightBottom);

        // Idle stopwatch icon, centered
        float iconSize = 170f;
        GameObject idleIcon = CreateImage("Idle Stopwatch Guideline Icon", canvasT,
            MakeStopwatchSprite((int)iconSize),
            new Vector2((RIGHT_W - iconSize) / 2f, (RIGHT_H - iconSize) / 2f), new Vector2(iconSize, iconSize),
            useCenterAnchor: false);

        // Guideline image (hidden until a row is pressed)
        Vector2 guidelinePos = new Vector2(30, 30);
        Vector2 guidelineSize = new Vector2(RIGHT_W - 60, RIGHT_H - 60);
        GameObject guidelineGO = new GameObject("Guideline Image", typeof(RectTransform), typeof(Image));
        guidelineGO.transform.SetParent(canvasT, false);
        SetTopLeft(guidelineGO.GetComponent<RectTransform>(), guidelinePos.x, guidelinePos.y, guidelineSize);
        Image guidelineImg = guidelineGO.GetComponent<Image>();
        guidelineImg.preserveAspect = true;
        guidelineImg.sprite = null;
        guidelineGO.SetActive(false);

        controller.idleStopwatchIcon = idleIcon;
        controller.guidelineImage = guidelineImg;
    }

    // ------------------------------------------------------------------
    // Generic UI helpers (all use a top-left, y-down coordinate system so
    // positions read like normal mockup coordinates: x = right, y = down)
    // ------------------------------------------------------------------

    static Vector2 TopLeft(float x, float y) => new Vector2(x, y);

    static void SetTopLeft(RectTransform rt, float x, float y, Vector2 size)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, -y);
    }

    static GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, bool useCenterAnchor = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (useCenterAnchor)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
        else
        {
            SetTopLeft(rt, pos.x, pos.y, size);
        }
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        return go;
    }

    static Text CreateText(string name, Transform parent, string content, int fontSize, Color color,
        FontStyle style, TextAnchor anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), pos.x, pos.y, size);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = UIFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.4f);
        shadow.effectDistance = new Vector2(1, -1);

        return text;
    }

    static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), pos.x, pos.y, size);
        go.GetComponent<Image>().sprite = sprite;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.35f);
        shadow.effectDistance = new Vector2(0, -2);

        return go.GetComponent<Button>();
    }

    /// <summary>
    /// A thin 3D box placed just behind the canvas plane so the panel reads
    /// as a physical slab with depth rather than a flat, paper-thin plane.
    /// </summary>
    static void AddDepthSlab(Transform canvasT, float widthUi, float heightUi, Color32 color)
    {
        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Depth Slab";
        Object.DestroyImmediate(slab.GetComponent<Collider>());
        slab.transform.SetParent(canvasT, false);

        float thicknessWorld = 0.03f;
        float thicknessLocal = thicknessWorld / UI_SCALE;

        slab.transform.localScale = new Vector3(widthUi, heightUi, thicknessLocal);
        // Canvas front faces local -Z; push the slab back along +Z by half its thickness.
        slab.transform.localPosition = new Vector3(widthUi / 2f, -heightUi / 2f, thicknessLocal / 2f);

        Renderer rend = slab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        rend.sharedMaterial = mat;
    }

    // ------------------------------------------------------------------
    // Procedural sprite generation (rounded rects, gradients, stopwatch icon)
    // ------------------------------------------------------------------

    static bool InCorner(int x, int y, int cx, int cy, int r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

    static bool InsideRoundedRect(int x, int y, int w, int h, int r)
    {
        if (x >= r && x < w - r) return true;
        if (y >= r && y < h - r) return true;
        if (x < r && y < r) return InCorner(x, y, r, r, r);
        if (x >= w - r && y < r) return InCorner(x, y, w - r - 1, r, r);
        if (x < r && y >= h - r) return InCorner(x, y, r, h - r - 1, r);
        if (x >= w - r && y >= h - r) return InCorner(x, y, w - r - 1, h - r - 1, r);
        return true;
    }

    static Sprite MakeRoundedSprite(int w, int h, int r, Color32 fill)
    {
        return MakeRoundedGradientSprite(w, h, r, fill, fill);
    }

    static Sprite MakeRoundedGradientSprite(int w, int h, int r, Color32 top, Color32 bottom)
    {
        w = Mathf.Max(w, 4); h = Mathf.Max(h, 4); r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color32 rowColor = Color32.Lerp(bottom, top, t); // texture y=0 is bottom
            for (int x = 0; x < w; x++)
            {
                bool inside = InsideRoundedRect(x, y, w, h, r);
                pixels[y * w + x] = inside ? rowColor : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeStopwatchSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        float cx = size * 0.5f;
        float cy = size * 0.46f; // leave room for the crown above
        float faceR = size * 0.40f;
        float ringThickness = size * 0.035f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outer lime ring
                if (dist <= faceR && dist >= faceR - ringThickness)
                {
                    pixels[y * size + x] = BorderGreen;
                    continue;
                }
                // Dark face
                if (dist < faceR - ringThickness)
                {
                    pixels[y * size + x] = DarkGreen;
                }
            }
        }

        // Tick marks (12, radial)
        for (int i = 0; i < 12; i++)
        {
            float angle = i * (Mathf.PI * 2f / 12f);
            float innerR = faceR * 0.78f;
            float outerR = faceR * 0.92f;
            DrawRadialLine(pixels, size, cx, cy, angle, innerR, outerR, Lime, 2);
        }

        // Hand pointing to ~11 o'clock
        DrawRadialLine(pixels, size, cx, cy, -Mathf.PI / 2f - 0.9f, 0, faceR * 0.62f, Lime, 3);

        // Center hub
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= 9) pixels[y * size + x] = Lime;
            }

        // Top crown / stopwatch button
        int crownW = Mathf.RoundToInt(size * 0.16f);
        int crownH = Mathf.RoundToInt(size * 0.10f);
        int crownX0 = Mathf.RoundToInt(cx - crownW / 2f);
        int crownY0 = Mathf.RoundToInt(cy + faceR - ringThickness * 0.5f);
        for (int y = crownY0; y < crownY0 + crownH && y < size; y++)
            for (int x = crownX0; x < crownX0 + crownW && x < size; x++)
                if (x >= 0 && y >= 0) pixels[y * size + x] = BorderGreen;

        // Small side knobs
        int knobSize = Mathf.RoundToInt(size * 0.06f);
        DrawSquare(pixels, size, (int)(cx - faceR * 0.75f), (int)(cy + faceR * 0.75f), knobSize, Lime);
        DrawSquare(pixels, size, (int)(cx + faceR * 0.75f), (int)(cy + faceR * 0.75f), knobSize, Lime);

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void DrawRadialLine(Color32[] pixels, int size, float cx, float cy, float angle, float rStart, float rEnd, Color32 color, int thickness)
    {
        int steps = Mathf.CeilToInt(rEnd - rStart) + 4;
        for (int s = 0; s <= steps; s++)
        {
            float r = Mathf.Lerp(rStart, rEnd, s / (float)steps);
            float px = cx + Mathf.Cos(angle) * r;
            float py = cy + Mathf.Sin(angle) * r;
            DrawSquare(pixels, size, Mathf.RoundToInt(px), Mathf.RoundToInt(py), thickness, color);
        }
    }

    static void DrawSquare(Color32[] pixels, int size, int px, int py, int half, Color32 color)
    {
        for (int y = py - half; y <= py + half; y++)
            for (int x = px - half; x <= px + half; x++)
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = color;
    }
}
