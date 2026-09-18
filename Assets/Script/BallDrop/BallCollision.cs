using UnityEngine;

public class BallCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Transform hand = other.transform;

        bool isLeftHand =
            hand.CompareTag("LeftHand") ||
            (hand.parent != null && hand.parent.CompareTag("LeftHand"));

        bool isRightHand =
            hand.CompareTag("RightHand") ||
            (hand.parent != null && hand.parent.CompareTag("RightHand"));

        bool isRedBall = CompareTag("RedBall");
        bool isBlueBall = CompareTag("BlueBall");

        BallDropGameManager gameManager =
            FindFirstObjectByType<BallDropGameManager>();

        if (isRedBall && isLeftHand)
        {
            Debug.Log("NICE! Red ball + Left Hand");
            gameManager.AddIntercepted();
        }
        else if (isBlueBall && isRightHand)
        {
            Debug.Log("NICE! Blue ball + Right Hand");
            gameManager.AddIntercepted();
        }
        else if (isRedBall && isRightHand)
        {
            Debug.Log("WRONG HAND! Red ball + Right Hand");
        }
        else if (isBlueBall && isLeftHand)
        {
            Debug.Log("WRONG HAND! Blue ball + Left Hand");
        }

        Destroy(gameObject);
    }
}