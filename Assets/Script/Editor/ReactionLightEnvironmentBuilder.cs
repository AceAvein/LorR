#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Long clinic hallway + right door → facility-escape Reaction Light room.
/// Menu: LorR / Environments / Build Reaction Light Room
/// </summary>
public static class ReactionLightEnvironmentBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Reaction Light";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/ReactionLightEnvironment.prefab";

    static readonly Color Concrete = new Color(0.42f, 0.44f, 0.46f);
    static readonly Color Caution = new Color(0.92f, 0.72f, 0.08f);
    static readonly Color Panel = new Color(0.22f, 0.24f, 0.26f);
    static readonly Color Pipe = new Color(0.55f, 0.58f, 0.55f);
    static readonly Color RedLight = new Color(0.75f, 0.12f, 0.1f);

    [MenuItem("LorR/Environments/Build Reaction Light Room")]
    public static void Build()
    {
        LorRClinicEnvironmentKit.EnsureModuleFolders(RootFolder, RootFolder + "/Materials", PrefabsFolder);

        var shell = LorRClinicEnvironmentKit.BuildShell(
            "ReactionLightEnvironment",
            "REACTION\nLIGHT",
            roomWidth: 8.5f,
            roomDepth: 8.5f,
            roomHeight: 3.2f);

        Transform room = shell.room;
        Material concrete = LorRClinicEnvironmentKit.Mat("RLEnv_Concrete", Concrete, 0.1f, 0.3f, false);
        Material caution = LorRClinicEnvironmentKit.Mat("RLEnv_Caution", Caution, 0.1f, 0.4f, false);
        Material panel = LorRClinicEnvironmentKit.Mat("RLEnv_Panel", Panel, 0.4f, 0.35f, false);
        Material pipe = LorRClinicEnvironmentKit.Mat("RLEnv_Pipe", Pipe, 0.55f, 0.4f, false);
        Material red = LorRClinicEnvironmentKit.Mat("RLEnv_RedLight", RedLight, 0.1f, 0.5f, true, RedLight * 0.4f);

        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CautionStripA", new Vector3(0f, 0.05f, -3.2f), new Vector3(7.5f, 0.02f, 0.28f), caution);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CautionStripB", new Vector3(0f, 0.05f, 3.2f), new Vector3(7.5f, 0.02f, 0.28f), caution);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "FloorPlate", new Vector3(0f, 0.04f, 0.3f), new Vector3(5.0f, 0.02f, 4.0f), concrete);

        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "UtilityPanelL", new Vector3(-4.05f, 1.45f, 0.2f), new Vector3(0.1f, 1.8f, 2.6f), panel);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "UtilityPanelR", new Vector3(4.05f, 1.45f, 0.2f), new Vector3(0.1f, 1.8f, 2.6f), panel);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "BreakerBox", new Vector3(-4.0f, 1.9f, -2.2f), new Vector3(0.12f, 0.7f, 0.55f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "ServerRack", new Vector3(3.9f, 1.1f, -2.5f), new Vector3(0.7f, 2.2f, 0.6f), panel);

        GameObject pipeA = LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cylinder, "PipeA", new Vector3(-3.7f, 2.7f, 1.2f), new Vector3(0.14f, 2.4f, 0.14f), pipe);
        pipeA.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject pipeB = LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cylinder, "PipeB", new Vector3(3.7f, 2.7f, -0.8f), new Vector3(0.12f, 2.0f, 0.12f), pipe);
        pipeB.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cylinder, "VertPipe", new Vector3(3.5f, 1.5f, 2.8f), new Vector3(0.16f, 1.5f, 0.16f), pipe);

        // Next-set door
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "SetDoorFrame", new Vector3(0f, 1.25f, 4.1f), new Vector3(1.55f, 2.5f, 0.12f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "SetDoorLeaf", new Vector3(0f, 1.2f, 4.05f), new Vector3(1.25f, 2.3f, 0.06f), panel);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "ExitLight", new Vector3(0f, 2.65f, 4.0f), new Vector3(0.55f, 0.18f, 0.08f), red);
        LorRClinicEnvironmentKit.AddWorldLabel(room, "SetDoorLabel", "SET 2 →", new Vector3(0f, 2.55f, 3.95f), 34, Caution, 0.0012f, new Vector2(480f, 90f));

        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(-2.4f, 2.0f, 4.1f), new Vector3(0.9f, 0.6f, 0.05f), shell.poster, shell.dark, "SAFETY");
        LorRClinicEnvironmentKit.BuildPoster(room, new Vector3(2.4f, 2.0f, 4.1f), new Vector3(0.9f, 0.6f, 0.05f), shell.poster, shell.dark, "POWER");
        LorRClinicEnvironmentKit.BuildTrashBin(room, new Vector3(3.6f, 0f, -3.6f), shell.dark, caution);
        LorRClinicEnvironmentKit.BuildPlant(room, new Vector3(-3.6f, 0f, -3.6f), shell.pot, shell.plant);
        LorRClinicEnvironmentKit.BuildClock(room, new Vector3(3.5f, 2.5f, 4.05f), shell.dark, shell.poster);

        // Cable trays / crates
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CrateA", new Vector3(-3.2f, 0.3f, 2.8f), new Vector3(0.7f, 0.6f, 0.7f), concrete);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CrateB", new Vector3(-3.2f, 0.3f, 2.0f), new Vector3(0.55f, 0.5f, 0.55f), shell.dark);
        LorRClinicEnvironmentKit.Prim(room, PrimitiveType.Cube, "CableTray", new Vector3(0f, 2.95f, 0f), new Vector3(5.5f, 0.08f, 0.35f), shell.dark);

        CreateAnchor(room, "GeneratorConsoleAnchor", new Vector3(0f, 0f, 0.2f));
        CreateAnchor(room, "NpcStandAnchor", new Vector3(2.8f, 0f, -2.5f));

        PrefabUtility.SaveAsPrefabAsset(shell.root.gameObject, PrefabPath);
        Object.DestroyImmediate(shell.root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Reaction Light Environment",
            "Rebuilt:\n" + PrefabPath +
            "\n\nLong hallway, right door, richer facility room.",
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
