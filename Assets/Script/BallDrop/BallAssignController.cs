using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BallAssignController : MonoBehaviour
{
    [System.Serializable]
    public class SportBall
    {
        public string ballName;
        public Sprite icon;
        [HideInInspector] public GameObject template;
    }

    [Tooltip("Single pole-screen preview for the next dispensed ball (NDH only).")]
    public Image nonDominantHandImage;
    [HideInInspector] public Image leftHandImage;
    [HideInInspector] public Image rightHandImage;
    public GameObject assignCanvas;
    public GameObject objectiveScreen;
    public GameObject handCanvas;
    public GameObject gameCanvas;
    public BallDropSystem dropSystem;
    public SportBall[] sportsBalls;

    BallDropRound round;
    bool countingDown;
    TMP_Text ndhLabel;

    void Awake()
    {
        ResolveReferences();
        BindTemplates();

        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
        {
            ShowObjective();
            enabled = false;
            return;
        }

        round = GetComponent<BallDropRound>();
        if (round == null)
            round = gameObject.AddComponent<BallDropRound>();
        EnsureStartButton();
        ShowObjective();
        WireButtons();
    }

    void EnsureStartButton()
    {
        BallDropStartButton startButton = GetComponent<BallDropStartButton>();
        if (startButton == null)
            startButton = gameObject.AddComponent<BallDropStartButton>();
        startButton.Bind(this);
    }

    void ResolveReferences()
    {
        if (assignCanvas == null)
            assignCanvas = FindRoot("Ball assign canvas");
        if (objectiveScreen == null)
            objectiveScreen = FindRoot("ObjectiveScreen");
        if (handCanvas == null)
            handCanvas = FindRoot("Select Non Dominant Hand");
        if (handCanvas == null)
            handCanvas = FindRoot("Select hand");
        if (gameCanvas == null)
            gameCanvas = FindRoot("GameCanvas");
        if (dropSystem == null)
            dropSystem = FindFirstObjectByType<BallDropSystem>(FindObjectsInactive.Include);

        if (assignCanvas != null)
        {
            if (nonDominantHandImage == null)
                nonDominantHandImage = FindImageByNames(assignCanvas.transform,
                    "Non Dominant Hand Image", "NonDominantHandImage", "Left Hand", "Right Hand");
            if (nonDominantHandImage == null && leftHandImage != null)
                nonDominantHandImage = leftHandImage;
            if (nonDominantHandImage == null)
                nonDominantHandImage = FindImage(assignCanvas.transform, "Objective_Panel/Left Hand/Left Hand");
            if (nonDominantHandImage == null)
                nonDominantHandImage = FindImage(assignCanvas.transform, "Objective_Panel/Left Hand");

            leftHandImage = nonDominantHandImage;
            rightHandImage = null;

            ndhLabel = FindLabelUnder(assignCanvas.transform, "Non Dominant Hand Text");
            if (ndhLabel != null)
                ndhLabel.text = "Catch this";
        }
    }

    void BindTemplates()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < sportsBalls.Length; i++)
        {
            SportBall sport = sportsBalls[i];
            sport.template = null;

            for (int t = 0; t < transforms.Length; t++)
            {
                Transform candidate = transforms[t];
                if (candidate.parent != null || candidate.name != sport.ballName)
                    continue;

                sport.template = candidate.gameObject;
                sport.template.SetActive(false);
                break;
            }

            if (sport.template == null)
                Debug.LogWarning("Ball assign could not find a scene object named " + sport.ballName);
        }
    }

    void WireButtons()
    {
        Wire(objectiveScreen, "Objective_Panel/Btn_Start", OnObjectiveStartPressed);
        Wire(assignCanvas, "Btn_Start", StartGame);
    }

    void OnObjectiveStartPressed()
    {
        // Color Sort pattern: first Start unlocks walk + Dialogue circle; game starts later via pole StartButton.
        if (BallDropIntroDirector.TryConsumeStartButton())
            return;

        StartFromObjective();
    }

    public void ShowObjective()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(true);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (assignCanvas != null)
            assignCanvas.SetActive(false);
        if (gameCanvas != null)
            gameCanvas.SetActive(false);
        if (round != null)
            round.HideResults();

        ScreenHudFit hud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (hud != null)
            hud.HideGameplay();
        if (round != null)
            round.HideWristHud();
    }

    public void StartFromObjective()
    {
        ScoringChecker.Ensure().TryContinue(StartFromObjectiveAfterCheck);
    }

    public void ShowHandSelect()
    {
        StartFromObjective();
    }

    void StartFromObjectiveAfterCheck()
    {
        ApplyProfileHand();
        ShowAssign();
    }

    public void ChooseLeftHand()
    {
        StartFromObjective();
    }

    public void ChooseRightHand()
    {
        StartFromObjective();
    }

    void ApplyProfileHand()
    {
        ProfileManager.TryGetNonDominantLeft(out bool leftHand);
        if (round != null)
            round.SetNonDominantLeft(leftHand);
    }

    public void ShowAssign()
    {
        ShowRandomPreview();

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (assignCanvas != null)
            assignCanvas.SetActive(true);
    }

    public void StartGame()
    {
        if (countingDown)
            return;
        if (!BallDropIntroDirector.IsTrainingUnlocked)
            return;

        ScoringChecker.Ensure().TryContinue(StartGameAfterCheck);
    }

    /// <summary>
    /// World StartButton poke: apply profile hand, preview next ball, then countdown + drop.
    /// </summary>
    public void StartFromPoleButton()
    {
        if (countingDown)
            return;
        if (round != null && round.running)
            return;
        if (!BallDropIntroDirector.IsTrainingUnlocked)
            return;

        ScoringChecker.Ensure().TryContinue(StartFromPoleButtonAfterCheck);
    }

    void StartFromPoleButtonAfterCheck()
    {
        if (countingDown)
            return;
        if (round != null && round.running)
            return;

        ApplyProfileHand();
        ShowRandomPreview();

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (assignCanvas != null)
            assignCanvas.SetActive(false);

        StartGameAfterCheck();
    }

    void StartGameAfterCheck()
    {
        if (countingDown)
            return;
        if (round != null && round.running)
            return;

        List<GameObject> templates = CollectTemplates();
        if (templates.Count == 0 || dropSystem == null)
        {
            Debug.LogWarning("Ball assign needs at least one sports ball before the drop can start.");
            return;
        }

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (assignCanvas != null)
            assignCanvas.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (gameCanvas != null)
            gameCanvas.SetActive(false);

        StartCoroutine(CountdownThenStart(templates));
    }

    IEnumerator CountdownThenStart(List<GameObject> templates)
    {
        countingDown = true;
        BallDropWalk walk = BallDropWalk.FindActive();
        if (walk != null)
            walk.SnapToStanceAndLock();

        GameObject screen = FindNamed("CountdownScreen");
        TMP_Text label = FindLabel("Text_Countdown");
        if (screen != null)
            screen.SetActive(true);

        int[] steps = { 3, 2, 1 };
        for (int i = 0; i < steps.Length; i++)
        {
            if (label != null)
                label.text = steps[i].ToString();
            LevelVoices.PlayCount(steps[i]);
            yield return new WaitForSeconds(1f);
        }

        if (screen != null)
            screen.SetActive(false);
        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        if (round != null)
            round.Begin(dropSystem, gameCanvas);

        ShowRandomPreview();
        dropSystem.Begin(this, templates);
        countingDown = false;
    }

    public void ShowUpcomingBall(GameObject template)
    {
        SportBall ball = FindSport(template);
        ApplyIcon(nonDominantHandImage, ball);
        if (ndhLabel != null)
            ndhLabel.text = ball != null && !string.IsNullOrEmpty(ball.ballName)
                ? "Catch this"
                : "Catch this";
    }

    void ShowRandomPreview()
    {
        SportBall ball = PickRandomSport();
        ApplyIcon(nonDominantHandImage, ball);
    }

    SportBall PickRandomSport()
    {
        List<SportBall> available = new List<SportBall>();
        for (int i = 0; i < sportsBalls.Length; i++)
        {
            if (sportsBalls[i] != null && sportsBalls[i].template != null)
                available.Add(sportsBalls[i]);
        }

        if (available.Count == 0)
            return null;

        return available[Random.Range(0, available.Count)];
    }

    SportBall FindSport(GameObject template)
    {
        if (template == null || sportsBalls == null)
            return null;

        string name = template.name;
        for (int i = 0; i < sportsBalls.Length; i++)
        {
            SportBall sport = sportsBalls[i];
            if (sport == null)
                continue;
            if (sport.template == template)
                return sport;
            if (!string.IsNullOrEmpty(sport.ballName) && sport.ballName == name)
                return sport;
        }

        return null;
    }

    List<GameObject> CollectTemplates()
    {
        List<GameObject> templates = new List<GameObject>();
        if (sportsBalls == null)
            return templates;

        for (int i = 0; i < sportsBalls.Length; i++)
        {
            if (sportsBalls[i] != null && sportsBalls[i].template != null)
                templates.Add(sportsBalls[i].template);
        }

        return templates;
    }

    static GameObject FindNamed(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }

    static TMP_Text FindLabel(string objectName)
    {
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }

    static TMP_Text FindLabelUnder(Transform root, string objectName)
    {
        if (root == null)
            return null;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].gameObject.name == objectName)
                return labels[i];
        }

        return null;
    }

    static void ApplyIcon(Image image, SportBall ball)
    {
        if (image == null)
            return;

        image.sprite = ball != null ? ball.icon : null;
        image.preserveAspect = true;
        image.enabled = image.sprite != null;
    }

    static void Wire(GameObject root, string path, UnityEngine.Events.UnityAction action)
    {
        if (root == null)
            return;

        Transform buttonTransform = root.transform.Find(path);
        if (buttonTransform == null)
            return;

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
            return;

        int persistentCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        button.onClick.AddListener(action);
    }

    static Image FindImage(Transform root, string path)
    {
        Transform target = root.Find(path);
        return target != null ? target.GetComponent<Image>() : null;
    }

    static Image FindImageByNames(Transform root, params string[] names)
    {
        if (root == null || names == null)
            return null;

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int n = 0; n < names.Length; n++)
        {
            string want = names[n];
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == want)
                    return images[i];
            }
        }

        return null;
    }

    static GameObject FindRoot(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].parent == null && transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }
}
