using UnityEngine;

public class PathManager : MonoBehaviour
{
    public Transform penTip;
    public Transform tracePath;

    public float traceDistance = 0.15f;

    private float totalTraceTime = 0f;
    private float correctTraceTime = 0f;

    void Update()
    {
        if (penTip == null || tracePath == null)
            return;

        totalTraceTime += Time.deltaTime;

        float distance = Vector3.Distance(penTip.position, tracePath.position);

        if (distance <= traceDistance)
        {
            correctTraceTime += Time.deltaTime;
        }

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log("Accuracy: " + GetAccuracy().ToString("F1") + "%");
        }
    }

    public float GetAccuracy()
    {
        if (totalTraceTime <= 0f)
            return 0f;

        return (correctTraceTime / totalTraceTime) * 100f;
    }
}