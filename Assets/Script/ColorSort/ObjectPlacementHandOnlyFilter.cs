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
    const float HandReachMeters = 0.55f;

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable as IXRInteractable);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return AllowsHandGrab(interactor, interactable as IXRInteractable);
    }

    /// <summary>True when this interactor is distant/ray-style (should be rejected).</summary>
    public static bool IsRay(IXRInteractor interactor)
    {
        return !AllowsHandGrab(interactor, null);
    }

    public static bool AllowsHandGrab(IXRInteractor interactor, IXRInteractable interactable)
    {
        if (interactor == null)
            return false;

        // Classic teleport/manipulation ray — never for groceries/crates.
        if (interactor is XRRayInteractor)
            return false;

        if (interactor is XRDirectInteractor)
            return true;

        if (interactor is NearFarInteractor nearFar)
        {
            // Already locked onto a far-ray target — reject.
            if (nearFar.selectionRegion.Value == NearFarInteractor.Region.Far)
                return false;

            // NearFar is both near-hand and far-ray. Gate by reach so far cast cannot grab.
            return IsWithinHandReach(interactor, interactable);
        }

        // Poke / other: not a grab hand for these items.
        return false;
    }

    static bool IsWithinHandReach(IXRInteractor interactor, IXRInteractable interactable)
    {
        Transform item = null;
        if (interactable is Component c)
            item = c.transform;
        if (item == null)
            return false;

        Vector3 hand = interactor.transform.position;
        float dist = Vector3.Distance(hand, item.position);

        Collider col = item.GetComponent<Collider>();
        if (col != null)
            dist = Vector3.Distance(hand, col.ClosestPoint(hand));

        return dist <= HandReachMeters;
    }

    public static void AttachTo(XRGrabInteractable grab)
    {
        if (grab == null)
            return;

        ObjectPlacementHandOnlyFilter filter = grab.GetComponent<ObjectPlacementHandOnlyFilter>();
        if (filter == null)
            filter = grab.gameObject.AddComponent<ObjectPlacementHandOnlyFilter>();

        // IXRFilterList has no Contains — Remove then Add keeps a single registration.
        grab.hoverFilters.Remove(filter);
        grab.hoverFilters.Add(filter);
        grab.selectFilters.Remove(filter);
        grab.selectFilters.Add(filter);
    }
}
