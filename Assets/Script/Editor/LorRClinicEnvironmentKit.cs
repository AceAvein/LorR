#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared clinic-style hallway + door + signage + room shell for all LorR modules.
/// Same look everywhere; interiors differ per module builder.
/// </summary>
public static class LorRClinicEnvironmentKit
{
    public const string SharedRoot = "Assets/Scenes/Scene Simulation/Shared Clinic";
    public const string SharedMats = SharedRoot + "/Materials";

    public static readonly Color Wall = new Color(0.90f, 0.92f, 0.93f);
    public static readonly Color WallAccent = new Color(0.55f, 0.72f, 0.74f);
    public static readonly Color Floor = new Color(0.78f, 0.80f, 0.82f);
    public static readonly Color FloorTrim = new Color(0.45f, 0.55f, 0.58f);
    public static readonly Color Ceiling = new Color(0.95f, 0.96f, 0.97f);
    public static readonly Color DoorFrame = new Color(0.35f, 0.42f, 0.46f);
    public static readonly Color DoorLeaf = new Color(0.72f, 0.82f, 0.84f);
    public static readonly Color SignBoard = new Color(0.18f, 0.32f, 0.36f);
    public static readonly Color SignAccent = new Color(0.20f, 0.65f, 0.70f);
    public static readonly Color Baseboard = new Color(0.40f, 0.48f, 0.50f);
    public static readonly Color Glass = new Color(0.70f, 0.85f, 0.90f, 0.35f);
    public static readonly Color Wood = new Color(0.58f, 0.42f, 0.28f);
    public static readonly Color Plant = new Color(0.25f, 0.55f, 0.28f);
    public static readonly Color Pot = new Color(0.55f, 0.35f, 0.25f);
    public static readonly Color Poster = new Color(0.85f, 0.88f, 0.90f);
    public static readonly Color DarkMetal = new Color(0.25f, 0.28f, 0.30f);

    public struct Shell
    {
        public Transform root;
        public Transform hallway;
        public Transform door;
        public Transform room;
        public Transform roomFloor;
        public Transform spawnMarker;
        public Material wall;
        public Material floor;
        public Material ceiling;
        public Material wood;
        public Material accent;
        public Material dark;
        public Material plant;
        public Material pot;
        public Material poster;
    }

