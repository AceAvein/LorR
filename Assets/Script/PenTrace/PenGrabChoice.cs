using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum PenGrabKind
{
    Pencil,
    Ballpen,
    Highlighter
}

public class PenGrabChoice : MonoBehaviour
{
    public PenGrabKind kind = PenGrabKind.Pencil;

    XRGrabInteractable grab;
    Vector3 homePosition;
    Quaternion homeRotation;
    Vector3 homeScale;
    Transform homeParent;
    bool pendingRelease;
    IXRSelectInteractor pendingInteractor;

    public PenGrabKind Kind
    {
        get { return kind; }
    }

    void Awake()
    {
        homeParent = transform.parent;
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homeScale = transform.localScale;

        EnsureGrab();
        InferKindFromName();
    }

    void OnDestroy()
    {
        if (grab == null)
            return;

        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void LateUpdate()
    {
        if (!pendingRelease || grab == null || pendingInteractor == null)
            return;

        if (grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.SelectExit(pendingInteractor, grab);

        pendingInteractor = null;
        pendingRelease = false;
    }

    public void ReturnHome()
    {
        pendingRelease = false;
        pendingInteractor = null;

        if (grab != null && grab.isSelected && grab.interactionManager != null)
        {
            var interactors = grab.interactorsSelecting;
            for (int i = interactors.Count - 1; i >= 0; i--)
                grab.interactionManager.SelectExit(interactors[i], grab);
        }

        transform.SetParent(homeParent, true);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        transform.localScale = homeScale;
        gameObject.SetActive(true);
    }

    public void HideOnTable()
    {
        gameObject.SetActive(false);
    }

    void EnsureGrab()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        DisableExtraChildColliders();
        BoxCollider box = EnsureTightGrabBox();

        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.useDynamicAttach = true;
        grab.throwOnDetach = false;
        grab.attachEaseInTime = 0f;
        grab.colliders.Clear();
        grab.colliders.Add(box);

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void DisableExtraChildColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.gameObject == gameObject)
                continue;

            // Child helpers like PenTip often have oversized boxes that block grabs.
            col.enabled = false;
        }
    }

    BoxCollider EnsureTightGrabBox()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;
            if (renderer.gameObject.name.IndexOf("PenTip", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        if (!hasBounds)
        {
            box.center = Vector3.zero;
            box.size = Vector3.one;
            box.isTrigger = false;
            return box;
        }

        box.center = transform.InverseTransformPoint(bounds.center);
        Vector3 lossy = transform.lossyScale;
        Vector3 size = new Vector3(
            bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));

        // Tiny pad only — keep collider close to the mesh size.
        size *= 1.05f;
        box.size = size;
        box.isTrigger = false;
        return box;
    }

    void InferKindFromName()
    {
        string n = gameObject.name;
        if (n.IndexOf("Ballpen", System.StringComparison.OrdinalIgnoreCase) >= 0)
            kind = PenGrabKind.Ballpen;
        else if (n.IndexOf("Highlighter", System.StringComparison.OrdinalIgnoreCase) >= 0)
            kind = PenGrabKind.Highlighter;
        else
            kind = PenGrabKind.Pencil;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (GamePauseMenu.IsFrozen || args.interactorObject == null)
        {
            pendingRelease = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        bool rightHand = IsRightHand(args.interactorObject);
        PenSelect select = FindFirstObjectByType<PenSelect>(FindObjectsInactive.Include);
        if (select == null)
        {
            pendingRelease = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        if (!select.TryEquipFromGrab(kind, rightHand))
        {
            pendingRelease = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        pendingRelease = true;
        pendingInteractor = args.interactorObject;
        HideOnTable();
    }

    void OnReleased(SelectExitEventArgs args)
    {
    }

    static bool IsRightHand(IXRSelectInteractor interactor)
    {
        if (interactor == null)
            return true;

        if (interactor.handedness == InteractorHandedness.Right)
            return true;
        if (interactor.handedness == InteractorHandedness.Left)
            return false;

        Transform current = interactor.transform;
        while (current != null)
        {
            string name = current.name;
            if (name.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (name.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            current = current.parent;
        }

        return true;
    }
}
