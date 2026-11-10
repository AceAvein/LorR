#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds storyboard Pen Tracing classroom props:
/// pen select workstation + 3 desks with paper/board, bell, metrics monitor, chair.
/// Menu: LorR / Pen Tracing / Build Classroom Prefab
/// </summary>
public static class PenTracingClassroomBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Pen Tracing";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/PenTracingClassroom.prefab";

    static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
    static readonly Color WoodDark = new Color(0.35f, 0.24f, 0.14f);
    static readonly Color Metal = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color MetalDark = new Color(0.28f, 0.30f, 0.33f);
    static readonly Color Paper = new Color(0.95f, 0.94f, 0.90f);
    static readonly Color PaperLine = new Color(0.75f, 0.82f, 0.92f);
    static readonly Color BellBrass = new Color(0.78f, 0.62f, 0.22f);
    static readonly Color ScreenDark = new Color(0.08f, 0.10f, 0.12f);
    static readonly Color PencilYellow = new Color(0.92f, 0.78f, 0.18f);
    static readonly Color BallpenBlue = new Color(0.15f, 0.35f, 0.85f);
    static readonly Color Highlighter = new Color(0.85f, 0.95f, 0.2f);
    static readonly Color ChairCloth = new Color(0.25f, 0.35f, 0.48f);

    [MenuItem("LorR/Pen Tracing/Build Classroom Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material wood = Mat("PT_Wood", Wood, 0.05f, 0.35f, false);
        Material woodDark = Mat("PT_WoodDark", WoodDark, 0.05f, 0.3f, false);
        Material metal = Mat("PT_Metal", Metal, 0.55f, 0.4f, false);
        Material metalDark = Mat("PT_MetalDark", MetalDark, 0.6f, 0.35f, false);
        Material paper = Mat("PT_Paper", Paper, 0.0f, 0.2f, false);
        Material paperLine = Mat("PT_PaperLine", PaperLine, 0.0f, 0.2f, false);
        Material brass = Mat("PT_Bell", BellBrass, 0.7f, 0.55f, false);
        Material screen = Mat("PT_Screen", ScreenDark, 0.0f, 0.2f, true, new Color(0.3f, 0.7f, 1f) * 0.2f);
        Material pencil = Mat("PT_Pencil", PencilYellow, 0.05f, 0.4f, false);
        Material ballpen = Mat("PT_Ballpen", BallpenBlue, 0.15f, 0.5f, false);
        Material highlighter = Mat("PT_Highlighter", Highlighter, 0.05f, 0.45f, false);
        Material cloth = Mat("PT_Chair", ChairCloth, 0.0f, 0.25f, false);

        GameObject root = new GameObject("PenTracingClassroom");
        PenTracingClassroom marker = root.AddComponent<PenTracingClassroom>();

        marker.penSelectStation = BuildPenSelectStation(root.transform, wood, woodDark, metal, pencil, ballpen, highlighter);

        float[] deskX = { -1.4f, 0f, 1.4f };
        for (int i = 0; i < 3; i++)
        {
            Transform desk = BuildTraceDesk(
                root.transform,
                i + 1,
                new Vector3(deskX[i], 0f, 0.9f),
                wood, woodDark, metal, metalDark, paper, paperLine, brass, screen, cloth,
                out Transform board, out Transform bell, out TMP_Text monitor);

            marker.desks[i] = desk;
            marker.tracingBoards[i] = board;
            marker.bells[i] = bell;
            marker.deskMonitors[i] = monitor;
        }

        // NPC observer stand (front of room, storyboard)
        Prim(root.transform, PrimitiveType.Cylinder, "NpcStandMarker", new Vector3(0f, 0.01f, 2.2f), new Vector3(0.5f, 0.02f, 0.5f), metalDark);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Pen Tracing Classroom",
            "Built prefab:\n" + PrefabPath +
            "\n\n• Pen select station (Pencil / Ballpen / Highlighter)\n" +
            "• 3 desks with TracingBoard, bell, monitor, chair\n\n" +
            "Drag into Pen Control Tracing.unity.\n" +
            "Keep existing PathManager/patterns; this is storyboard environment.",
            "OK");

        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
        if (prefab != null)
            Selection.activeObject = prefab;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Scene Simulation"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Scene Simulation");
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Pen Tracing");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");
    }

    static Transform BuildPenSelectStation(Transform parent, Material wood, Material woodDark, Material metal, Material pencil, Material ballpen, Material highlighter)
    {
        GameObject station = new GameObject("PenSelectStation");
        station.transform.SetParent(parent, false);
        station.transform.localPosition = new Vector3(0f, 0f, -0.2f);

        // Table
        Prim(station.transform, PrimitiveType.Cube, "TableTop", new Vector3(0f, 0.78f, 0f), new Vector3(1.2f, 0.06f, 0.55f), wood);
        BuildTableLegs(station.transform, woodDark, 1.0f, 0.4f);

        AddWorldText(station.transform, "SelectPenLabel", "SELECT PEN", new Vector3(0f, 1.05f, 0f), 40, Color.white, 0.0012f, new Vector2(420f, 80f));

        // Pen stands + visual pens (names help PenGrabChoice if you later add grab)
        BuildPenSlot(station.transform, "Pencil", new Vector3(-0.35f, 0.84f, 0f), pencil, metal, new Vector3(0.02f, 0.18f, 0.02f));
        BuildPenSlot(station.transform, "Ballpen", new Vector3(0f, 0.84f, 0f), ballpen, metal, new Vector3(0.018f, 0.17f, 0.018f));
        BuildPenSlot(station.transform, "Highlighter", new Vector3(0.35f, 0.84f, 0f), highlighter, metal, new Vector3(0.028f, 0.14f, 0.028f));

        return station.transform;
    }

    static void BuildPenSlot(Transform parent, string penName, Vector3 pos, Material penMat, Material standMat, Vector3 penScale)
    {
        GameObject slot = new GameObject(penName + "Slot");
        slot.transform.SetParent(parent, false);
        slot.transform.localPosition = pos;

        Prim(slot.transform, PrimitiveType.Cylinder, "Stand", new Vector3(0f, 0.02f, 0f), new Vector3(0.08f, 0.02f, 0.08f), standMat);

        GameObject pen = Prim(slot.transform, PrimitiveType.Cylinder, penName, new Vector3(0f, 0.12f, 0f), penScale, penMat);
        pen.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
        // tip
        Prim(pen.transform, PrimitiveType.Sphere, "Tip", new Vector3(0f, -0.55f, 0f), new Vector3(0.8f, 0.35f, 0.8f), standMat);

        AddWorldText(slot.transform, "Label", penName.ToUpperInvariant(), new Vector3(0f, 0.28f, 0.12f), 24, Color.white, 0.001f, new Vector2(200f, 50f));
    }

    static Transform BuildTraceDesk(
        Transform parent,
        int index,
        Vector3 localPos,
        Material wood,
        Material woodDark,
        Material metal,
        Material metalDark,
        Material paper,
        Material paperLine,
        Material brass,
        Material screen,
        Material cloth,
        out Transform board,
        out Transform bell,
        out TMP_Text monitorText)
    {
        GameObject desk = new GameObject("TraceDesk_0" + index);
        desk.transform.SetParent(parent, false);
        desk.transform.localPosition = localPos;

        Prim(desk.transform, PrimitiveType.Cube, "DeskTop", new Vector3(0f, 0.76f, 0f), new Vector3(1.0f, 0.06f, 0.65f), wood);
        BuildTableLegs(desk.transform, woodDark, 0.85f, 0.5f);

        // Paper / tracing surface — named TracingBoard for storyboard clarity
        GameObject boardGo = Prim(desk.transform, PrimitiveType.Cube, "TracingBoard", new Vector3(0f, 0.81f, 0.05f), new Vector3(0.55f, 0.01f, 0.4f), paper);
        board = boardGo.transform;
        // Guide lines (visual only)
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineA", new Vector3(0f, 0.6f, -0.15f), new Vector3(0.9f, 0.2f, 0.02f), paperLine);
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineB", new Vector3(0f, 0.6f, 0.05f), new Vector3(0.9f, 0.2f, 0.02f), paperLine);
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineC", new Vector3(0f, 0.6f, 0.25f), new Vector3(0.9f, 0.2f, 0.02f), paperLine);

        // Finished paper tray (storyboard: get paper after tracing)
        Prim(desk.transform, PrimitiveType.Cube, "PaperTray", new Vector3(0.35f, 0.8f, -0.2f), new Vector3(0.18f, 0.02f, 0.22f), woodDark);
        Prim(desk.transform, PrimitiveType.Cube, "FinishedPaper", new Vector3(0.35f, 0.82f, -0.2f), new Vector3(0.15f, 0.005f, 0.2f), paper);

        // Bell
        GameObject bellRoot = new GameObject("Bell");
        bellRoot.transform.SetParent(desk.transform, false);
        bellRoot.transform.localPosition = new Vector3(-0.35f, 0.82f, -0.18f);
        Prim(bellRoot.transform, PrimitiveType.Cylinder, "BellBase", new Vector3(0f, 0.01f, 0f), new Vector3(0.08f, 0.01f, 0.08f), metalDark);
        Prim(bellRoot.transform, PrimitiveType.Sphere, "BellDome", new Vector3(0f, 0.05f, 0f), new Vector3(0.1f, 0.07f, 0.1f), brass);
        Prim(bellRoot.transform, PrimitiveType.Cylinder, "BellButton", new Vector3(0f, 0.09f, 0f), new Vector3(0.03f, 0.015f, 0.03f), metal);
        bell = bellRoot.transform;

        // Metrics monitor
        GameObject monitor = new GameObject("DeskMonitor");
        monitor.transform.SetParent(desk.transform, false);
        monitor.transform.localPosition = new Vector3(0f, 1.15f, -0.28f);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorFrame", Vector3.zero, new Vector3(0.35f, 0.22f, 0.04f), metalDark);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorScreen", new Vector3(0f, 0f, -0.02f), new Vector3(0.3f, 0.17f, 0.02f), screen);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorArm", new Vector3(0f, -0.18f, 0.05f), new Vector3(0.03f, 0.2f, 0.03f), metal);

        GameObject canvasGo = new GameObject("MonitorCanvas");
        canvasGo.transform.SetParent(monitor.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.03f);
        canvasGo.transform.localScale = Vector3.one * 0.001f;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 160f);
        GameObject textGo = new GameObject("MetricsLabel");
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "DESK " + index + "\nREADY";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.6f, 0.9f, 1f);
        tmp.rectTransform.sizeDelta = new Vector2(260f, 140f);
        monitorText = tmp;

        // Chair
        BuildChair(desk.transform, cloth, metalDark);

        AddWorldText(desk.transform, "DeskLabel", "DESK " + index, new Vector3(0f, 1.4f, 0f), 32, Color.white, 0.0012f, new Vector2(240f, 60f));

        return desk.transform;
    }

    static void BuildChair(Transform desk, Material cloth, Material metal)
    {
        GameObject chair = new GameObject("Chair");
        chair.transform.SetParent(desk, false);
        chair.transform.localPosition = new Vector3(0f, 0f, 0.55f);

        Prim(chair.transform, PrimitiveType.Cube, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.05f, 0.4f), cloth);
        Prim(chair.transform, PrimitiveType.Cube, "Back", new Vector3(0f, 0.7f, 0.18f), new Vector3(0.4f, 0.45f, 0.05f), cloth);
        Prim(chair.transform, PrimitiveType.Cylinder, "LegFL", new Vector3(-0.15f, 0.22f, 0.15f), new Vector3(0.04f, 0.22f, 0.04f), metal);
        Prim(chair.transform, PrimitiveType.Cylinder, "LegFR", new Vector3(0.15f, 0.22f, 0.15f), new Vector3(0.04f, 0.22f, 0.04f), metal);
        Prim(chair.transform, PrimitiveType.Cylinder, "LegBL", new Vector3(-0.15f, 0.22f, -0.15f), new Vector3(0.04f, 0.22f, 0.04f), metal);
        Prim(chair.transform, PrimitiveType.Cylinder, "LegBR", new Vector3(0.15f, 0.22f, -0.15f), new Vector3(0.04f, 0.22f, 0.04f), metal);
    }

    static void BuildTableLegs(Transform parent, Material mat, float spanX, float spanZ)
    {
        float y = 0.36f;
        float h = 0.7f;
        Prim(parent, PrimitiveType.Cube, "LegFL", new Vector3(-spanX * 0.45f, y, spanZ * 0.4f), new Vector3(0.06f, h, 0.06f), mat);
        Prim(parent, PrimitiveType.Cube, "LegFR", new Vector3(spanX * 0.45f, y, spanZ * 0.4f), new Vector3(0.06f, h, 0.06f), mat);
        Prim(parent, PrimitiveType.Cube, "LegBL", new Vector3(-spanX * 0.45f, y, -spanZ * 0.4f), new Vector3(0.06f, h, 0.06f), mat);
        Prim(parent, PrimitiveType.Cube, "LegBR", new Vector3(spanX * 0.45f, y, -spanZ * 0.4f), new Vector3(0.06f, h, 0.06f), mat);
    }

    static void AddWorldText(Transform parent, string name, string value, Vector3 localPos, float fontSize, Color color, float scale, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * scale;
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.GetComponent<RectTransform>().sizeDelta = size;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = size;
    }

    static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Apply(go, mat);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Material Mat(string name, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor = default)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyMatSettings(existing, baseColor, metallic, smooth, emission, emissionColor);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.name = name;
        ApplyMatSettings(mat, baseColor, metallic, smooth, emission, emissionColor);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyMatSettings(Material mat, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", smooth);
        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissionColor == default ? baseColor * 0.3f : emissionColor);
        }
    }

    static void Apply(GameObject go, Material mat)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
    }
}
#endif
