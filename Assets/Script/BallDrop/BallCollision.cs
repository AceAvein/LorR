using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BallCollision : MonoBehaviour
{
    XRGrabInteractable grab;
    bool counted;

    void Awake()
    {
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
            ballCollider.isTrigger = false;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;

        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = gameObject.AddComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.useDynamicAttach = true;
        grab.throwOnDetach = false;
        grab.attachEaseInTime = 0f;

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grab == null)
            return;

        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        BallFall fall = GetComponent<BallFall>();
        if (fall != null)
            fall.enabled = false;

        if (counted || args.interactorObject == null)
            return;

        bool isLeftHand = args.interactorObject.handedness == InteractorHandedness.Left;
        bool isRightHand = args.interactorObject.handedness == InteractorHandedness.Right;
        bool isRedBall = CompareTag("RedBall");
        bool isBlueBall = CompareTag("BlueBall");

        if ((isRedBall && isLeftHand) || (isBlueBall && isRightHand))
        {
            counted = true;

            BallDropGameManager gameManager = FindFirstObjectByType<BallDropGameManager>();
            if (gameManager != null)
                gameManager.AddIntercepted();
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        BallFall fall = GetComponent<BallFall>();
        if (fall != null)
            fall.enabled = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
