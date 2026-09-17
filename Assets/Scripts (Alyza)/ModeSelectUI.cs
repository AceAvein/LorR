using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectUI : MonoBehaviour
{
    public void OnModeSelected(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Back button papunta sa Main Menu galing sa Mode Select
    public void OnBackClicked()
    {
        SceneManager.LoadScene("Menu");
    }
    public void OnTrainingRangeClicked()
    {
        SceneManager.LoadScene("TrainingRange");
    }

    // Training Modes na to
    public void OnPenTraceClicked()
    {
        SceneManager.LoadScene("PenControlTracing");
    }

    public void OnReactionLightClicked()
    {
        SceneManager.LoadScene("ReactionLight");
    }

    public void OnBallDropClicked()
    {
        SceneManager.LoadScene("Balldrop");
    }

    public void OnColorSortClicked()
    {
        SceneManager.LoadScene("ColorSort");
    }

}