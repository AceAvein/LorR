#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a factory-style Object Placement station:
/// side supply box (groceries come out), industrial conveyor with legs/feet,
/// A/B/C sort bins, start button, set screen.
/// Menu: LorR / Object Placement / Build Station Prefab
/// </summary>
public static class ObjectPlacementStationBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Object Placement";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ObjectPlacementStation.prefab";

    // Industrial factory palette
    static readonly Color Steel = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color SteelDark = new Color(0.28f, 0.30f, 0.33f);
    static readonly Color WarningYellow = new Color(0.92f, 0.72f, 0.08f);
    static readonly Color BeltRubber = new Color(0.12f, 0.12f, 0.13f);
    static readonly Color Cardboard = new Color(0.72f, 0.55f, 0.32f);
    static readonly Color CardboardDark = new Color(0.48f, 0.34f, 0.18f);
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
        Material cardboardMat = Mat("OP_Cardboard", Cardboard, 0.0f, 0.2f, false);
        Material cardboardDarkMat = Mat("OP_CardboardDark", CardboardDark, 0.0f, 0.2f, false);
        Material pedestalMat = Mat("OP_Pedestal", Pedestal, 0.25f, 0.35f, false);
        Material buttonMat = Mat("OP_StartButton", ButtonGreen, 0.05f, 0.55f, true, ButtonGreen * 0.4f);
        Material screenMat = Mat("OP_Screen", ScreenDark, 0.0f, 0.2f, true, new Color(0.15f, 0.7f, 0.35f) * 0.25f);
        Material redMat = Mat("OP_BoxRed", Red, 0.08f, 0.45f, false);
        Material blueMat = Mat("OP_BoxBlue", Blue, 0.08f, 0.45f, false);
        Material yellowMat = Mat("OP_BoxYellow", Yellow, 0.08f, 0.45f, false);
        Material labelMat = Mat("OP_LabelPlate", PlasticWhite, 0.0f, 0.35f, false);

        GameObject root = new GameObject("ObjectPlacementStation");
        ObjectPlacementStation station = root.AddComponent<ObjectPlacementStation>();

        Transform conveyor = BuildFactoryConveyor(root.transform, steelMat, steelDarkMat, warnMat, beltMat);
        station.conveyor = conveyor;

        Transform supply = BuildSupplyBox(root.transform, cardboardMat, cardboardDarkMat, steelDarkMat);
        station.supplyBox = supply;
        Transform spawn = supply.Find("GrocerySpawnPoint");
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
            "Rebuilt factory station:\n" + PrefabPath +
            "\n\n• Left supply crate (grocery exit)\n• Industrial conveyor with legs/feet\n• A/B/C sort bins (Red/Blue/Yellow)\n\nRe-drag into Color Sort.unity if replacing old prefab.",
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

    static Transform BuildFactoryConveyor(Transform parent, Material steel, Material steelDark, Material warn, Material belt)
    {
        GameObject conveyor = new GameObject("Conveyor");
        conveyor.transform.SetParent(parent, false);
        conveyor.transform.localPosition = new Vector3(0.15f, 0.95f, 0.40f);

        // Main chassis
        Prim(conveyor.transform, PrimitiveType.Cube, "Chassis", new Vector3(0f, -0.06f, 0f), new Vector3(2.1f, 0.14f, 0.58f), steel);
        // Belt top
        Prim(conveyor.transform, PrimitiveType.Cube, "BeltSurface", new Vector3(0f, 0.05f, 0f), new Vector3(1.95f, 0.025f, 0.42f), belt);
        // Side rails
        Prim(conveyor.transform, PrimitiveType.Cube, "RailLeft", new Vector3(0f, 0.08f, 0.24f), new Vector3(2.05f, 0.08f, 0.035f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "RailRight", new Vector3(0f, 0.08f, -0.24f), new Vector3(2.05f, 0.08f, 0.035f), steelDark);
        // Warning stripes on rails
        Prim(conveyor.transform, PrimitiveType.Cube, "WarnStripeL", new Vector3(-0.7f, 0.12f, 0.24f), new Vector3(0.35f, 0.02f, 0.04f), warn);
        Prim(conveyor.transform, PrimitiveType.Cube, "WarnStripeR", new Vector3(0.7f, 0.12f, -0.24f), new Vector3(0.35f, 0.02f, 0.04f), warn);

        // End rollers
        GameObject rollL = Prim(conveyor.transform, PrimitiveType.Cylinder, "RollerLeft", new Vector3(-1.02f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), steelDark);
        rollL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject rollR = Prim(conveyor.transform, PrimitiveType.Cylinder, "RollerRight", new Vector3(1.02f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), steelDark);
        rollR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // Motor / gearbox on right end (factory detail)
        Prim(conveyor.transform, PrimitiveType.Cube, "MotorHousing", new Vector3(1.15f, -0.12f, 0.28f), new Vector3(0.22f, 0.18f, 0.22f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cylinder, "MotorDrum", new Vector3(1.15f, -0.12f, 0.42f), new Vector3(0.12f, 0.05f, 0.12f), steel);

        // Control box on conveyor side
        Prim(conveyor.transform, PrimitiveType.Cube, "ControlBox", new Vector3(0.85f, 0.18f, 0.30f), new Vector3(0.16f, 0.2f, 0.08f), steelDark);

        // Industrial legs with foot plates (4 corners)
        BuildLeg(conveyor.transform, "Leg_FL", new Vector3(-0.75f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_FR", new Vector3(0.75f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BL", new Vector3(-0.75f, -0.55f, -0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BR", new Vector3(0.75f, -0.55f, -0.2f), steel, steelDark);

        // Cross braces between legs
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceFront", new Vector3(0f, -0.75f, 0.2f), new Vector3(1.4f, 0.04f, 0.04f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceBack", new Vector3(0f, -0.75f, -0.2f), new Vector3(1.4f, 0.04f, 0.04f), steelDark);

        return conveyor.transform;
    }

    static void BuildLeg(Transform parent, string name, Vector3 localPos, Material steel, Material dark)
    {
        GameObject leg = new GameObject(name);
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = localPos;

        // Vertical post
        Prim(leg.transform, PrimitiveType.Cube, "Post", new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.85f, 0.07f), steel);
        // Foot plate (paa)
        Prim(leg.transform, PrimitiveType.Cube, "FootPlate", new Vector3(0f, -0.45f, 0f), new Vector3(0.22f, 0.03f, 0.22f), dark);
        // Anchor bolts (visual)
        Prim(leg.transform, PrimitiveType.Cylinder, "BoltA", new Vector3(0.07f, -0.42f, 0.07f), new Vector3(0.03f, 0.02f, 0.03f), dark);
        Prim(leg.transform, PrimitiveType.Cylinder, "BoltB", new Vector3(-0.07f, -0.42f, -0.07f), new Vector3(0.03f, 0.02f, 0.03f), dark);
    }

    static Transform BuildSupplyBox(Transform parent, Material cardboard, Material cardboardDark, Material metal)
    {
        // Storyboard: side delivery box where unsorted groceries come out
        GameObject supply = new GameObject("SupplyCrate");
        supply.transform.SetParent(parent, false);
        supply.transform.localPosition = new Vector3(-1.15f, 0.55f, 0.40f);

        // Pallet under crate
        Prim(supply.transform, PrimitiveType.Cube, "Pallet", new Vector3(0f, -0.28f, 0f), new Vector3(0.55f, 0.06f, 0.45f), cardboardDark);
        Prim(supply.transform, PrimitiveType.Cube, "PalletSlatA", new Vector3(0f, -0.22f, 0.12f), new Vector3(0.55f, 0.03f, 0.06f), cardboard);
        Prim(supply.transform, PrimitiveType.Cube, "PalletSlatB", new Vector3(0f, -0.22f, -0.12f), new Vector3(0.55f, 0.03f, 0.06f), cardboard);

        // Open-front cardboard crate
        Prim(supply.transform, PrimitiveType.Cube, "CrateFloor", new Vector3(0f, -0.15f, 0f), new Vector3(0.5f, 0.04f, 0.4f), cardboard);
        Prim(supply.transform, PrimitiveType.Cube, "CrateBack", new Vector3(0f, 0.1f, 0.18f), new Vector3(0.5f, 0.45f, 0.04f), cardboard);
        Prim(supply.transform, PrimitiveType.Cube, "CrateLeft", new Vector3(-0.24f, 0.1f, 0f), new Vector3(0.04f, 0.45f, 0.4f), cardboard);
        Prim(supply.transform, PrimitiveType.Cube, "CrateRight", new Vector3(0.24f, 0.1f, 0f), new Vector3(0.04f, 0.45f, 0.4f), cardboard);
        Prim(supply.transform, PrimitiveType.Cube, "CrateTopLip", new Vector3(0f, 0.32f, 0.05f), new Vector3(0.5f, 0.04f, 0.3f), cardboardDark);

        // Metal chute / mouth facing conveyor (where groceries exit toward belt)
        Prim(supply.transform, PrimitiveType.Cube, "ExitChute", new Vector3(0.32f, 0.05f, 0f), new Vector3(0.18f, 0.2f, 0.28f), metal);

        // Spawn anchor at chute mouth (for future grocery spawn)
        GameObject spawn = new GameObject("GrocerySpawnPoint");
        spawn.transform.SetParent(supply.transform, false);
        spawn.transform.localPosition = new Vector3(0.42f, 0.08f, 0f);

        // Sample grocery placeholders inside crate (visual only)
        Prim(supply.transform, PrimitiveType.Cube, "SampleItem_A", new Vector3(-0.08f, -0.02f, -0.05f), new Vector3(0.12f, 0.12f, 0.12f), cardboardDark);
        Prim(supply.transform, PrimitiveType.Sphere, "SampleItem_B", new Vector3(0.05f, -0.02f, 0.05f), new Vector3(0.1f, 0.1f, 0.1f), cardboard);
        Prim(supply.transform, PrimitiveType.Cylinder, "SampleItem_C", new Vector3(-0.02f, 0.0f, 0.08f), new Vector3(0.08f, 0.06f, 0.08f), cardboardDark);

        // Small label
        AddWorldText(supply.transform, "SupplyLabel", "SUPPLY", new Vector3(0f, 0.4f, -0.22f), 36, new Color(0.15f, 0.1f, 0.05f), 0.0012f, new Vector2(280f, 80f));

        return supply.transform;
    }

    static Transform BuildSortBins(Transform parent, Material steelDark, Material red, Material blue, Material yellow, Material label)
    {
        GameObject boxes = new GameObject("Boxes");
        boxes.transform.SetParent(parent, false);

        // Factory tote-style bins with feet
        CreateBin(boxes.transform, "Red Box", "A", new Vector3(-0.35f, 0.42f, -0.05f), red, label, steelDark);
        CreateBin(boxes.transform, "Blue Box", "B", new Vector3(0.20f, 0.42f, -0.05f), blue, label, steelDark);
        CreateBin(boxes.transform, "Yellow Box", "C", new Vector3(0.75f, 0.42f, -0.05f), yellow, label, steelDark);

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
        // keep collider for ColorSortRound

        // Rim
        Prim(box.transform, PrimitiveType.Cube, "Rim", new Vector3(0f, 0.48f, 0f), new Vector3(1.05f, 0.08f, 1.05f), boxMat);

        // Bin feet (paa)
        Prim(box.transform, PrimitiveType.Cube, "FootFL", new Vector3(-0.35f, -0.52f, 0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootFR", new Vector3(0.35f, -0.52f, 0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBL", new Vector3(-0.35f, -0.52f, -0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBR", new Vector3(0.35f, -0.52f, -0.35f), new Vector3(0.18f, 0.08f, 0.18f), footMat);

        // Front label plate + letter
        GameObject plate = Prim(box.transform, PrimitiveType.Cube, "LabelPlate", new Vector3(0f, 0.1f, -0.52f), new Vector3(0.55f, 0.4f, 0.05f), labelMat);
        AddWorldText(plate.transform, "Letter", letter, new Vector3(0f, 0f, -0.7f), 72, Color.black, 0.02f, new Vector2(80f, 80f));
    }

    static Transform BuildStartButton(Transform parent, Material pedestal, Material button, Material metal)
    {
        GameObject root = new GameObject("StartConveyorButton");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-1.15f, 0f, 0.05f);

        // Heavy factory pedestal with foot
        Prim(root.transform, PrimitiveType.Cylinder, "PedestalPost", new Vector3(0f, 0.4f, 0f), new Vector3(0.1f, 0.4f, 0.1f), pedestal);
        Prim(root.transform, PrimitiveType.Cylinder, "PedestalFoot", new Vector3(0f, 0.03f, 0f), new Vector3(0.28f, 0.03f, 0.28f), metal);
        Prim(root.transform, PrimitiveType.Cylinder, "PedestalTop", new Vector3(0f, 0.82f, 0f), new Vector3(0.2f, 0.03f, 0.2f), pedestal);

        GameObject btn = Prim(root.transform, PrimitiveType.Cylinder, "Btn_Start_World", new Vector3(0f, 0.88f, 0f), new Vector3(0.13f, 0.035f, 0.13f), button);
        // keep collider

        AddWorldText(root.transform, "PressStartCue", "PRESS START!", new Vector3(0f, 1.1f, 0f), 42, Color.white, 0.0014f, new Vector2(480f, 100f));
        return root.transform;
    }

    static void BuildSetScreen(Transform parent, Material steelDark, Material screenMat, ObjectPlacementStation station)
    {
        GameObject screenRoot = new GameObject("SetScreen");
        screenRoot.transform.SetParent(parent, false);
        screenRoot.transform.localPosition = new Vector3(0.25f, 1.65f, 0.62f);

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
