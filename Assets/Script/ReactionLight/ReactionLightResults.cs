using UnityEngine;
using UnityEngine.SceneManagement;

public struct SetRecord
{
    public int tier;
    public bool cleared;
    public float reaction;
}

public struct ReactionLightReport
{
    public float averageReactionMs;
    public float accuracy;
    public int correct;
    public int incorrect;
    public int missed;
}

public class ReactionLightResults : MonoBehaviour
{
    public void Show(ReactionLightReport report)
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        Transform panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = CreatePanel();
        panel.gameObject.SetActive(true);
        ResultsPanelStyle.FitPanel(panel, "ReactionLight/ResultsPanel");
        ResultsPanelStyle.ClearValues(panel);

        ResultsPanelStyle.AddValue(panel, "Value_AvgReaction", 0).text = report.averageReactionMs.ToString("0") + " ms";
        ResultsPanelStyle.AddValue(panel, "Value_Accuracy", 1).text = report.accuracy.ToString("0") + "%";
        ResultsPanelStyle.AddValue(panel, "Value_Correct", 2).text = report.correct.ToString();
        ResultsPanelStyle.AddValue(panel, "Value_Incorrect", 3).text = report.incorrect.ToString();
        ResultsPanelStyle.AddValue(panel, "Value_Missed", 4).text = report.missed.ToString();
    }

    Transform CreatePanel()
    {
        GameObject panelObject = new GameObject("ResultsPanel");
        panelObject.transform.SetParent(transform, false);
        panelObject.AddComponent<UnityEngine.UI.Image>();
        ResultsPanelStyle.FitPanel(panelObject.transform, "ReactionLight/ResultsPanel");
        return panelObject.transform;
    }
}
