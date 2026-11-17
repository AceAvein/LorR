using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-authored cutscene controls for Ball Drop intro (mirrors Color Sort IntroCutsceneControls).
/// Move/rotate shot empties in the Scene View; director prefers these over computed offsets.
/// </summary>
[DisallowMultipleComponent]
public class BallDropIntroSceneSetup : MonoBehaviour
{
    public enum ShotId
    {
        PoleOverview = 0,
        LeftCatch = 1,
        RightCatch = 2,
        Miss = 3
    }

    [Header("Cinema Shot Markers (move/rotate in Scene View)")]
    public Transform shotPoleOverview;
    public Transform shotLeftCatch;
    public Transform shotRightCatch;
    public Transform shotMiss;

    [Header("Demo Anchors (optional — auto-found from PoleScreen if empty)")]
    public Transform poleScreen;
    public Transform leftCatchAnchor;
    public Transform rightCatchAnchor;
    public Transform ballSpawnLeft;
    public Transform ballSpawnRight;
    public Transform missEnd;

    [Header("Cutscene Dialogue UI")]
    public Canvas effectsCanvas;
    public RectTransform guidePanel;
    public Image guidePlate;
    public TMP_Text guideText;

    [Header("Preview")]
    [SerializeField] float gizmoFov = 55f;
    [SerializeField] float gizmoAspect = 1.777f;
    [SerializeField] float gizmoDrawDistance = 0.55f;

    public Transform GetShot(ShotId id)
    {
        switch (id)
        {
            case ShotId.PoleOverview: return shotPoleOverview;
            case ShotId.LeftCatch: return shotLeftCatch;
            case ShotId.RightCatch: return shotRightCatch;
            case ShotId.Miss: return shotMiss;
            default: return null;
        }
    }

    public bool TryGetShotPose(ShotId id, out Vector3 position, out Quaternion rotation)
    {
        Transform t = GetShot(id);
        if (t == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = t.position;
        rotation = t.rotation;
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawShotGizmo(shotPoleOverview, new Color(1f, 0.55f, 0.1f), "Pole");
        DrawShotGizmo(shotLeftCatch, Color.cyan, "Left");
        DrawShotGizmo(shotRightCatch, Color.yellow, "Right");
        DrawShotGizmo(shotMiss, Color.magenta, "Miss");
    }

    void DrawShotGizmo(Transform shot, Color color, string label)
    {
        if (shot == null)
            return;

        Gizmos.color = color;
        Gizmos.DrawSphere(shot.position, 0.06f);
        Gizmos.DrawLine(shot.position, shot.position + shot.forward * 0.45f);

        float dist = Mathf.Max(0.2f, gizmoDrawDistance);
        float halfH = Mathf.Tan(gizmoFov * 0.5f * Mathf.Deg2Rad) * dist;
        float halfW = halfH * Mathf.Max(0.5f, gizmoAspect);
        Vector3 c = shot.position + shot.forward * dist;
        Vector3 r = shot.right * halfW;
        Vector3 u = shot.up * halfH;
        Vector3 tl = c - r + u;
        Vector3 tr = c + r + u;
        Vector3 bl = c - r - u;
        Vector3 br = c + r - u;
        Gizmos.DrawLine(shot.position, tl);
        Gizmos.DrawLine(shot.position, tr);
        Gizmos.DrawLine(shot.position, bl);
        Gizmos.DrawLine(shot.position, br);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);

        UnityEditor.Handles.Label(shot.position + Vector3.up * 0.12f, label);
    }
#endif
}
