using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BallFall : MonoBehaviour
{
    public float fallSpeed = 2f;

    XRGrabInteractable grab;

    void Update()
    {
        if (grab == null)
            grab = GetComponent<XRGrabInteractable>();

        if (grab != null && grab.isSelected)
            return;

        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }
}