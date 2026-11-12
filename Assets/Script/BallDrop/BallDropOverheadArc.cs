using UnityEngine;

/// <summary>
/// Marker for the storyboard Ball Drop overhead dropper (Catch-the-Stick style arc).
/// Assign <see cref="spawnRoot"/> to BallDropSystem.transformAbove.
/// Children may be SpawnPoint_* directly, or Hanger_* with a SpawnPoint_* child.
/// </summary>
public class BallDropOverheadArc : MonoBehaviour
{
    [Tooltip("Parent of hangers/spawn points (wired to BallDropSystem.transformAbove).")]
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

        System.Collections.Generic.List<Transform> found = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < spawnRoot.childCount; i++)
        {
            Transform child = spawnRoot.GetChild(i);
            if (child == null)
                continue;

            string n = child.name;
            if (n == "SpawnPoint" || n.StartsWith("SpawnPoint"))
            {
                found.Add(child);
                continue;
            }

            if (!n.StartsWith("Hanger_"))
                continue;

            Transform nested = null;
            for (int c = 0; c < child.childCount; c++)
            {
                Transform sp = child.GetChild(c);
                if (sp != null && (sp.name == "SpawnPoint" || sp.name.StartsWith("SpawnPoint")))
                {
                    nested = sp;
                    break;
                }
            }

            found.Add(nested != null ? nested : (child.childCount > 0 ? child.GetChild(0) : child));
        }

        if (found.Count == 0)
        {
            spawnPoints = new Transform[spawnRoot.childCount];
            for (int i = 0; i < spawnRoot.childCount; i++)
                spawnPoints[i] = spawnRoot.GetChild(i);
            return;
        }

        spawnPoints = found.ToArray();
    }
}
