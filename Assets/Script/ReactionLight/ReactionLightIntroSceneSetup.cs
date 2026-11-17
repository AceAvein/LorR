using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-authored cutscene controls for Reaction Light intro.
/// Edit Shot_BoardOverview in Scene View — full position + rotation used as-is.
/// </summary>
[DisallowMultipleComponent]
public class ReactionLightIntroSceneSetup : MonoBehaviour
{
    public enum ShotId
    {
        BoardOverview = 0
    }

    [Header("Cinema Shot Markers")]
    public Transform shotBoardOverview;

    [Header("Optional anchors")]
    public Transform boardFocus;
    public Transform riveraPresenterMark;

    [Header("Cutscene Dialogue UI")]
    public Canvas effectsCanvas;
    public RectTransform guidePanel;
    public Image guidePlate;
    public TMP_Text guideText;

    public bool TryGetShotPose(ShotId id, out Vector3 position, out Quaternion rotation)
    {
        Transform t = id == ShotId.BoardOverview ? shotBoardOverview : null;
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
        if (shotBoardOverview == null)
            return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f);
        Gizmos.DrawSphere(shotBoardOverview.position, 0.06f);
        Gizmos.DrawLine(shotBoardOverview.position, shotBoardOverview.position + shotBoardOverview.forward * 0.45f);
        UnityEditor.Handles.Label(shotBoardOverview.position + Vector3.up * 0.12f, "BoardOverview");
    }
#endif
}
