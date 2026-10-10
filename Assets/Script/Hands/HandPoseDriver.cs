using UnityEngine;
using UnityEngine.InputSystem;

public class HandPoseDriver : MonoBehaviour
{
    InputAction grip;
    InputAction trigger;
    Animator animator;

    public void Configure(bool leftHand)
    {
        animator = GetComponent<Animator>();

        grip?.Dispose();
        trigger?.Dispose();

        string hand = leftHand ? "{LeftHand}" : "{RightHand}";
        grip = new InputAction(type: InputActionType.Value, binding: $"<XRController>{hand}/grip");
        trigger = new InputAction(type: InputActionType.Value, binding: $"<XRController>{hand}/trigger");
        grip.Enable();
        trigger.Enable();
    }

    void OnDestroy()
    {
        grip?.Dispose();
        trigger?.Dispose();
    }

    void Update()
    {
        if (animator == null)
            return;

        animator.SetFloat("Grip", grip != null ? grip.ReadValue<float>() : 0f);
        animator.SetFloat("Trigger", trigger != null ? trigger.ReadValue<float>() : 0f);
    }
}
