using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Phase I grocery: rides belt until grabbed; Fruit=Grip→Yellow, Vegetable=Trigger→Red.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementGroceryItem : MonoBehaviour
{
    const float InputThreshold = 0.45f;

    public string ItemId { get; private set; }
    public ObjectPlacementGroceryKind Kind { get; private set; }
    public bool Resolved { get; private set; }
    public bool IsHeld => grab != null && grab.isSelected;

    ObjectPlacementPhaseIRound host;
    XRGrabInteractable grab;
    Rigidbody body;
    BoxCollider grabCollider;
    Transform beltRoot;
    float beltDirection = 1f;
    float beltSpeed;
    bool onBelt = true;
    bool grabWired;
    bool pendingReject;
    IXRSelectInteractor pendingInteractor;
    InputAction leftGrip;
    InputAction rightGrip;
    InputAction leftTrigger;
    InputAction rightTrigger;

    public void Configure(string id, ObjectPlacementGroceryKind kind)
    {
        ItemId = id;
        Kind = kind;
        EnsurePhysics();
    }

    public void Bind(ObjectPlacementPhaseIRound round)
    {
        host = round;
        EnsurePhysics();
    }

    public void PlaceOnBelt(Transform spawn, Transform belt, float direction, float speed)
    {
        Resolved = false;
        onBelt = true;
        beltRoot = belt;
        beltDirection = Mathf.Sign(direction) == 0f ? 1f : Mathf.Sign(direction);
        beltSpeed = speed;

        Vector3 pos = spawn != null ? spawn.position : (belt != null ? belt.position + Vector3.up * 0.05f : transform.position);
        transform.SetPositionAndRotation(pos, spawn != null ? spawn.rotation : Quaternion.identity);
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        gameObject.SetActive(true);
        if (grab != null)
            grab.enabled = true;
    }

    public void SetBeltSpeed(float speed)
    {
        beltSpeed = speed;
    }

    void OnEnable()
    {
        EnsureInput();
    }

    void OnDisable()
    {
        DisposeInput();
    }

    void OnDestroy()
    {
        if (grab != null && grabWired)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            grab.hoverEntered.RemoveListener(OnHoverEntered);
        }

        DisposeInput();
    }

    void EnsurePhysics()
    {
        if (grabCollider == null)
        {
            Bounds bounds = new Bounds(transform.position, Vector3.one * 0.08f);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool has = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                if (!has)
                {
                    bounds = renderers[i].bounds;
                    has = true;
                }
                else
                    bounds.Encapsulate(renderers[i].bounds);
            }

            grabCollider = gameObject.AddComponent<BoxCollider>();
            if (has)
            {
                grabCollider.center = transform.InverseTransformPoint(bounds.center);
                Vector3 lossy = transform.lossyScale;
                grabCollider.size = new Vector3(
                    bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                    bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                    bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
            }
            else
            {
                grabCollider.center = Vector3.zero;
                grabCollider.size = Vector3.one * 0.08f;
            }
        }

        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.useDynamicAttach = true;
        grab.throwOnDetach = false;
        grab.attachEaseInTime = 0f;
        grab.selectMode = InteractableSelectMode.Single;
        grab.colliders.Clear();
        grab.colliders.Add(grabCollider);

        if (!grabWired)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
            grab.hoverEntered.AddListener(OnHoverEntered);
            grabWired = true;
        }
    }

    void EnsureInput()
    {
        if (leftGrip != null)
            return;

        leftGrip = new InputAction(type: InputActionType.Value, binding: "<XRController>{LeftHand}/grip");
        rightGrip = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/grip");
        leftTrigger = new InputAction(type: InputActionType.Value, binding: "<XRController>{LeftHand}/trigger");
        rightTrigger = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/trigger");
        leftGrip.Enable();
        rightGrip.Enable();
        leftTrigger.Enable();
        rightTrigger.Enable();
    }

    void DisposeInput()
    {
        leftGrip?.Dispose();
        rightGrip?.Dispose();
        leftTrigger?.Dispose();
        rightTrigger?.Dispose();
        leftGrip = null;
        rightGrip = null;
        leftTrigger = null;
        rightTrigger = null;
    }

    void Update()
    {
        if (Resolved || GamePauseMenu.IsFrozen)
            return;

        TryTriggerSelectWhileHovered();

        if (!onBelt || IsHeld || beltRoot == null)
            return;

        Vector3 delta = beltRoot.right * (beltSpeed * beltDirection * Time.deltaTime);
        transform.position += delta;

        // Drop off the end of the belt → count as miss and recycle.
        Vector3 local = beltRoot.InverseTransformPoint(transform.position);
        float half = Mathf.Abs(beltRoot.localScale.x) * 0.5f + 0.15f;
        bool pastEnd = beltDirection >= 0f ? local.x > half : local.x < -half;
        if (pastEnd && host != null && !Resolved)
        {
            Resolved = true;
            if (grab != null)
                grab.enabled = false;
            gameObject.SetActive(false);
            host.OnItemResolved(this, false);
        }
    }

    void LateUpdate()
    {
        if (pendingInteractor != null && grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.SelectExit(pendingInteractor, grab);

        pendingInteractor = null;

        if (pendingReject)
        {
            pendingReject = false;
            onBelt = true;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }
    }

    void TryTriggerSelectWhileHovered()
    {
        if (Kind != ObjectPlacementGroceryKind.Vegetable || grab == null || grab.isSelected)
            return;

        if (grab.interactorsHovering == null || grab.interactorsHovering.Count == 0)
            return;

        for (int i = 0; i < grab.interactorsHovering.Count; i++)
        {
            IXRHoverInteractor hover = grab.interactorsHovering[i];
            if (!(hover is IXRSelectInteractor selectInteractor))
                continue;

            bool left = IsLeftHand(selectInteractor);
            float trigger = ReadTrigger(left);
            float grip = ReadGrip(left);
            if (trigger < InputThreshold || trigger < grip)
                continue;

            if (grab.interactionManager != null && !grab.isSelected)
                grab.interactionManager.SelectEnter(selectInteractor, (IXRSelectInteractable)grab);
            break;
        }
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Trigger-select path handled in Update for vegetables.
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (Resolved || GamePauseMenu.IsFrozen)
        {
            pendingReject = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        bool left = IsLeftHand(args.interactorObject);
        float grip = ReadGrip(left);
        float trigger = ReadTrigger(left);

        bool ok;
        if (Kind == ObjectPlacementGroceryKind.Fruit)
            ok = grip >= InputThreshold && grip >= trigger;
        else
            ok = trigger >= InputThreshold && trigger >= grip;

        if (!ok)
        {
            pendingReject = true;
            pendingInteractor = args.interactorObject;
            host?.OnWrongControl(this);
            return;
        }

        onBelt = false;
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (Resolved || pendingReject)
            return;

        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        TryScoreDrop();
    }

    void TryScoreDrop()
    {
        if (host == null || Resolved)
            return;

        if (host.TryScoreItem(this, out bool correct))
        {
            Resolved = true;
            if (grab != null)
                grab.enabled = false;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            gameObject.SetActive(false);
            host.OnItemResolved(this, correct);
            return;
        }

        // Missed box — allow re-grab; soft land near belt if close.
        onBelt = false;
    }

    public void ForceResolve()
    {
        Resolved = true;
        if (grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        if (grab != null)
            grab.enabled = false;
        gameObject.SetActive(false);
    }

    static bool IsLeftHand(IXRInteractor interactor)
    {
        if (interactor == null)
            return true;

        Transform t = interactor.transform;
        string n = t != null ? t.name : string.Empty;
        if (n.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        Transform root = t;
        while (root != null)
        {
            string rn = root.name;
            if (rn.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (rn.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            root = root.parent;
        }

        return true;
    }

    float ReadGrip(bool left)
    {
        EnsureInput();
        InputAction action = left ? leftGrip : rightGrip;
        return action != null ? action.ReadValue<float>() : 0f;
    }

    float ReadTrigger(bool left)
    {
        EnsureInput();
        InputAction action = left ? leftTrigger : rightTrigger;
        return action != null ? action.ReadValue<float>() : 0f;
    }
}