    public static void EnsureSharedFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Scene Simulation"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Scene Simulation");
        if (!AssetDatabase.IsValidFolder(SharedRoot))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Shared Clinic");
        if (!AssetDatabase.IsValidFolder(SharedMats))
            AssetDatabase.CreateFolder(SharedRoot, "Materials");
    }

    public static void EnsureModuleFolders(string rootFolder, string matsFolder, string prefabsFolder)
    {
        EnsureSharedFolders();
        string parent = System.IO.Path.GetDirectoryName(rootFolder).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(rootFolder);
        if (!AssetDatabase.IsValidFolder(rootFolder))
            AssetDatabase.CreateFolder(parent, leaf);
        if (!AssetDatabase.IsValidFolder(matsFolder))
            AssetDatabase.CreateFolder(rootFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
            AssetDatabase.CreateFolder(rootFolder, "Prefabs");
    }

    /// <summary>
    /// Builds storyboard lobby: long hallway along +Z, door on RIGHT wall into room (+X).
    /// Player walks hallway → turns right into training room.
    /// </summary>
    public static Shell BuildShell(string rootName, string signText, float roomWidth = 8f, float roomDepth = 7f, float roomHeight = 3.0f)
    {
        EnsureSharedFolders();

        Material wall = Mat("Clinic_Wall", Wall, 0.0f, 0.35f, false);
        Material wallAccent = Mat("Clinic_WallAccent", WallAccent, 0.05f, 0.4f, false);
        Material floor = Mat("Clinic_Floor", Floor, 0.05f, 0.45f, false);
        Material floorTrim = Mat("Clinic_FloorTrim", FloorTrim, 0.1f, 0.4f, false);
        Material ceiling = Mat("Clinic_Ceiling", Ceiling, 0.0f, 0.25f, false);
        Material doorFrame = Mat("Clinic_DoorFrame", DoorFrame, 0.35f, 0.4f, false);
        Material doorLeaf = Mat("Clinic_DoorLeaf", DoorLeaf, 0.1f, 0.45f, false);
        Material signBoard = Mat("Clinic_SignBoard", SignBoard, 0.1f, 0.35f, false);
        Material signAccent = Mat("Clinic_SignAccent", SignAccent, 0.15f, 0.5f, true, SignAccent * 0.25f);
        Material baseboard = Mat("Clinic_Baseboard", Baseboard, 0.15f, 0.35f, false);
        Material wood = Mat("Clinic_Wood", Wood, 0.05f, 0.38f, false);
        Material plant = Mat("Clinic_Plant", Plant, 0.0f, 0.35f, false);
        Material pot = Mat("Clinic_Pot", Pot, 0.05f, 0.4f, false);
        Material poster = Mat("Clinic_Poster", Poster, 0.0f, 0.3f, false);
        Material dark = Mat("Clinic_DarkMetal", DarkMetal, 0.5f, 0.35f, false);
        Material glass = Mat("Clinic_Glass", new Color(0.72f, 0.86f, 0.90f), 0.0f, 0.85f, false);

        GameObject root = new GameObject(rootName);
        float hallLen = 12f;
        float hallW = 3.0f;
        float hallH = 2.8f;
        float doorW = 1.25f;
        float doorH = 2.2f;
        float doorZ = hallLen * 0.58f; // door along right wall

        // --- Long hallway (spawn near z=0, walk toward +Z) ---
        Transform hallway = new GameObject("Hallway").transform;
        hallway.SetParent(root.transform, false);

        Prim(hallway, PrimitiveType.Cube, "HallFloor", new Vector3(0f, 0.02f, hallLen * 0.5f), new Vector3(hallW, 0.04f, hallLen), floor);
        Prim(hallway, PrimitiveType.Cube, "HallCeiling", new Vector3(0f, hallH, hallLen * 0.5f), new Vector3(hallW, 0.06f, hallLen), ceiling);
        Prim(hallway, PrimitiveType.Cube, "HallWallL", new Vector3(-hallW * 0.5f, hallH * 0.5f, hallLen * 0.5f), new Vector3(0.08f, hallH, hallLen), wall);
        Prim(hallway, PrimitiveType.Cube, "HallEnd", new Vector3(0f, hallH * 0.5f, hallLen), new Vector3(hallW, hallH, 0.1f), wall);
        Prim(hallway, PrimitiveType.Cube, "HallBaseL", new Vector3(-hallW * 0.5f + 0.04f, 0.08f, hallLen * 0.5f), new Vector3(0.06f, 0.16f, hallLen), baseboard);
        Prim(hallway, PrimitiveType.Cube, "HallFloorRunner", new Vector3(0f, 0.045f, hallLen * 0.5f), new Vector3(0.35f, 0.01f, hallLen - 0.4f), floorTrim);
        Prim(hallway, PrimitiveType.Cube, "HallAccentL", new Vector3(-hallW * 0.5f + 0.05f, 1.35f, hallLen * 0.5f), new Vector3(0.04f, 0.1f, hallLen - 0.3f), wallAccent);

        // Right wall split around door opening
        float rightX = hallW * 0.5f;
        float zBefore = doorZ - doorW * 0.5f;
        float zAfter = hallLen - (doorZ + doorW * 0.5f);
        Prim(hallway, PrimitiveType.Cube, "HallWallR_Before", new Vector3(rightX, hallH * 0.5f, zBefore * 0.5f), new Vector3(0.08f, hallH, zBefore), wall);
        Prim(hallway, PrimitiveType.Cube, "HallWallR_After", new Vector3(rightX, hallH * 0.5f, doorZ + doorW * 0.5f + zAfter * 0.5f), new Vector3(0.08f, hallH, zAfter), wall);
        Prim(hallway, PrimitiveType.Cube, "HallWallR_AboveDoor", new Vector3(rightX, doorH + (hallH - doorH) * 0.5f, doorZ), new Vector3(0.08f, hallH - doorH, doorW), wall);
        Prim(hallway, PrimitiveType.Cube, "HallBaseR_Before", new Vector3(rightX - 0.04f, 0.08f, zBefore * 0.5f), new Vector3(0.06f, 0.16f, zBefore), baseboard);
        Prim(hallway, PrimitiveType.Cube, "HallBaseR_After", new Vector3(rightX - 0.04f, 0.08f, doorZ + doorW * 0.5f + zAfter * 0.5f), new Vector3(0.06f, 0.16f, zAfter), baseboard);

        // Ceiling lights down the hall
        int lightCount = 6;
        for (int i = 0; i < lightCount; i++)
        {
            float z = 1.2f + i * (hallLen - 2f) / (lightCount - 1);
            Prim(hallway, PrimitiveType.Cube, "HallLight_" + i, new Vector3(0f, hallH - 0.05f, z), new Vector3(0.7f, 0.04f, 0.4f), ceiling);
            Prim(hallway, PrimitiveType.Cube, "HallLightGlow_" + i, new Vector3(0f, hallH - 0.08f, z), new Vector3(0.55f, 0.02f, 0.28f), signAccent);
        }

        // Left-wall windows / frames (storyboard)
        for (int i = 0; i < 3; i++)
        {
            float z = 2.5f + i * 3.0f;
            Prim(hallway, PrimitiveType.Cube, "WindowFrame_" + i, new Vector3(-hallW * 0.5f + 0.04f, 1.55f, z), new Vector3(0.06f, 1.1f, 1.4f), dark);
            Prim(hallway, PrimitiveType.Cube, "WindowGlass_" + i, new Vector3(-hallW * 0.5f + 0.01f, 1.55f, z), new Vector3(0.03f, 0.95f, 1.2f), glass);
        }

        // Clinic hallway props
        BuildPlant(hallway, new Vector3(-1.05f, 0f, 1.4f), pot, plant);
        BuildPlant(hallway, new Vector3(-1.05f, 0f, 8.5f), pot, plant);
        Prim(hallway, PrimitiveType.Cube, "Bench", new Vector3(-0.85f, 0.28f, 4.2f), new Vector3(0.55f, 0.08f, 1.4f), wood);
        Prim(hallway, PrimitiveType.Cube, "BenchLegA", new Vector3(-0.85f, 0.14f, 3.7f), new Vector3(0.06f, 0.28f, 0.06f), dark);
        Prim(hallway, PrimitiveType.Cube, "BenchLegB", new Vector3(-0.85f, 0.14f, 4.7f), new Vector3(0.06f, 0.28f, 0.06f), dark);
        Prim(hallway, PrimitiveType.Cylinder, "WaterCooler", new Vector3(1.05f, 0.55f, 2.0f), new Vector3(0.35f, 0.55f, 0.35f), poster);
        Prim(hallway, PrimitiveType.Sphere, "WaterJug", new Vector3(1.05f, 1.15f, 2.0f), new Vector3(0.32f, 0.28f, 0.32f), glass);
        BuildPoster(hallway, new Vector3(-hallW * 0.5f + 0.06f, 1.9f, 10.5f), new Vector3(0.05f, 0.7f, 0.9f), poster, dark, "CLINIC");
        Transform clinicPoster = hallway.Find("Poster_CLINIC");
        if (clinicPoster != null)
            clinicPoster.localRotation = Quaternion.Euler(0f, 90f, 0f);

        // --- Door on RIGHT wall (faces into hallway / -X) ---
        Transform door = new GameObject("EntranceDoor").transform;
        door.SetParent(root.transform, false);
        door.localPosition = new Vector3(rightX, 0f, doorZ);
        door.localRotation = Quaternion.Euler(0f, -90f, 0f); // local +Z points into hallway

        Prim(door, PrimitiveType.Cube, "DoorFrameTop", new Vector3(0f, doorH + 0.08f, 0f), new Vector3(doorW + 0.28f, 0.16f, 0.16f), doorFrame);
        Prim(door, PrimitiveType.Cube, "DoorFrameL", new Vector3(-doorW * 0.5f - 0.08f, doorH * 0.5f, 0f), new Vector3(0.16f, doorH, 0.16f), doorFrame);
        Prim(door, PrimitiveType.Cube, "DoorFrameR", new Vector3(doorW * 0.5f + 0.08f, doorH * 0.5f, 0f), new Vector3(0.16f, doorH, 0.16f), doorFrame);
        Prim(door, PrimitiveType.Cube, "DoorLeaf", new Vector3(0.28f, doorH * 0.5f, 0.02f), new Vector3(doorW * 0.9f, doorH * 0.95f, 0.05f), doorLeaf);
        Prim(door, PrimitiveType.Cube, "DoorWindow", new Vector3(0.28f, doorH * 0.65f, -0.02f), new Vector3(0.32f, 0.4f, 0.02f), glass);
        Prim(door, PrimitiveType.Cube, "DoorHandle", new Vector3(-0.15f, doorH * 0.45f, -0.05f), new Vector3(0.04f, 0.12f, 0.08f), dark);

        // Signage ABOVE door on right wall (readable from hallway)
        Transform sign = new GameObject("ModuleSignage").transform;
        sign.SetParent(hallway, false);
        sign.localPosition = new Vector3(rightX - 0.06f, doorH + 0.45f, doorZ);
        sign.localRotation = Quaternion.Euler(0f, -90f, 0f);
        Prim(sign, PrimitiveType.Cube, "SignBoard", Vector3.zero, new Vector3(1.35f, 0.58f, 0.07f), signBoard);
        Prim(sign, PrimitiveType.Cube, "SignStripe", new Vector3(0f, 0.22f, -0.04f), new Vector3(1.2f, 0.06f, 0.02f), signAccent);
        AddWorldLabel(sign, "SignLabel", signText, new Vector3(0f, -0.04f, -0.045f), 30, Color.white, 0.00115f, new Vector2(1050f, 180f));

        // --- Room to the RIGHT of hallway (+X) ---
        Transform room = new GameObject("TrainingRoom").transform;
        room.SetParent(root.transform, false);
        room.localPosition = new Vector3(rightX + roomWidth * 0.5f, 0f, doorZ);

        Transform roomFloor = Prim(room, PrimitiveType.Cube, "RoomFloor", new Vector3(0f, 0.02f, 0f), new Vector3(roomWidth, 0.04f, roomDepth), floor).transform;
        Prim(room, PrimitiveType.Cube, "RoomCeiling", new Vector3(0f, roomHeight, 0f), new Vector3(roomWidth, 0.08f, roomDepth), ceiling);
        Prim(room, PrimitiveType.Cube, "RoomWallFar", new Vector3(roomWidth * 0.5f, roomHeight * 0.5f, 0f), new Vector3(0.1f, roomHeight, roomDepth), wall);
        Prim(room, PrimitiveType.Cube, "RoomWallBack", new Vector3(0f, roomHeight * 0.5f, roomDepth * 0.5f), new Vector3(roomWidth, roomHeight, 0.1f), wall);
        Prim(room, PrimitiveType.Cube, "RoomWallFront", new Vector3(0f, roomHeight * 0.5f, -roomDepth * 0.5f), new Vector3(roomWidth, roomHeight, 0.1f), wall);

        // Left room wall (toward hallway) with door opening
        float side = (roomDepth - doorW) * 0.5f;
        Prim(room, PrimitiveType.Cube, "RoomWallNear_Before", new Vector3(-roomWidth * 0.5f, roomHeight * 0.5f, -(doorW * 0.5f + side * 0.5f)), new Vector3(0.1f, roomHeight, side), wall);
        Prim(room, PrimitiveType.Cube, "RoomWallNear_After", new Vector3(-roomWidth * 0.5f, roomHeight * 0.5f, doorW * 0.5f + side * 0.5f), new Vector3(0.1f, roomHeight, side), wall);
        Prim(room, PrimitiveType.Cube, "RoomWallNear_Above", new Vector3(-roomWidth * 0.5f, doorH + (roomHeight - doorH) * 0.5f, 0f), new Vector3(0.1f, roomHeight - doorH, doorW), wall);

        Prim(room, PrimitiveType.Cube, "RoomBaseFar", new Vector3(roomWidth * 0.5f - 0.05f, 0.08f, 0f), new Vector3(0.06f, 0.16f, roomDepth - 0.2f), baseboard);
        Prim(room, PrimitiveType.Cube, "RoomBaseBack", new Vector3(0f, 0.08f, roomDepth * 0.5f - 0.05f), new Vector3(roomWidth - 0.2f, 0.16f, 0.06f), baseboard);
        Prim(room, PrimitiveType.Cube, "RoomAccentBand", new Vector3(roomWidth * 0.5f - 0.06f, 1.4f, 0f), new Vector3(0.04f, 0.1f, roomDepth - 0.4f), wallAccent);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Prim(room, PrimitiveType.Cube, "RoomLight_" + x + "_" + z,
                    new Vector3(x * (roomWidth * 0.28f), roomHeight - 0.06f, z * (roomDepth * 0.28f)),
                    new Vector3(0.75f, 0.05f, 0.5f), ceiling);
                Prim(room, PrimitiveType.Cube, "RoomLightGlow_" + x + "_" + z,
                    new Vector3(x * (roomWidth * 0.28f), roomHeight - 0.09f, z * (roomDepth * 0.28f)),
                    new Vector3(0.58f, 0.02f, 0.35f), signAccent);
            }
        }

        Prim(room, PrimitiveType.Cube, "FloorTrimX", new Vector3(0f, 0.045f, 0f), new Vector3(roomWidth - 0.5f, 0.01f, 0.08f), floorTrim);
        Prim(room, PrimitiveType.Cube, "FloorTrimZ", new Vector3(0f, 0.045f, 0f), new Vector3(0.08f, 0.01f, roomDepth - 0.5f), floorTrim);

        Transform spawn = new GameObject("PlayerSpawnMarker").transform;
        spawn.SetParent(hallway, false);
        spawn.localPosition = new Vector3(0f, 0.02f, 1.0f);
        Prim(spawn, PrimitiveType.Cylinder, "SpawnDisc", new Vector3(0f, 0.01f, 0f), new Vector3(0.55f, 0.02f, 0.55f), floorTrim);

        return new Shell
        {
            root = root.transform,
            hallway = hallway,
            door = door,
            room = room,
            roomFloor = roomFloor,
            spawnMarker = spawn,
            wall = wall,
            floor = floor,
            ceiling = ceiling,
            wood = wood,
            accent = wallAccent,
            dark = dark,
            plant = plant,
            pot = pot,
            poster = poster
        };
    }

    public static void BuildPlant(Transform parent, Vector3 pos, Material potMat, Material leafMat)
    {
        GameObject plant = new GameObject("Plant");
        plant.transform.SetParent(parent, false);
        plant.transform.localPosition = pos;
        Prim(plant.transform, PrimitiveType.Cylinder, "Pot", new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.12f, 0.22f), potMat);
        Prim(plant.transform, PrimitiveType.Sphere, "Foliage", new Vector3(0f, 0.38f, 0f), new Vector3(0.35f, 0.32f, 0.35f), leafMat);
    }

    public static void BuildPoster(Transform parent, Vector3 pos, Vector3 scale, Material posterMat, Material frameMat, string title)
    {
        GameObject poster = new GameObject("Poster_" + title.Replace(" ", ""));
        poster.transform.SetParent(parent, false);
        poster.transform.localPosition = pos;
        Prim(poster.transform, PrimitiveType.Cube, "Frame", Vector3.zero, scale, frameMat);
        Prim(poster.transform, PrimitiveType.Cube, "Paper", new Vector3(0f, 0f, -0.02f), new Vector3(scale.x * 0.9f, scale.y * 0.88f, scale.z * 0.5f), posterMat);
        AddWorldLabel(poster.transform, "Title", title, new Vector3(0f, 0f, -0.04f), 22, new Color(0.2f, 0.25f, 0.3f), 0.001f, new Vector2(420f, 120f));
    }

    public static void BuildShelf(Transform parent, Vector3 pos, Material wood, Material dark)
    {
        GameObject shelf = new GameObject("Shelf");
        shelf.transform.SetParent(parent, false);
        shelf.transform.localPosition = pos;
        Prim(shelf.transform, PrimitiveType.Cube, "UprightL", new Vector3(-0.45f, 0.7f, 0f), new Vector3(0.06f, 1.4f, 0.28f), wood);
        Prim(shelf.transform, PrimitiveType.Cube, "UprightR", new Vector3(0.45f, 0.7f, 0f), new Vector3(0.06f, 1.4f, 0.28f), wood);
        Prim(shelf.transform, PrimitiveType.Cube, "ShelfA", new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.04f, 0.3f), wood);
        Prim(shelf.transform, PrimitiveType.Cube, "ShelfB", new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.04f, 0.3f), wood);
        Prim(shelf.transform, PrimitiveType.Cube, "ShelfC", new Vector3(0f, 1.15f, 0f), new Vector3(0.9f, 0.04f, 0.3f), wood);
        Prim(shelf.transform, PrimitiveType.Cube, "BoxA", new Vector3(-0.2f, 0.48f, 0f), new Vector3(0.25f, 0.2f, 0.2f), dark);
        Prim(shelf.transform, PrimitiveType.Cube, "BoxB", new Vector3(0.2f, 0.88f, 0f), new Vector3(0.28f, 0.18f, 0.18f), dark);
    }

    public static void BuildTrashBin(Transform parent, Vector3 pos, Material dark, Material accent)
    {
        GameObject bin = new GameObject("TrashBin");
        bin.transform.SetParent(parent, false);
        bin.transform.localPosition = pos;
        Prim(bin.transform, PrimitiveType.Cylinder, "Body", new Vector3(0f, 0.28f, 0f), new Vector3(0.28f, 0.28f, 0.28f), dark);
        Prim(bin.transform, PrimitiveType.Cylinder, "Rim", new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.03f, 0.3f), accent);
    }

    public static void BuildClock(Transform parent, Vector3 pos, Material dark, Material faceMat)
    {
        GameObject clock = new GameObject("WallClock");
        clock.transform.SetParent(parent, false);
        clock.transform.localPosition = pos;
        GameObject face = Prim(clock.transform, PrimitiveType.Cylinder, "Face", Vector3.zero, new Vector3(0.35f, 0.03f, 0.35f), faceMat);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(clock.transform, PrimitiveType.Cube, "HandH", new Vector3(0f, 0f, -0.02f), new Vector3(0.02f, 0.1f, 0.01f), dark);
        Prim(clock.transform, PrimitiveType.Cube, "HandM", new Vector3(0.04f, 0.05f, -0.02f), new Vector3(0.015f, 0.14f, 0.01f), dark);
    }

    public static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
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

    public static void AddWorldLabel(Transform parent, string name, string value, Vector3 localPos, float fontSize, Color color, float scale, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * scale;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.rectTransform.sizeDelta = size;
    }

    public static Material Mat(string name, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor = default)
    {
        string path = SharedMats + "/" + name + ".mat";
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
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;
    }
}
#endif
