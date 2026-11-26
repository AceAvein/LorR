using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct FinishedPattern
{
    public int tier;
    public float accuracy;
    public float avgDeviationMm;
    public float maxDeviationMm;
    public float seconds;
    public int starsGot;
    public int starsOnPattern;
}

public struct RoundReport
{
    public float avgDeviationMm;
    public float maxDeviationMm;
    public int patternsCompleted;
    public int patternTotal;
    public int timeUsed;
}

public class ResultsScreen : MonoBehaviour
{
    public Sprite panelSprite;

    TMP_Text avgDeviation;
    TMP_Text maxDeviation;
    TMP_Text pathCompletion;
    TMP_Text timeUsed;
    Transform valueRoot;
    bool built;

    void Awake()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
        {
            gameObject.SetActive(false);
            return;
        }

        EnsureVisuals();
        gameObject.SetActive(false);
    }

    public void Show(RoundReport report)
    {
        float completionPct = report.patternTotal <= 0
            ? 0f
            : (100f * report.patternsCompleted / report.patternTotal);

        ProgressionSessionRecorder.Record(
            ProgressionConfig.PenTrace,
            completionPct,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "avgDeviationMm", report.avgDeviationMm },
                { "maxDeviationMm", report.maxDeviationMm },
                { "patternsCompleted", report.patternsCompleted },
                { "patternTotal", report.patternTotal },
                { "timeUsed", report.timeUsed },
                { "completionPct", completionPct }
            });

        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        EnsureVisuals();

        avgDeviation.text = report.avgDeviationMm.ToString("0.0") + " mm";
        maxDeviation.text = report.maxDeviationMm.ToString("0.0") + " mm";
        pathCompletion.text = report.patternsCompleted + " / " + report.patternTotal;
        timeUsed.text = report.timeUsed + "s";
    }

    void EnsureVisuals()
    {
        if (built)
            return;

        built = true;
        HideOldLabels();
        valueRoot = transform.Find("ResultsPanel");
        if (valueRoot == null)
            valueRoot = transform;
        valueRoot.gameObject.SetActive(true);
        ResultsPanelStyle.FitPanel(valueRoot, "PenTrace/ResultsPanel", panelSprite);
        ResultsPanelStyle.ClearValues(valueRoot);

        avgDeviation = ResultsPanelStyle.AddValue(valueRoot, "Value_AvgDeviation", 0);
        maxDeviation = ResultsPanelStyle.AddValue(valueRoot, "Value_MaxDeviation", 1);
        pathCompletion = ResultsPanelStyle.AddValue(valueRoot, "Value_PathCompletion", 2);
        timeUsed = ResultsPanelStyle.AddValue(valueRoot, "Value_Time", 3);
    }

    void HideOldLabels()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string name = labels[i].gameObject.name;
            if (name == "Text_TimesUp" || name == "Text_Accuracy")
                labels[i].gameObject.SetActive(false);
        }
    }
}
