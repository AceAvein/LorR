using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Phase I end: Yellow/Red crates become two-hand grab only after all sets.
/// Drop on the facing conveyor → rides inward and despawns.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementSortBox : MonoBehaviour
{
    public enum BoxId
    {
        Yellow,
        Red
    }

    ObjectPlacementPhaseIRound host;
    BoxId id;
    Transform belt;
    float beltDirection = 1f;
    Vector3 homePos;
    Quaternion homeRot;
    Transform homeParent;
    XRGrabInteractable grab;
    Rigidbody body;
    Collider boxCollider;
    bool grabEnabled;
    bool delivering;
    bool delivered;
    Coroutine rideRoutine;

    public bool IsDelivered => delivered;
    public BoxId Id => id;

    public void Configure(ObjectPlacementPhaseIRound round, BoxId boxId, Transform targetBelt, float direction)
    {
        host = round;
        id = boxId;
        belt = targetBelt;
        beltDirection = Mathf.Sign(direction) == 0f ? 1f : Mathf.Sign(direction);
        homePos = transform.position;
        homeRot = transform.rotation;
        homeParent = transform.parent;
        boxCollider = GetComponent<Collider>();
        EnsureGrab(false);
    }

    public void SetGrabEnabled(bool enabled)
    {
        grabEnabled = enabled && !delivered && !delivering;
        EnsureGrab(grabEnabled);
        if (grab != null)
            grab.enabled = grabEnabled;
    }

    void EnsureGrab(bool forUse)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (boxCollider != null)
            boxCollider.isTrigger = !forUse;

        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = false;
        grab.useDynamicAttach = true;
        grab.selectMode = InteractableSelectMode.Multiple;
        // Manual two-hand midpoint in Update (one hand cannot lift).
        grab.trackPosition = false;
        grab.trackRotation = false;
        ObjectPlacementHandOnlyFilter.AttachTo(grab);

        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
        grab.enabled = forUse;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!ObjectPlacementHandOnlyFilter.AllowsHandGrab(args.interactorObject, grab))
        {
            if (grab != null && grab.interactionManager != null)
                grab.interactionManager.SelectExit(args.interactorObject, grab);
            return;
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (delivering || delivered)
            return;

        if (grab != null && grab.interactorsSelecting.Count == 0)
            TryAcceptDrop();
    }

    void Update()
    {
        if (!grabEnabled || delivering || delivered || grab == null)
            return;

        if (grab.interactorsSelecting.Count >= 2)
        {
            IXRSelectInteractor a = grab.interactorsSelecting[0];
            IXRSelectInteractor b = grab.interactorsSelecting[1];
            if (a != null && b != null)
            {
                Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;
                transform.position = mid;
            }
        }
        else if (grab.interactorsSelecting.Count == 1)
        {
            // One hand cannot lift the crate.
            transform.SetPositionAndRotation(homePos, homeRot);
        }
    }

    void TryAcceptDrop()
    {
        if (host == null || delivering || delivered)
            return;

        if (!OverlapsTargetBelt())
        {
            ReturnHome();
            host.OnSortBoxMissed(this);
            return;
        }

        BeginRide();
    }

    bool OverlapsTargetBelt()
    {
        if (belt == null)
            return false;

        Bounds item = GetWorldBounds(transform);
        Collider beltCol = belt.GetComponent<Collider>();
        if (beltCol != null)
        {
            Bounds b = beltCol.bounds;
            b.Expand(0.2f);
            return b.Intersects(item);
        }

        // Fallback: near belt surface along up-axis and within length.
        Vector3 local = belt.InverseTransformPoint(transform.position);
        float halfX = Mathf.Max(0.2f, Mathf.Abs(belt.lossyScale.x) * 0.55f);
        float halfZ = Mathf.Max(0.15f, Mathf.Abs(belt.lossyScale.z) * 0.55f);
        return Mathf.Abs(local.x) <= halfX && Mathf.Abs(local.z) <= halfZ && local.y > -0.05f && local.y < 0.45f;
    }

    void BeginRide()
    {
        delivering = true;
        grabEnabled = false;
        if (grab != null)
        {
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            grab.enabled = false;
        }

        if (boxCollider != null)
            boxCollider.enabled = false;

        transform.SetParent(belt != null ? belt : null, true);
        if (rideRoutine != null)
            StopCoroutine(rideRoutine);
        rideRoutine = StartCoroutine(RideInward());
        host?.OnSortBoxAccepted(this);
    }

    IEnumerator RideInward()
    {
        // Slide along belt into the infeed / tunnel, then despawn.
        float travel = 1.6f;
        float speed = host != null && host.BeltFlowSpeed > 0.01f ? host.BeltFlowSpeed * 1.35f : 0.5f;
        Vector3 axis = belt != null ? belt.right * beltDirection : Vector3.right * beltDirection;
        float moved = 0f;
        while (moved < travel)
        {
            float step = speed * Time.deltaTime;
            transform.position += axis * step;
            moved += step;
            yield return null;
        }

        delivered = true;
        delivering = false;
        gameObject.SetActive(false);
        host?.OnSortBoxDelivered(this);
    }

    public void ReturnHome()
    {
        if (delivered)
            return;

        transform.SetParent(homeParent, true);
        transform.SetPositionAndRotation(homePos, homeRot);
        if (body != null)
        {
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    static Bounds GetWorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.25f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
