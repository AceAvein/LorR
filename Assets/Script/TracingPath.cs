using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPattern", menuName = "LorR-VR/Tracing Pattern")]
public class TracingPath : ScriptableObject
{
    public string patternName = "Pattern 1";
    [Tooltip("Ordered points that make up the path, in local space")]
    public List<Vector3> pathPoints = new List<Vector3>();
    [Tooltip("Allowed distance (meters) from the path to still count as 'on track'")]
    public float toleranceRadius = 0.08f;
    [Tooltip("Time limit in seconds for this pattern")]
    public float timeLimit = 60f;
    [Range(1, 5)] public int difficultyLevel = 1;
}