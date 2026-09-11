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
        GUILayout.Label("Auto-Generate Shape");

        // Sized for a TracingSurface panel that is 2 units wide, 1.5 tall.
        if (GUILayout.Button("Generate Circle"))
        {
            path.pathPoints = PathGenerator.GenerateCircle(0.5f, 32);
            EditorUtility.SetDirty(path);
        }
        if (GUILayout.Button("Generate Wave"))
        {
            path.pathPoints = PathGenerator.GenerateWave(1.2f, 0.3f, 32);
            EditorUtility.SetDirty(path);
        }
    }
}