using UnityEngine;
using UnityEngine.InputSystem;

public class PenTrail : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer trail;

    public InputActionReference indexButton;
    public InputActionReference rightIndexButton;

    InputAction drawAction;
    bool useRightHand;
    private bool wasPressed = false;

    void Start()
    {
        trail.positionCount = 0;
        BindDrawTrigger();
    }

    public void UseRightHand(bool rightHand)
    {
        useRightHand = rightHand;
        BindDrawTrigger();
    }

    void BindDrawTrigger()
    {
        drawAction?.Dispose();

        string hand = useRightHand ? "{RightHand}" : "{LeftHand}";
        drawAction = new InputAction(type: InputActionType.Button, binding: "<XRController>" + hand + "/{TriggerButton}");
        drawAction.Enable();
    }

    void OnDestroy()
    {
        drawAction?.Dispose();
    }

    void Update()
    {
        if (drawAction == null)
            return;

        bool isPressed = drawAction.IsPressed();

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