using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GamePauseMenu : MonoBehaviour
{
    public static bool IsFrozen { get; private set; }

    static GamePauseMenu active;

    InputAction menuButton;
    GameObject settings;
    GameObject countdownScreen;
    TMP_Text countdownText;
    bool countingDown;
    readonly List<XRGrabInteractable> frozenGrabs = new List<XRGrabInteractable>();
    readonly List<GameObject> hiddenObjects = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    static void TryCreate(Scene scene)
    {
        if (!scene.IsValid())
            return;

        if (scene.name != "PenControlTracing" && scene.name != "Ball Drop" && scene.name != "BallDrop_HalfwayTest" && scene.name != "ReactionLight" && scene.name != "ColorSort")
            return;

        if (active != null)
            return;

        GameObject host = new GameObject("GamePauseMenu");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GamePauseMenu>();
    }

    void Awake()
    {
        if (active != null && active != this)
        {
            Destroy(gameObject);
            return;
        }

        active = this;
        FindPanels();
        if (settings != null)
            settings.SetActive(false);
        WireButtons();

        menuButton = new InputAction(type: InputActionType.Button);
        menuButton.AddBinding("<XRController>{RightHand}/primaryButton");
        menuButton.AddBinding("<OculusTouchController>{RightHand}/primaryButton");
        menuButton.Enable();
    }

    void OnDestroy()
    {
        if (active == this)
            active = null;

        if (menuButton != null)
        {
            menuButton.Disable();
            menuButton.Dispose();
        }

        if (IsFrozen)
        {
            IsFrozen = false;
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        if (menuButton == null || !menuButton.WasPressedThisFrame())
            return;

        if (settings == null || countingDown)
            return;

        if (settings.activeSelf)
            return;

        if (countdownScreen != null && countdownScreen.activeInHierarchy)
            return;

        Open();
    }

    void Open()
    {
        IsFrozen = true;
        Time.timeScale = 0f;
        FreezeGrabs();
        HideGameElements();
        settings.SetActive(true);
    }

    public void Resume()
    {
        if (countingDown || settings == null)
            return;

        settings.SetActive(false);
        RestoreGameElements();
        StartCoroutine(CountdownThenContinue());
    }

    public void GoToMenu()
    {
        IsFrozen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    IEnumerator CountdownThenContinue()
    {
        countingDown = true;

        if (countdownScreen == null)
            FindPanels();

        if (countdownScreen != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "ReactionLight" || sceneName == "ColorSort")
                PlaceInFront(countdownScreen);
            countdownScreen.SetActive(true);
        }

        int[] steps = { 3, 2, 1 };
        for (int i = 0; i < steps.Length; i++)
        {
            if (countdownText != null)
                countdownText.text = steps[i].ToString();

            LevelVoices.PlayCount(steps[i]);
            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownScreen != null)
            countdownScreen.SetActive(false);

        RestoreGrabs();
        IsFrozen = false;
        Time.timeScale = 1f;
        countingDown = false;
    }

    void FindPanels()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (settings == null && IsSettingsRoot(transforms[i]))
                settings = transforms[i].gameObject;

            if (countdownScreen == null && transforms[i].name == "CountdownScreen")
                countdownScreen = transforms[i].gameObject;
        }

        if (countdownScreen != null)
            countdownText = countdownScreen.GetComponentInChildren<TMP_Text>(true);

        if (countdownText == null)
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i].gameObject.name == "Text_Countdown")
                {
                    countdownText = labels[i];
                    break;
                }
            }
        }
    }

    void WireButtons()
    {
        if (settings == null)
            return;

        Button[] buttons = settings.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string name = button.gameObject.name;
            if (name == "Resume" || name == "Continue" || name == "Back")
                button.onClick.AddListener(Resume);
            else if (name == "Menu" || name == "Exit" || name == "Quit")
                button.onClick.AddListener(GoToMenu);
        }
    }

    static bool IsSettingsRoot(Transform t)
    {
        if (t.name != "Settings" && t.name != "Settings_Window")
            return false;
        return t.GetComponent<Canvas>() != null;
    }

    void HideGameElements()
    {
        hiddenObjects.Clear();
        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (!go.activeSelf || ShouldKeep(go))
                continue;

            hiddenObjects.Add(go);
            go.SetActive(false);
        }
    }

    void RestoreGameElements()
    {
        for (int i = 0; i < hiddenObjects.Count; i++)
        {
            if (hiddenObjects[i] != null)
                hiddenObjects[i].SetActive(true);
        }

        hiddenObjects.Clear();
    }

    bool ShouldKeep(GameObject go)
    {
        if (go == gameObject || go == settings)
            return true;

        if (settings != null && go.transform.IsChildOf(settings.transform))
            return true;

        string n = go.name;
        if (n == "EventSystem" || n == "XR Interaction Manager" || n == "Directional Light")
            return true;
        if (n == "Neon Backdrop" || n == "Neon Floor")
            return true;
        if (n.Contains("XR Origin") || n.Contains("XR Rig") || n.Contains("OVR") || n.Contains("Camera Rig"))
            return true;
        if (go.GetComponent("XROrigin") != null)
            return true;
        if (go.GetComponent<Light>() != null)
            return true;
        if (go.GetComponent<LevelMusic>() != null || n == "LevelMusic")
            return true;

        Camera headset = Camera.main;
        if (headset != null && headset.transform.root == go.transform)
            return true;

        return false;
    }

    void FreezeGrabs()
    {
        frozenGrabs.Clear();
        DropBall[] balls = FindObjectsByType<DropBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            XRGrabInteractable grab = balls[i].GetComponent<XRGrabInteractable>();
            if (grab == null || !grab.enabled)
                continue;

            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);

            grab.enabled = false;
            frozenGrabs.Add(grab);
        }
    }

    void RestoreGrabs()
    {
        for (int i = 0; i < frozenGrabs.Count; i++)
        {
            if (frozenGrabs[i] != null)
                frozenGrabs[i].enabled = true;
        }

        frozenGrabs.Clear();
    }

    static void PlaceInFront(GameObject panel)
    {
        Camera headset = Camera.main;
        if (headset == null || panel == null)
            return;

        Vector3 forward = headset.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = headset.transform.position + forward * 0.75f;
        position.y = headset.transform.position.y;
        panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }
}
