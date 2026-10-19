using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectUI : MonoBehaviour
{
    public void OnModeSelected(string sceneName)
    {
        OpenChecked(sceneName);
    }

    public void OnBackClicked()
    {
        SceneManager.LoadScene("Menu");
    }

    public void OnTrainingRangeClicked()
    {
        SceneManager.LoadScene("TrainingRange");
    }

    public void OnPenTraceClicked()
    {
        OpenChecked("PenControlTracing");
    }

    public void OnReactionLightClicked()
    {
        OpenChecked("ReactionLight");
    }

    public void OnBallDropClicked()
    {
        OpenChecked("Ball Drop");
    }

    public void OnColorSortClicked()
    {
        OpenChecked("ColorSort");
    }

    void OpenChecked(string sceneName)
    {
        ScoringChecker.Ensure().TryOpenModule(sceneName);
    }
}
