using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-authored cutscene controls for Pen Control Tracing intro.
/// Edit Shot_TableOverview in Scene View — full position + rotation used as-is.
/// </summary>
[DisallowMultipleComponent]
public class PenTraceIntroSceneSetup : MonoBehaviour
{
    public enum ShotId
    {
        TableOverview = 0,
        PenSelect = 1
    }

    [Header("Cinema Shot Markers")]
    public Transform shotTableOverview;
    public Transform shotPenSelect;

    [Header("Optional anchors")]
    public Transform tableFocus;
    public Transform penSelectFocus;
    public Transform riveraPresenterMark;

    [Header("Cutscene Dialogue UI")]
    public Canvas effectsCanvas;
    public RectTransform guidePanel;
    public Image guidePlate;
    public TMP_Text guideText;

    public bool TryGetShotPose(ShotId id, out Vector3 position, out Quaternion rotation)
    {
        Transform t = id == ShotId.TableOverview ? shotTableOverview
            : id == ShotId.PenSelect ? shotPenSelect
            : null;
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
        DrawShot(shotTableOverview, new Color(0.2f, 1f, 0.45f), "TableOverview");
        DrawShot(shotPenSelect, new Color(1f, 0.75f, 0.2f), "PenSelect");
    }

    static void DrawShot(Transform shot, Color color, string label)
    {
        if (shot == null)
            return;
        Gizmos.color = color;
        Gizmos.DrawSphere(shot.position, 0.06f);
        Gizmos.DrawLine(shot.position, shot.position + shot.forward * 0.45f);
        UnityEditor.Handles.Label(shot.position + Vector3.up * 0.12f, label);
    }
#endif
}
