using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestKeyboardTest : MonoBehaviour
{
    public InputField inputField;

    void Update()
    {
        if (inputField != null &&
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == inputField.gameObject)
        {
            Debug.Log("INPUT FIELD SELECTED");

            if (!TouchScreenKeyboard.visible)
            {
                Debug.Log("OPENING KEYBOARD");

                TouchScreenKeyboard.Open(
                    inputField.text,
                    TouchScreenKeyboardType.Default
                );
            }
        }
    }
}