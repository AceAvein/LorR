#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Long clinic hallway + right door → half-court Ball Drop room.
/// Menu: LorR / Environments / Build Ball Drop Room
/// </summary>
public static class BallDropEnvironmentBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Ball Drop";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/BallDropEnvironment.prefab";

    static readonly Color Court = new Color(0.78f, 0.55f, 0.32f);
    static readonly Color Paint = new Color(0.95f, 0.95f, 0.96f);
    static readonly Color Key = new Color(0.55f, 0.25f, 0.18f);
    static readonly Color Rim = new Color(0.85f, 0.35f, 0.1f);
    static readonly Color Backboard = new Color(0.9f, 0.92f, 0.95f);
    static readonly Color Bleacher = new Color(0.55f, 0.38f, 0.25f);

    [MenuItem("LorR/Environments/Build Ball Drop Room")]
    public static void Build()
    {
        LorRClinicEnvironmentKit.EnsureModuleFolders(RootFolder, RootFolder + "/Materials", PrefabsFolder);

        var shell = LorRClinicEnvironmentKit.BuildShell(
            "BallDropEnvironment",
            "BALL DROP",
            roomWidth: 10f,
            roomDepth: 11f,
            roomHeight: 4.2f);

        Transform room = shell.room;
        Material court = LorRClinicEnvironmentKit.Mat("BDEnv_Court", Court, 0.05f, 0.4f, false);
        Material paint = LorRClinicEnvironmentKit.Mat("BDEnv_Paint", Paint, 0.0f, 0.35f, false);
        Material key = LorRClinicEnvironmentKit.Mat("BDEnv_Key", Key, 0.05f, 0.35f, false);
        Material rim = LorRClinicEnvironmentKit.Mat("BDEnv_Rim", Rim, 0.6f, 0.55f, false);
        Material board = LorRClinicEnvironmentKit.Mat("BDEnv_Backboard", Backboard, 0.0f, 0.45f, false);
        Material bleacher = LorRClinicEnvironmentKit.Mat("BDEnv_Bleacher", Bleacher, 0.05f, 0.35f, false);

        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CourtFloor", new Vector3(0.3f, 0.05f, 0.4f), new Vector3(8.2f, 0.04f, 9.2f), court);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "SidelineL", new Vector3(-3.85f, 0.08f, 0.4f), new Vector3(0.08f, 0.01f, 9.0f), paint);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "SidelineR", new Vector3(4.4f, 0.08f, 0.4f), new Vector3(0.08f, 0.01f, 9.0f), paint);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CenterLine", new Vector3(0.3f, 0.08f, -1.8f), new Vector3(8.0f, 0.01f, 0.08f), paint);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cylinder, "CenterCircle", new Vector3(0.3f, 0.08f, -1.8f), new Vector3(2.0f, 0.01f, 2.0f), paint);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "KeyPaint", new Vector3(0.3f, 0.08f, 2.6f), new Vector3(3.4f, 0.01f, 4.0f), key);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "FreeThrowLine", new Vector3(0.3f, 0.09f, 0.9f), new Vector3(3.4f, 0.01f, 0.08f), paint);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Baseline", new Vector3(0.3f, 0.08f, 4.9f), new Vector3(8.0f, 0.01f, 0.08f), paint);

        GameObject hoop = new GameObject("Hoop");
        hoop.transform.SetParent(room, false);
        hoop.transform.localPosition = new Vector3(0.3f, 0f, 4.85f);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cube, "Pole", new Vector3(0f, 1.6f, 0.2f), new Vector3(0.14f, 3.2f, 0.14f), shell.dark);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cube, "Arm", new Vector3(0f, 3.1f, -0.4f), new Vector3(0.12f, 0.12f, 0.85f), shell.dark);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cube, "Backboard", new Vector3(0f, 3.15f, -0.85f), new Vector3(1.7f, 1.05f, 0.08f), board);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cube, "BackboardSquare", new Vector3(0f, 3.0f, -0.9f), new Vector3(0.55f, 0.4f, 0.02f), paint);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cylinder, "Rim", new Vector3(0f, 2.65f, -1.15f), new Vector3(0.58f, 0.03f, 0.58f), rim);
        LorRClinicEnvironmentKit.Prim(hoop.transform, PrimitiveType.Cylinder, "NetHint", new Vector3(0f, 2.4f, -1.15f), new Vector3(0.5f, 0.22f, 0.5f), paint);

        // Bleachers
        for (int i = 0; i < 3; i++)
        {
            float y = 0.25f + i * 0.35f;
            float x = -4.5f - i * 0.15f;
            LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "BleacherL_" + i, new Vector3(x, y, 0.2f), new Vector3(0.55f, 0.12f, 4.5f), bleacher);
            LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "BleacherR_" + i, new Vector3(5.0f + i * 0.15f, y, 0.2f), new Vector3(0.55f, 0.12f, 4.5f), bleacher);
        }

        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "BenchL", new Vector3(-3.9f, 0.28f, -3.5f), new Vector3(0.4f, 0.08f, 1.8f), shell.wood);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "BenchR", new Vector3(4.5f, 0.28f, -3.5f), new Vector3(0.4f, 0.08f, 1.8f), shell.wood);
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(-4.85f, 2.4f, 2.2f), new Vector3(0.06f, 0.9f, 1.3f), shell.poster, shell.dark, "CATCH");
        Transform catchPoster = room.Find("Poster_CATCH");
        if (catchPoster != null) catchPoster.localRotation = Quaternion.Euler(0f, 90f, 0f);
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(5.35f, 2.4f, 2.2f), new Vector3(0.06f, 0.9f, 1.3f), shell.poster, shell.dark, "TRAIN");
        Transform trainPoster = room.Find("Poster_TRAIN");
        if (trainPoster != null) trainPoster.localRotation = Quaternion.Euler(0f, -90f, 0f);

        LorRClinicEnvironmentKit.BuildTrashBin(room, new Vector3(4.6f, 0f, -4.8f), shell.dark, shell.accent);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-4.4f, 0f, -4.8f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildClock(room, new Vector3(0.3f, 3.6f, 5.35f), shell.dark, shell.poster);

        // Scoreboard hint
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "Scoreboard", new Vector3(0.3f, 3.7f, 5.3f), new Vector3(1.8f, 0.55f, 0.1f), shell.dark);
        LorRClinicEnvironmentKit.AddWorldLabel(room, "ScoreLabel", "BALL DROP", new Vector3(0.3f, 3.7f, 5.22f), 28, Color.white, 0.0012f, new Vector2(600f, 120f));

        CreateAnchor(room, "TrainingZoneAnchor", new Vector3(0.3f, 0f, -1.8f));
        CreateAnchor(room, "DispenserAnchor", new Vector3(0.3f, 0f, 0.3f));
        CreateAnchor(room, "ArcAnchor", new Vector3(0.3f, 0f, 2.0f));
        CreateAnchor(room, "NpcStandAnchor", new Vector3(3.6f, 0f, -3.2f));
        CreateAnchor(room, "ScreenAnchor", new Vector3(4.0f, 0f, 2.8f));

        PrefabUtility.SaveAsPrefabAsset(shell.root.gameObject, PrefabPath);
        Object.DestroyImmediate(shell.root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Ball Drop Environment",
            "Rebuilt:\n" + PrefabPath +
            "\n\nLong hallway, right door, richer half-court.",
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
