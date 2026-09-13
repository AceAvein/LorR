using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneUI : MonoBehaviour
{
    public void OnBackToModeSelect()
    {
        SceneManager.LoadScene("03_ModeSelection"); 
    }
}