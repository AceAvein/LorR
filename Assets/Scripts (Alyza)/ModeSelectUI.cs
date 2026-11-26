using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeSelectUI : MonoBehaviour
{
    GameObject selectPanel;
    GameObject phasePanel;
    string pendingMode;
    bool pendingGuided;
    Button[] phaseButtons;

    static readonly Color LockedColor = new Color(0.45f, 0.5f, 0.52f, 1f);
    static readonly Color OpenColor = new Color(0.2f, 0.55f, 0.58f, 1f);

    void Awake()
    {
        selectPanel = FindNamed("Select Guided or Simulation");
        if (selectPanel != null)
            selectPanel.SetActive(false);

        phasePanel = FindNamed("Phases");
        if (phasePanel != null)
            phasePanel.SetActive(false);

        WireModeButtons();
        WirePhaseButtons();
    }

    public void OnModeSelected(string sceneName)
    {
        AskPath(sceneName);
    }

    public void OnBackClicked()
    {
        if (phasePanel != null && phasePanel.activeSelf)
        {
            HidePhase();
            if (selectPanel != null)
                selectPanel.SetActive(true);
            return;
        }

        HideSelect();
        HidePhase();
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

        pendingGuided = true;
        ShowPhaseSelect();
    }

    public void OnSimulationClicked()
    {
        if (string.IsNullOrEmpty(pendingMode))
            return;

        pendingGuided = false;
        ShowPhaseSelect();
    }

    void AskPath(string modeName)
    {
        pendingMode = modeName;
        pendingGuided = false;

        HidePhase();
        if (selectPanel == null)
            selectPanel = FindNamed("Select Guided or Simulation");
        if (selectPanel != null)
        {
            selectPanel.SetActive(true);
            MenuDesktopClickSupport.Apply();
        }
        else
            ShowPhaseSelect();
    }

    void ShowPhaseSelect()
    {
        HideSelect();

        if (phasePanel == null)
            phasePanel = FindNamed("Phases");

        if (phasePanel == null)
        {
            // Fallback: no phase UI — launch Phase 1.
            LaunchPhase(1);
            return;
        }

        RefreshPhaseLocks();
        phasePanel.SetActive(true);
        MenuDesktopClickSupport.Apply();
    }

    void RefreshPhaseLocks()
    {
        string profileId = ProgressionService.CurrentProfileId();
        string moduleId = ProgressionSessionContext.ModuleIdFromPendingScene(pendingMode);

        for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            Button button = FindPhaseButton(phase);
            if (button == null)
                continue;

            bool unlocked = string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(moduleId)
                ? phase == 1
                : ProgressionService.IsPhaseUnlocked(profileId, moduleId, phase);

            button.interactable = unlocked;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = unlocked ? OpenColor : LockedColor;
        }
    }

    void OnPhaseClicked(int phase)
    {
        string profileId = ProgressionService.CurrentProfileId();
        string moduleId = ProgressionSessionContext.ModuleIdFromPendingScene(pendingMode);
        string mode = pendingGuided ? ProgressionConfig.ModeGuided : ProgressionConfig.ModeUnguided;

        if (!string.IsNullOrEmpty(profileId) && !string.IsNullOrEmpty(moduleId))
        {
            if (!ProgressionService.IsPhaseUnlocked(profileId, moduleId, phase))
            {
                Debug.LogWarning("[ModeSelect] Phase " + phase + " locked for " + moduleId);
                return;
            }

            if (!pendingGuided &&
                !ProgressionService.CanPlay(profileId, moduleId, phase, mode, out _, out string blockMessage))
            {
                Debug.LogWarning("[ModeSelect] " + blockMessage);
                return;
            }
        }

        LaunchPhase(phase);
    }

    void LaunchPhase(int phase)
    {
        string moduleId = ProgressionSessionContext.ModuleIdFromPendingScene(pendingMode);
        string mode = pendingGuided ? ProgressionConfig.ModeGuided : ProgressionConfig.ModeUnguided;
        ProgressionSessionContext.Set(moduleId, mode, phase);

        string sceneName = pendingGuided ? ("Guided " + pendingMode) : pendingMode;
        HideSelect();
        HidePhase();
        SceneManager.LoadScene(sceneName);
    }

    void HideSelect()
    {
        if (selectPanel != null)
            selectPanel.SetActive(false);
    }

    void HidePhase()
    {
        if (phasePanel != null)
            phasePanel.SetActive(false);
    }

    void WireModeButtons()
    {
        if (selectPanel == null)
            return;

        Button[] buttons = selectPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].gameObject.name;
            if (name == "Guided")
            {
                buttons[i].onClick.RemoveListener(OnGuidedClicked);
                buttons[i].onClick.AddListener(OnGuidedClicked);
            }
            else if (name == "Simulation")
            {
                buttons[i].onClick.RemoveListener(OnSimulationClicked);
                buttons[i].onClick.AddListener(OnSimulationClicked);
            }
        }
    }

    void WirePhaseButtons()
    {
        if (phasePanel == null)
            return;

        phaseButtons = phasePanel.GetComponentsInChildren<Button>(true);
        for (int phase = 1; phase <= ProgressionConfig.PhaseCount; phase++)
        {
            Button button = FindPhaseButton(phase);
            if (button == null)
                continue;

            int captured = phase;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnPhaseClicked(captured));
        }
    }

    Button FindPhaseButton(int phase)
    {
        if (phasePanel == null)
            return null;

        string target = "Phase " + phase;
        Button[] buttons = phasePanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == target)
                return buttons[i];
        }

        return null;
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
