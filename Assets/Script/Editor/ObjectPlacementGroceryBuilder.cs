#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Legacy procedural grocery bake — Phase I now uses Kenney/Quaternius FBX under
/// Assets/ObjectPlacement/Items (and Resources copy for runtime).
/// Menu kept for optional regenerating placeholder primitives only.
/// </summary>
public static class ObjectPlacementGroceryBuilder
{
    [MenuItem("LorR/Object Placement/Build Grocery Prefabs (legacy primitives)")]
    public static void BuildPrefabs()
    {
        EditorUtility.DisplayDialog(
            "Object Placement Groceries",
            "Phase I uses FBX packs in:\nAssets/ObjectPlacement/Items\n\n" +
            "Runtime loads from:\nResources/ObjectPlacement/Items\n\n" +
            "Legacy primitive bake is disabled.",
            "OK");
    }
}
#endif
