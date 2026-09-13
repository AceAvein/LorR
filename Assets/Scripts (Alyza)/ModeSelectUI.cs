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
        SceneManager.LoadScene("02_MainMenu");
    }
    public void OnTrainingRangeClicked()
    {
        SceneManager.LoadScene("06_TrainingRange");
    }
}