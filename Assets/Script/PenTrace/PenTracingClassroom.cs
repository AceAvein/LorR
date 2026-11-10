using TMPro;
using UnityEngine;

/// <summary>
/// Marker for storyboard Pen Tracing classroom layout:
/// pen select station + 3 desks (paper, bell, monitor).
/// Does not replace existing PathManager / pattern logic by itself.
/// </summary>
public class PenTracingClassroom : MonoBehaviour
{
    public Transform penSelectStation;
    public Transform[] desks = new Transform[3];
    public Transform[] bells = new Transform[3];
    public Transform[] tracingBoards = new Transform[3];
    public TMP_Text[] deskMonitors = new TMP_Text[3];
}
