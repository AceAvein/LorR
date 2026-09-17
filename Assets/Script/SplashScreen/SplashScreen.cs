using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    void Start()
    {
        Invoke("GoToProfileSelect", 3f);
    }

    void GoToProfileSelect()
    {
        SceneManager.LoadScene("ProfileSelect");
    }
}
