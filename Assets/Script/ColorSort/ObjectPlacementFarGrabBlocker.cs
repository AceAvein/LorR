using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// On Color Sort load: register filters on every NearFar / Ray interactor so
/// far-ray cannot grab grocery items or sort crates (hands / near only).
/// </summary>
public static class ObjectPlacementFarGrabBlocker
{
    static bool installed;
    static readonly List<NearFarInteractor> nearFars = new List<NearFarInteractor>();
    static readonly List<XRRayInteractor> rays = new List<XRRayInteractor>();
    static readonly List<XRSelectFilterDelegate> selectDelegates = new List<XRSelectFilterDelegate>();
    static readonly List<XRHoverFilterDelegate> hoverDelegates = new List<XRHoverFilterDelegate>();

    public static void Install()
    {
        if (installed)
            return;
        installed = true;

        NearFarInteractor[] nfs = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < nfs.Length; i++)
            WireNearFar(nfs[i]);

        XRRayInteractor[] rs = Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < rs.Length; i++)
            WireRay(rs[i]);
    }

    static void WireNearFar(NearFarInteractor nf)
    {
        if (nf == null)
            return;

        nearFars.Add(nf);

        XRSelectFilterDelegate select = new XRSelectFilterDelegate((interactor, interactable) =>
            AllowSelect(interactor, interactable));
        XRHoverFilterDelegate hover = new XRHoverFilterDelegate((interactor, interactable) =>
            AllowHover(interactor, interactable));

        nf.selectFilters.Add(select);
        nf.hoverFilters.Add(hover);
        selectDelegates.Add(select);
        hoverDelegates.Add(hover);
    }

    static void WireRay(XRRayInteractor ray)
    {
        if (ray == null)
            return;

        // Teleport rays keep working; only block grab targets.
        rays.Add(ray);

        XRSelectFilterDelegate select = new XRSelectFilterDelegate((interactor, interactable) =>
            !IsOurGrabTarget(interactable));
        XRHoverFilterDelegate hover = new XRHoverFilterDelegate((interactor, interactable) =>
            !IsOurGrabTarget(interactable));

        ray.selectFilters.Add(select);
        ray.hoverFilters.Add(hover);
        selectDelegates.Add(select);
        hoverDelegates.Add(hover);
    }

    static bool AllowSelect(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (!IsOurGrabTarget(interactable))
            return true;
        return ObjectPlacementHandOnlyFilter.AllowsHandGrab(interactor, interactable);
    }

    static bool AllowHover(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        if (!IsOurGrabTarget(interactable))
            return true;
        return ObjectPlacementHandOnlyFilter.AllowsHandGrab(interactor, interactable as IXRInteractable);
    }

    static bool IsOurGrabTarget(IXRInteractable interactable)
    {
        Component c = interactable as Component;
        if (c == null)
            return false;
        return c.GetComponent<ObjectPlacementGroceryItem>() != null
            || c.GetComponent<ObjectPlacementSortBox>() != null;
    }
}
