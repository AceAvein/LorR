using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsNav : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    static void Attach(Scene scene)
    {
        if (!scene.IsValid())
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < transforms.Length; t++)
            {
                if (transforms[t].name != "ResultsScreen")
                    continue;

                if (transforms[t].GetComponent<ResultsNav>() == null)
                    transforms[t].gameObject.AddComponent<ResultsNav>();
            }
        }
    }

    void Awake()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string key = Key(buttons[i].gameObject.name);
            if (key == "tryagain" || key == "resume")
                buttons[i].onClick.AddListener(TryAgain);
            else if (key == "exit" || key == "menu" || key == "quit")
                buttons[i].onClick.AddListener(Exit);
        }
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    static string Key(string name)
    {
        return name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
