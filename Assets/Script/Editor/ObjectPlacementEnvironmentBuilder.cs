#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Long clinic hallway + right door → office Object Placement room.
/// Menu: LorR / Environments / Build Object Placement Room
/// </summary>
public static class ObjectPlacementEnvironmentBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Object Placement";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ObjectPlacementEnvironment.prefab";

    static readonly Color GlassTint = new Color(0.65f, 0.82f, 0.88f, 1f);
    static readonly Color Carpet = new Color(0.55f, 0.60f, 0.62f);
    static readonly Color DeskGray = new Color(0.70f, 0.72f, 0.74f);

    [MenuItem("LorR/Environments/Build Object Placement Room")]
    public static void Build()
    {
        LorRClinicEnvironmentKit.EnsureModuleFolders(RootFolder, RootFolder + "/Materials", PrefabsFolder);

        var shell = LorRClinicEnvironmentKit.BuildShell(
            "ObjectPlacementEnvironment",
            "OBJECT\nPLACEMENT",
            roomWidth: 9.5f,
            roomDepth: 8.5f,
            roomHeight: 3.0f);

        Transform room = shell.room;
        Material glass = LorRClinicEnvironmentKit.Mat("OPEnv_Glass", GlassTint, 0.0f, 0.85f, false);
        Material carpet = LorRClinicEnvironmentKit.Mat("OPEnv_Carpet", Carpet, 0.0f, 0.25f, false);
        Material desk = LorRClinicEnvironmentKit.Mat("OPEnv_Desk", DeskGray, 0.15f, 0.4f, false);

        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "OfficeCarpet", new Vector3(0f, 0.04f, 0.15f), new Vector3(8.2f, 0.02f, 7.0f), carpet);

        // Glass barrier + door
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassBarrierL", new Vector3(-2.4f, 1.4f, 1.6f), new Vector3(3.0f, 2.55f, 0.06f), glass);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassBarrierR", new Vector3(2.4f, 1.4f, 1.6f), new Vector3(3.0f, 2.55f, 0.06f), glass);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassFrameL", new Vector3(-2.4f, 1.4f, 1.62f), new Vector3(3.05f, 2.6f, 0.03f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassFrameR", new Vector3(2.4f, 1.4f, 1.62f), new Vector3(3.05f, 2.6f, 0.03f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassDoorFrame", new Vector3(0f, 1.25f, 1.6f), new Vector3(1.45f, 2.55f, 0.1f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "GlassDoor", new Vector3(0f, 1.2f, 1.58f), new Vector3(1.2f, 2.35f, 0.05f), glass);

        // NPC side
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "NpcDesk", new Vector3(0f, 0.75f, 3.15f), new Vector3(1.5f, 0.08f, 0.75f), desk);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "NpcDeskLegL", new Vector3(-0.6f, 0.35f, 3.15f), new Vector3(0.08f, 0.7f, 0.08f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "NpcDeskLegR", new Vector3(0.6f, 0.35f, 3.15f), new Vector3(0.08f, 0.7f, 0.08f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Monitor", new Vector3(0.2f, 1.05f, 3.3f), new Vector3(0.45f, 0.32f, 0.05f), shell.dark);
        CreateAnchor(room, "NpcStandAnchor", new Vector3(0f, 0f, 3.7f));

        // Office fill
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(-3.8f, 1.95f, 4.1f), new Vector3(1.1f, 0.75f, 0.05f), shell.poster, shell.dark, "SORTING");
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(3.8f, 1.95f, 4.1f), new Vector3(1.1f, 0.75f, 0.05f), shell.poster, shell.dark, "DONATION");
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(4.6f, 1.85f, 0f), new Vector3(0.05f, 0.7f, 1.0f), shell.poster, shell.dark, "GUIDE");
        Transform guide = room.Find("Poster_GUIDE");
        if (guide != null) guide.localRotation = Quaternion.Euler(0f, -90f, 0f);

        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-4.3f, 0f, -3.5f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(4.3f, 0f, -3.5f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-4.3f, 0f, 3.6f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildShelf(room, new Vector3(-4.3f, 0f, 0.4f), shell.wood, shell.dark);
        LorRClinicEnvironmentKit.BuildShelf(room, new Vector3(4.3f, 0f, 0.4f), shell.wood, shell.dark);
        LorRClinicEnvironmentKit.BuildTrashBin(room, new Vector3(4.3f, 0f, 2.2f), shell.dark, shell.accent);
        LorRClinicEnvironmentKit.BuildClock(room, new Vector3(0f, 2.55f, 4.1f), shell.dark, shell.poster);

        // Side office desk + printer
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "SideDesk", new Vector3(-3.2f, 0.75f, -2.6f), new Vector3(1.4f, 0.07f, 0.65f), desk);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Printer", new Vector3(-3.0f, 0.95f, -2.6f), new Vector3(0.45f, 0.28f, 0.4f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "FileCabinet", new Vector3(3.5f, 0.55f, -3.2f), new Vector3(0.55f, 1.1f, 0.5f), shell.wood);

        CreateAnchor(room, "StationAnchor", new Vector3(0.2f, 0f, -1.0f));
        CreateAnchor(room, "SideConveyorAnchor", new Vector3(-2.6f, 0f, -1.6f));
        CreateAnchor(room, "FrontConveyorAnchor", new Vector3(0.2f, 0f, 0.5f));

        PrefabUtility.SaveAsPrefabAsset(shell.root.gameObject, PrefabPath);
        Object.DestroyImmediate(shell.root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Object Placement Environment",
            "Rebuilt:\n" + PrefabPath +
            "\n\nLong hallway, right door, richer office.",
            "OK");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
    }

    static void CreateAnchor(Transform parent, string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
    }
}
#endif
