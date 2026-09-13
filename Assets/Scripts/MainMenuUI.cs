using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void OnExitClicked()
    {
        SceneManager.LoadScene("01_ProfileSelection");
    }

    public void OnStartClicked()
    {
        SceneManager.LoadScene("03_ModeSelection");
    }

    public void OnProgressClicked()
    {
        SceneManager.LoadScene("11_Progress");
    }

    public void OnSettingsClicked()
    {
        SceneManager.LoadScene("04_Settings");
    }

    public void OnTutorialClicked()
    {
        SceneManager.LoadScene("05_Tutorial");
    }

    public void OnBackClicked()
    {
        SceneManager.LoadScene("02_MainMenu");
    }
}