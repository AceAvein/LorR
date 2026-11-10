#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the storyboard Reaction Light generator console:
/// tilted 4x3 (12) slot board + proper status screen, polished industrial look.
/// Menu: LorR / Reaction Light / Build Generator Console Prefab
/// </summary>
public static class ReactionLightConsoleBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Reaction Light";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ReactionLightGeneratorConsole.prefab";

    static readonly Color BoardColor = new Color(0.22f, 0.26f, 0.32f);
    static readonly Color BoardDark = new Color(0.14f, 0.16f, 0.20f);
    static readonly Color FrameColor = new Color(0.18f, 0.20f, 0.24f);
    static readonly Color Metal = new Color(0.45f, 0.48f, 0.52f);
    static readonly Color RingColor = new Color(0.38f, 0.42f, 0.48f);
    static readonly Color WellColor = new Color(0.08f, 0.09f, 0.11f);
    static readonly Color ScreenColor = new Color(0.04f, 0.07f, 0.10f);
    static readonly Color BezelColor = new Color(0.12f, 0.13f, 0.15f);
    static readonly Color PadIdle = new Color(0.88f, 0.10f, 0.10f);
    static readonly Color Accent = new Color(0.15f, 0.65f, 0.95f);
    static readonly Color Warn = new Color(0.95f, 0.7f, 0.1f);

    [MenuItem("LorR/Reaction Light/Build Generator Console Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material boardMat = GetOrCreateMaterial("RL_Board", BoardColor, 0.25f, 0.4f, false);
        Material boardDarkMat = GetOrCreateMaterial("RL_BoardDark", BoardDark, 0.3f, 0.35f, false);
        Material frameMat = GetOrCreateMaterial("RL_Frame", FrameColor, 0.4f, 0.35f, false);
        Material metalMat = GetOrCreateMaterial("RL_Metal", Metal, 0.55f, 0.45f, false);
        Material ringMat = GetOrCreateMaterial("RL_SlotRing", RingColor, 0.15f, 0.55f, false);
        Material wellMat = GetOrCreateMaterial("RL_SlotWell", WellColor, 0.05f, 0.25f, false);
        Material screenMat = GetOrCreateMaterial("RL_Screen", ScreenColor, 0.0f, 0.2f, true, Accent * 0.35f);
        Material bezelMat = GetOrCreateMaterial("RL_Bezel", BezelColor, 0.35f, 0.35f, false);
        Material padMat = GetOrCreateMaterial("RL_LightPad", PadIdle, 0.05f, 0.7f, true, PadIdle * 0.4f);
        Material accentMat = GetOrCreateMaterial("RL_Accent", Accent, 0.2f, 0.55f, true, Accent * 0.3f);
        Material warnMat = GetOrCreateMaterial("RL_Warn", Warn, 0.1f, 0.45f, false);

        GameObject root = new GameObject("ReactionLightGeneratorConsole");
        ReactionLightBoard board = root.AddComponent<ReactionLightBoard>();

        BuildCabinetBase(root.transform, frameMat, metalMat, warnMat);
        BuildFrame(root.transform, frameMat, metalMat, accentMat);
        Transform boardRoot = BuildBoard(root.transform, boardMat, boardDarkMat, accentMat);
        Transform[] slots = BuildSlots(boardRoot, ringMat, wellMat, padMat);
        board.slots = slots;
        BuildStatusScreen(root.transform, bezelMat, screenMat, metalMat, accentMat, board);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Reaction Light Console",
            "Rebuilt:\n" + PrefabPath +
            "\n\n• Proper monitor screen with TIME / SET COMPLETE\n" +
            "• Polished industrial console look\n\n" +
            "Re-drag into Reaction Light.unity and remove old Balls.",
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

    static void BuildCabinetBase(Transform parent, Material frame, Material metal, Material warn)
    {
        // Lower generator cabinet under the board
        Prim(parent, PrimitiveType.Cube, "Cabinet", new Vector3(0f, 0.45f, 0.22f), new Vector3(1.15f, 0.7f, 0.55f), frame);
        Prim(parent, PrimitiveType.Cube, "CabinetDoor", new Vector3(0f, 0.4f, -0.02f), new Vector3(0.95f, 0.5f, 0.03f), metal);
        Prim(parent, PrimitiveType.Cube, "CabinetHandle", new Vector3(0.35f, 0.4f, -0.05f), new Vector3(0.04f, 0.12f, 0.03f), warn);
        Prim(parent, PrimitiveType.Cube, "FootL", new Vector3(-0.45f, 0.04f, 0.22f), new Vector3(0.16f, 0.08f, 0.35f), metal);
        Prim(parent, PrimitiveType.Cube, "FootR", new Vector3(0.45f, 0.04f, 0.22f), new Vector3(0.16f, 0.08f, 0.35f), metal);
        Prim(parent, PrimitiveType.Cube, "WarnStripe", new Vector3(0f, 0.78f, -0.04f), new Vector3(1.0f, 0.03f, 0.04f), warn);
    }

    static void BuildFrame(Transform parent, Material frame, Material metal, Material accent)
    {
        Prim(parent, PrimitiveType.Cube, "FrameLeft", new Vector3(-0.58f, 1.25f, 0.08f), new Vector3(0.09f, 1.35f, 0.1f), frame);
        Prim(parent, PrimitiveType.Cube, "FrameRight", new Vector3(0.58f, 1.25f, 0.08f), new Vector3(0.09f, 1.35f, 0.1f), frame);
        Prim(parent, PrimitiveType.Cube, "FrameTop", new Vector3(0f, 1.95f, 0.08f), new Vector3(1.25f, 0.08f, 0.12f), metal);
        Prim(parent, PrimitiveType.Cube, "FrameCross", new Vector3(0f, 1.55f, 0.1f), new Vector3(1.1f, 0.04f, 0.05f), accent);

        // Side vents
        for (int i = 0; i < 4; i++)
        {
            float y = 0.95f + i * 0.12f;
            Prim(parent, PrimitiveType.Cube, "VentL_" + i, new Vector3(-0.58f, y, -0.02f), new Vector3(0.02f, 0.03f, 0.18f), metal);
            Prim(parent, PrimitiveType.Cube, "VentR_" + i, new Vector3(0.58f, y, -0.02f), new Vector3(0.02f, 0.03f, 0.18f), metal);
        }
    }

    static Transform BuildBoard(Transform parent, Material boardMat, Material darkMat, Material accent)
    {
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(parent, false);
        board.transform.localPosition = new Vector3(0f, 0.98f, 0.38f);
        board.transform.localRotation = Quaternion.Euler(26f, 0f, 0f);
        board.transform.localScale = new Vector3(1.12f, 0.09f, 0.82f);
        ApplyMat(board, boardMat);
        Object.DestroyImmediate(board.GetComponent<Collider>());

        // Raised rim around playfield
        Prim(board.transform, PrimitiveType.Cube, "RimFront", new Vector3(0f, 0.7f, -0.48f), new Vector3(1.02f, 0.35f, 0.04f), darkMat);
        Prim(board.transform, PrimitiveType.Cube, "RimBack", new Vector3(0f, 0.7f, 0.48f), new Vector3(1.02f, 0.35f, 0.04f), darkMat);
        Prim(board.transform, PrimitiveType.Cube, "RimLeft", new Vector3(-0.5f, 0.7f, 0f), new Vector3(0.04f, 0.35f, 0.95f), darkMat);
        Prim(board.transform, PrimitiveType.Cube, "RimRight", new Vector3(0.5f, 0.7f, 0f), new Vector3(0.04f, 0.35f, 0.95f), darkMat);
        Prim(board.transform, PrimitiveType.Cube, "AccentLine", new Vector3(0f, 0.85f, -0.52f), new Vector3(0.9f, 0.12f, 0.03f), accent);

        return board.transform;
    }

    static Transform[] BuildSlots(Transform board, Material ringMat, Material wellMat, Material padMat)
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
            slot.transform.localPosition = new Vector3(
                (startX + col * stepX) / board.localScale.x,
                0.58f,
                (startZ + row * stepZ) / board.localScale.z);
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = new Vector3(
                1f / board.localScale.x,
                1f / board.localScale.y,
                1f / board.localScale.z);

            Prim(slot.transform, PrimitiveType.Cylinder, "RingOuter", Vector3.zero, new Vector3(0.17f, 0.01f, 0.17f), ringMat);
            Prim(slot.transform, PrimitiveType.Cylinder, "Well", new Vector3(0f, -0.01f, 0f), new Vector3(0.13f, 0.012f, 0.13f), wellMat);
            Prim(slot.transform, PrimitiveType.Cylinder, "RingInner", new Vector3(0f, 0.01f, 0f), new Vector3(0.125f, 0.008f, 0.125f), ringMat);

            string ballName = i == 0 ? "Ball" : "Ball (" + i + ")";
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = ballName;
            ball.transform.SetParent(slot.transform, false);
            ball.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ball.transform.localScale = Vector3.one * 0.115f;
            ApplyMat(ball, padMat);

            slots[i] = ball.transform;
        }

        return slots;
    }

    static void BuildStatusScreen(Transform parent, Material bezel, Material screenMat, Material metal, Material accent, ReactionLightBoard boardComp)
    {
        GameObject screenRoot = new GameObject("StatusScreen");
        screenRoot.transform.SetParent(parent, false);
        // Same side as the play board (+Z) so the player sees TIME / SET COMPLETE
        screenRoot.transform.localPosition = new Vector3(0f, 1.58f, 0.12f);

        // Monitor housing between the posts — face toward board / player (+Z)
        Prim(screenRoot.transform, PrimitiveType.Cube, "MonitorHousing", Vector3.zero, new Vector3(1.05f, 0.48f, 0.08f), metal);
        Prim(screenRoot.transform, PrimitiveType.Cube, "MonitorBezel", new Vector3(0f, 0f, 0.03f), new Vector3(0.92f, 0.38f, 0.04f), bezel);
        Prim(screenRoot.transform, PrimitiveType.Cube, "ScreenPanel", new Vector3(0f, 0f, 0.05f), new Vector3(0.82f, 0.3f, 0.02f), screenMat);
        Prim(screenRoot.transform, PrimitiveType.Cube, "StatusLed", new Vector3(0.42f, 0.18f, 0.06f), new Vector3(0.03f, 0.03f, 0.02f), accent);

        // Soft hood shade above screen
        Prim(screenRoot.transform, PrimitiveType.Cube, "ScreenHood", new Vector3(0f, 0.22f, 0.06f), new Vector3(0.95f, 0.04f, 0.12f), bezel);

        GameObject canvasGo = new GameObject("ScreenCanvas");
        canvasGo.transform.SetParent(screenRoot.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, 0.062f);
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(780f, 280f);

        TMP_Text timeText = CreateLabel(canvasGo.transform, "TimeLabel", "TIME: 30", 58, new Vector2(0f, 55f), new Color(0.7f, 0.95f, 1f));
        TMP_Text statusText = CreateLabel(canvasGo.transform, "StatusLabel", "SET COMPLETE! PROCEED", 40, new Vector2(0f, -55f), new Color(0.45f, 1f, 0.65f));

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
        rect.sizeDelta = new Vector2(740f, 110f);
        rect.anchoredPosition = anchoredPos;
        return tmp;
    }

    static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        ApplyMat(go, mat);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
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
