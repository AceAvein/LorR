using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
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
    readonly List<RayBackup> rayBackups = new List<RayBackup>();

    struct RayBackup
    {
        public NearFarInteractor nearFar;
        public bool nearCasting;
        public bool blockUI;
        public CurveInteractionCaster farCaster;
        public float castDistance;
        public int hitType;
        public CurveVisualController curve;
        public float restLength;
        public float maxVisual;
        public bool extendEmpty;
        public int dynamicsMode;
        public XRRayInteractor ray;
        public float maxRay;
        public int rayHitType;
        public XRInteractorLineVisual line;
        public bool autoAdjust;
        public float lineLength;
        public float minLine;
        public bool stopFirst;
    }

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

        if (!ModuleScenes.IsPlayModule(scene.name))
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

        RestoreUiRays();
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
        StretchUiRays();
        settings.SetActive(true);
    }

    public void Resume()
    {
        if (countingDown || settings == null)
            return;

        settings.SetActive(false);
        RestoreUiRays();
        RestoreGameElements();
        StartCoroutine(CountdownThenContinue());
    }

    public void GoToMenu()
    {
        RestoreUiRays();
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
            if (ModuleScenes.IsReactionLight(sceneName) || ModuleScenes.IsColorSort(sceneName))
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
        if (n == "GameManager" || n == "BallDropSystem" || n == "PenTracingArea" || n == "PathManager")
            return true;
        if (n == "CountdownScreen" || n == "GuidedLesson" || n == "GuidedStepCanvas")
            return true;
        if (n.Contains("XR Origin") || n.Contains("XR Rig") || n.Contains("OVR") || n.Contains("Camera Rig"))
            return true;
        if (go.GetComponent("XROrigin") != null)
            return true;
        if (go.GetComponent<Light>() != null)
            return true;
        if (go.GetComponent<LevelMusic>() != null || n == "LevelMusic")
            return true;
        if (go.GetComponent<BallDropSystem>() != null || go.GetComponent<BallDropRound>() != null)
            return true;
        if (go.GetComponent<GuidedBallDropLesson>() != null || go.GetComponent<GuidedLesson>() != null)
            return true;
        if (go.GetComponent<GuidedBallDropVoices>() != null || n == "GuidedBallDropVoices")
            return true;
        if (go.GetComponent<DropBall>() != null)
            return true;
        if (go.GetComponent<PathManager>() != null || go.GetComponent<PatternManager>() != null)
            return true;
        if (go.GetComponent<TracingTimer>() != null || go.GetComponent<Countdown>() != null)
            return true;
        if (go.GetComponent<ColorSortRound>() != null || go.GetComponent<ReactionLightRound>() != null)
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

    void StretchUiRays()
    {
        RestoreUiRays();

        NearFarInteractor[] nearFars = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < nearFars.Length; i++)
        {
            NearFarInteractor nf = nearFars[i];
            RayBackup backup = new RayBackup();
            backup.nearFar = nf;
            backup.nearCasting = nf.enableNearCasting;
            backup.blockUI = nf.blockUIOnInteractableSelection;
            nf.enableNearCasting = false;
            nf.blockUIOnInteractableSelection = false;

            CurveInteractionCaster far = nf.farInteractionCaster as CurveInteractionCaster;
            if (far != null)
            {
                backup.farCaster = far;
                backup.castDistance = far.castDistance;
                backup.hitType = (int)far.hitDetectionType;
                far.castDistance = 30f;
                far.hitDetectionType = CurveInteractionCaster.HitDetectionType.Raycast;
            }

            CurveVisualController curve = nf.GetComponentInChildren<CurveVisualController>(true);
            if (curve != null)
            {
                backup.curve = curve;
                backup.restLength = curve.restingVisualLineLength;
                backup.maxVisual = curve.maxVisualCurveDistance;
                backup.extendEmpty = curve.extendLineToEmptyHit;
                backup.dynamicsMode = (int)curve.lineDynamicsMode;
                curve.restingVisualLineLength = 10f;
                curve.maxVisualCurveDistance = 30f;
                curve.extendLineToEmptyHit = true;
                curve.lineDynamicsMode = LineDynamicsMode.Traditional;
            }

            rayBackups.Add(backup);
        }

        XRRayInteractor[] rays = FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < rays.Length; i++)
        {
            XRRayInteractor ray = rays[i];
            RayBackup backup = new RayBackup();
            backup.ray = ray;
            backup.maxRay = ray.maxRaycastDistance;
            backup.rayHitType = (int)ray.hitDetectionType;
            ray.maxRaycastDistance = 30f;
            ray.hitDetectionType = XRRayInteractor.HitDetectionType.Raycast;

            XRInteractorLineVisual line = ray.GetComponent<XRInteractorLineVisual>();
            if (line != null)
            {
                backup.line = line;
                backup.autoAdjust = line.autoAdjustLineLength;
                backup.lineLength = line.lineLength;
                backup.minLine = line.minLineLength;
                backup.stopFirst = line.stopLineAtFirstRaycastHit;
                line.autoAdjustLineLength = false;
                line.lineLength = 10f;
                line.minLineLength = 10f;
                line.stopLineAtFirstRaycastHit = false;
            }

            rayBackups.Add(backup);
        }
    }

    void RestoreUiRays()
    {
        for (int i = 0; i < rayBackups.Count; i++)
        {
            RayBackup backup = rayBackups[i];
            if (backup.nearFar != null)
            {
                backup.nearFar.enableNearCasting = backup.nearCasting;
                backup.nearFar.blockUIOnInteractableSelection = backup.blockUI;
            }

            if (backup.farCaster != null)
            {
                backup.farCaster.castDistance = backup.castDistance;
                backup.farCaster.hitDetectionType = (CurveInteractionCaster.HitDetectionType)backup.hitType;
            }

            if (backup.curve != null)
            {
                backup.curve.restingVisualLineLength = backup.restLength;
                backup.curve.maxVisualCurveDistance = backup.maxVisual;
                backup.curve.extendLineToEmptyHit = backup.extendEmpty;
                backup.curve.lineDynamicsMode = (LineDynamicsMode)backup.dynamicsMode;
            }

            if (backup.ray != null)
            {
                backup.ray.maxRaycastDistance = backup.maxRay;
                backup.ray.hitDetectionType = (XRRayInteractor.HitDetectionType)backup.rayHitType;
            }

            if (backup.line != null)
            {
                backup.line.autoAdjustLineLength = backup.autoAdjust;
                backup.line.lineLength = backup.lineLength;
                backup.line.minLineLength = backup.minLine;
                backup.line.stopLineAtFirstRaycastHit = backup.stopFirst;
            }
        }

        rayBackups.Clear();
    }

    public static void PlaceInFront(GameObject panel, float distance = 0.75f)
    {
        Camera headset = Camera.main;
        if (headset == null || panel == null)
            return;

        Vector3 forward = headset.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = headset.transform.position + forward * distance;
        position.y = headset.transform.position.y;
        panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
    }
}
