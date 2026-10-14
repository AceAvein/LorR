using System.Collections.Generic;
using TMPro;
using UnityEngine;

public struct BallRecord
{
    public bool forLeftHand;
    public int tier;
    public bool caught;
    public float reactionSeconds;
    public int wrongAttempts;
}

public struct BallDropReport
{
    public int caught;
    public int spawned;
    public string nonDominantHand;
    public float accuracy;
    public float averageReaction;
    public int timeAdded;
    public int timeUsed;
    public int timeLeft;
    public int dominantCaught;
    public int dominantSpawned;
    public float dominantAccuracy;
    public int nonDominantCaught;
    public int nonDominantSpawned;
    public float nonDominantAccuracy;
    public int handDifference;
    public int wrongGrabs;
    public string lastBall;
    public float totalScore;
}

public class BallDropRound : MonoBehaviour
{
    public static BallDropRound Instance { get; private set; }

    public float timeLimit = 30f;
    public float catchBonus = 2f;

    public bool nonDominantIsLeft { get; private set; }
    public bool running { get; private set; }

    readonly List<BallRecord> records = new List<BallRecord>();

    TMP_Text timerText;
    readonly List<TMP_Text> levelValues = new List<TMP_Text>();
    readonly List<TMP_Text> timerValues = new List<TMP_Text>();
    readonly List<TMP_Text> ballsValues = new List<TMP_Text>();
    GameObject leftWristHud;
    GameObject rightWristHud;
    readonly List<GameObject> caughtImages = new List<GameObject>();
    readonly List<GameObject> missedImages = new List<GameObject>();
    GameObject resultsScreen;
    GameObject gameCanvas;
    BallDropSystem dropSystem;
    BallDropGameManager hud;

    float timeRemaining;
    float elapsed;
    float feedbackHideAt;
    int spawnedCount;
    int currentTier;
    int wrongGrabs;
    bool ended;
    bool feedbackShowing;

    void Awake()
    {
        Instance = this;
        hud = GetComponent<BallDropGameManager>();
        timerText = FindText("Text_Timer");
        FindTexts("Level Value", levelValues);
        FindTexts("Timer Value", timerValues);
        FindTexts("Balls Value", ballsValues);
        ResolveWristHuds();
        HideWristHud();
        FindNamed("Caught", caughtImages);
        FindNamed("Missed", missedImages);
        resultsScreen = FindRoot("ResultsScreen");
        if (resultsScreen != null)
            resultsScreen.SetActive(false);

        HideFeedback();
        timeRemaining = timeLimit;
        RefreshHud();
    }

    public void SetNonDominantLeft(bool leftHand)
    {
        nonDominantIsLeft = leftHand;
    }

    public void Begin(BallDropSystem system, GameObject canvas)
    {
        dropSystem = system;
        gameCanvas = canvas;
        records.Clear();
        wrongGrabs = 0;
        elapsed = 0f;
        timeRemaining = timeLimit;
        spawnedCount = 0;
        currentTier = 0;
        ended = false;
        running = true;
        LevelVoices.Reset();
        HideFeedback();
        ScreenHudFit screenHud = FindFirstObjectByType<ScreenHudFit>(FindObjectsInactive.Include);
        if (screenHud != null)
            screenHud.ShowGameplay();

        if (hud != null)
            hud.ResetCount();

        ShowWristHud();
        RefreshHud();
    }

