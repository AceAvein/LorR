using UnityEngine;
using UnityEngine.SceneManagement;

public struct ColorSortReport
{
    public float accuracy;
    public int completionTime;
    public int correct;
    public int incorrect;
}

public class ColorSortResults : MonoBehaviour
{
    public void Show(ColorSortReport report)
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        Transform panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = transform;
        panel.gameObject.SetActive(true);
        ResultsPanelStyle.FitPanel(panel, "ColorSort/ResultsPanel");
        ResultsPanelStyle.ClearValues(panel);

        ResultsPanelStyle.AddValue(panel, "Value_Accuracy", 0).text = report.accuracy.ToString("0") + "%";
        ResultsPanelStyle.AddValue(panel, "Value_CompletionTime", 1).text = report.completionTime + "s";
        ResultsPanelStyle.AddValue(panel, "Value_Correct", 2).text = report.correct.ToString();
        ResultsPanelStyle.AddValue(panel, "Value_Incorrect", 3).text = report.incorrect.ToString();
    }
}
