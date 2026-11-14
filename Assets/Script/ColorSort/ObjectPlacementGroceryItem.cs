using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Phase I grocery: rides left belt at FlowMark speed, stops at stopper,
/// Fruit=Grip→Yellow, Vegetable=Trigger→Red. Settles visible inside boxes.
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
    Transform stopper;
    float beltDirection = 1f;
    float beltSpeed;
    bool onBelt = true;
    bool atStopper;
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

    public void PlaceOnBelt(Transform spawn, Transform belt, float direction, float speed, Transform beltStopper)
    {
        Resolved = false;
        atStopper = false;
        onBelt = true;
        host?.LeaveBeltQueue(this);
        beltRoot = belt;
        stopper = beltStopper;
        beltDirection = Mathf.Sign(direction) == 0f ? 1f : Mathf.Sign(direction);
        beltSpeed = speed;

        Vector3 pos = spawn != null ? spawn.position : (belt != null ? belt.position + Vector3.up * 0.05f : transform.position);
        Quaternion rot = spawn != null ? spawn.rotation : Quaternion.identity;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(pos, rot);

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
        }

        DisposeInput();
    }

    void EnsurePhysics()
    {
        Collider[] childCols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < childCols.Length; i++)
        {
            if (childCols[i] != null && childCols[i].gameObject != gameObject)
                Object.Destroy(childCols[i]);
        }

        if (grabCollider == null)
            grabCollider = GetComponent<BoxCollider>();
        if (grabCollider == null)
            grabCollider = gameObject.AddComponent<BoxCollider>();

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

        if (has)
        {
            grabCollider.center = transform.InverseTransformPoint(bounds.center);
            Vector3 lossy = transform.lossyScale;
            grabCollider.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
        }

        grabCollider.isTrigger = false;

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
        ObjectPlacementHandOnlyFilter.AttachTo(grab);

        if (!grabWired)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
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

        if (IsHeld || beltRoot == null)
            return;

        // Belts only move after Btn_Start_World starts the conveyor.
        if (host != null && !host.BeltIsRunning)
            return;

        if (atStopper)
        {
            // Track assigned queue slot; slides forward when the front item is taken.
            FollowQueueSlot(false);
            return;
        }

        if (!onBelt)
            return;

        // Queue full — left belt and in-transit items freeze until the line moves again.
        if (host != null && host.LeftBeltMotionPaused)
        {
            ZeroVelocity();
            return;
        }

        // Ride belt until queue zone, then join line without overlapping.
        if (InQueueZone() && host != null)
        {
            if (host.BeltQueueHasRoom)
            {
                int slot = host.JoinBeltQueue(this);
                if (slot >= 0)
                {
                    FollowQueueSlot(false);
                    return;
                }
            }

            // Queue full — stay put mid-belt; do not stack on the stopper.
            ZeroVelocity();
            return;
        }

        Vector3 delta = beltRoot.right * (beltSpeed * beltDirection * Time.deltaTime);
        transform.position += delta;
    }

    bool InQueueZone()
    {
        if (host == null)
            return stopper != null && Vector3.Distance(transform.position, stopper.position) < 0.75f;

        Vector3 front = host.GetBeltQueueSlotWorld(0);
        Vector3 back = host.GetBeltQueueSlotWorld(ObjectPlacementPhaseIRound.MaxBeltQueue);
        float zone = Vector3.Distance(front, back) + 0.12f;
        return Vector3.Distance(transform.position, front) <= zone;
    }

    void FollowQueueSlot(bool snapIfClose)
    {
        if (host == null)
            return;

        // Already requeued (incl. miss overflow) or newly joining under MaxBeltQueue.
        if (!host.IsInBeltQueue(this))
        {
            if (host.JoinBeltQueue(this) < 0 && !atStopper)
                return;
        }

        Vector3 target = host.GetBeltQueueWorldPosition(this);
        float dist = Vector3.Distance(transform.position, target);
        if (snapIfClose || dist < 0.03f)
        {
            transform.position = target;
            atStopper = true;
            onBelt = false;
            ZeroVelocity();
            return;
        }

        MoveToward(target, Mathf.Max(beltSpeed, 0.45f));
        if (Vector3.Distance(transform.position, target) < 0.03f)
        {
            transform.position = target;
            atStopper = true;
            onBelt = false;
            ZeroVelocity();
        }
    }

    void MoveToward(Vector3 target, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        ZeroVelocity();
    }

    void ZeroVelocity()
    {
        if (body == null)
            return;
        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    void SnapToStopper()
    {
        if (host != null)
        {
            host.JoinBeltQueue(this);
            transform.position = host.GetBeltQueueWorldPosition(this);
        }
        else if (stopper != null)
        {
            Vector3 p = transform.position;
            Vector3 stop = stopper.position;
            if (beltRoot != null)
            {
                Vector3 local = beltRoot.InverseTransformPoint(stop);
                local.y = beltRoot.InverseTransformPoint(p).y;
                local.x -= beltDirection * 0.06f;
                p = beltRoot.TransformPoint(local);
            }
            else
            {
                p = stop + Vector3.up * 0.04f;
            }

            transform.position = p;
        }

        atStopper = true;
        onBelt = false;
        ZeroVelocity();
    }

    void LateUpdate()
    {
        if (pendingInteractor != null && grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.SelectExit(pendingInteractor, grab);

        pendingInteractor = null;

        if (pendingReject)
        {
            pendingReject = false;
            if (atStopper)
                onBelt = false;
            else
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

            // Never promote a ray / far hover into a grab.
            if (!ObjectPlacementHandOnlyFilter.AllowsHandGrab(selectInteractor, grab))
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

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (Resolved || GamePauseMenu.IsFrozen)
        {
            pendingReject = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        if (!ObjectPlacementHandOnlyFilter.AllowsHandGrab(args.interactorObject, grab))
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
        atStopper = false;
        host?.LeaveBeltQueue(this);
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

        // Score after a short settle so bounds overlap the bin.
        if (host != null)
            host.RequestScoreCheck(this);
    }

    public void SettleInBox(Transform box, bool correct)
    {
        Resolved = true;
        onBelt = false;
        atStopper = false;
        host?.LeaveBeltQueue(this);

        if (grab != null)
        {
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            grab.enabled = false;
        }

        ZeroVelocity();

        if (box != null)
        {
            // Place in box local space above the visible floor — collider centers
            // (especially Red Box) often sit low and bury meshes in world space.
            transform.SetParent(box, false);
            Transform floor = box.Find("Floor");
            Vector3 local;
            if (floor != null)
            {
                local = floor.localPosition + new Vector3(
                    Random.Range(-0.09f, 0.09f),
                    0.08f + Random.Range(0f, 0.06f),
                    Random.Range(-0.09f, 0.09f));
            }
            else
            {
                local = new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    0.16f + Random.Range(0f, 0.08f),
                    Random.Range(-0.1f, 0.1f));
            }

            transform.localPosition = local;
            transform.localRotation = Quaternion.Euler(
                Random.Range(-18f, 18f),
                Random.Range(0f, 360f),
                Random.Range(-18f, 18f));
        }

        gameObject.SetActive(true);
        host?.OnItemResolved(this, correct);
    }

    public void ReturnToStopper()
    {
        if (Resolved)
            return;

        atStopper = false;
        onBelt = true;
        if (host != null)
        {
            // Always last in this set's line so misses never stack on the front.
            host.RequeueAtEnd(this);
            FollowQueueSlot(true);
        }
        else
        {
            SnapToStopper();
        }

        ZeroVelocity();
        if (grab != null)
            grab.enabled = true;
    }

    public void ForceResolve()
    {
        Resolved = true;
        if (grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        if (grab != null)
            grab.enabled = false;
    }

    static bool IsLeftHand(IXRInteractor interactor)
    {
        if (interactor == null)
            return true;

        Transform t = interactor.transform;
        while (t != null)
        {
            string n = t.name;
            if (n.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            t = t.parent;
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
