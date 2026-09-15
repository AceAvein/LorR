using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRPenController : MonoBehaviour
{
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    public Transform penTip;
    public Transform tracingSurface;

    void Update()
    {
        if (penTip == null || tracingSurface == null)
            return;

        if (TryGetHit(rightRay, out Vector3 rightHit))
        {
            penTip.position = rightHit;
            return;
        }

        if (TryGetHit(leftRay, out Vector3 leftHit))
        {
            penTip.position = leftHit;
        }
    }

    bool TryGetHit(XRRayInteractor rayInteractor, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        if (rayInteractor == null)
            return false;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (hit.collider.transform == tracingSurface ||
                hit.collider.transform.IsChildOf(tracingSurface))
            {
                hitPoint = hit.point;

                Debug.Log("VR HIT: " + hitPoint);

                return true;
            }
        }

        return false;
    }
}