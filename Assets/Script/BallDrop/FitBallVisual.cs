using UnityEngine;

public class FitBallVisual : MonoBehaviour
{
    public float targetDiameter = 0.15f;

    public void Fit()
    {
        if (!TryGetWorldBounds(out Bounds bounds))
            return;

        float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (largest < 0.0001f)
            return;

        transform.localScale *= targetDiameter / largest;

        if (!TryGetWorldBounds(out bounds))
            return;

        Vector3 center = transform.parent != null ? transform.parent.position : transform.position;
        transform.position += center - bounds.center;
    }

    bool TryGetWorldBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter.sharedMesh == null)
                continue;

            Encapsulate(filter.transform, filter.sharedMesh.bounds, ref bounds, ref found);
        }

        if (found)
            return true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static void Encapsulate(Transform meshTransform, Bounds localBounds, ref Bounds worldBounds, ref bool found)
    {
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = meshTransform.TransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z)));
                    if (!found)
                    {
                        worldBounds = new Bounds(corner, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        worldBounds.Encapsulate(corner);
                    }
                }
            }
        }
    }
}
