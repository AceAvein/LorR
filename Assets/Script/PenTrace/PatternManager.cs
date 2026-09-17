using UnityEngine;

public class PatternManager : MonoBehaviour
{
    public GameObject[] patterns;
    public PathManager pathManager;

    private int currentPattern = 0;

    void Start()
    {
        ShowPattern(currentPattern);
    }

    public void ShowPattern(int index)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            patterns[i].SetActive(i == index);
        }

        currentPattern = index;

        Transform pattern = patterns[index].transform;

        LineRenderer newTracePath = pattern.Find("TracePath").GetComponent<LineRenderer>();
        Transform newTraceEnd = pattern.Find("TraceEnd");

        pathManager.tracePath = newTracePath;
        pathManager.traceEnd = newTraceEnd;
        pathManager.ResetPattern();
    }

    public void NextPattern()
    {
        currentPattern++;

        if (currentPattern >= patterns.Length)
        {
            currentPattern = 0;
        }

        ShowPattern(currentPattern);
    }

    public void ResetPatterns()
    {
        currentPattern = 0;
        ShowPattern(currentPattern);
    }
}