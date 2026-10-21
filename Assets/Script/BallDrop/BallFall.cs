using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BallFall : MonoBehaviour
{
    public float fallSpeed = 2f;
    public float reachBelowEye = 0.9f;
    public float sideReach = 1.8f;

    XRGrabInteractable grab;

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (grab == null)
            grab = GetComponent<XRGrabInteractable>();

        if (grab != null && grab.isSelected)
            return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        if (!OutOfReach())
            return;

        DropBall identity = GetComponent<DropBall>();
        DropBall.NotifyMiss(identity);

        BallDropRound round = BallDropRound.Instance;
        if (round != null && round.running)
            round.Miss(identity);

        if (identity == null || !identity.resolved)
            Destroy(gameObject);
    }

    bool OutOfReach()
    {
        if (transform.position.y <= 0.05f)
            return true;

        float eye = 1.6f;
        Camera view = Camera.main;
        if (view != null)
        {
            eye = view.transform.position.y;

            Vector3 ballFlat = transform.position;
            ballFlat.y = 0f;
            Vector3 playerFlat = view.transform.position;
            playerFlat.y = 0f;
            if (Vector3.Distance(ballFlat, playerFlat) > sideReach)
                return true;
        }

        float reach = eye - reachBelowEye;
        if (reach < 0.45f)
            reach = 0.45f;

        return transform.position.y <= reach;
    }
}
