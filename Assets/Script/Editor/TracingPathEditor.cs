using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TracingPath))]
public class TracingPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        TracingPath path = (TracingPath)target;

        GUILayout.Space(10);
        GUILayout.Label("Auto-Generate Shape (sized for a 2 x 1.5 unit panel)");

        // Level 1 - Straight Line (easiest). Many points so it dashes away gradually, not all at once.
        if (GUILayout.Button("Generate Line (Level 1 - Easiest)"))
        {
            path.pathPoints = PathGenerator.GenerateLine(1.2f, 24);
            EditorUtility.SetDirty(path);
        }

        // Level 2 - Gentle Wave
        if (GUILayout.Button("Generate Wave (Level 2)"))
        {
            path.pathPoints = PathGenerator.GenerateWave(1.2f, 0.3f, 32);
            EditorUtility.SetDirty(path);
        }

        // Level 3 - Circle
        if (GUILayout.Button("Generate Circle (Level 3)"))
        {
            path.pathPoints = PathGenerator.GenerateCircle(0.5f, 32);
            EditorUtility.SetDirty(path);
        }

        // Level 4 - Zigzag (sharper corners, more direction changes)
        if (GUILayout.Button("Generate Zigzag (Level 4)"))
        {
            path.pathPoints = PathGenerator.GenerateZigzag(1.2f, 0.35f, 4, 8);
            EditorUtility.SetDirty(path);
        }

        // Level 5 - Star (hardest: most complex shape, longest sequence, sharpest turns)
        if (GUILayout.Button("Generate Star (Level 5 - Hardest)"))
        {
            path.pathPoints = PathGenerator.GenerateStar(0.55f, 0.22f, 5, 10);
            EditorUtility.SetDirty(path);
        }
    }
}