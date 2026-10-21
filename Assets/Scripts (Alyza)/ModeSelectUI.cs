using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeSelectUI : MonoBehaviour
{
    GameObject selectPanel;
    string pendingMode;

    void Awake()
    {
        selectPanel = FindNamed("Select Guided or Simulation");
        if (selectPanel != null)
            selectPanel.SetActive(false);

        WireButtons();
    }

    public void OnModeSelected(string sceneName)
    {
        AskPath(sceneName);
    }

    public void OnBackClicked()
    {
        HideSelect();
        SceneManager.LoadScene("Menu");
    }

    public void OnTrainingRangeClicked()
    {
        SceneManager.LoadScene("TrainingRange");
    }

    public void OnPenTraceClicked()
    {
        AskPath("Pen Control Tracing");
    }

    public void OnReactionLightClicked()
    {
        AskPath("Reaction Light");
    }

    public void OnBallDropClicked()
    {
        AskPath("Ball Drop");
    }

    public void OnColorSortClicked()
    {
        AskPath("Color Sort");
    }

    public void OnGuidedClicked()
    {
        if (string.IsNullOrEmpty(pendingMode))
            return;

        OpenChecked("Guided " + pendingMode);
    }

    public void OnSimulationClicked()
    {
        if (string.IsNullOrEmpty(pendingMode))
            return;

        OpenChecked(pendingMode);
    }

    void AskPath(string modeName)
    {
        pendingMode = modeName;
        if (selectPanel == null)
            selectPanel = FindNamed("Select Guided or Simulation");
        if (selectPanel != null)
            selectPanel.SetActive(true);
        else
            OpenChecked(modeName);
    }

    void HideSelect()
    {
        if (selectPanel != null)
            selectPanel.SetActive(false);
    }

    void OpenChecked(string sceneName)
    {
        ScoringChecker.Ensure().TryOpenModule(sceneName);
    }

    void WireButtons()
    {
        if (selectPanel == null)
            return;

        Button[] buttons = selectPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].gameObject.name;
            if (name == "Guided")
                buttons[i].onClick.AddListener(OnGuidedClicked);
            else if (name == "Simulation")
                buttons[i].onClick.AddListener(OnSimulationClicked);
        }
    }

    static GameObject FindNamed(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }
}
