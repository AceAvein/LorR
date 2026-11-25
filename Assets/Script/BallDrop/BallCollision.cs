using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BallCollision : MonoBehaviour
{
    XRGrabInteractable grab;
    bool counted;
    bool pendingReject;
    bool pendingDestroy;
    IXRSelectInteractor pendingInteractor;

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

        if (ballCollider != null)
        {
            grab.colliders.Clear();
            grab.colliders.Add(ballCollider);
        }

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

    void LateUpdate()
    {
        if (pendingInteractor != null && grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.SelectExit(pendingInteractor, grab);

        pendingInteractor = null;

        if (pendingReject)
        {
            pendingReject = false;
            BallFall fall = GetComponent<BallFall>();
            if (fall != null)
                fall.enabled = true;
        }

        if (pendingDestroy)
            Destroy(gameObject);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (GamePauseMenu.IsFrozen)
        {
            pendingReject = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        BallFall fall = GetComponent<BallFall>();
        if (fall != null)
            fall.enabled = false;

        if (counted || args.interactorObject == null)
            return;

        bool isLeftHand = IsLeftHand(args.interactorObject);
        DropBall identity = GetComponent<DropBall>();

        // Profile non-dominant hand catches every ball (no left/right ball assignment).
        bool correctHand = ProfileManager.MatchesNonDominantHand(isLeftHand);

        if (!correctHand)
        {
            if (BallDropRound.Instance != null)
                BallDropRound.Instance.WrongGrab(identity);

            DropBall.NotifyGrab(identity, false);

            pendingReject = true;
            pendingInteractor = args.interactorObject;
            return;
        }

        counted = true;
        DropBall.NotifyGrab(identity, true);

        if (BallDropRound.Instance != null)
            BallDropRound.Instance.Catch(identity);
        else
        {
            BallDropGameManager gameManager = FindFirstObjectByType<BallDropGameManager>();
            if (gameManager != null)
                gameManager.AddIntercepted();
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        pendingDestroy = true;
        pendingInteractor = args.interactorObject;
    }

    static bool IsLeftHand(IXRSelectInteractor interactor)
    {
        if (interactor == null)
            return true;

        if (interactor.handedness == InteractorHandedness.Left)
            return true;
        if (interactor.handedness == InteractorHandedness.Right)
            return false;

        Transform current = interactor.transform;
        while (current != null)
        {
            string name = current.name;
            if (name.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (name.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            current = current.parent;
        }

        return true;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (pendingDestroy)
            return;

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
