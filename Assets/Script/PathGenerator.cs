using System.Collections.Generic;
using UnityEngine;

public static class PathGenerator
{
    public static List<Vector3> GenerateLine(float length, int segments)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = Mathf.Lerp(-length / 2, length / 2, t);
            pts.Add(new Vector3(x, 0, 0));
        }
        return pts;
    }

    public static List<Vector3> GenerateWave(float width, float amplitude, int segments)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = Mathf.Lerp(-width / 2, width / 2, t);
            float y = Mathf.Sin(t * Mathf.PI * 2) * amplitude;
            pts.Add(new Vector3(x, y, 0));
        }
        return pts;
    }

    public static List<Vector3> GenerateCircle(float radius, int segments)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            pts.Add(new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0));
        }
        return pts;
    }

    public static List<Vector3> GenerateZigzag(float width, float amplitude, int zigzagCount, int segmentsPerZig)
    {
        var pts = new List<Vector3>();
        int totalSegments = zigzagCount * segmentsPerZig;
        for (int i = 0; i <= totalSegments; i++)
        {
            float t = (float)i / totalSegments;
            float x = Mathf.Lerp(-width / 2, width / 2, t);
            // Triangle wave instead of sine, for sharp zigzag corners
            float zigPhase = (t * zigzagCount) % 1f;
            float y = (zigPhase < 0.5f)
                ? Mathf.Lerp(-amplitude, amplitude, zigPhase * 2f)
                : Mathf.Lerp(amplitude, -amplitude, (zigPhase - 0.5f) * 2f);
            pts.Add(new Vector3(x, y, 0));
        }
        return pts;
    }

    public static List<Vector3> GenerateStar(float outerRadius, float innerRadius, int points, int segmentsPerEdge)
    {
        var pts = new List<Vector3>();
        int totalPoints = points * 2; // outer and inner vertices alternate
        var vertices = new List<Vector3>();
        for (int i = 0; i < totalPoints; i++)
        {
            float angle = i * Mathf.PI / points - Mathf.PI / 2f;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0));
        }
        vertices.Add(vertices[0]); // close the loop

        // Interpolate along each edge so the path has enough points to dash away smoothly
        for (int i = 0; i < vertices.Count - 1; i++)
        {
            for (int s = 0; s < segmentsPerEdge; s++)
            {
                float t = (float)s / segmentsPerEdge;
                pts.Add(Vector3.Lerp(vertices[i], vertices[i + 1], t));
            }
        }
        pts.Add(vertices[vertices.Count - 1]);
        return pts;
    }
}