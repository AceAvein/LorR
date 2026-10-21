using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    void Start()
    {
        Invoke("GoToMenu", 3f);
    }

    void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
