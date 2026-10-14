using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

    public Image leftHandImage;
    public Image rightHandImage;
    public GameObject assignCanvas;
    public GameObject objectiveScreen;
    public GameObject handCanvas;
    public GameObject gameCanvas;
    public BallDropSystem dropSystem;
    public SportBall[] sportsBalls;

    SportBall leftBall;
    SportBall rightBall;
    BallDropRound round;

    void Awake()
    {
        ResolveReferences();
        BindTemplates();
        round = GetComponent<BallDropRound>();
        if (round == null)
            round = gameObject.AddComponent<BallDropRound>();
        ShowObjective();
        WireButtons();
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
            if (leftHandImage == null)
                leftHandImage = FindImage(assignCanvas.transform, "Objective_Panel/Left Hand/Left Hand");
            if (rightHandImage == null)
                rightHandImage = FindImage(assignCanvas.transform, "Objective_Panel/Right Hand/Right Hand");
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
        Wire(objectiveScreen, "Objective_Panel/Btn_Start", ShowHandSelect);
        Wire(handCanvas, "Objective_Panel/Left Hand", ChooseLeftHand);
        Wire(handCanvas, "Objective_Panel/Right Hand", ChooseRightHand);
        Wire(assignCanvas, "Btn_Start", StartGame);
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

    public void ShowHandSelect()
    {
        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(true);
        if (assignCanvas != null)
            assignCanvas.SetActive(false);
        if (gameCanvas != null)
            gameCanvas.SetActive(false);
    }

    public void ChooseLeftHand()
    {
        if (round != null)
            round.SetNonDominantLeft(true);
        ShowAssign();
    }

    public void ChooseRightHand()
    {
        if (round != null)
            round.SetNonDominantLeft(false);
        ShowAssign();
    }

    public void ShowAssign()
    {
        PickPair();
        ShowIcons();

        if (objectiveScreen != null)
            objectiveScreen.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (assignCanvas != null)
            assignCanvas.SetActive(true);
    }

    public void StartGame()
    {
        if (leftBall == null || rightBall == null)
            PickPair();

        if (leftBall == null || rightBall == null || dropSystem == null)
        {
            Debug.LogWarning("Ball assign needs two sports balls before the drop can start.");
            return;
        }

        if (assignCanvas != null)
            assignCanvas.SetActive(false);
        if (handCanvas != null)
            handCanvas.SetActive(false);
        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        if (round != null)
            round.Begin(dropSystem, gameCanvas);

        dropSystem.Begin(leftBall.template, rightBall.template);
    }

    void PickPair()
    {
        List<SportBall> available = new List<SportBall>();
        for (int i = 0; i < sportsBalls.Length; i++)
        {
            if (sportsBalls[i].template != null)
                available.Add(sportsBalls[i]);
        }

        if (available.Count < 2)
        {
            leftBall = null;
            rightBall = null;
            return;
        }

        int leftIndex = Random.Range(0, available.Count);
        int rightIndex = Random.Range(0, available.Count - 1);
        if (rightIndex >= leftIndex)
            rightIndex++;

        leftBall = available[leftIndex];
        rightBall = available[rightIndex];
    }

    void ShowIcons()
    {
        ApplyIcon(leftHandImage, leftBall);
        ApplyIcon(rightHandImage, rightBall);
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
