using UnityEngine;
using TMPro;

public class BallDropGameManager : MonoBehaviour
{
    public int intercepted = 0;
    public TMP_Text interceptedText;

    public void AddIntercepted()
    {
        intercepted++;

        interceptedText.text = "Intercepted: " + intercepted;

        Debug.Log("Intercepted: " + intercepted);
    }
}