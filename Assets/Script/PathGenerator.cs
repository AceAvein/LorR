using System.Collections.Generic;
using UnityEngine;

public static class PathGenerator
{
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
}