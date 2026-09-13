using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectiveManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("09.1_ReactionLight");
    }
}