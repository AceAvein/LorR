using UnityEngine;

public static class PenTraceBoards
{
    public const int SetCount = 3;
    public const int PatternsPerSet = 3;

    static readonly string[] SetNames =
    {
        "TracingBoard_Set1",
        "TracingBoard_Set2",
        "TracingBoard_Set3"
    };

    public static Transform FindSet(int setIndex)
    {
        if (setIndex < 0 || setIndex >= SetNames.Length)
            return null;

        return FindNamed(SetNames[setIndex]);
    }

    public static Vector3 BoardCenter(Transform board)
    {
        if (board == null)
            return Vector3.zero;

        Renderer renderer = board.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds.center;

        return board.position;
    }

    public static Vector3 BoardNormal(Transform board)
    {
        if (board == null)
            return Vector3.up;

        Vector3 normal = board.up;
        if (normal.sqrMagnitude < 0.0001f)
            return Vector3.up;

        if (Vector3.Dot(normal, Vector3.up) < 0f)
            normal = -normal;

        return normal.normalized;
    }

    // Stable desk-facing axis so every set orients the same, independent of headset position.
    public static Vector3 BoardFacing(Transform board)
    {
        if (board == null)
            return Vector3.forward;

        Transform desk = board.parent != null ? board.parent : board;
        Vector3 facing = Vector3.ProjectOnPlane(desk.forward, Vector3.up);
        if (facing.sqrMagnitude < 0.001f)
            facing = Vector3.ProjectOnPlane(board.forward, Vector3.up);
        if (facing.sqrMagnitude < 0.001f)
            facing = Vector3.ProjectOnPlane(board.right, Vector3.up);
        if (facing.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return facing.normalized;
    }

    static Transform FindNamed(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }
}
