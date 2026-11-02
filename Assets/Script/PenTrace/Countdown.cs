using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class Countdown : MonoBehaviour
{
    public GameObject countdownScreen;
    public TMP_Text countdownText;
    public GameObject tracingArea;
    public GameObject objectiveScreen;
    public TracingTimer tracingTimer;

    public void StartCountdown()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        objectiveScreen.SetActive(false);

        PathManager paths = FindFirstObjectByType<PathManager>();
        if (paths != null)
            paths.ClearRound();

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        countdownScreen.SetActive(true);
        tracingArea.SetActive(false);

        countdownText.text = "3";
        LevelVoices.PlayCount(3);
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        LevelVoices.PlayCount(2);
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        LevelVoices.PlayCount(1);
        yield return new WaitForSeconds(1f);

        countdownScreen.SetActive(false);
        PlaceTracingArea();
        tracingArea.SetActive(true);
        tracingTimer.StartTimer();
    }

    void PlaceTracingArea()
    {
        if (tracingArea == null)
            return;

        if (!PlaceOnWhiteBoard())
            PlaceInFrontOfHeadset();

        Transform board = tracingArea.transform.Find("TracingBoard");
        if (board == null)
            return;

        TracingBoardSurface surface = board.GetComponent<TracingBoardSurface>();
        if (surface == null)
            surface = board.gameObject.AddComponent<TracingBoardSurface>();

        surface.reachDistance = 0.08f;
    }

    bool PlaceOnWhiteBoard()
    {
        Transform whiteBoard = PenTracePortal.WhiteBoardTransform();
        if (whiteBoard == null)
            return false;

        Vector3 normal = PenTracePortal.WhiteBoardNormal();
        if (normal.sqrMagnitude < 0.001f)
            return false;

        Vector3 center = PenTracePortal.WhiteBoardCenter();
        Vector3 position = center + normal * 0.02f;
        Vector3 forward = -normal;
        if (Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f)
            return false;

        tracingArea.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        return true;
    }

    void PlaceInFrontOfHeadset()
    {
        Camera headset = Camera.main;
        if (headset == null)
            return;

        Vector3 forward = headset.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = headset.transform.position + forward * 0.45f;
        position.y = headset.transform.position.y - 0.3f;
        tracingArea.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }
}