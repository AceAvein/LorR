using TMPro;
using UnityEngine;

public class BallDropResults : MonoBehaviour
{
    TMP_Text interceptionRate;
    TMP_Text successCount;
    TMP_Text failCount;
    TMP_Text averageReaction;
    bool built;

    public void Show(BallDropReport report)
    {
        EnsureVisuals();

        interceptionRate.text = report.interceptionRate.ToString("0") + "%";
        successCount.text = report.success.ToString();
        failCount.text = report.fail.ToString();
        averageReaction.text = report.averageReaction.ToString("0.0") + "s";
    }

    void EnsureVisuals()
    {
        if (built)
            return;

        built = true;
        HideOldLabel();

        Transform panel = transform.Find("ResultsPanel");
        if (panel == null)
            panel = CreatePanel();
        panel.gameObject.SetActive(true);
        ResultsPanelStyle.FitPanel(panel, "BallDrop/ResultsPanel");
        ResultsPanelStyle.ClearValues(panel);

        interceptionRate = ResultsPanelStyle.AddValue(panel, "Value_Interception", 0);
        successCount = ResultsPanelStyle.AddValue(panel, "Value_Success", 1);
        failCount = ResultsPanelStyle.AddValue(panel, "Value_Fail", 2);
        averageReaction = ResultsPanelStyle.AddValue(panel, "Value_AvgReaction", 3);
    }

    void HideOldLabel()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == "Text_Results")
                labels[i].gameObject.SetActive(false);
        }
    }

    Transform CreatePanel()
    {
        GameObject panelObject = new GameObject("ResultsPanel");
        panelObject.transform.SetParent(transform, false);
        panelObject.AddComponent<UnityEngine.UI.Image>();
        ResultsPanelStyle.FitPanel(panelObject.transform, "BallDrop/ResultsPanel");
        return panelObject.transform;
    }
}
