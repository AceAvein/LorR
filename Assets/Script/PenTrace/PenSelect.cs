using UnityEngine;
using UnityEngine.SceneManagement;

public class PenSelect : MonoBehaviour
{
    public GameObject selectPenCanvas;
    public GameObject selectHandCanvas;
    public Countdown countdown;

    public GameObject pencil;
    public GameObject ballpen;
    public GameObject highlighter;

    public Transform penTip;
    public Vector3 pencilTip = new Vector3(0f, 3.035f, -0.267f);
    public Vector3 ballpenTip = new Vector3(0.0014f, 1.0702f, 0.0187f);
    public Vector3 highlighterTip = new Vector3(-1.292f, 0f, 0.04f);

    bool useRightHand;
    bool hasPen;
    PenGrabKind equippedKind;
    PenGrabChoice[] grabs;

    public bool WritingWithRightHand
    {
        get { return useRightHand; }
    }

    public bool HasPen
    {
        get { return hasPen; }
    }

    Vector3 pencilPosition;
    Quaternion pencilRotation;
    Vector3 ballpenPosition;
    Quaternion ballpenRotation;
    Vector3 highlighterPosition;
    Quaternion highlighterRotation;

    void Awake()
    {
        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);

        if (selectHandCanvas != null)
            selectHandCanvas.SetActive(false);

        RememberPose(pencil, ref pencilPosition, ref pencilRotation);
        RememberPose(ballpen, ref ballpenPosition, ref ballpenRotation);
        RememberPose(highlighter, ref highlighterPosition, ref highlighterRotation);

        ShowOnly(null);
        ResolveGrabs();
        SetGrabsVisible(false);

        if (!ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            SetPortalVisible(false);
    }

