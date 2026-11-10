#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the storyboard Reaction Light generator console:
/// tilted 4x3 (12) slot board + status screen, uniform low-poly look.
/// Menu: LorR / Reaction Light / Build Generator Console Prefab
/// </summary>
public static class ReactionLightConsoleBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Reaction Light";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ReactionLightGeneratorConsole.prefab";

    static readonly Color BoardColor = new Color(0.18f, 0.22f, 0.28f);
    static readonly Color FrameColor = new Color(0.12f, 0.14f, 0.18f);
    static readonly Color RingColor = new Color(0.35f, 0.40f, 0.48f);
    static readonly Color ScreenColor = new Color(0.05f, 0.08f, 0.12f);
    static readonly Color PadIdle = new Color(0.85f, 0.08f, 0.08f);

    [MenuItem("LorR/Reaction Light/Build Generator Console Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material boardMat = GetOrCreateMaterial("RL_Board", BoardColor, 0.2f, 0.4f, false);
        Material frameMat = GetOrCreateMaterial("RL_Frame", FrameColor, 0.35f, 0.35f, false);
        Material ringMat = GetOrCreateMaterial("RL_SlotRing", RingColor, 0.1f, 0.55f, false);
        Material screenMat = GetOrCreateMaterial("RL_Screen", ScreenColor, 0.0f, 0.2f, true, new Color(0.15f, 0.55f, 0.95f) * 0.25f);
        Material padMat = GetOrCreateMaterial("RL_LightPad", PadIdle, 0.05f, 0.65f, true, PadIdle * 0.35f);

        GameObject root = new GameObject("ReactionLightGeneratorConsole");
        ReactionLightBoard board = root.AddComponent<ReactionLightBoard>();

        // Room-scale console: board roughly table height, facing +Z
        BuildFrame(root.transform, frameMat);
        Transform boardRoot = BuildBoard(root.transform, boardMat);
        Transform[] slots = BuildSlots(boardRoot, ringMat, padMat);
        board.slots = slots;
        BuildStatusScreen(root.transform, screenMat, board);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Reaction Light Console",
            "Built prefab:\n" + PrefabPath + "\n\nDrag into Reaction Light.unity.\nRemove old floating Ball objects so only these 12 remain.",
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
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Reaction Light");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");
    }

    static void BuildFrame(Transform parent, Material mat)
    {
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "FrameLeft";
        left.transform.SetParent(parent, false);
        left.transform.localPosition = new Vector3(-0.55f, 1.15f, 0.05f);
        left.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
        ApplyMat(left, mat);
        Object.DestroyImmediate(left.GetComponent<Collider>());

        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = "FrameRight";
        right.transform.SetParent(parent, false);
        right.transform.localPosition = new Vector3(0.55f, 1.15f, 0.05f);
        right.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
        ApplyMat(right, mat);
        Object.DestroyImmediate(right.GetComponent<Collider>());
    }

    static Transform BuildBoard(Transform parent, Material mat)
    {
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(parent, false);
        // Tilted toward player (storyboard)
        board.transform.localPosition = new Vector3(0f, 0.95f, 0.35f);
        board.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
        board.transform.localScale = new Vector3(1.05f, 0.08f, 0.78f);
        ApplyMat(board, mat);
        Object.DestroyImmediate(board.GetComponent<Collider>());
        return board.transform;
    }

    static Transform[] BuildSlots(Transform board, Material ringMat, Material padMat)
    {
        Transform[] slots = new Transform[ReactionLightBoard.SlotCount];
        const float startX = -0.36f;
        const float startZ = 0.26f;
        const float stepX = 0.24f;
        const float stepZ = -0.22f;

        for (int i = 0; i < ReactionLightBoard.SlotCount; i++)
        {
            int col = i % ReactionLightBoard.Columns;
            int row = i / ReactionLightBoard.Columns;

            GameObject slot = new GameObject("Slot_" + i.ToString("00"));
            slot.transform.SetParent(board, false);
            // Board is scaled; place in local board space on top face
            slot.transform.localPosition = new Vector3(
                (startX + col * stepX) / board.localScale.x,
                0.55f,
                (startZ + row * stepZ) / board.localScale.z);
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = new Vector3(
                1f / board.localScale.x,
                1f / board.localScale.y,
                1f / board.localScale.z);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(slot.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.16f, 0.012f, 0.16f);
            ApplyMat(ring, ringMat);
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            GameObject well = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            well.name = "Well";
            well.transform.SetParent(slot.transform, false);
            well.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            well.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
            ApplyMat(well, ringMat);
            Object.DestroyImmediate(well.GetComponent<Collider>());

            string ballName = i == 0 ? "Ball" : "Ball (" + i + ")";
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = ballName;
            ball.transform.SetParent(slot.transform, false);
            ball.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            ball.transform.localScale = Vector3.one * 0.11f;
            ApplyMat(ball, padMat);
            // Keep sphere collider for poke detection radius approximation

            slots[i] = ball.transform;
        }

        return slots;
    }

    static void BuildStatusScreen(Transform parent, Material screenMat, ReactionLightBoard boardComp)
    {
        GameObject screenRoot = new GameObject("StatusScreen");
        screenRoot.transform.SetParent(parent, false);
        screenRoot.transform.localPosition = new Vector3(0f, 1.55f, 0.05f);
        screenRoot.transform.localRotation = Quaternion.identity;

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "ScreenPanel";
        panel.transform.SetParent(screenRoot.transform, false);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = new Vector3(0.95f, 0.42f, 0.04f);
        ApplyMat(panel, screenMat);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        GameObject canvasGo = new GameObject("ScreenCanvas");
        canvasGo.transform.SetParent(screenRoot.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.025f);
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 400f);

        TMP_Text timeText = CreateLabel(canvasGo.transform, "TimeLabel", "TIME: 30", 64, new Vector2(0f, 90f), new Color(0.85f, 0.95f, 1f));
        TMP_Text statusText = CreateLabel(canvasGo.transform, "StatusLabel", "SET COMPLETE! PROCEED", 48, new Vector2(0f, -70f), new Color(0.55f, 0.95f, 0.7f));

        boardComp.timeLabel = timeText;
        boardComp.statusLabel = statusText;
        statusText.gameObject.SetActive(false);
    }

    static TMP_Text CreateLabel(Transform parent, string name, string value, float fontSize, Vector2 anchoredPos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        RectTransform rect = tmp.rectTransform;
        rect.sizeDelta = new Vector2(860f, 120f);
        rect.anchoredPosition = anchoredPos;
        return tmp;
    }

    static Material GetOrCreateMaterial(string name, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor = default)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyMaterialSettings(existing, baseColor, metallic, smooth, emission, emissionColor);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        ApplyMaterialSettings(mat, baseColor, metallic, smooth, emission, emissionColor);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyMaterialSettings(Material mat, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor)
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

    static void ApplyMat(GameObject go, Material mat)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
    }
}
#endif