    public void HideWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(false);
        if (rightWristHud != null)
            rightWristHud.SetActive(false);
    }

    public void ShowWristHud()
    {
        ResolveWristHuds();
        if (leftWristHud != null)
            leftWristHud.SetActive(nonDominantIsLeft);
        if (rightWristHud != null)
            rightWristHud.SetActive(!nonDominantIsLeft);
    }

    void ResolveWristHuds()
    {
        if (leftWristHud != null && rightWristHud != null)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != "Hand Canvas")
                continue;

            if (IsUnder(transforms[i], "Left Hand Model"))
                leftWristHud = transforms[i].gameObject;
            else if (IsUnder(transforms[i], "Right Hand Model"))
                rightWristHud = transforms[i].gameObject;
        }
    }

    static bool IsUnder(Transform child, string ancestorName)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name == ancestorName)
                return true;

            current = current.parent;
        }

        return false;
    }

    public void BallSpawned(int count, int tier)
    {
        spawnedCount = count;
        currentTier = tier;
        LevelVoices.PlayTier(tier);
        RefreshHud();
    }

    public void HideResults()
    {
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (feedbackShowing && Time.time >= feedbackHideAt)
            HideFeedback();

        if (!running)
            return;

        timeRemaining -= Time.deltaTime;
        elapsed += Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            RefreshHud();
            EndRound();
            return;
        }

        RefreshHud();
    }

    public void WrongGrab(DropBall ball)
    {
        if (!running || ball == null || ball.resolved)
            return;

        ball.wrongAttempts++;
        wrongGrabs++;
    }

    public void Catch(DropBall ball)
    {
        if (ball == null || ball.resolved)
            return;

        ball.resolved = true;
        float reaction = ball.spawnedAt <= 0f ? 0f : Time.time - ball.spawnedAt;
        records.Add(MakeRecord(ball, true, reaction));
        BallDropSounds.PlayCatch();

        if (running)
            timeRemaining += catchBonus;

        if (hud != null)
            hud.AddIntercepted();

        ShowFeedback(true);
        RefreshHud();
        TryFinishDeck();
    }

    public void Miss(DropBall ball)
    {
        if (!running || ball == null || ball.resolved)
            return;

        ball.resolved = true;
        records.Add(MakeRecord(ball, false, 0f));
        BallDropSounds.PlayMiss();
        ShowFeedback(false);
        Destroy(ball.gameObject);
        TryFinishDeck();
    }

    void TryFinishDeck()
    {
        if (!running || records.Count < BallDropSystem.BallTotal)
            return;

        EndRound();
    }

    public void StopSpawning()
    {
        if (dropSystem != null)
            dropSystem.StopSpawning();
    }

    void EndRound()
    {
        if (ended)
            return;

        ended = true;
        running = false;
        StopSpawning();
        ResolveLeftovers();

        if (gameCanvas != null)
            gameCanvas.SetActive(false);

        if (resultsScreen == null)
            resultsScreen = FindRoot("ResultsScreen");

        if (resultsScreen == null)
            return;

        resultsScreen.SetActive(true);
        BallDropResults results = resultsScreen.GetComponent<BallDropResults>();
        if (results == null)
            results = resultsScreen.AddComponent<BallDropResults>();

        results.Show(BuildReport());
    }

    void ResolveLeftovers()
    {
        DropBall[] balls = FindObjectsByType<DropBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            DropBall ball = balls[i];
            if (ball == null || ball.resolved)
                continue;

            ball.resolved = true;
            records.Add(MakeRecord(ball, false, 0f));
            Destroy(ball.gameObject);
        }
    }

    BallDropReport BuildReport()
    {
        int caught = 0;
        int dominantCaught = 0;
        int dominantSpawned = 0;
        int nonDominantCaught = 0;
        int nonDominantSpawned = 0;
        float reactionSum = 0f;
        float score = 0f;

        for (int i = 0; i < records.Count; i++)
        {
            BallRecord record = records[i];
            bool nonDominant = record.forLeftHand == nonDominantIsLeft;

            if (nonDominant)
                nonDominantSpawned++;
            else
                dominantSpawned++;

            if (!record.caught)
                continue;

            caught++;
            reactionSum += record.reactionSeconds;
            score += TierPoints(record.tier);

            if (nonDominant)
                nonDominantCaught++;
            else
                dominantCaught++;
        }

        BallDropReport report = new BallDropReport();
        report.caught = caught;
        report.spawned = records.Count;
        report.nonDominantHand = nonDominantIsLeft ? "Left" : "Right";
        report.accuracy = records.Count == 0 ? 0f : caught * 100f / records.Count;
        report.averageReaction = caught == 0 ? 0f : reactionSum / caught;
        report.timeAdded = Mathf.RoundToInt(caught * catchBonus);
        report.timeUsed = Mathf.Max(0, Mathf.RoundToInt(elapsed));
        report.timeLeft = Mathf.Max(0, Mathf.RoundToInt(timeRemaining));
        report.dominantCaught = dominantCaught;
        report.dominantSpawned = dominantSpawned;
        report.dominantAccuracy = dominantSpawned == 0 ? 0f : dominantCaught * 100f / dominantSpawned;
        report.nonDominantCaught = nonDominantCaught;
        report.nonDominantSpawned = nonDominantSpawned;
        report.nonDominantAccuracy = nonDominantSpawned == 0 ? 0f : nonDominantCaught * 100f / nonDominantSpawned;
        report.handDifference = Mathf.RoundToInt(report.dominantAccuracy) - Mathf.RoundToInt(report.nonDominantAccuracy);
        report.wrongGrabs = wrongGrabs;
        report.lastBall = LastBallText();
        report.totalScore = score;
        return report;
    }

    string LastBallText()
    {
        if (records.Count == 0)
            return "None";

        BallRecord record = records[records.Count - 1];
        string hand = record.forLeftHand == nonDominantIsLeft ? "Non-dominant" : "Dominant";
        string outcome = "Missed";
        if (record.caught)
            outcome = "Correct";
        else if (record.wrongAttempts > 0)
            outcome = "Wrong hand";

        return outcome + " · " + hand;
    }

    static BallRecord MakeRecord(DropBall ball, bool caught, float reaction)
    {
        BallRecord record = new BallRecord();
        record.forLeftHand = ball.forLeftHand;
        record.tier = ball.tier;
        record.caught = caught;
        record.reactionSeconds = reaction;
        record.wrongAttempts = ball.wrongAttempts;
        return record;
    }

    static int TierPoints(int tier)
    {
        if (tier <= 0)
            return 10;
        if (tier == 1)
            return 20;
        return 30;
    }

    void RefreshHud()
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));

        if (timerText == null)
            timerText = FindText("Text_Timer");
        if (timerText != null)
            timerText.text = "Time: " + seconds;

        if (levelValues.Count == 0)
            FindTexts("Level Value", levelValues);
        SetTexts(levelValues, TierName(currentTier));

        if (timerValues.Count == 0)
            FindTexts("Timer Value", timerValues);
        SetTexts(timerValues, seconds.ToString());

        if (ballsValues.Count == 0)
            FindTexts("Balls Value", ballsValues);
        SetTexts(ballsValues, spawnedCount + "/" + BallDropSystem.BallTotal);
    }

    void ShowFeedback(bool caught)
    {
        if (caughtImages.Count == 0)
            FindNamed("Caught", caughtImages);
        if (missedImages.Count == 0)
            FindNamed("Missed", missedImages);

        SetActive(caughtImages, caught);
        SetActive(missedImages, !caught);

        feedbackHideAt = Time.time + 1f;
        feedbackShowing = true;
    }

    void HideFeedback()
    {
        feedbackShowing = false;
        SetActive(caughtImages, false);
        SetActive(missedImages, false);
    }

    static string TierName(int tier)
    {
        if (tier <= 0)
            return "Easy";
        if (tier == 1)
            return "Moderate";
        return "Hard";
    }

    static void SetTexts(List<TMP_Text> labels, string value)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
                labels[i].text = value;
        }
    }

    static void FindTexts(string objectName, List<TMP_Text> results)
    {
        results.Clear();
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                results.Add(labels[i]);
        }
    }

    static TMP_Text FindText(string objectName)
    {
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].gameObject.name == objectName)
                return labels[i];
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

    static void FindNamed(string objectName, List<GameObject> results)
    {
        results.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                results.Add(transforms[i].gameObject);
        }
    }

    static void SetActive(List<GameObject> images, bool active)
    {
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null)
                images[i].SetActive(active);
        }
    }
}
