using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LightManager : MonoBehaviour
{
    public Button[] lights;

    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    public float greenLightDuration = 1f;

    private int greenLightIndex;
    private Coroutine lightTimer;

    void Start()
    {
        ChangeLight();
    }

    public void ChangeLight()
    {
        if (lightTimer != null)
        {
            StopCoroutine(lightTimer);
        }

        greenLightIndex = Random.Range(0, lights.Length);

        for (int i = 0; i < lights.Length; i++)
        {
            Image image = lights[i].GetComponent<Image>();
            image.color = redColor;
        }

        Image greenImage = lights[greenLightIndex].GetComponent<Image>();
        greenImage.color = greenColor;

        lightTimer = StartCoroutine(GreenLightTimer());
    }

    IEnumerator GreenLightTimer()
    {
        yield return new WaitForSeconds(greenLightDuration);

        Image image = lights[greenLightIndex].GetComponent<Image>();
        image.color = redColor;

        if (GameReactionManager.Instance != null)
        {
            GameReactionManager.Instance.TooSlow();
        }

        yield return new WaitForSeconds(0.2f);

        ChangeLight();
    }

    public void LightClicked(Button clickedButton)
    {
        if (lightTimer != null)
        {
            StopCoroutine(lightTimer);
        }

        int clickedIndex = System.Array.IndexOf(lights, clickedButton);

        if (clickedIndex == greenLightIndex)
        {
            if (GameReactionManager.Instance != null)
            {
                GameReactionManager.Instance.CorrectAnswer();
            }
        }
        else
        {
            if (GameReactionManager.Instance != null)
            {
                GameReactionManager.Instance.WrongAnswer();
            }
        }

        ChangeLight();
    }
}