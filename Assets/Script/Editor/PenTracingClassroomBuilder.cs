#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds storyboard Pen Tracing classroom props:
/// pen select table at front, then 3 desks with paper, entrance bell, table-mounted screen.
/// Menu: LorR / Pen Tracing / Build Classroom Prefab
/// </summary>
public static class PenTracingClassroomBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Pen Tracing";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/PenTracingClassroom.prefab";

    static readonly Color Wood = new Color(0.58f, 0.40f, 0.24f);
    static readonly Color WoodDark = new Color(0.34f, 0.22f, 0.13f);
    static readonly Color WoodLight = new Color(0.72f, 0.55f, 0.36f);
    static readonly Color Metal = new Color(0.62f, 0.65f, 0.68f);
    static readonly Color MetalDark = new Color(0.25f, 0.27f, 0.30f);
    static readonly Color Paper = new Color(0.96f, 0.95f, 0.91f);
    static readonly Color PaperLine = new Color(0.70f, 0.80f, 0.92f);
    static readonly Color BellBrass = new Color(0.82f, 0.64f, 0.20f);
    static readonly Color ScreenDark = new Color(0.07f, 0.09f, 0.11f);
    static readonly Color PencilYellow = new Color(0.94f, 0.80f, 0.16f);
    static readonly Color BallpenBlue = new Color(0.12f, 0.38f, 0.88f);
    static readonly Color Highlighter = new Color(0.78f, 0.95f, 0.18f);
    static readonly Color Accent = new Color(0.20f, 0.55f, 0.85f);

    [MenuItem("LorR/Pen Tracing/Build Classroom Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material wood = Mat("PT_Wood", Wood, 0.05f, 0.38f, false);
        Material woodDark = Mat("PT_WoodDark", WoodDark, 0.05f, 0.32f, false);
        Material woodLight = Mat("PT_WoodLight", WoodLight, 0.04f, 0.4f, false);
        Material metal = Mat("PT_Metal", Metal, 0.55f, 0.45f, false);
        Material metalDark = Mat("PT_MetalDark", MetalDark, 0.65f, 0.35f, false);
        Material paper = Mat("PT_Paper", Paper, 0.0f, 0.22f, false);
        Material paperLine = Mat("PT_PaperLine", PaperLine, 0.0f, 0.22f, false);
        Material brass = Mat("PT_Bell", BellBrass, 0.75f, 0.6f, true, BellBrass * 0.15f);
        Material screen = Mat("PT_Screen", ScreenDark, 0.0f, 0.25f, true, new Color(0.25f, 0.7f, 1f) * 0.22f);
        Material pencil = Mat("PT_Pencil", PencilYellow, 0.05f, 0.42f, false);
        Material ballpen = Mat("PT_Ballpen", BallpenBlue, 0.2f, 0.55f, false);
        Material highlighter = Mat("PT_Highlighter", Highlighter, 0.05f, 0.48f, false);
        Material accent = Mat("PT_Accent", Accent, 0.2f, 0.5f, true, Accent * 0.2f);

        GameObject root = new GameObject("PenTracingClassroom");
        PenTracingClassroom marker = root.AddComponent<PenTracingClassroom>();

        // 1) Pen table FIRST / at the front (bungad / unahan)
        marker.penSelectStation = BuildPenSelectStation(
            root.transform, wood, woodDark, woodLight, metal, metalDark, pencil, ballpen, highlighter, accent);

        // Shared entrance bell at bungad (near pen station)
        Transform entranceBell = BuildBell(root.transform, "EntranceBell", new Vector3(0.55f, 0.84f, -0.05f), brass, metalDark, metal);
        // Keep first bell slot as entrance bell for wiring convenience
        marker.bells[0] = entranceBell;

        // 2) Three tracing desks deeper in the room (no chairs)
        float[] deskX = { -1.35f, 0f, 1.35f };
        for (int i = 0; i < 3; i++)
        {
            Transform desk = BuildTraceDesk(
                root.transform,
                i + 1,
                new Vector3(deskX[i], 0f, 1.15f),
                wood, woodDark, woodLight, metal, metalDark, paper, paperLine, brass, screen, accent,
                out Transform board, out Transform deskBell, out TMP_Text monitor);

            marker.desks[i] = desk;
            marker.tracingBoards[i] = board;
            // desks still expose a small desk bell; entrance bell remains primary "bungad"
            if (i > 0)
                marker.bells[i] = deskBell;
            marker.deskMonitors[i] = monitor;
        }

        Prim(root.transform, PrimitiveType.Cylinder, "NpcStandMarker", new Vector3(0f, 0.015f, 2.35f), new Vector3(0.45f, 0.02f, 0.45f), metalDark);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Pen Tracing Classroom",
            "Rebuilt:\n" + PrefabPath +
            "\n\n• Pen table at front\n• Entrance bell at bungad\n• Screens mounted on desks\n• No chairs\n\nRe-drag into Pen Control Tracing.unity",
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

    static Transform BuildPenSelectStation(
        Transform parent,
        Material wood,
        Material woodDark,
        Material woodLight,
        Material metal,
        Material metalDark,
        Material pencil,
        Material ballpen,
        Material highlighter,
        Material accent)
    {
        GameObject station = new GameObject("PenSelectStation");
        station.transform.SetParent(parent, false);
        // Front / first in flow
        station.transform.localPosition = new Vector3(0f, 0f, 0.05f);

        // Nicer table: thicker top + edge trim + shelf
        Prim(station.transform, PrimitiveType.Cube, "TableTop", new Vector3(0f, 0.78f, 0f), new Vector3(1.35f, 0.07f, 0.62f), wood);
        Prim(station.transform, PrimitiveType.Cube, "TableEdge", new Vector3(0f, 0.74f, 0f), new Vector3(1.38f, 0.025f, 0.65f), woodDark);
        Prim(station.transform, PrimitiveType.Cube, "UnderShelf", new Vector3(0f, 0.45f, 0f), new Vector3(1.15f, 0.03f, 0.45f), woodLight);
        BuildTableLegs(station.transform, woodDark, metalDark, 1.15f, 0.48f);

        // Accent runner strip
        Prim(station.transform, PrimitiveType.Cube, "AccentStrip", new Vector3(0f, 0.82f, -0.28f), new Vector3(1.2f, 0.015f, 0.04f), accent);

        AddWorldText(station.transform, "SelectPenLabel", "SELECT PEN", new Vector3(0f, 1.12f, -0.05f), 44, Color.white, 0.0013f, new Vector2(460f, 90f));

        BuildPenSlot(station.transform, "Pencil", new Vector3(-0.4f, 0.86f, 0.05f), pencil, metal, metalDark, new Vector3(0.022f, 0.19f, 0.022f));
        BuildPenSlot(station.transform, "Ballpen", new Vector3(0f, 0.86f, 0.05f), ballpen, metal, metalDark, new Vector3(0.02f, 0.18f, 0.02f));
        BuildPenSlot(station.transform, "Highlighter", new Vector3(0.4f, 0.86f, 0.05f), highlighter, metal, metalDark, new Vector3(0.03f, 0.15f, 0.03f));

        return station.transform;
    }

    static void BuildPenSlot(Transform parent, string penName, Vector3 pos, Material penMat, Material standMat, Material baseMat, Vector3 penScale)
    {
        GameObject slot = new GameObject(penName + "Slot");
        slot.transform.SetParent(parent, false);
        slot.transform.localPosition = pos;

        Prim(slot.transform, PrimitiveType.Cylinder, "StandBase", new Vector3(0f, 0.01f, 0f), new Vector3(0.1f, 0.015f, 0.1f), baseMat);
        Prim(slot.transform, PrimitiveType.Cylinder, "StandCup", new Vector3(0f, 0.04f, 0f), new Vector3(0.07f, 0.03f, 0.07f), standMat);

        GameObject pen = Prim(slot.transform, PrimitiveType.Cylinder, penName, new Vector3(0f, 0.14f, 0f), penScale, penMat);
        pen.transform.localRotation = Quaternion.Euler(12f, 0f, 18f);
        Prim(pen.transform, PrimitiveType.Sphere, "Tip", new Vector3(0f, -0.55f, 0f), new Vector3(0.85f, 0.4f, 0.85f), baseMat);
        Prim(pen.transform, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0.45f, 0f), new Vector3(1.05f, 0.12f, 1.05f), standMat);

        AddWorldText(slot.transform, "Label", penName.ToUpperInvariant(), new Vector3(0f, 0.32f, 0.14f), 22, Color.white, 0.001f, new Vector2(210f, 48f));
    }

    static Transform BuildTraceDesk(
        Transform parent,
        int index,
        Vector3 localPos,
        Material wood,
        Material woodDark,
        Material woodLight,
        Material metal,
        Material metalDark,
        Material paper,
        Material paperLine,
        Material brass,
        Material screen,
        Material accent,
        out Transform board,
        out Transform bell,
        out TMP_Text monitorText)
    {
        GameObject desk = new GameObject("TraceDesk_0" + index);
        desk.transform.SetParent(parent, false);
        desk.transform.localPosition = localPos;

        Prim(desk.transform, PrimitiveType.Cube, "DeskTop", new Vector3(0f, 0.76f, 0f), new Vector3(1.05f, 0.07f, 0.68f), wood);
        Prim(desk.transform, PrimitiveType.Cube, "DeskEdge", new Vector3(0f, 0.72f, 0f), new Vector3(1.08f, 0.025f, 0.71f), woodDark);
        Prim(desk.transform, PrimitiveType.Cube, "DeskShelf", new Vector3(0f, 0.42f, 0f), new Vector3(0.9f, 0.03f, 0.5f), woodLight);
        BuildTableLegs(desk.transform, woodDark, metalDark, 0.9f, 0.52f);
        Prim(desk.transform, PrimitiveType.Cube, "AccentStrip", new Vector3(0f, 0.805f, -0.3f), new Vector3(0.95f, 0.012f, 0.035f), accent);

        // Tracing paper board
        GameObject boardGo = Prim(desk.transform, PrimitiveType.Cube, "TracingBoard", new Vector3(-0.05f, 0.81f, 0.08f), new Vector3(0.58f, 0.012f, 0.42f), paper);
        board = boardGo.transform;
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineA", new Vector3(0f, 0.7f, -0.2f), new Vector3(0.88f, 0.15f, 0.02f), paperLine);
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineB", new Vector3(0f, 0.7f, 0f), new Vector3(0.88f, 0.15f, 0.02f), paperLine);
        Prim(boardGo.transform, PrimitiveType.Cube, "GuideLineC", new Vector3(0f, 0.7f, 0.2f), new Vector3(0.88f, 0.15f, 0.02f), paperLine);
        Prim(boardGo.transform, PrimitiveType.Cube, "PaperClip", new Vector3(0f, 0.9f, 0.48f), new Vector3(0.2f, 0.4f, 0.08f), metal);

        // Finished paper tray
        Prim(desk.transform, PrimitiveType.Cube, "PaperTray", new Vector3(0.38f, 0.8f, 0.18f), new Vector3(0.2f, 0.025f, 0.24f), woodDark);
        Prim(desk.transform, PrimitiveType.Cube, "FinishedPaper", new Vector3(0.38f, 0.82f, 0.18f), new Vector3(0.16f, 0.006f, 0.2f), paper);

        // Small desk bell (secondary); entrance bell is primary at bungad
        bell = BuildBell(desk.transform, "DeskBell", new Vector3(-0.38f, 0.82f, -0.22f), brass, metalDark, metal);

        // Screen STUCK to table (rear edge, flush / slightly tilted)
        GameObject monitor = new GameObject("DeskMonitor");
        monitor.transform.SetParent(desk.transform, false);
        monitor.transform.localPosition = new Vector3(0.28f, 0.95f, -0.28f);
        monitor.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorBase", new Vector3(0f, -0.12f, 0.04f), new Vector3(0.28f, 0.03f, 0.12f), metalDark);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorFrame", Vector3.zero, new Vector3(0.36f, 0.24f, 0.04f), metalDark);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorBezel", new Vector3(0f, 0f, -0.01f), new Vector3(0.33f, 0.21f, 0.02f), metal);
        Prim(monitor.transform, PrimitiveType.Cube, "MonitorScreen", new Vector3(0f, 0f, -0.025f), new Vector3(0.29f, 0.17f, 0.015f), screen);

        GameObject canvasGo = new GameObject("MonitorCanvas");
        canvasGo.transform.SetParent(monitor.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.035f);
        canvasGo.transform.localScale = Vector3.one * 0.001f;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 160f);
        GameObject textGo = new GameObject("MetricsLabel");
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "DESK " + index + "\nREADY";
        tmp.fontSize = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.55f, 0.9f, 1f);
        tmp.rectTransform.sizeDelta = new Vector2(260f, 140f);
        monitorText = tmp;

        AddWorldText(desk.transform, "DeskLabel", "DESK " + index, new Vector3(0f, 1.28f, 0f), 30, Color.white, 0.00115f, new Vector2(220f, 55f));

        return desk.transform;
    }

    static Transform BuildBell(Transform parent, string name, Vector3 localPos, Material brass, Material baseMat, Material buttonMat)
    {
        GameObject bellRoot = new GameObject(name);
        bellRoot.transform.SetParent(parent, false);
        bellRoot.transform.localPosition = localPos;

        Prim(bellRoot.transform, PrimitiveType.Cylinder, "BellPlate", new Vector3(0f, 0.005f, 0f), new Vector3(0.1f, 0.01f, 0.1f), baseMat);
        Prim(bellRoot.transform, PrimitiveType.Sphere, "BellDome", new Vector3(0f, 0.055f, 0f), new Vector3(0.11f, 0.075f, 0.11f), brass);
        Prim(bellRoot.transform, PrimitiveType.Cylinder, "BellButton", new Vector3(0f, 0.1f, 0f), new Vector3(0.035f, 0.016f, 0.035f), buttonMat);
        return bellRoot.transform;
    }

    static void BuildTableLegs(Transform parent, Material woodMat, Material footMat, float spanX, float spanZ)
    {
        float y = 0.36f;
        float h = 0.7f;
        Vector3[] corners =
        {
            new Vector3(-spanX * 0.45f, y, spanZ * 0.4f),
            new Vector3(spanX * 0.45f, y, spanZ * 0.4f),
            new Vector3(-spanX * 0.45f, y, -spanZ * 0.4f),
            new Vector3(spanX * 0.45f, y, -spanZ * 0.4f)
        };
        string[] names = { "LegFL", "LegFR", "LegBL", "LegBR" };
        for (int i = 0; i < 4; i++)
        {
            Prim(parent, PrimitiveType.Cube, names[i], corners[i], new Vector3(0.055f, h, 0.055f), woodMat);
            Prim(parent, PrimitiveType.Cube, names[i] + "Foot", corners[i] + new Vector3(0f, -h * 0.5f - 0.01f, 0f), new Vector3(0.1f, 0.025f, 0.1f), footMat);
        }
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
