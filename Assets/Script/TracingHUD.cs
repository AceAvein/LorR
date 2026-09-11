using TMPro;
using UnityEngine;

public class TracingHUD : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text patternLabel;
    public TracingController controller;

    void Update()
    {
        if (controller == null || controller.activePath == null) return;
        float remaining = Mathf.Max(0, controller.activePath.timeLimit - controller.elapsedTime);
        int min = Mathf.FloorToInt(remaining / 60);
        int sec = Mathf.FloorToInt(remaining % 60);
        timerText.text = $"{min}:{sec:00}";
        patternLabel.text = controller.activePath.patternName;
    }
}