#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Clinic hallway (long) + right door signage → classroom Pen Tracing room.
/// Menu: LorR / Environments / Build Pen Tracing Room
/// </summary>
public static class PenTracingEnvironmentBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Pen Tracing";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/PenTracingEnvironment.prefab";

    [MenuItem("LorR/Environments/Build Pen Tracing Room")]
    public static void Build()
    {
        LorRClinicEnvironmentKit.EnsureModuleFolders(RootFolder, RootFolder + "/Materials", PrefabsFolder);

        var shell = LorRClinicEnvironmentKit.BuildShell(
            "PenTracingEnvironment",
            "PEN CONTROL\nTRACING",
            roomWidth: 8.5f,
            roomDepth: 7.5f,
            roomHeight: 3.0f);

        Transform room = shell.room;

        // Classroom furniture / props
        LorRClinicEnvironmentKit.BuildShelf(room, new Vector3(3.5f, 0f, 2.6f), shell.wood, shell.dark);
        LorRClinicEnvironmentKit.BuildShelf(room, new Vector3(3.5f, 0f, -2.4f), shell.wood, shell.dark);
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(4.1f, 1.85f, 0.2f), new Vector3(0.05f, 0.7f, 1.0f), shell.poster, shell.dark, "PRECISION");
        Transform p1 = room.Find("Poster_PRECISION");
        if (p1 != null) p1.localRotation = Quaternion.Euler(0f, -90f, 0f);
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(0.8f, 1.9f, 3.6f), new Vector3(1.0f, 0.7f, 0.05f), shell.poster, shell.dark, "FOCUS");
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(-1.2f, 1.9f, 3.6f), new Vector3(0.9f, 0.65f, 0.05f), shell.poster, shell.dark, "HAND");
        LorRClinicEnvironmentKit.BuildClock(room, new Vector3(2.2f, 2.45f, 3.6f), shell.dark, shell.poster);
        LorRClinicEnvironmentKit.BuildTrashBin(room, new Vector3(3.6f, 0f, -3.3f), shell.dark, shell.accent);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-3.5f, 0f, 3.2f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(3.6f, 0f, 3.2f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-3.5f, 0f, -3.2f), shell.pot, shell.plant);

        // Whiteboard + chalk tray
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Whiteboard", new Vector3(0.5f, 1.6f, 3.6f), new Vector3(2.6f, 1.25f, 0.06f), shell.poster);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "WhiteboardFrame", new Vector3(0.5f, 1.6f, 3.62f), new Vector3(2.75f, 1.38f, 0.04f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "WhiteboardTray", new Vector3(0.5f, 0.92f, 3.55f), new Vector3(2.6f, 0.05f, 0.12f), shell.wood);

        // Extra classroom desks (fill) — gameplay desk still via anchor
        BuildSideDesk(room, new Vector3(-2.6f, 0f, 1.2f), shell.wood, shell.dark);
        BuildSideDesk(room, new Vector3(-2.6f, 0f, -0.6f), shell.wood, shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "TeacherDesk", new Vector3(2.4f, 0.78f, 2.8f), new Vector3(1.3f, 0.08f, 0.65f), shell.wood);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "TeacherDeskLegL", new Vector3(1.95f, 0.38f, 2.8f), new Vector3(0.07f, 0.76f, 0.07f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "TeacherDeskLegR", new Vector3(2.85f, 0.38f, 2.8f), new Vector3(0.07f, 0.76f, 0.07f), shell.dark);

        // Cubbies / supply cabinet
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Cabinet", new Vector3(-3.7f, 0.85f, 0.3f), new Vector3(0.55f, 1.7f, 1.1f), shell.wood);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CabinetHandle", new Vector3(-3.4f, 0.9f, 0.3f), new Vector3(0.04f, 0.15f, 0.04f), shell.dark);

        CreateAnchor(room, "PenSelectStationAnchor", new Vector3(0.2f, 0f, -2.4f));
        CreateAnchor(room, "TraceDeskAnchor", new Vector3(0.4f, 0f, 0.3f));
        CreateAnchor(room, "NpcStandAnchor", new Vector3(2.4f, 0f, 2.2f));

        PrefabUtility.SaveAsPrefabAsset(shell.root.gameObject, PrefabPath);
        Object.DestroyImmediate(shell.root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Pen Tracing Environment",
            "Rebuilt:\n" + PrefabPath +
            "\n\nLong hallway, door on RIGHT, richer classroom.\nRe-run menu & replace prefab in scene.",
            "OK");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
    }

    static void BuildSideDesk(Transform parent, Vector3 pos, Material wood, Material dark)
    {
        GameObject desk = new GameObject("FillDesk");
        desk.transform.SetParent(parent, false);
        desk.transform.localPosition = pos;
        LorRClinicEnvironmentKit.Prim(desk.transform, PrimitiveType.Cube, "Top", new Vector3(0f, 0.76f, 0f), new Vector3(1.0f, 0.06f, 0.6f), wood);
        LorRClinicEnvironmentKit.Prim(desk.transform, PrimitiveType.Cube, "LegFL", new Vector3(-0.4f, 0.36f, 0.22f), new Vector3(0.05f, 0.72f, 0.05f), dark);
        LorRClinicEnvironmentKit.Prim(desk.transform, PrimitiveType.Cube, "LegFR", new Vector3(0.4f, 0.36f, 0.22f), new Vector3(0.05f, 0.72f, 0.05f), dark);
        LorRClinicEnvironmentKit.Prim(desk.transform, PrimitiveType.Cube, "LegBL", new Vector3(-0.4f, 0.36f, -0.22f), new Vector3(0.05f, 0.72f, 0.05f), dark);
        LorRClinicEnvironmentKit.Prim(desk.transform, PrimitiveType.Cube, "LegBR", new Vector3(0.4f, 0.36f, -0.22f), new Vector3(0.05f, 0.72f, 0.05f), dark);
    }

    static void CreateAnchor(Transform parent, string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
    }
}
#endif
