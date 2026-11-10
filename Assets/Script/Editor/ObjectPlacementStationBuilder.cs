#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a factory/airport-style Object Placement station:
/// conveyor with integrated left tunnel mouth (groceries exit here),
/// industrial legs/feet, A/B/C sort bins, start button, set screen.
/// Menu: LorR / Object Placement / Build Station Prefab
/// </summary>
public static class ObjectPlacementStationBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Object Placement";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ObjectPlacementStation.prefab";

    static readonly Color Steel = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color SteelDark = new Color(0.28f, 0.30f, 0.33f);
    static readonly Color WarningYellow = new Color(0.92f, 0.72f, 0.08f);
    static readonly Color BeltRubber = new Color(0.12f, 0.12f, 0.13f);
    static readonly Color TunnelInterior = new Color(0.16f, 0.17f, 0.19f);
    static readonly Color Pedestal = new Color(0.32f, 0.34f, 0.38f);
    static readonly Color ButtonGreen = new Color(0.12f, 0.72f, 0.22f);
    static readonly Color ScreenDark = new Color(0.06f, 0.08f, 0.10f);
    static readonly Color Red = new Color(0.82f, 0.14f, 0.10f);
    static readonly Color Blue = new Color(0.12f, 0.32f, 0.78f);
    static readonly Color Yellow = new Color(0.92f, 0.78f, 0.12f);
    static readonly Color PlasticWhite = new Color(0.90f, 0.91f, 0.93f);

    [MenuItem("LorR/Object Placement/Build Station Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material steelMat = Mat("OP_Steel", Steel, 0.55f, 0.45f, false);
        Material steelDarkMat = Mat("OP_SteelDark", SteelDark, 0.6f, 0.35f, false);
        Material warnMat = Mat("OP_Warning", WarningYellow, 0.15f, 0.4f, false);
        Material beltMat = Mat("OP_Belt", BeltRubber, 0.05f, 0.25f, false);
        Material tunnelMat = Mat("OP_TunnelInterior", TunnelInterior, 0.1f, 0.2f, false);
        Material pedestalMat = Mat("OP_Pedestal", Pedestal, 0.25f, 0.35f, false);
        Material buttonMat = Mat("OP_StartButton", ButtonGreen, 0.05f, 0.55f, true, ButtonGreen * 0.4f);
        Material screenMat = Mat("OP_Screen", ScreenDark, 0.0f, 0.2f, true, new Color(0.15f, 0.7f, 0.35f) * 0.25f);
        Material redMat = Mat("OP_BoxRed", Red, 0.08f, 0.45f, false);
        Material blueMat = Mat("OP_BoxBlue", Blue, 0.08f, 0.45f, false);
        Material yellowMat = Mat("OP_BoxYellow", Yellow, 0.08f, 0.45f, false);
        Material labelMat = Mat("OP_LabelPlate", PlasticWhite, 0.0f, 0.35f, false);

        GameObject root = new GameObject("ObjectPlacementStation");
        ObjectPlacementStation station = root.AddComponent<ObjectPlacementStation>();

        Transform conveyor = BuildFactoryConveyor(root.transform, steelMat, steelDarkMat, warnMat, beltMat, tunnelMat);
        station.conveyor = conveyor;

        Transform tunnel = conveyor.Find("ExitTunnel");
        station.supplyBox = tunnel;
        Transform spawn = tunnel != null ? tunnel.Find("GrocerySpawnPoint") : null;
        station.grocerySpawnPoint = spawn;

        Transform boxes = BuildSortBins(root.transform, steelDarkMat, redMat, blueMat, yellowMat, labelMat);
        station.boxesRoot = boxes;
        station.redBox = boxes.Find("Red Box");
        station.blueBox = boxes.Find("Blue Box");
        station.yellowBox = boxes.Find("Yellow Box");

        station.startButton = BuildStartButton(root.transform, pedestalMat, buttonMat, steelDarkMat);
        BuildSetScreen(root.transform, steelDarkMat, screenMat, station);

        root.transform.position = new Vector3(0f, 0f, 0.75f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Object Placement Station",
            "Rebuilt station:\n" + PrefabPath +
            "\n\n• Left conveyor EXIT TUNNEL (airport/factory mouth)\n• Groceries come out onto the belt\n• Industrial legs/feet + A/B/C bins\n\nRe-drag into Color Sort.unity.",
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
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Object Placement");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");
    }

    static Transform BuildFactoryConveyor(Transform parent, Material steel, Material steelDark, Material warn, Material belt, Material tunnelInterior)
    {
        GameObject conveyor = new GameObject("Conveyor");
        conveyor.transform.SetParent(parent, false);
        conveyor.transform.localPosition = new Vector3(0.2f, 0.95f, 0.40f);

        // Main run (open sorting section)
        Prim(conveyor.transform, PrimitiveType.Cube, "Chassis", new Vector3(0.15f, -0.06f, 0f), new Vector3(1.7f, 0.14f, 0.58f), steel);
        Prim(conveyor.transform, PrimitiveType.Cube, "BeltSurface", new Vector3(0.15f, 0.05f, 0f), new Vector3(1.55f, 0.025f, 0.42f), belt);
        Prim(conveyor.transform, PrimitiveType.Cube, "RailFront", new Vector3(0.15f, 0.08f, 0.24f), new Vector3(1.6f, 0.08f, 0.035f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "RailBack", new Vector3(0.15f, 0.08f, -0.24f), new Vector3(1.6f, 0.08f, 0.035f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "WarnStripe", new Vector3(0.55f, 0.12f, 0.24f), new Vector3(0.35f, 0.02f, 0.04f), warn);

        // Right end roller + motor
        GameObject rollR = Prim(conveyor.transform, PrimitiveType.Cylinder, "RollerRight", new Vector3(0.95f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), steelDark);
        rollR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(conveyor.transform, PrimitiveType.Cube, "MotorHousing", new Vector3(1.05f, -0.12f, 0.28f), new Vector3(0.22f, 0.18f, 0.22f), steelDark);

        // Airport/factory EXIT TUNNEL on LEFT — groceries come out of this mouth onto the belt
        BuildExitTunnel(conveyor.transform, steel, steelDark, warn, belt, tunnelInterior);

        // Legs with feet
        BuildLeg(conveyor.transform, "Leg_FL", new Vector3(-0.55f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_FR", new Vector3(0.7f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BL", new Vector3(-0.55f, -0.55f, -0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BR", new Vector3(0.7f, -0.55f, -0.2f), steel, steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceFront", new Vector3(0.1f, -0.75f, 0.2f), new Vector3(1.15f, 0.04f, 0.04f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceBack", new Vector3(0.1f, -0.75f, -0.2f), new Vector3(1.15f, 0.04f, 0.04f), steelDark);

        // Extra support under tunnel
        BuildLeg(conveyor.transform, "Leg_TunnelF", new Vector3(-1.15f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_TunnelB", new Vector3(-1.15f, -0.55f, -0.2f), steel, steelDark);

        return conveyor.transform;
    }

    static void BuildExitTunnel(Transform conveyor, Material steel, Material steelDark, Material warn, Material belt, Material interior)
    {
        GameObject tunnel = new GameObject("ExitTunnel");
        tunnel.transform.SetParent(conveyor, false);
        tunnel.transform.localPosition = new Vector3(-1.05f, 0.12f, 0f);

        // Covered hood / box section (like baggage claim mouth)
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelRoof", new Vector3(0f, 0.28f, 0f), new Vector3(0.85f, 0.06f, 0.62f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelLeftWall", new Vector3(0f, 0.05f, 0.29f), new Vector3(0.85f, 0.42f, 0.05f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelRightWall", new Vector3(0f, 0.05f, -0.29f), new Vector3(0.85f, 0.42f, 0.05f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelBack", new Vector3(-0.4f, 0.05f, 0f), new Vector3(0.05f, 0.42f, 0.58f), steelDark);

        // Dark interior so the opening reads as a tunnel
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelCeilingIn", new Vector3(0.05f, 0.22f, 0f), new Vector3(0.7f, 0.03f, 0.5f), interior);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelFloorIn", new Vector3(0.05f, -0.12f, 0f), new Vector3(0.7f, 0.03f, 0.5f), interior);

        // Belt continues into the tunnel
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelBelt", new Vector3(0.1f, -0.07f, 0f), new Vector3(0.75f, 0.025f, 0.4f), belt);

        // Mouth frame facing the open conveyor (where items appear)
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameTop", new Vector3(0.42f, 0.28f, 0f), new Vector3(0.06f, 0.08f, 0.64f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameBot", new Vector3(0.42f, -0.14f, 0f), new Vector3(0.06f, 0.06f, 0.64f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameL", new Vector3(0.42f, 0.07f, 0.30f), new Vector3(0.06f, 0.4f, 0.06f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameR", new Vector3(0.42f, 0.07f, -0.30f), new Vector3(0.06f, 0.4f, 0.06f), steelDark);

        // Warning chevrons around mouth
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnTop", new Vector3(0.45f, 0.32f, 0f), new Vector3(0.04f, 0.03f, 0.55f), warn);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnL", new Vector3(0.45f, 0.07f, 0.33f), new Vector3(0.04f, 0.35f, 0.03f), warn);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnR", new Vector3(0.45f, 0.07f, -0.33f), new Vector3(0.04f, 0.35f, 0.03f), warn);

        // Rubber flap strip at mouth (airport vibe)
        Prim(tunnel.transform, PrimitiveType.Cube, "RubberFlap", new Vector3(0.48f, 0.12f, 0f), new Vector3(0.02f, 0.22f, 0.48f), interior);

        // Spawn point just outside the mouth, on the belt
        GameObject spawn = new GameObject("GrocerySpawnPoint");
        spawn.transform.SetParent(tunnel.transform, false);
        spawn.transform.localPosition = new Vector3(0.55f, -0.02f, 0f);

        AddWorldText(tunnel.transform, "ExitLabel", "EXIT", new Vector3(0.5f, 0.42f, 0f), 36, WarningYellow, 0.001f, new Vector2(220f, 70f));
    }

    static void BuildLeg(Transform parent, string name, Vector3 localPos, Material steel, Material dark)
    {
        GameObject leg = new GameObject(name);
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = localPos;

        Prim(leg.transform, PrimitiveType.Cube, "Post", Vector3.zero, new Vector3(0.07f, 0.85f, 0.07f), steel);
        Prim(leg.transform, PrimitiveType.Cube, "FootPlate", new Vector3(0f, -0.45f, 0f), new Vector3(0.22f, 0.03f, 0.22f), dark);
        Prim(leg.transform, PrimitiveType.Cylinder, "BoltA", new Vector3(0.07f, -0.42f, 0.07f), new Vector3(0.03f, 0.02f, 0.03f), dark);
        Prim(leg.transform, PrimitiveType.Cylinder, "BoltB", new Vector3(-0.07f, -0.42f, -0.07f), new Vector3(0.03f, 0.02f, 0.03f), dark);
    }

    static Transform BuildSortBins(Transform parent, Material steelDark, Material red, Material blue, Material yellow, Material label)
    {
        GameObject boxes = new GameObject("Boxes");
        boxes.transform.SetParent(parent, false);

        CreateBin(boxes.transform, "Red Box", "A", new Vector3(-0.2f, 0.42f, -0.05f), red, label, steelDark);
        CreateBin(boxes.transform, "Blue Box", "B", new Vector3(0.35f, 0.42f, -0.05f), blue, label, steelDark);
        CreateBin(boxes.transform, "Yellow Box", "C", new Vector3(0.9f, 0.42f, -0.05f), yellow, label, steelDark);

        return boxes.transform;
    }

    static void CreateBin(Transform parent, string objectName, string letter, Vector3 localPos, Material boxMat, Material labelMat, Material footMat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = new Vector3(0.38f, 0.5f, 0.38f);
        Apply(box, boxMat);

        Prim(box.transform, PrimitiveType.Cube, "Rim", new Vector3(0f, 0.48f, 0f), new Vector3(1.05f, 0.08f, 1.05f), boxMat);
        Prim(box.transform, PrimitiveType.Cube, "FootFL", new Vector3(-0.35f, -0.52f, 0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootFR", new Vector3(0.35f, -0.52f, 0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBL", new Vector3(-0.35f, -0.52f, -0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBR", new Vector3(0.35f, -0.52f, -0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);

        GameObject plate = Prim(box.transform, PrimitiveType.Cube, "LabelPlate", new Vector3(0f, 0.1f, -0.52f), new Vector3(0.55f, 0.4f, 0.05f), labelMat);
        AddWorldText(plate.transform, "Letter", letter, new Vector3(0f, 0f, -0.7f), 72, Color.black, 0.02f, new Vector2(80f, 80f));
    }

    static Transform BuildStartButton(Transform parent, Material pedestal, Material button, Material metal)
    {
        GameObject root = new GameObject("StartConveyorButton");
        root.transform.SetParent(parent, false);
        // Near tunnel mouth / left side
        root.transform.localPosition = new Vector3(-1.05f, 0f, 0.0f);

        Prim(root.transform, PrimitiveType.Cylinder, "PedestalPost", new Vector3(0f, 0.4f, 0f), new Vector3(0.1f, 0.4f, 0.1f), pedestal);
        Prim(root.transform, PrimitiveType.Cylinder, "PedestalFoot", new Vector3(0f, 0.03f, 0f), new Vector3(0.28f, 0.03f, 0.28f), metal);
        Prim(root.transform, PrimitiveType.Cylinder, "PedestalTop", new Vector3(0f, 0.82f, 0f), new Vector3(0.2f, 0.03f, 0.2f), pedestal);
        Prim(root.transform, PrimitiveType.Cylinder, "Btn_Start_World", new Vector3(0f, 0.88f, 0f), new Vector3(0.13f, 0.035f, 0.13f), button);

        AddWorldText(root.transform, "PressStartCue", "PRESS START!", new Vector3(0f, 1.1f, 0f), 42, Color.white, 0.0014f, new Vector2(480f, 100f));
        return root.transform;
    }

    static void BuildSetScreen(Transform parent, Material steelDark, Material screenMat, ObjectPlacementStation station)
    {
        GameObject screenRoot = new GameObject("SetScreen");
        screenRoot.transform.SetParent(parent, false);
        screenRoot.transform.localPosition = new Vector3(0.35f, 1.65f, 0.62f);

        Prim(screenRoot.transform, PrimitiveType.Cube, "MonitorFrame", Vector3.zero, new Vector3(0.95f, 0.42f, 0.06f), steelDark);
        Prim(screenRoot.transform, PrimitiveType.Cube, "ScreenPanel", new Vector3(0f, 0f, -0.02f), new Vector3(0.82f, 0.32f, 0.03f), screenMat);
        Prim(screenRoot.transform, PrimitiveType.Cube, "MonitorPost", new Vector3(0f, -0.5f, 0.02f), new Vector3(0.07f, 0.6f, 0.07f), steelDark);
        Prim(screenRoot.transform, PrimitiveType.Cube, "MonitorBase", new Vector3(0f, -0.82f, 0.02f), new Vector3(0.28f, 0.04f, 0.2f), steelDark);

        GameObject canvasGo = new GameObject("ScreenCanvas");
        canvasGo.transform.SetParent(screenRoot.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.04f);
        canvasGo.transform.localScale = Vector3.one * 0.001f;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 280f);

        GameObject textGo = new GameObject("SetLabel");
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "SCREEN SET 1";
        tmp.fontSize = 60;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.45f, 1f, 0.55f);
        tmp.rectTransform.sizeDelta = new Vector2(740f, 200f);
        station.setScreenLabel = tmp;
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
