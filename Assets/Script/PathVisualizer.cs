using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    [Tooltip("Optional: assign a custom 'Start Here' marker prefab.")]
    public GameObject startMarkerPrefab;

    private LineRenderer lr;
    private LineRenderer traceRenderer;

    private TracingPath currentPath;
    private bool[] segmentCleared;
    private GameObject startMarkerInstance;

    private List<Vector3> tracePoints = new List<Vector3>();

    void Awake()
    {
        // Original target-path renderer
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;

        // Create a separate renderer for the player's actual tracing
        GameObject traceObject = new GameObject("PlayerTraceLine");
        traceObject.transform.SetParent(transform, false);

        traceRenderer = traceObject.AddComponent<LineRenderer>();
        traceRenderer.useWorldSpace = false;

        traceRenderer.positionCount = 0;
        traceRenderer.startWidth = 0.025f;
        traceRenderer.endWidth = 0.025f;

        Material traceMaterial = new Material(
            Shader.Find("Universal Render Pipeline/Unlit")
        );

        traceMaterial.color = Color.yellow;
        traceRenderer.material = traceMaterial;
    }

    public void LoadPath(TracingPath path, Transform anchor)
    {
        currentPath = path;

        transform.position = anchor.position;
        transform.rotation = anchor.rotation;

        // Load the original target path
        lr.positionCount = path.pathPoints.Count;
        lr.SetPositions(path.pathPoints.ToArray());

        segmentCleared = new bool[path.pathPoints.Count];

        // Reset player's tracing line
        tracePoints.Clear();
        traceRenderer.positionCount = 0;

        SpawnStartMarker(path.pathPoints[0]);
    }

    public void AddTracePoint(Vector3 worldPosition)
    {
        // Convert world position into PathVisualizer's local space
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        // Don't add hundreds of points in almost exactly the same position
        if (tracePoints.Count > 0)
        {
            float distance = Vector3.Distance(
                tracePoints[tracePoints.Count - 1],
                localPosition
            );

            if (distance < 0.01f)
                return;
        }

        tracePoints.Add(localPosition);

        traceRenderer.positionCount = tracePoints.Count;
        traceRenderer.SetPositions(tracePoints.ToArray());
    }

    void SpawnStartMarker(Vector3 localStartPoint)
    {
        if (startMarkerInstance != null)
            Destroy(startMarkerInstance);

        if (startMarkerPrefab != null)
        {
            startMarkerInstance = Instantiate(startMarkerPrefab, transform);
        }
        else
        {
            startMarkerInstance =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            startMarkerInstance.name = "StartMarker";

            startMarkerInstance.transform.SetParent(transform, false);
            startMarkerInstance.transform.localScale = Vector3.one * 0.08f;

            var renderer = startMarkerInstance.GetComponent<Renderer>();

            var mat = new Material(
                Shader.Find("Universal Render Pipeline/Unlit")
            );

            mat.color = Color.green;
            renderer.material = mat;

            var collider = startMarkerInstance.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);
        }

        startMarkerInstance.transform.localPosition = localStartPoint;
    }

    public void ClearPointVisual(int index)
    {
        if (segmentCleared[index])
            return;

        segmentCleared[index] = true;

        if (index == 0 && startMarkerInstance != null)
        {
            Destroy(startMarkerInstance);
            startMarkerInstance = null;
        }

        var remaining = new List<Vector3>();

        for (int i = 0; i < currentPath.pathPoints.Count; i++)
        {
            if (!segmentCleared[i])
                remaining.Add(currentPath.pathPoints[i]);
        }

        lr.positionCount = remaining.Count;
        lr.SetPositions(remaining.ToArray());
    }
}