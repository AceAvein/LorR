using UnityEngine;

public class TracingBoardSurface : MonoBehaviour
{
    public float reachDistance = 0.02f;
    public float surfaceLift = 0.002f;
    public float edgeMargin = 0.01f;

    Transform paperBounds;

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

    public void SetPaperBounds(Transform paper)
    {
        paperBounds = paper;
    }

    public Vector3 Project(Vector3 worldPoint)
    {
        Vector3 normal = transform.up;
        float signedDistance = Vector3.Dot(worldPoint - transform.position, normal);
        return worldPoint - normal * signedDistance + normal * surfaceLift;
    }

    public bool TryProject(Vector3 worldPoint, out Vector3 onBoard)
    {
        Vector3 normal = transform.up;
        float signedDistance = Vector3.Dot(worldPoint - transform.position, normal);
        onBoard = worldPoint - normal * signedDistance + normal * surfaceLift;

        if (Mathf.Abs(signedDistance) > reachDistance)
            return false;

        return IsInsidePaper(onBoard);
    }

    bool IsInsidePaper(Vector3 worldPoint)
    {
        Transform paper = paperBounds;
        if (paper == null)
        {
            PatternManager patterns = FindFirstObjectByType<PatternManager>(FindObjectsInactive.Include);
            int setIndex = patterns != null ? patterns.CurrentSetIndex : 0;
            paper = PenTraceBoards.FindSet(setIndex);
            paperBounds = paper;
        }

        if (paper == null)
            return true;

        // TracingBoard_Set uses a unit cube mesh scaled on X/Z.
        Vector3 local = paper.InverseTransformPoint(worldPoint);
        float limit = 0.5f + edgeMargin;
        return Mathf.Abs(local.x) <= limit && Mathf.Abs(local.z) <= limit;
    }
}
