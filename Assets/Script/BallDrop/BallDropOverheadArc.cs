using UnityEngine;

/// <summary>
/// Marker for the storyboard Ball Drop overhead dropper (Catch-the-Stick style arc).
/// Assign <see cref="spawnRoot"/> to BallDropSystem.transformAbove (children = spawn points only).
/// </summary>
public class BallDropOverheadArc : MonoBehaviour
{
    [Tooltip("Parent whose children are spawn points only (wired to BallDropSystem.transformAbove).")]
    public Transform spawnRoot;

    public Transform[] spawnPoints;

    public bool HasSpawnPoints()
    {
        return spawnRoot != null && spawnRoot.childCount > 0;
    }

    public void RefreshSpawnPointCache()
    {
        if (spawnRoot == null)
        {
            spawnPoints = System.Array.Empty<Transform>();
            return;
        }

        spawnPoints = new Transform[spawnRoot.childCount];
        for (int i = 0; i < spawnRoot.childCount; i++)
            spawnPoints[i] = spawnRoot.GetChild(i);
    }
}
