using UnityEngine;
using UnityEngine.UI;

public class LightButton : MonoBehaviour
{
    public LightManager lightManager;

    public void ClickLight()
    {
        Button button = GetComponent<Button>();

        lightManager.LightClicked(button);
    }
}