    public void Open()
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        ScoringChecker.Ensure().TryContinue(OpenAfterCheck);
    }

    public void EquipGuidedPencil(bool rightHand)
    {
        useRightHand = rightHand;

        Transform hand = PlayerHands.WritingController(rightHand);
        if (hand != null)
            PlacePen(pencil, pencilPosition, pencilRotation, hand, pencilTip);

        ShowOnly(pencil);
        PlayerHands.SetWritingHand(rightHand);
        hasPen = true;
        equippedKind = PenGrabKind.Pencil;
        ApplyTrailHand(rightHand, PenGrabKind.Pencil);

        if (penTip != null && pencil != null)
        {
            penTip.SetParent(pencil.transform, false);
            penTip.localPosition = pencilTip;
            penTip.localRotation = Quaternion.identity;
            float scale = pencil.transform.localScale.x;
            if (scale > 0.0001f)
                penTip.localScale = Vector3.one * (1f / scale);
        }

        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);
        if (selectHandCanvas != null)
            selectHandCanvas.SetActive(false);
    }

    public void HideGuidedPencil()
    {
        ShowOnly(null);
        hasPen = false;
        PlayerHands.ShowBothHands();
    }

    void OpenAfterCheck()
    {
        if (countdown != null && countdown.objectiveScreen != null)
            countdown.objectiveScreen.SetActive(false);

        if (selectHandCanvas != null)
            selectHandCanvas.SetActive(false);
        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);

        hasPen = false;
        ShowOnly(null);
        PlayerHands.ShowBothHands();
        ResolveGrabs();
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null)
                grabs[i].ReturnHome();
        }

        SetPortalVisible(false);
    }

    public bool TryEquipFromGrab(PenGrabKind kind, bool rightHand)
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return false;

        // Switching pens must use the other hand.
        if (hasPen && useRightHand == rightHand)
            return false;

        if (hasPen && equippedKind != kind)
            ReturnGrabToTable(equippedKind);

        useRightHand = rightHand;
        equippedKind = kind;
        hasPen = true;

        GameObject pen = PenFor(kind);
        Vector3 tip = TipFor(kind);
        Vector3 localPos;
        Quaternion localRot;
        PoseFor(kind, out localPos, out localRot);

        Transform hand = PlayerHands.WritingController(rightHand);
        if (hand != null)
            PlacePen(pen, localPos, localRot, hand, tip);

        ShowOnly(pen);
        PlayerHands.SetWritingHand(rightHand);

        if (penTip != null && pen != null)
        {
            penTip.SetParent(pen.transform, false);
            penTip.localPosition = tip;
            penTip.localRotation = Quaternion.identity;
            float scale = pen.transform.localScale.x;
            if (scale > 0.0001f)
                penTip.localScale = Vector3.one * (1f / scale);
        }

        ApplyTrailHand(rightHand, kind);

        HideGrab(kind);
        RevealPortalAndArm();
        return true;
    }

    void ApplyTrailHand(bool rightHand, PenGrabKind kind)
    {
        Color color = ColorFor(kind);
        PenSounds.Tool tool = ToolFor(kind);

        PenTrail[] trails = FindObjectsByType<PenTrail>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null)
                continue;

            trails[i].UseRightHand(rightHand);
            trails[i].SetInkColor(color);
            trails[i].SetDrawTool(tool);
        }
    }

    // Kept for scene UI hooks / guided leftovers — no longer used in play flow.
    public void ChooseLeftHand() { }
    public void ChooseRightHand() { }
    public void ChoosePencil() { }
    public void ChooseBallpen() { }
    public void ChooseHighlighter() { }

    void RevealPortalAndArm()
    {
        SetPortalVisible(true);

        if (countdown == null)
            return;

        PenTracePortal portal = PenTracePortal.Instance;
        if (portal == null)
            portal = FindFirstObjectByType<PenTracePortal>(FindObjectsInactive.Include);

        if (portal != null)
            portal.ArmForCountdown(countdown);
        else
            countdown.StartCountdown();
    }

    void ReturnGrabToTable(PenGrabKind kind)
    {
        ResolveGrabs();
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null && grabs[i].Kind == kind)
                grabs[i].ReturnHome();
        }
    }

    void HideGrab(PenGrabKind kind)
    {
        ResolveGrabs();
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null && grabs[i].Kind == kind)
                grabs[i].HideOnTable();
        }
    }

    void ResolveGrabs()
    {
        if (grabs != null && grabs.Length > 0)
            return;

        grabs = FindObjectsByType<PenGrabChoice>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (grabs != null && grabs.Length > 0)
            return;

        EnsureGrab("PencilGrab", PenGrabKind.Pencil);
        EnsureGrab("BallpenGrab", PenGrabKind.Ballpen);
        EnsureGrab("HighlighterGrab", PenGrabKind.Highlighter);
        grabs = FindObjectsByType<PenGrabChoice>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    static void EnsureGrab(string objectName, PenGrabKind kind)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != objectName)
                continue;

            PenGrabChoice choice = transforms[i].GetComponent<PenGrabChoice>();
            if (choice == null)
                choice = transforms[i].gameObject.AddComponent<PenGrabChoice>();
            choice.kind = kind;
            return;
        }
    }

    void SetGrabsVisible(bool visible)
    {
        ResolveGrabs();
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] == null)
                continue;

            if (visible)
                grabs[i].ReturnHome();
            else
                grabs[i].HideOnTable();
        }
    }

    static void SetPortalVisible(bool visible)
    {
        PenTracePortal portal = PenTracePortal.Instance;
        if (portal != null)
        {
            portal.SetPortalVisible(visible);
            return;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == "PORTAL")
                transforms[i].gameObject.SetActive(visible);
        }
    }

    GameObject PenFor(PenGrabKind kind)
    {
        if (kind == PenGrabKind.Ballpen)
            return ballpen;
        if (kind == PenGrabKind.Highlighter)
            return highlighter;
        return pencil;
    }

    Vector3 TipFor(PenGrabKind kind)
    {
        if (kind == PenGrabKind.Ballpen)
            return ballpenTip;
        if (kind == PenGrabKind.Highlighter)
            return highlighterTip;
        return pencilTip;
    }

    void PoseFor(PenGrabKind kind, out Vector3 position, out Quaternion rotation)
    {
        if (kind == PenGrabKind.Ballpen)
        {
            position = ballpenPosition;
            rotation = ballpenRotation;
            return;
        }

        if (kind == PenGrabKind.Highlighter)
        {
            position = highlighterPosition;
            rotation = highlighterRotation;
            return;
        }

        position = pencilPosition;
        rotation = pencilRotation;
    }

    static Color ColorFor(PenGrabKind kind)
    {
        if (kind == PenGrabKind.Ballpen)
            return new Color(0.15f, 0.45f, 0.95f);
        if (kind == PenGrabKind.Highlighter)
            return new Color(0.62f, 0.28f, 0.86f);
        return new Color(0.62f, 0.62f, 0.62f);
    }

    static PenSounds.Tool ToolFor(PenGrabKind kind)
    {
        if (kind == PenGrabKind.Ballpen)
            return PenSounds.Tool.Ballpen;
        if (kind == PenGrabKind.Highlighter)
            return PenSounds.Tool.Marker;
        return PenSounds.Tool.Pencil;
    }

    static void RememberPose(GameObject pen, ref Vector3 position, ref Quaternion rotation)
    {
        if (pen == null)
            return;

        position = pen.transform.localPosition;
        rotation = pen.transform.localRotation;
    }

    void PlacePen(GameObject pen, Vector3 position, Quaternion rotation, Transform hand, Vector3 tipLocal)
    {
        if (pen == null)
            return;

        Transform penTransform = pen.transform;
        penTransform.SetParent(hand, false);

        if (!useRightHand)
        {
            penTransform.localPosition = position;
            penTransform.localRotation = rotation;
            return;
        }

        Vector3 scale = penTransform.localScale;
        Vector3 tipOffset = rotation * Vector3.Scale(tipLocal, scale);
        Vector3 mirroredOffset = new Vector3(-tipOffset.x, tipOffset.y, tipOffset.z);

        if (tipOffset.sqrMagnitude < 0.000001f)
        {
            penTransform.localPosition = new Vector3(-position.x, position.y, position.z);
            penTransform.localRotation = rotation;
            return;
        }

        Quaternion rightRotation = Quaternion.FromToRotation(tipOffset, mirroredOffset) * rotation;
        Vector3 rightOffset = rightRotation * Vector3.Scale(tipLocal, scale);
        Vector3 leftTip = position + tipOffset;
        Vector3 rightTip = new Vector3(-leftTip.x, leftTip.y, leftTip.z);

        penTransform.localRotation = rightRotation;
        penTransform.localPosition = rightTip - rightOffset;
    }

    void ShowOnly(GameObject active)
    {
        if (pencil != null)
            pencil.SetActive(pencil == active);

        if (ballpen != null)
            ballpen.SetActive(ballpen == active);

        if (highlighter != null)
            highlighter.SetActive(highlighter == active);
    }
}
