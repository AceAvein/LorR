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

    const float PaperLift = 0.003f;

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

    public void PlaceTracingArea()
    {
        if (tracingArea == null)
            return;

        int setIndex = 0;
        PatternManager patterns = FindFirstObjectByType<PatternManager>(FindObjectsInactive.Include);
        if (patterns != null)
            setIndex = patterns.CurrentSetIndex;

        if (!PlaceOnTracingBoard(setIndex))
            PlaceInFrontOfHeadset();

        Transform board = tracingArea.transform.Find("TracingBoard");
        if (board == null)
            return;

        TracingBoardSurface surface = board.GetComponent<TracingBoardSurface>();
        if (surface == null)
            surface = board.gameObject.AddComponent<TracingBoardSurface>();

        surface.reachDistance = 0.08f;
        surface.surfaceLift = 0.002f;

        // Invisible draw plane must not block CharacterController walking near desks.
        Collider boardCollider = board.GetComponent<Collider>();
        if (boardCollider != null)
            boardCollider.enabled = false;
    }

    bool PlaceOnTracingBoard(int setIndex)
    {
        Transform paper = PenTraceBoards.FindSet(setIndex);
        if (paper == null)
            return false;

        Vector3 normal = PenTraceBoards.BoardNormal(paper);
        Vector3 center = PenTraceBoards.BoardCenter(paper);
        Vector3 position = center + normal * PaperLift;

        Vector3 along = PenTraceBoards.BoardFacing(paper);
        if (along.sqrMagnitude < 0.001f)
            return false;

        // Desk-local facing (same for every TraceDesk) — matches table-1 Start-left without headset bias.
        Quaternion rotation = Quaternion.LookRotation(-normal, along);
        tracingArea.transform.SetPositionAndRotation(position, rotation);
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
