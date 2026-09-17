using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToMenu : MonoBehaviour
{
 public void Exit()
    {
        SceneManager.LoadScene("Menu");
    }
}
