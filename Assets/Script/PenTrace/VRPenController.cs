using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRPenController : MonoBehaviour
{
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    public Transform penTip;
    public Transform tracingSurface;
}
