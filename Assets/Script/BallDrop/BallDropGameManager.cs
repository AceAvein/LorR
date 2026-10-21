using UnityEngine;
using TMPro;

public class BallDropGameManager : MonoBehaviour
{
    public int intercepted = 0;
    public TMP_Text interceptedText;

    public void ResetCount()
    {
        intercepted = 0;
        if (interceptedText != null)
            interceptedText.text = "Intercepted: 0";
    }

    public void AddIntercepted()
    {
        intercepted++;

        if (interceptedText != null)
            interceptedText.text = "Intercepted: " + intercepted;
    }
}