using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Hard-disables ray / far-cast interaction with Object Placement grab targets.
/// Near-hand (NearFar near casting / Direct) still works.
/// </summary>
public static class ObjectPlacementFarGrabBlocker
{
    static bool installed;
    static readonly List<NearFarBackup> nearFarBackups = new List<NearFarBackup>();
    static readonly List<XRSelectFilterDelegate> selectDelegates = new List<XRSelectFilterDelegate>();
    static readonly List<XRHoverFilterDelegate> hoverDelegates = new List<XRHoverFilterDelegate>();

    struct NearFarBackup
    {
        public NearFarInteractor nearFar;
        public bool enableFarCasting;
        public float castDistance;
        public CurveInteractionCaster farCaster;
        public bool curveEnabled;
        public CurveVisualController curve;
    }

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

        NearFarBackup backup = new NearFarBackup();
        backup.nearFar = nf;
        backup.enableFarCasting = nf.enableFarCasting;
        // Kill far-ray grab for this module. Near casting still reaches items by hand.
        nf.enableFarCasting = false;

        CurveInteractionCaster far = nf.farInteractionCaster as CurveInteractionCaster;
        if (far != null)
        {
            backup.farCaster = far;
            backup.castDistance = far.castDistance;
            far.castDistance = 0.01f;
        }

        CurveVisualController curve = nf.GetComponentInChildren<CurveVisualController>(true);
        if (curve != null)
        {
            backup.curve = curve;
            backup.curveEnabled = curve.enabled;
            // Keep component alive; shorten so the far beam is not usable on items.
            curve.restingVisualLineLength = Mathf.Min(curve.restingVisualLineLength, 0.05f);
            curve.maxVisualCurveDistance = Mathf.Min(curve.maxVisualCurveDistance, 0.05f);
        }

        nearFarBackups.Add(backup);

        XRSelectFilterDelegate select = new XRSelectFilterDelegate((interactor, interactable) =>
            AllowNearFar(interactor, interactable));
        XRHoverFilterDelegate hover = new XRHoverFilterDelegate((interactor, interactable) =>
            AllowNearFarHover(interactor, interactable));

        nf.selectFilters.Add(select);
        nf.hoverFilters.Add(hover);
        selectDelegates.Add(select);
        hoverDelegates.Add(hover);
    }

    static void WireRay(XRRayInteractor ray)
    {
        if (ray == null)
            return;

        // Teleport rays stay; no Object Placement item may be hovered/selected by any ray.
        XRSelectFilterDelegate select = new XRSelectFilterDelegate((interactor, interactable) =>
            !ObjectPlacementHandOnlyFilter.IsOurGrabTarget(interactable));
        XRHoverFilterDelegate hover = new XRHoverFilterDelegate((interactor, interactable) =>
            !ObjectPlacementHandOnlyFilter.IsOurGrabTarget(interactable));

        ray.selectFilters.Add(select);
        ray.hoverFilters.Add(hover);
        selectDelegates.Add(select);
        hoverDelegates.Add(hover);
    }

    static bool AllowNearFar(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        float reach;
        if (!ObjectPlacementHandOnlyFilter.IsOurGrabTarget(interactable, out reach))
            return true;

        // Far region / ray path — never.
        if (interactor is NearFarInteractor nf && nf.selectionRegion.Value == NearFarInteractor.Region.Far)
            return false;

        return ObjectPlacementHandOnlyFilter.AllowsHandGrab(interactor, interactable, reach);
    }

    static bool AllowNearFarHover(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        float reach;
        if (!ObjectPlacementHandOnlyFilter.IsOurGrabTarget(interactable, out reach))
            return true;

        if (interactor is NearFarInteractor nf && nf.selectionRegion.Value == NearFarInteractor.Region.Far)
            return false;

        return ObjectPlacementHandOnlyFilter.AllowsHandGrab(interactor, interactable as IXRInteractable, reach);
    }
}
