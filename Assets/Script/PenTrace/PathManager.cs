using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer tracePath;
    public Transform traceEnd;
    public GameObject finishedScreen;
    public TMP_Text averageAccuracyText;
    public PatternManager patternManager;

    public float traceDistance = 0.08f;
    public float followDistance = 0.2f;
    public float lookAhead = 0.3f;
    public float accuracyRange = 0.08f;

    float progress;
    float totalLength;
    float accuracySum;
    int accuracySamples;
    bool patternCompleted;
    LineRenderer coloredPath;
    readonly List<Vector3> points = new List<Vector3>();

    static readonly Color DoneColor = new Color(0.2f, 0.85f, 0.35f);
    static readonly Color CurrentColor = new Color(1f, 0.92f, 0.25f);
    static readonly Color RemainingColor = new Color(0.55f, 0.55f, 0.55f);

    void Update()
    {
        if (patternCompleted || penTip == null || tracePath == null)
            return;

        RebuildPath();
        PaintGuide();

        if (totalLength <= 0.01f)
            return;

        PenTrail trail = PenTrail.Active;
        if (trail == null || !trail.IsDrawing())
            return;

        SampleAccuracy(DistanceToPath(penTip.position));
        TryAdvance(penTip.position);
        if (patternManager != null)
            patternManager.CheckStars(penTip.position);

        if (progress >= totalLength - 0.01f)
        {
            progress = totalLength;
            PaintGuide();
            patternCompleted = true;
            StartCoroutine(PatternComplete());
        }
    }

    void RebuildPath()
    {
        points.Clear();
        totalLength = 0f;

        for (int i = 0; i < tracePath.positionCount; i++)
        {
            Vector3 point = tracePath.GetPosition(i);
            if (!tracePath.useWorldSpace)
                point = tracePath.transform.TransformPoint(point);

            if (points.Count > 0)
                totalLength += Vector3.Distance(points[points.Count - 1], point);

            points.Add(point);
        }
    }

    void TryAdvance(Vector3 point)
    {
        float limit = Mathf.Min(progress + lookAhead, totalLength);
        float walked = 0f;
        float bestDistance = float.MaxValue;
        float bestAlong = progress;
        bool found = false;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            float segmentStart = walked;
            float segmentEnd = walked + segmentLength;
            float from = Mathf.Max(segmentStart, progress);
            float to = Mathf.Min(segmentEnd, limit);

            if (segmentLength > 0.0001f && to >= from)
            {
                Vector3 windowStart = Vector3.Lerp(start, end, (from - segmentStart) / segmentLength);
                Vector3 windowEnd = Vector3.Lerp(start, end, (to - segmentStart) / segmentLength);
                Vector3 closest = ClosestPointOnLine(point, windowStart, windowEnd);
                float distance = Vector3.Distance(point, closest);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    float along = Vector3.Dot(closest - start, end - start) / (segmentLength * segmentLength);
                    bestAlong = segmentStart + Mathf.Clamp01(along) * segmentLength;
                    found = true;
                }
            }

            walked = segmentEnd;
        }

        if (found && bestDistance <= followDistance && bestAlong > progress)
            progress = bestAlong;
    }

    float DistanceToPath(Vector3 point)
    {
        float best = float.MaxValue;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 closest = ClosestPointOnLine(point, points[i], points[i + 1]);
            float distance = Vector3.Distance(point, closest);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    void SampleAccuracy(float distance)
    {
        float closeness = 1f - Mathf.Clamp01(distance / accuracyRange);
        accuracySum += closeness;
        accuracySamples++;
    }

    void PaintGuide()
    {
        if (tracePath == null || totalLength <= 0f)
            return;

        if (coloredPath != tracePath)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                tracePath.material = new Material(shader);

            tracePath.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            tracePath.widthMultiplier = 0.02f;
            coloredPath = tracePath;
        }

        float covered = Mathf.Clamp01(progress / totalLength);
        Gradient gradient = new Gradient();

        if (covered >= 0.999f)
        {
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(DoneColor, 0f),
                    new GradientColorKey(DoneColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
        }
        else
        {
            float edge = Mathf.Clamp(covered, 0.001f, 0.98f);
            float band = Mathf.Min(edge + 0.02f, 0.99f);

            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(DoneColor, 0f),
                    new GradientColorKey(DoneColor, edge),
                    new GradientColorKey(CurrentColor, band),
                    new GradientColorKey(RemainingColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
        }

        tracePath.colorGradient = gradient;
    }

    static Vector3 ClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
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
        if (accuracySamples == 0)
            return 0f;

        return (accuracySum / accuracySamples) * 100f;
    }

    IEnumerator PatternComplete()
    {
        averageAccuracyText.text = "Average Tracing Accuracy: " + GetAccuracy().ToString("F1") + "%";
        finishedScreen.SetActive(true);

        yield return new WaitForSeconds(1f);

        finishedScreen.SetActive(false);
        patternManager.NextPattern();
    }

    public void ClearRound()
    {
        accuracySum = 0f;
        accuracySamples = 0;
    }

    public void ResetPattern()
    {
        progress = 0f;
        patternCompleted = false;

        if (PenTrail.Active != null)
            PenTrail.Active.ClearInk();

        RebuildPath();
        PaintGuide();
    }
}
