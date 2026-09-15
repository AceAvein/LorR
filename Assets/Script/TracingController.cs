using UnityEngine;
using System;

public class TracingController : MonoBehaviour
{
    [Header("References")]
    public Transform penTip;
    public PathVisualizer pathVisualizer;
    public TracingPath activePath;

    [Header("Runtime State (read-only)")]
    public int currentTargetIndex = 0;
    public float pathDeviation;
    public float accuracyScore = 100f;
    public int errorCount = 0;
    public float elapsedTime = 0f;
    public bool isTracing = false;

    private float totalDeviationSum = 0f;
    private int sampleCount = 0;
    private bool wasOffPath = false;

    public Action OnPatternComplete;
    public Action OnTimeUp;

    public void BeginPattern(TracingPath path)
    {
        activePath = path;
        currentTargetIndex = 0;
        accuracyScore = 100f;
        errorCount = 0;
        elapsedTime = 0f;
        totalDeviationSum = 0f;
        sampleCount = 0;
        isTracing = true;
        pathVisualizer.LoadPath(path, transform);
    }

    void Update()
    {
        if (!isTracing || activePath == null) return;

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= activePath.timeLimit)
        {
            isTracing = false;
            OnTimeUp?.Invoke();
            return;
        }

        EvaluateTracing();
    }

    void EvaluateTracing()
    {
        if (currentTargetIndex >= activePath.pathPoints.Count)
        {
            isTracing = false;
            OnPatternComplete?.Invoke();
            return;
        }

        Vector3 targetWorldPoint = transform.TransformPoint(activePath.pathPoints[currentTargetIndex]);
        pathDeviation = Vector3.Distance(penTip.position, targetWorldPoint);
        Debug.Log("PenTip distance from target: " + pathDeviation);
        Debug.Log("PenTip: " + penTip.position);
        Debug.Log("Target: " + targetWorldPoint);

        totalDeviationSum += pathDeviation;
        sampleCount++;

        bool onPath = pathDeviation <= activePath.toleranceRadius;

       if (onPath)
        {
            wasOffPath = false;

            // Record the PenTip's actual movement
            pathVisualizer.AddTracePoint(penTip.position);

            if (pathDeviation <= activePath.toleranceRadius * 0.6f)
            {
                pathVisualizer.ClearPointVisual(currentTargetIndex);
                currentTargetIndex++;
            }
        }
        else
        {
            if (!wasOffPath)
            {
                errorCount++;
                wasOffPath = true;
            }
        }

        UpdateAccuracyScore();
    }

    void UpdateAccuracyScore()
    {
        float avgDeviation = sampleCount > 0 ? totalDeviationSum / sampleCount : 0f;
        float normalizedDeviation = Mathf.Clamp01(avgDeviation / (activePath.toleranceRadius * 3f));
        float deviationPenalty = normalizedDeviation * 60f;
        float errorPenalty = Mathf.Min(errorCount * 4f, 40f);
        accuracyScore = Mathf.Clamp(100f - deviationPenalty - errorPenalty, 0f, 100f);
    }

    public float GetCompletionPercent() =>
        activePath == null ? 0 : (float)currentTargetIndex / activePath.pathPoints.Count * 100f;
}