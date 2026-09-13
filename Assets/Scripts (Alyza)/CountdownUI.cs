using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    public TMP_Text countdownText;
    public System.Action OnCountdownFinished;

    public void StartCountdown()
    {
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        string[] steps = { "3", "2", "1", "GO!" };
        foreach (var step in steps)
        {
            countdownText.text = step;
            yield return new WaitForSeconds(1f);
        }
        OnCountdownFinished?.Invoke();
    }
}