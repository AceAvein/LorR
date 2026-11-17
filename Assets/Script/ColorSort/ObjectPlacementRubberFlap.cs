using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tunnel mouth rubber flap.
/// Grocery: stays open while an item is blocking/overlapping the mouth; closes when clear.
/// Boxes: one open→close cycle per sorted crate.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementRubberFlap : MonoBehaviour
{
    public enum FlapKind
    {
        Grocery,
        Boxes
    }

    [SerializeField] FlapKind kind = FlapKind.Grocery;
    [SerializeField] float openAngle = 78f;
    [SerializeField] float openDuration = 0.18f;
    [SerializeField] float holdOpen = 0.28f;
    [SerializeField] float closeDuration = 0.28f;
    [SerializeField] float closeDelay = 0.08f;
    [Tooltip("Local hinge axis. Thin X flap typically swings on Z.")]
    [SerializeField] Vector3 hingeAxis = new Vector3(0f, 0f, 1f);

    Transform hinge;
    Quaternion hingeClosedLocal;
    Quaternion hingeOpenLocal;
    Collider flapCollider;
    readonly HashSet<int> blockingCargo = new HashSet<int>();
    readonly HashSet<int> consumedBoxCargo = new HashSet<int>();
    readonly Queue<int> pendingBoxCycles = new Queue<int>();
    Coroutine animRoutine;
    bool animating;
    float closeAfter = -1f;
    float openAmount;

    public FlapKind Kind
    {
        get => kind;
        set => kind = value;
    }

    public static ObjectPlacementRubberFlap EnsureOn(Transform flap, FlapKind flapKind)
    {
        if (flap == null)
            return null;

        ObjectPlacementRubberFlap script = flap.GetComponent<ObjectPlacementRubberFlap>();
        if (script == null)
            script = flap.gameObject.AddComponent<ObjectPlacementRubberFlap>();
        script.kind = flapKind;
        script.Setup();
        return script;
    }

    void Awake()
    {
        Setup();
    }

    void Setup()
    {
        EnsureHinge();
        EnsureTrigger();
        if (hinge != null)
        {
            hingeClosedLocal = hinge.localRotation;
            Vector3 axis = hingeAxis.sqrMagnitude > 0.0001f ? hingeAxis.normalized : Vector3.forward;
            hingeOpenLocal = hingeClosedLocal * Quaternion.AngleAxis(openAngle, axis);
            hinge.localRotation = hingeClosedLocal;
            openAmount = 0f;
        }
    }

    void EnsureHinge()
    {
        if (hinge != null)
            return;

        if (transform.parent != null && transform.parent.name.IndexOf("Hinge", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            hinge = transform.parent;
            return;
        }

        Vector3 topWorld = transform.TransformPoint(new Vector3(0f, 0.5f, 0f));
        GameObject hingeGo = new GameObject(gameObject.name + " Hinge");
        hingeGo.transform.SetParent(transform.parent, false);
        hingeGo.transform.SetPositionAndRotation(topWorld, transform.rotation);
        hingeGo.transform.SetSiblingIndex(transform.GetSiblingIndex());
        transform.SetParent(hingeGo.transform, true);
        hinge = hingeGo.transform;
    }

    void EnsureTrigger()
    {
        flapCollider = GetComponent<Collider>();
        if (flapCollider == null)
            flapCollider = gameObject.AddComponent<BoxCollider>();
        flapCollider.isTrigger = true;

        BoxCollider box = flapCollider as BoxCollider;
        if (box != null)
        {
            Vector3 size = box.size;
            size.x = Mathf.Max(size.x, 8f);
            size.y = Mathf.Max(size.y, 1.05f);
            size.z = Mathf.Max(size.z, 1.05f);
            box.size = size;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (hinge == null || kind != FlapKind.Grocery || animating)
            return;

        // Grocery: open while blocked by item(s); close only when mouth is clear.
        bool blocked = blockingCargo.Count > 0 || (closeAfter > 0f && Time.time < closeAfter);
        float target = blocked ? 1f : 0f;
        float speed = blocked ? (1f / Mathf.Max(0.05f, openDuration)) : (1f / Mathf.Max(0.05f, closeDuration));
        float next = Mathf.MoveTowards(openAmount, target, speed * Time.deltaTime);
        if (!Mathf.Approximately(next, openAmount))
        {
            openAmount = next;
            hinge.localRotation = Quaternion.Slerp(hingeClosedLocal, hingeOpenLocal, openAmount);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        int cargoId;
        if (!TryGetCargoId(other, out cargoId))
            return;

        if (kind == FlapKind.Grocery)
        {
            blockingCargo.Add(cargoId);
            closeAfter = -1f;
            return;
        }

        // Boxes: one open→close cycle per crate.
        if (!consumedBoxCargo.Add(cargoId))
            return;

        pendingBoxCycles.Enqueue(cargoId);
        if (!animating)
            animRoutine = StartCoroutine(RunBoxCycles());
    }

    void OnTriggerStay(Collider other)
    {
        if (kind != FlapKind.Grocery)
            return;

        int cargoId;
        if (!TryGetCargoId(other, out cargoId))
            return;

        blockingCargo.Add(cargoId);
        closeAfter = -1f;
    }

    void OnTriggerExit(Collider other)
    {
        int cargoId;
        if (!TryGetCargoId(other, out cargoId))
            return;

        if (kind == FlapKind.Grocery)
        {
            blockingCargo.Remove(cargoId);
            if (blockingCargo.Count == 0)
                closeAfter = Time.time + closeDelay;
            return;
        }

        consumedBoxCargo.Remove(cargoId);
    }

    IEnumerator RunBoxCycles()
    {
        animating = true;
        while (pendingBoxCycles.Count > 0)
        {
            pendingBoxCycles.Dequeue();
            yield return PlayBoxOpenCloseOnce();
        }

        animating = false;
        animRoutine = null;
    }

    IEnumerator PlayBoxOpenCloseOnce()
    {
        if (hinge == null)
            yield break;

        Quaternion fromPose = hinge.localRotation;

        float t = 0f;
        float open = Mathf.Max(0.05f, openDuration);
        while (t < open)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / open);
            u = u * u * (3f - 2f * u);
            hinge.localRotation = Quaternion.Slerp(fromPose, hingeOpenLocal, u);
            yield return null;
        }

        hinge.localRotation = hingeOpenLocal;
        if (holdOpen > 0f)
            yield return new WaitForSeconds(holdOpen);

        t = 0f;
        float close = Mathf.Max(0.05f, closeDuration);
        Quaternion from = hinge.localRotation;
        while (t < close)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / close);
            u = u * u * (3f - 2f * u);
            hinge.localRotation = Quaternion.Slerp(from, hingeClosedLocal, u);
            yield return null;
        }

        hinge.localRotation = hingeClosedLocal;
    }

    bool TryGetCargoId(Collider other, out int cargoId)
    {
        cargoId = 0;
        if (other == null)
            return false;

        Transform root = null;
        if (kind == FlapKind.Grocery)
        {
            ObjectPlacementGroceryItem item = other.GetComponentInParent<ObjectPlacementGroceryItem>();
            if (item != null)
                root = item.transform;
        }
        else
        {
            ObjectPlacementSortBox box = other.GetComponentInParent<ObjectPlacementSortBox>();
            if (box != null)
                root = box.transform;
        }

        if (root == null)
            return false;

        cargoId = root.GetInstanceID();
        return true;
    }

    public static void WireStationFlaps(ObjectPlacementStation station)
    {
        if (station == null)
            return;

        Transform groceryFlap = FindFlap(station.transform, "RubberFlap Grocery");
        if (groceryFlap == null && station.supplyBoxLeft != null)
            groceryFlap = FindFlap(station.supplyBoxLeft, "RubberFlap");
        if (groceryFlap == null && station.conveyorLeft != null)
            groceryFlap = FindFlap(station.conveyorLeft, "RubberFlap");

        Transform boxesFlap = FindFlap(station.transform, "RubberFlap Boxes");
        if (boxesFlap == null && station.supplyBox != null)
        {
            boxesFlap = FindFlap(station.supplyBox, "RubberFlap Boxes");
            if (boxesFlap == null)
                boxesFlap = FindFlap(station.supplyBox, "RubberFlap");
        }

        if (groceryFlap != null)
        {
            if (groceryFlap.name.IndexOf("Grocery", System.StringComparison.OrdinalIgnoreCase) < 0)
                groceryFlap.name = "RubberFlap Grocery";
            EnsureOn(groceryFlap, FlapKind.Grocery);
        }

        if (boxesFlap != null && boxesFlap != groceryFlap)
        {
            if (boxesFlap.name.IndexOf("Boxes", System.StringComparison.OrdinalIgnoreCase) < 0)
                boxesFlap.name = "RubberFlap Boxes";
            EnsureOn(boxesFlap, FlapKind.Boxes);
        }
    }

    static Transform FindFlap(Transform root, string exactName)
    {
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == exactName)
                return all[i];
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            if (all[i].name.IndexOf("RubberFlap", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                all[i].name.IndexOf(exactName.Replace("RubberFlap ", ""), System.StringComparison.OrdinalIgnoreCase) >= 0)
                return all[i];
        }

        return null;
    }
}
