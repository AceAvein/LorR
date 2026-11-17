using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-authored cutscene controls for Color Sort intro.
/// Place shot empties in the Scene View and edit GuidePanel RectTransform for dialogue position.
/// ObjectPlacementIntroDirector finds this at runtime and prefers these over computed offsets.
/// </summary>
[DisallowMultipleComponent]
public class ObjectPlacementIntroSceneSetup : MonoBehaviour
{
    public enum ShotId
    {
        Wide = 0,
        BoxCloseUp = 1,
        Conveyor = 2,
        ItemPickup = 3,
        ItemPlace = 4,
        FinalBoxes = 5,
        FinalWide = 6
    }

    [Header("Cinema Shot Markers (move/rotate in Scene View)")]
    public Transform shotWide;
    public Transform shotBoxCloseUp;
    public Transform shotConveyor;
    public Transform shotItemPickup;
    public Transform shotItemPlace;
    public Transform shotFinalBoxes;
    public Transform shotFinalWide;

    [Header("Cutscene Dialogue UI (edit GuidePanel anchors in Scene/Game View)")]
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
            case ShotId.Wide: return shotWide;
            case ShotId.BoxCloseUp: return shotBoxCloseUp;
            case ShotId.Conveyor: return shotConveyor;
            case ShotId.ItemPickup: return shotItemPickup;
            case ShotId.ItemPlace: return shotItemPlace;
            case ShotId.FinalBoxes: return shotFinalBoxes;
            case ShotId.FinalWide: return shotFinalWide;
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
        DrawShotGizmo(shotWide, Color.cyan, "Wide");
        DrawShotGizmo(shotBoxCloseUp, Color.yellow, "Boxes");
        DrawShotGizmo(shotConveyor, new Color(1f, 0.55f, 0.1f), "Conveyor");
        DrawShotGizmo(shotItemPickup, Color.green, "Pickup");
        DrawShotGizmo(shotItemPlace, new Color(0.4f, 1f, 0.4f), "Place");
        DrawShotGizmo(shotFinalBoxes, Color.magenta, "FinalBoxes");
        DrawShotGizmo(shotFinalWide, Color.white, "FinalWide");
    }

    void DrawShotGizmo(Transform shot, Color color, string label)
    {
        if (shot == null)
            return;

        Gizmos.color = color;
        Gizmos.DrawSphere(shot.position, 0.06f);
        Gizmos.DrawLine(shot.position, shot.position + shot.forward * 0.45f);

        // Simple frustum preview.
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
