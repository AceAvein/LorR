using UnityEngine;
using System.Collections;
using TMPro;

public class PathManager : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer tracePath;
    public Transform traceEnd;
    public GameObject finishedScreen;
    public TMP_Text averageAccuracyText;

    public float traceDistance = 0.08f;

    private float totalTraceTime = 0f;
    private float correctTraceTime = 0f;

    void Update()
    {
        if (penTip == null || tracePath == null)
            return;

        totalTraceTime += Time.deltaTime;

        if (IsPenOnPath())
        {
            correctTraceTime += Time.deltaTime;
        }

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log("Accuracy: " + GetAccuracy().ToString("F1") + "%");
        }

        if (Vector3.Distance(penTip.position, traceEnd.position) <= 0.08f)
        {
            StartCoroutine(PatternComplete());
        }
    }

    bool IsPenOnPath()
    {
        Vector3 penPosition = penTip.position;

        for (int i = 0; i < tracePath.positionCount - 1; i++)
        {
            Vector3 pointA = tracePath.GetPosition(i);
            Vector3 pointB = tracePath.GetPosition(i + 1);

            if (!tracePath.useWorldSpace)
            {
                pointA = tracePath.transform.TransformPoint(pointA);
                pointB = tracePath.transform.TransformPoint(pointB);
            }

            Vector3 closestPoint = ClosestPointOnLine(
                penPosition,
                pointA,
                pointB
            );

            float distance = Vector3.Distance(penPosition, closestPoint);

            if (distance <= traceDistance)
            {
                return true;
            }
        }

        return false;
    }

    Vector3 ClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;

        float lineLengthSquared = lineDirection.sqrMagnitude;

        if (lineLengthSquared == 0f)
            return lineStart;

        float t = Vector3.Dot(point - lineStart, lineDirection) / lineLengthSquared;

        t = Mathf.Clamp01(t);

        return lineStart + lineDirection * t;
    }

    public float GetAccuracy()
    {
        if (totalTraceTime <= 0f)
            return 0f;

        return (correctTraceTime / totalTraceTime) * 100f;
    }

    IEnumerator PatternComplete()
    {
        averageAccuracyText.text = "Average Tracing Accuracy: " + GetAccuracy().ToString("F1") + "%";
        
        finishedScreen.SetActive(true);

        yield return new WaitForSeconds(1f);

        finishedScreen.SetActive(false);

        Debug.Log("Pattern Complete! Next pattern would start here.");
    }
}