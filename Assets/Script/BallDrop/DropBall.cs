using System;
using UnityEngine;

public class DropBall : MonoBehaviour
{
    public static event Action<DropBall, bool> Grabbed;
    public static event Action<DropBall> Missed;

    public bool forLeftHand;
    public int tier;
    public float spawnedAt;
    public int wrongAttempts;
    public bool resolved;

    public static void NotifyGrab(DropBall ball, bool correct)
    {
        Grabbed?.Invoke(ball, correct);
    }

    public static void NotifyMiss(DropBall ball)
    {
        Missed?.Invoke(ball);
    }
}
