using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    private LineRenderer lr;
    private TracingPath currentPath;
    private bool[] segmentCleared;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
    }

    public void LoadPath(TracingPath path, Transform anchor)
    {
        currentPath = path;
        transform.position = anchor.position;
        transform.rotation = anchor.rotation;

        lr.positionCount = path.pathPoints.Count;
        lr.SetPositions(path.pathPoints.ToArray());
        segmentCleared = new bool[path.pathPoints.Count];
    }

    public void ClearPointVisual(int index)
    {
        if (segmentCleared[index]) return;
        segmentCleared[index] = true;

        var remaining = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < currentPath.pathPoints.Count; i++)
            if (!segmentCleared[i]) remaining.Add(currentPath.pathPoints[i]);

        lr.positionCount = remaining.Count;
        lr.SetPositions(remaining.ToArray());
    }
}