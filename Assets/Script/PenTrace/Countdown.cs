using UnityEngine;
using TMPro;
using System.Collections;

public class Countdown : MonoBehaviour
{
    public GameObject countdownScreen;
    public TMP_Text countdownText;
    public GameObject tracingArea;
    public GameObject objectiveScreen;
    public TracingTimer tracingTimer;

    public void StartCountdown()
    {
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
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
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

        Transform board = tracingArea.transform.Find("TracingBoard");
        if (board != null && board.GetComponent<TracingBoardSurface>() == null)
            board.gameObject.AddComponent<TracingBoardSurface>();
    }
}