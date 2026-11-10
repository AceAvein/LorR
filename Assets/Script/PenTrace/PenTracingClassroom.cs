using TMPro;
using UnityEngine;

/// <summary>
/// Marker for Pen Tracing classroom layout:
/// pen select station + tracing desk (paper, bell, monitor).
/// Does not replace existing PathManager / pattern logic by itself.
/// </summary>
public class PenTracingClassroom : MonoBehaviour
{
    public Transform penSelectStation;
    public Transform[] desks = new Transform[1];
    public Transform[] bells = new Transform[2];
    public Transform[] tracingBoards = new Transform[1];
    public TMP_Text[] deskMonitors = new TMP_Text[1];
}
