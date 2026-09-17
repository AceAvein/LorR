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
        tracingArea.SetActive(true);
        tracingTimer.StartTimer();
    }
}