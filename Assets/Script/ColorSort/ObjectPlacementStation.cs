using TMPro;
using UnityEngine;

/// <summary>
/// Marker for the Object Placement / Color Sort factory station.
/// Box GameObjects must stay named Red Box / Blue Box / Yellow Box for ColorSortRound.
/// Simulation flow: front conveyor = grocery items; left conveyor = sorted boxes.
/// Both belts scroll toward the left (-X along belt.right).
/// </summary>
public class ObjectPlacementStation : MonoBehaviour
{
    public Transform boxesRoot;
    public Transform redBox;
    public Transform blueBox;
    public Transform yellowBox;
    public Transform startButton;

    [Header("Front conveyor (items in)")]
    public Transform conveyor;
    public Transform supplyBox;
    public Transform grocerySpawnPoint;
    public Transform beltSurface;
    [Tooltip("Item stopper at the leftward end of the front belt.")]
    public Transform beltStopper;

    [Header("Left conveyor (sorted boxes out)")]
    public Transform conveyorLeft;
    public Transform supplyBoxLeft;
    public Transform grocerySpawnPointLeft;
    public Transform beltSurfaceLeft;
    [Tooltip("Legacy alias; item stopper now prefers beltStopper on the front belt.")]
    public Transform beltStopperLeft;

    public TMP_Text setScreenLabel;

    void Awake()
    {
        if (GetComponent<ObjectPlacementConveyorController>() == null)
            gameObject.AddComponent<ObjectPlacementConveyorController>();
        ObjectPlacementRubberFlap.WireStationFlaps(this);
    }

    public void SetScreenText(string value)
    {
        if (setScreenLabel == null)
            return;
        setScreenLabel.text = value;
    }

    public Transform ItemBelt => beltSurface != null ? beltSurface : conveyor;

    public Transform BoxBelt =>
        beltSurfaceLeft != null ? beltSurfaceLeft : (conveyorLeft != null ? conveyorLeft : ItemBelt);

    /// <summary>
    /// Mesh-local X range of a belt surface. Prefer MeshFilter bounds so rotation/AABB
    /// never shrink the usable length to the middle of the belt.
    /// </summary>
    public static void GetBeltLocalXRange(Transform belt, out float minX, out float maxX)
    {
        minX = -0.5f;
        maxX = 0.5f;
        if (belt == null)
            return;

        MeshFilter mf = belt.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            minX = b.min.x;
            maxX = b.max.x;
            if (maxX > minX + 0.01f)
                return;
        }

        Renderer r = belt.GetComponent<Renderer>();
        if (r != null)
        {
#if UNITY_2021_2_OR_NEWER
            Bounds lb = r.localBounds;
            if (lb.size.x > 0.01f)
            {
                minX = lb.min.x;
                maxX = lb.max.x;
                return;
            }
#endif
        }
    }

    public static float GetBeltTopLocalY(Transform belt)
    {
        if (belt == null)
            return 0.06f;

        MeshFilter mf = belt.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            return mf.sharedMesh.bounds.max.y + 0.03f;

        Renderer r = belt.GetComponent<Renderer>();
        if (r != null)
        {
#if UNITY_2021_2_OR_NEWER
            return r.localBounds.max.y + 0.03f;
#else
            return belt.InverseTransformPoint(r.bounds.center + Vector3.up * r.bounds.extents.y).y + 0.03f;
#endif
        }

        return 0.06f;
    }

    /// <summary>
    /// World point on belt top. localX01: 0 = minX end, 1 = maxX end.
    /// inset: meters-ish in local units pulled inward from that end.
    /// </summary>
    public static Vector3 BeltPointFromEnd(Transform belt, bool atMaxXEnd, float insetLocal = 0.08f)
    {
        if (belt == null)
            return Vector3.zero;

        float minX, maxX;
        GetBeltLocalXRange(belt, out minX, out maxX);
        float span = Mathf.Max(0.05f, maxX - minX);
        insetLocal = Mathf.Clamp(insetLocal, 0.02f, span * 0.2f);
        float x = atMaxXEnd ? maxX - insetLocal : minX + insetLocal;
        Vector3 local = new Vector3(x, GetBeltTopLocalY(belt), 0f);
        return belt.TransformPoint(local);
    }

    /// <summary>Incoming end for flowDir along belt.right (-1 = toward minX).</summary>
    public static Vector3 BeltSpawnPoint(Transform belt, float flowDir)
    {
        // flow -X → spawn at maxX; flow +X → spawn at minX.
        bool spawnAtMax = flowDir < 0f;
        return BeltPointFromEnd(belt, spawnAtMax, 0.06f);
    }

    /// <summary>Outgoing end for flowDir along belt.right (-1 = toward minX).</summary>
    public static Vector3 BeltStopPoint(Transform belt, float flowDir, float insetLocal = 0.08f)
    {
        bool stopAtMax = flowDir > 0f;
        return BeltPointFromEnd(belt, stopAtMax, insetLocal);
    }

    public static Vector3 ProjectOntoBelt(Transform belt, Vector3 world)
    {
        if (belt == null)
            return world;

        float minX, maxX;
        GetBeltLocalXRange(belt, out minX, out maxX);
        Vector3 local = belt.InverseTransformPoint(world);
        float pad = Mathf.Min(0.04f, (maxX - minX) * 0.05f);
        local.x = Mathf.Clamp(local.x, minX + pad, maxX - pad);
        local.y = GetBeltTopLocalY(belt);
        local.z = 0f;
        return belt.TransformPoint(local);
    }

    /// <summary>Snap/create the item stopper at the true outgoing end of the front belt.</summary>
    public Transform EnsureItemStopper(float flowDir = -1f)
    {
        Transform belt = ItemBelt;
        if (belt == null)
            return beltStopper;

        Vector3 end = BeltStopPoint(belt, flowDir, 0.08f);

        if (beltStopper == null && beltStopperLeft != null &&
            conveyor != null && beltStopperLeft.IsChildOf(conveyor))
            beltStopper = beltStopperLeft;

        if (beltStopper == null)
        {
            Transform root = conveyor != null ? conveyor : belt;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.IndexOf("stop", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    beltStopper = all[i];
                    break;
                }
            }
        }

        if (beltStopper == null)
        {
            GameObject stop = new GameObject("BeltStopper");
            stop.transform.SetParent(conveyor != null ? conveyor : belt, false);
            BoxCollider col = stop.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.08f, 0.16f, 0.35f);
            beltStopper = stop.transform;
        }

        beltStopper.position = end;
        return beltStopper;
    }

    public Transform EnsureItemSpawn(float flowDir = -1f)
    {
        Transform belt = ItemBelt;
        if (belt == null)
            return grocerySpawnPoint;

        // Ignore spawn points that still live under the left (box) conveyor.
        if (grocerySpawnPoint != null && conveyorLeft != null && grocerySpawnPoint.IsChildOf(conveyorLeft))
            grocerySpawnPoint = null;

        Vector3 at = BeltSpawnPoint(belt, flowDir);

        if (grocerySpawnPoint == null)
        {
            Transform existing = belt.Find("GrocerySpawnPoint");
            if (existing == null && belt.parent != null)
                existing = belt.parent.Find("GrocerySpawnPoint");
            if (existing != null)
                grocerySpawnPoint = existing;
        }

        if (grocerySpawnPoint == null)
        {
            GameObject spawnGo = new GameObject("GrocerySpawnPoint");
            spawnGo.transform.SetParent(conveyor != null ? conveyor : belt, false);
            grocerySpawnPoint = spawnGo.transform;
        }

        grocerySpawnPoint.position = at;
        return grocerySpawnPoint;
    }
}
