using UnityEngine;
using UnityEngine.InputSystem;

public class PenTrail : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer trail;

    public InputActionReference indexButton;

    private bool wasPressed = false;

    void Start()
    {
        trail.positionCount = 0;
    }

    void Update()
    {
        bool isPressed = indexButton.action.IsPressed();

        // Start a new stroke
        if (isPressed && !wasPressed)
        {
            trail.positionCount = 0;
        }

        // Draw while holding
        if (isPressed)
        {
            AddTrailPoint();
        }

        wasPressed = isPressed;
    }

    void AddTrailPoint()
    {
        trail.positionCount++;

        trail.SetPosition(
            trail.positionCount - 1,
            penTip.position
        );
    }
}