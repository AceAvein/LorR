using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LightManager : MonoBehaviour
{
    public static LightManager Instance;

    [Header("Lights")]
    public Button[] lights;

    [Header("Colors")]
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    [Header("Difficulty")]
    public float greenLightDuration = 1.5f;

    private int greenLightIndex;
    private Coroutine lightTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateDifficulty();
        ChangeLight();
    }

    public void UpdateDifficulty()
    {
        int level = 1;

        if (GameReactionManager.Instance != null)
        {
            level = GameReactionManager.Instance.GetCurrentLevel();
        }

        switch (level)
        {
            case 1:
                greenLightDuration = 1.5f;
                break;

            case 2:
                greenLightDuration = 1.3f;
                break;

            case 3:
                greenLightDuration = 1.1f;
                break;

            case 4:
                greenLightDuration = 0.9f;
                break;

            case 5:
                greenLightDuration = 0.7f;
                break;
        }
    }

    public void ChangeLight()
    {
        if (lights == null || lights.Length == 0)
            return;

        if (lightTimer != null)
        {
            StopCoroutine(lightTimer);
        }

        UpdateDifficulty();

        // Random green light
        greenLightIndex = Random.Range(0, lights.Length);

        // Make all lights red
        for (int i = 0; i < lights.Length; i++)
        {
            Image image = lights[i].GetComponent<Image>();

            if (image != null)
            {
                image.color = redColor;
            }
        }

        // Make selected light green
        Image greenImage =
            lights[greenLightIndex].GetComponent<Image>();

        if (greenImage != null)
        {
            greenImage.color = greenColor;
        }

        // Start reaction timer
        lightTimer = StartCoroutine(GreenLightTimer());
    }

    IEnumerator GreenLightTimer()
    {
        yield return new WaitForSeconds(greenLightDuration);

        // Turn green light back to red
        if (greenLightIndex >= 0 &&
            greenLightIndex < lights.Length)
        {
            Image image =
                lights[greenLightIndex].GetComponent<Image>();

            if (image != null)
            {
                image.color = redColor;
            }
        }

        // Player failed to react in time
        if (GameReactionManager.Instance != null)
        {
            GameReactionManager.Instance.TooSlow();
        }

        yield return new WaitForSeconds(0.2f);

        ChangeLight();
    }

    public void LightClicked(Button clickedButton)
    {
        if (GameReactionManager.Instance == null)
            return;

        if (lightTimer != null)
        {
            StopCoroutine(lightTimer);
        }

        int clickedIndex =
            System.Array.IndexOf(lights, clickedButton);

        if (clickedIndex == greenLightIndex)
        {
            // Correct
            GameReactionManager.Instance.CorrectAnswer();
        }
        else
        {
            // Wrong
            GameReactionManager.Instance.WrongAnswer();
        }

        // Next random target
        ChangeLight();
    }
}