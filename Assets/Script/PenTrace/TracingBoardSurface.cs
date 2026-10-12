using UnityEngine;

public class TracingBoardSurface : MonoBehaviour
{
    public float reachDistance = 0.04f;

    public static TracingBoardSurface Current { get; private set; }

    void OnEnable()
    {
        Current = this;
    }

    void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    public bool TryProject(Vector3 worldPoint, out Vector3 onBoard)
    {
        Vector3 normal = transform.up;
        float signedDistance = Vector3.Dot(worldPoint - transform.position, normal);
        onBoard = worldPoint - normal * signedDistance;
        return Mathf.Abs(signedDistance) <= reachDistance;
    }
}
