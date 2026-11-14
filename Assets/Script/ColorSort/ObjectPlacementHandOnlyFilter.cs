using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Hands only: blocks XRRayInteractor and NearFar far-casting (the visible "ray").
/// Allows XRDirectInteractor and NearFar near (controller within hand reach).
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementHandOnlyFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    const float DefaultHandReachMeters = 0.55f;

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

    /// <summary>True when this interactor is distant/ray-style (should be rejected).</summary>
    public static bool IsRay(IXRInteractor interactor)
    {
        return !AllowsHandGrab(interactor, null, DefaultHandReachMeters);
    }

    public static bool AllowsHandGrab(IXRInteractor interactor, IXRInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable, DefaultHandReachMeters);
    }

    public static bool AllowsHandGrab(IXRInteractor interactor, IXRInteractable interactable, float reachMeters)
    {
        if (interactor == null)
            return false;

        if (interactor is XRRayInteractor)
            return false;

        if (interactor is XRDirectInteractor)
            return true;

        if (interactor is NearFarInteractor nearFar)
        {
            if (nearFar.selectionRegion.Value == NearFarInteractor.Region.Far)
                return false;

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

        return dist <= Mathf.Max(0.2f, reachMeters);
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
