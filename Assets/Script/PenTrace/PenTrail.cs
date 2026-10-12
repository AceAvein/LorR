using UnityEngine;
using UnityEngine.InputSystem;

public class PenTrail : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer trail;

    public InputActionReference indexButton;
    public InputActionReference rightIndexButton;

    InputActionReference activeButton;
    private bool wasPressed = false;

    void Awake()
    {
        activeButton = indexButton;
    }

    void Start()
    {
        trail.positionCount = 0;
    }

    public void UseRightHand(bool rightHand)
    {
        activeButton = rightHand && rightIndexButton != null ? rightIndexButton : indexButton;
        if (activeButton != null && activeButton.action != null)
            activeButton.action.Enable();
    }

    void Update()
    {
        if (activeButton == null || activeButton.action == null)
            return;

        bool isPressed = activeButton.action.IsPressed();

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