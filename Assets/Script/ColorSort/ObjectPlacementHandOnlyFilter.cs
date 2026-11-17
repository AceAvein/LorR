using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Hands only: blocks XRRayInteractor and NearFar far-casting.
/// Allows XRDirectInteractor and NearFar near (controller within hand reach).
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementHandOnlyFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    const float DefaultHandReachMeters = 0.45f;

    float handReachMeters = DefaultHandReachMeters;

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable as IXRInteractable, handReachMeters);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable as IXRInteractable, handReachMeters);
    }

    public static bool IsRay(IXRInteractor interactor)
    {
        if (interactor == null)
            return true;
        if (interactor is XRRayInteractor)
            return true;
        if (interactor is NearFarInteractor nf && nf.selectionRegion.Value == NearFarInteractor.Region.Far)
            return true;
        return false;
    }

    public static bool AllowsHandGrab(IXRInteractor interactor, IXRInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable, DefaultHandReachMeters);
    }

    public static bool AllowsHandGrab(IXRInteractor interactor, IXRInteractable interactable, float reachMeters)
    {
        if (interactor == null)
            return false;

        // No ray interactor may grab Object Placement cargo.
        if (interactor is XRRayInteractor)
            return false;

        if (interactor is XRDirectInteractor)
            return IsWithinHandReach(interactor, interactable, reachMeters);

        if (interactor is NearFarInteractor nearFar)
        {
            if (nearFar.selectionRegion.Value == NearFarInteractor.Region.Far)
                return false;
            // Near casting only — must be in hand reach.
            return IsWithinHandReach(interactor, interactable, reachMeters);
        }

        return false;
    }

    static bool IsWithinHandReach(IXRInteractor interactor, IXRInteractable interactable, float reachMeters)
    {
        Transform item = null;
        if (interactable is Component c)
            item = c.transform;
        if (item == null)
            return false;

        Vector3 hand = interactor.transform.position;
        float dist = Vector3.Distance(hand, item.position);

        Collider[] cols = item.GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null || !cols[i].enabled)
                continue;
            float d = Vector3.Distance(hand, cols[i].ClosestPoint(hand));
            if (d < dist)
                dist = d;
        }

        return dist <= Mathf.Max(0.15f, reachMeters);
    }

    public static bool IsOurGrabTarget(IXRInteractable interactable)
    {
        float reach;
        return IsOurGrabTarget(interactable, out reach);
    }

    public static bool IsOurGrabTarget(IXRInteractable interactable, out float reachMeters)
    {
        reachMeters = DefaultHandReachMeters;
        Component c = interactable as Component;
        if (c == null)
            return false;

        if (c.GetComponent<ObjectPlacementSortBox>() != null)
        {
            reachMeters = 1.25f;
            return true;
        }

        if (c.GetComponent<ObjectPlacementGroceryItem>() != null)
        {
            reachMeters = DefaultHandReachMeters;
            return true;
        }

        return false;
    }

    public static void AttachTo(XRGrabInteractable grab)
    {
        AttachTo(grab, DefaultHandReachMeters);
    }

    public static void AttachTo(XRGrabInteractable grab, float reachMeters)
    {
        if (grab == null)
            return;

        ObjectPlacementHandOnlyFilter filter = grab.GetComponent<ObjectPlacementHandOnlyFilter>();
        if (filter == null)
            filter = grab.gameObject.AddComponent<ObjectPlacementHandOnlyFilter>();

        filter.handReachMeters = reachMeters;

        grab.hoverFilters.Remove(filter);
        grab.hoverFilters.Add(filter);
        grab.selectFilters.Remove(filter);
        grab.selectFilters.Add(filter);
    }
}
