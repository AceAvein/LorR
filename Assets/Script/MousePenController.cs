using UnityEngine;
using UnityEngine.InputSystem;

public class MousePenController : MonoBehaviour
{
    public Transform penTip;
    public Transform tracingSurface;
    public float hoverOffset = -0.03f;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null || penTip == null || tracingSurface == null) return;
        if (Mouse.current == null) return;

        Plane panelPlane = new Plane(-tracingSurface.forward, tracingSurface.position);

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mouseScreenPos);

        if (panelPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            penTip.position = hitPoint + tracingSurface.forward * hoverOffset;
        }
    }
}