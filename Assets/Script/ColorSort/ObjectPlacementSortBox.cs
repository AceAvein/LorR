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
    Collider scoreCollider;
    BoxCollider grabCollider;
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
        scoreCollider = GetComponent<Collider>();
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

        // Keep score trigger for Phase I sorting; use a separate solid grab volume for hands.
        if (scoreCollider != null && scoreCollider != grabCollider)
            scoreCollider.isTrigger = true;

        EnsureGrabCollider();

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
        grab.colliders.Clear();
        if (grabCollider != null)
            grab.colliders.Add(grabCollider);

        // Looser hand-reach than groceries — crates are large.
        ObjectPlacementHandOnlyFilter.AttachTo(grab, 1.25f);

        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
        grab.enabled = forUse;

        if (grabCollider != null)
            grabCollider.enabled = forUse;
    }

    void EnsureGrabCollider()
    {
        if (grabCollider == null)
        {
            // Dedicated volume so we don't fight the score trigger collider.
            GameObject volume = transform.Find("GrabVolume") != null
                ? transform.Find("GrabVolume").gameObject
                : new GameObject("GrabVolume");
            volume.transform.SetParent(transform, false);
            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
            volume.transform.localScale = Vector3.one;
            grabCollider = volume.GetComponent<BoxCollider>();
            if (grabCollider == null)
                grabCollider = volume.AddComponent<BoxCollider>();
        }

        Bounds b = GetLocalRendererBounds();
        grabCollider.isTrigger = false;
        grabCollider.center = b.center;
        grabCollider.size = Vector3.Max(b.size, new Vector3(0.28f, 0.22f, 0.28f)) * 1.05f;
    }

    Bounds GetLocalRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(Vector3.up * 0.12f, new Vector3(0.35f, 0.28f, 0.35f));

        Bounds world = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            world.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = transform.InverseTransformPoint(world.center);
        Vector3 lossy = transform.lossyScale;
        Vector3 localSize = new Vector3(
            world.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            world.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            world.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
        return new Bounds(localCenter, localSize);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!grabEnabled || delivering || delivered)
        {
            RejectSelect(args.interactorObject);
            return;
        }

        if (!ObjectPlacementHandOnlyFilter.AllowsHandGrab(args.interactorObject, grab, 1.25f))
        {
            RejectSelect(args.interactorObject);
            return;
        }

        // Stay planted until both hands are on it.
        if (grab != null && grab.interactorsSelecting.Count < 2)
            transform.SetPositionAndRotation(homePos, homeRot);
    }

    void RejectSelect(IXRSelectInteractor interactor)
    {
        if (grab != null && grab.interactionManager != null && interactor != null)
            grab.interactionManager.SelectExit(interactor, grab);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (delivering || delivered)
            return;

        if (grab != null && grab.interactorsSelecting.Count == 0)
            TryAcceptDrop();
        else if (grab != null && grab.interactorsSelecting.Count == 1)
            transform.SetPositionAndRotation(homePos, homeRot);
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
        // Do not snap every frame with 1 hand — that cancels XR selection.
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
            b.Expand(0.25f);
            return b.Intersects(item);
        }

        Vector3 local = belt.InverseTransformPoint(transform.position);
        float halfX = Mathf.Max(0.25f, Mathf.Abs(belt.lossyScale.x) * 0.55f);
        float halfZ = Mathf.Max(0.18f, Mathf.Abs(belt.lossyScale.z) * 0.55f);
        return Mathf.Abs(local.x) <= halfX && Mathf.Abs(local.z) <= halfZ && local.y > -0.05f && local.y < 0.5f;
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

        if (grabCollider != null)
            grabCollider.enabled = false;

        transform.SetParent(belt != null ? belt : null, true);
        if (rideRoutine != null)
            StopCoroutine(rideRoutine);
        rideRoutine = StartCoroutine(RideInward());
        host?.OnSortBoxAccepted(this);
    }

    IEnumerator RideInward()
    {
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
