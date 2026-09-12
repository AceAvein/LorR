using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    [Tooltip("Optional: assign a custom 'Start Here' marker prefab. If left empty, a simple green sphere is created automatically.")]
    public GameObject startMarkerPrefab;

    private LineRenderer lr;
    private TracingPath currentPath;
    private bool[] segmentCleared;
    private GameObject startMarkerInstance;

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

        SpawnStartMarker(path.pathPoints[0]);
    }

    void SpawnStartMarker(Vector3 localStartPoint)
    {
        if (startMarkerInstance != null) Destroy(startMarkerInstance);

        if (startMarkerPrefab != null)
        {
            startMarkerInstance = Instantiate(startMarkerPrefab, transform);
        }
        else
        {
            // Fallback: build a simple glowing green sphere marker automatically
            startMarkerInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startMarkerInstance.name = "StartMarker";
            startMarkerInstance.transform.SetParent(transform, false);
            startMarkerInstance.transform.localScale = Vector3.one * 0.08f;

            var renderer = startMarkerInstance.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = Color.green;
            renderer.material = mat;

            // Remove its collider so it never interferes with pen/raycast logic
            var collider = startMarkerInstance.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        startMarkerInstance.transform.localPosition = localStartPoint;
    }

    public void ClearPointVisual(int index)
    {
        if (segmentCleared[index]) return;
        segmentCleared[index] = true;

        // Once the player actually reaches the start point, remove the marker
        if (index == 0 && startMarkerInstance != null)
        {
            Destroy(startMarkerInstance);
            startMarkerInstance = null;
        }

        var remaining = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < currentPath.pathPoints.Count; i++)
            if (!segmentCleared[i]) remaining.Add(currentPath.pathPoints[i]);

        lr.positionCount = remaining.Count;
        lr.SetPositions(remaining.ToArray());
    }
}