#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds Object Placement station:
/// - Infeed conveyor with ExitTunnel on the RIGHT (items emerge onto belt)
/// - Left conveyor, same style, same +X flow, own ExitTunnel + spawn
/// - Open A/B/C sort crates (grab-ready, not welded to conveyors)
/// - Start button + SET screen kept
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
    static readonly Color BinInterior = new Color(0.75f, 0.76f, 0.78f);

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
        Material binInMat = Mat("OP_BinInterior", BinInterior, 0.0f, 0.35f, false);

        GameObject root = new GameObject("ObjectPlacementStation");
        ObjectPlacementStation station = root.AddComponent<ObjectPlacementStation>();

        // Right / infeed conveyor — ExitTunnel on the RIGHT, flow +X
        Transform conveyor = BuildFactoryConveyor(
            root.transform,
            "Conveyor",
            new Vector3(0.35f, 0.95f, 0.55f),
            tunnelOnRight: true,
            steelMat, steelDarkMat, warnMat, beltMat, tunnelMat);
        station.conveyor = conveyor;
        station.beltSurface = conveyor.Find("BeltSurface");
        Transform tunnel = conveyor.Find("ExitTunnel");
        station.supplyBox = tunnel;
        station.grocerySpawnPoint = tunnel != null ? tunnel.Find("GrocerySpawnPoint") : null;

        // Left conveyor — same style, same +X flow, own mouth/box
        Transform conveyorLeft = BuildFactoryConveyor(
            root.transform,
            "ConveyorLeft",
            new Vector3(-1.35f, 0.95f, 0.05f),
            tunnelOnRight: true,
            steelMat, steelDarkMat, warnMat, beltMat, tunnelMat);
        station.conveyorLeft = conveyorLeft;
        station.beltSurfaceLeft = conveyorLeft.Find("BeltSurface");
        Transform tunnelLeft = conveyorLeft.Find("ExitTunnel");
        station.supplyBoxLeft = tunnelLeft;
        station.grocerySpawnPointLeft = tunnelLeft != null ? tunnelLeft.Find("GrocerySpawnPoint") : null;

        Transform boxes = BuildSortBins(root.transform, steelDarkMat, redMat, blueMat, yellowMat, labelMat, binInMat);
        station.boxesRoot = boxes;
        station.redBox = boxes.Find("Red Box");
        station.blueBox = boxes.Find("Blue Box");
        station.yellowBox = boxes.Find("Yellow Box");

        // Start button near right infeed tunnel (side belt)
        station.startButton = BuildStartButton(root.transform, pedestalMat, buttonMat, steelDarkMat);
        BuildSetScreen(root.transform, steelDarkMat, screenMat, station);

        root.transform.position = new Vector3(0f, 0f, 0.75f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Object Placement Station",
                "Rebuilt station:\n" + PrefabPath +
                "\n\n• Right infeed ExitTunnel (items emerge)\n" +
                "• Left conveyor + ExitTunnel (same +X flow)\n" +
                "• Open grab-ready A/B/C crates\n" +
                "• Start button + SET screen kept\n\n" +
                "Re-drag into Color Sort / Object Placement scene.",
                "OK");
        }

        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
        if (prefab != null)
            Selection.activeObject = prefab;

        Debug.Log("ObjectPlacementStation prefab rebuilt: " + PrefabPath);
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

    /// <summary>
    /// Factory belt. When tunnelOnRight, ExitTunnel sits at +X and mouth faces the open run (-X).
    /// BeltSurface is a dedicated child for later scroll/run code. Flow sense is +X.
    /// </summary>
    static Transform BuildFactoryConveyor(
        Transform parent,
        string name,
        Vector3 localPos,
        bool tunnelOnRight,
        Material steel,
        Material steelDark,
        Material warn,
        Material belt,
        Material tunnelInterior)
    {
        GameObject conveyor = new GameObject(name);
        conveyor.transform.SetParent(parent, false);
        conveyor.transform.localPosition = localPos;

        float side = tunnelOnRight ? 1f : -1f;

        // Open run toward the opposite side of the tunnel
        Prim(conveyor.transform, PrimitiveType.Cube, "Chassis",
            new Vector3(-0.15f * side, -0.06f, 0f), new Vector3(1.7f, 0.14f, 0.58f), steel);

        // Named belt surface — later code can scroll this material / move items along +X
        GameObject beltGo = Prim(conveyor.transform, PrimitiveType.Cube, "BeltSurface",
            new Vector3(-0.15f * side, 0.05f, 0f), new Vector3(1.55f, 0.025f, 0.42f), belt);
        beltGo.tag = "Untagged";

        Prim(conveyor.transform, PrimitiveType.Cube, "RailFront",
            new Vector3(-0.15f * side, 0.08f, 0.24f), new Vector3(1.6f, 0.08f, 0.035f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "RailBack",
            new Vector3(-0.15f * side, 0.08f, -0.24f), new Vector3(1.6f, 0.08f, 0.035f), steelDark);

        // Flow chevrons pointing +X (right)
        Prim(conveyor.transform, PrimitiveType.Cube, "FlowMark_0",
            new Vector3(-0.45f * side, 0.07f, 0f), new Vector3(0.12f, 0.01f, 0.08f), warn);
        Prim(conveyor.transform, PrimitiveType.Cube, "FlowMark_1",
            new Vector3(-0.15f * side, 0.07f, 0f), new Vector3(0.12f, 0.01f, 0.08f), warn);
        Prim(conveyor.transform, PrimitiveType.Cube, "FlowMark_2",
            new Vector3(0.15f * side, 0.07f, 0f), new Vector3(0.12f, 0.01f, 0.08f), warn);
        Prim(conveyor.transform, PrimitiveType.Cube, "WarnStripe",
            new Vector3(-0.55f * side, 0.12f, 0.24f), new Vector3(0.35f, 0.02f, 0.04f), warn);

        // Roller / motor on the +X end (flow direction)
        GameObject rollR = Prim(conveyor.transform, PrimitiveType.Cylinder, "RollerRight",
            new Vector3(0.95f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), steelDark);
        rollR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(conveyor.transform, PrimitiveType.Cube, "MotorHousing",
            new Vector3(1.05f, -0.12f, 0.28f), new Vector3(0.22f, 0.18f, 0.22f), steelDark);

        BuildExitTunnel(conveyor.transform, tunnelOnRight, steel, steelDark, warn, belt, tunnelInterior);

        BuildLeg(conveyor.transform, "Leg_FL", new Vector3(-0.7f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_FR", new Vector3(0.7f, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BL", new Vector3(-0.7f, -0.55f, -0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_BR", new Vector3(0.7f, -0.55f, -0.2f), steel, steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceFront",
            new Vector3(0f, -0.75f, 0.2f), new Vector3(1.25f, 0.04f, 0.04f), steelDark);
        Prim(conveyor.transform, PrimitiveType.Cube, "BraceBack",
            new Vector3(0f, -0.75f, -0.2f), new Vector3(1.25f, 0.04f, 0.04f), steelDark);

        float tunnelLegX = tunnelOnRight ? 1.15f : -1.15f;
        BuildLeg(conveyor.transform, "Leg_TunnelF", new Vector3(tunnelLegX, -0.55f, 0.2f), steel, steelDark);
        BuildLeg(conveyor.transform, "Leg_TunnelB", new Vector3(tunnelLegX, -0.55f, -0.2f), steel, steelDark);

        return conveyor.transform;
    }

    static void BuildExitTunnel(
        Transform conveyor,
        bool onRight,
        Material steel,
        Material steelDark,
        Material warn,
        Material belt,
        Material interior)
    {
        float side = onRight ? 1f : -1f;

        GameObject tunnel = new GameObject("ExitTunnel");
        tunnel.transform.SetParent(conveyor, false);
        tunnel.transform.localPosition = new Vector3(1.05f * side, 0.12f, 0f);
        // Flip so mouth faces the open belt (toward center / -side)
        tunnel.transform.localRotation = onRight ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelRoof", new Vector3(0f, 0.28f, 0f), new Vector3(0.85f, 0.06f, 0.62f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelLeftWall", new Vector3(0f, 0.05f, 0.29f), new Vector3(0.85f, 0.42f, 0.05f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelRightWall", new Vector3(0f, 0.05f, -0.29f), new Vector3(0.85f, 0.42f, 0.05f), steel);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelBack", new Vector3(-0.4f, 0.05f, 0f), new Vector3(0.05f, 0.42f, 0.58f), steelDark);

        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelCeilingIn", new Vector3(0.05f, 0.22f, 0f), new Vector3(0.7f, 0.03f, 0.5f), interior);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelFloorIn", new Vector3(0.05f, -0.12f, 0f), new Vector3(0.7f, 0.03f, 0.5f), interior);
        Prim(tunnel.transform, PrimitiveType.Cube, "TunnelBelt", new Vector3(0.1f, -0.07f, 0f), new Vector3(0.75f, 0.025f, 0.4f), belt);

        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameTop", new Vector3(0.42f, 0.28f, 0f), new Vector3(0.06f, 0.08f, 0.64f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameBot", new Vector3(0.42f, -0.14f, 0f), new Vector3(0.06f, 0.06f, 0.64f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameL", new Vector3(0.42f, 0.07f, 0.30f), new Vector3(0.06f, 0.4f, 0.06f), steelDark);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthFrameR", new Vector3(0.42f, 0.07f, -0.30f), new Vector3(0.06f, 0.4f, 0.06f), steelDark);

        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnTop", new Vector3(0.45f, 0.32f, 0f), new Vector3(0.04f, 0.03f, 0.55f), warn);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnL", new Vector3(0.45f, 0.07f, 0.33f), new Vector3(0.04f, 0.35f, 0.03f), warn);
        Prim(tunnel.transform, PrimitiveType.Cube, "MouthWarnR", new Vector3(0.45f, 0.07f, -0.33f), new Vector3(0.04f, 0.35f, 0.03f), warn);
        // Hinge at top of flap; slight opposite lean so left/right mouths read differently.
        GameObject flapHinge = new GameObject("RubberFlapHinge");
        flapHinge.transform.SetParent(tunnel.transform, false);
        flapHinge.transform.localPosition = new Vector3(0.48f, 0.23f, 0f);
        flapHinge.transform.localRotation = Quaternion.Euler(0f, 0f, onRight ? -8f : 8f);
        Prim(flapHinge.transform, PrimitiveType.Cube, "RubberFlap", new Vector3(0f, -0.11f, 0f), new Vector3(0.02f, 0.22f, 0.48f), interior);

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

    static Transform BuildSortBins(
        Transform parent,
        Material steelDark,
        Material red,
        Material blue,
        Material yellow,
        Material label,
        Material interior)
    {
        GameObject boxes = new GameObject("Boxes");
        boxes.transform.SetParent(parent, false);
        // Independent group — not parented under conveyors (grab later)

        CreateOpenBin(boxes.transform, "Red Box", "A", new Vector3(-0.15f, 0.42f, -0.35f), red, label, steelDark, interior);
        CreateOpenBin(boxes.transform, "Blue Box", "B", new Vector3(0.35f, 0.42f, -0.35f), blue, label, steelDark, interior);
        CreateOpenBin(boxes.transform, "Yellow Box", "C", new Vector3(0.85f, 0.42f, -0.35f), yellow, label, steelDark, interior);

        return boxes.transform;
    }

    /// <summary>
    /// Open-top crate: floor + 4 walls so interior is visible. Root keeps a BoxCollider for grab.
    /// </summary>
    static void CreateOpenBin(
        Transform parent,
        string objectName,
        string letter,
        Vector3 localPos,
        Material boxMat,
        Material labelMat,
        Material footMat,
        Material interiorMat)
    {
        GameObject box = new GameObject(objectName);
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = Vector3.one;

        // Grab collider on root (not welded into conveyor)
        BoxCollider grab = box.AddComponent<BoxCollider>();
        grab.size = new Vector3(0.38f, 0.42f, 0.38f);
        grab.center = new Vector3(0f, 0.21f, 0f);

        const float w = 0.38f;
        const float h = 0.42f;
        const float d = 0.38f;
        const float wall = 0.03f;
        const float floor = 0.03f;

        Prim(box.transform, PrimitiveType.Cube, "Floor",
            new Vector3(0f, floor * 0.5f, 0f), new Vector3(w, floor, d), interiorMat);
        Prim(box.transform, PrimitiveType.Cube, "WallFront",
            new Vector3(0f, h * 0.5f, -d * 0.5f + wall * 0.5f), new Vector3(w, h, wall), boxMat);
        Prim(box.transform, PrimitiveType.Cube, "WallBack",
            new Vector3(0f, h * 0.5f, d * 0.5f - wall * 0.5f), new Vector3(w, h, wall), boxMat);
        Prim(box.transform, PrimitiveType.Cube, "WallLeft",
            new Vector3(-w * 0.5f + wall * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d - wall * 2f), boxMat);
        Prim(box.transform, PrimitiveType.Cube, "WallRight",
            new Vector3(w * 0.5f - wall * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d - wall * 2f), boxMat);

        Prim(box.transform, PrimitiveType.Cube, "Rim",
            new Vector3(0f, h + 0.01f, 0f), new Vector3(w + 0.02f, 0.025f, d + 0.02f), boxMat);

        Prim(box.transform, PrimitiveType.Cube, "FootFL", new Vector3(-0.14f, -0.02f, 0.14f), new Vector3(0.07f, 0.04f, 0.07f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootFR", new Vector3(0.14f, -0.02f, 0.14f), new Vector3(0.07f, 0.04f, 0.07f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBL", new Vector3(-0.14f, -0.02f, -0.14f), new Vector3(0.07f, 0.04f, 0.07f), footMat);
        Prim(box.transform, PrimitiveType.Cube, "FootBR", new Vector3(0.14f, -0.02f, -0.14f), new Vector3(0.07f, 0.04f, 0.07f), footMat);

        GameObject plate = Prim(box.transform, PrimitiveType.Cube, "LabelPlate",
            new Vector3(0f, 0.22f, -d * 0.5f - 0.01f), new Vector3(0.2f, 0.16f, 0.02f), labelMat);
        AddWorldText(plate.transform, "Letter", letter, new Vector3(0f, 0f, -0.7f), 72, Color.black, 0.02f, new Vector2(80f, 80f));
    }

    static Transform BuildStartButton(Transform parent, Material pedestal, Material button, Material metal)
    {
        GameObject root = new GameObject("StartConveyorButton");
        root.transform.SetParent(parent, false);
        // Near right infeed tunnel
        root.transform.localPosition = new Vector3(1.15f, 0f, 0.15f);

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
        screenRoot.transform.localPosition = new Vector3(0.35f, 1.65f, 0.85f);

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
