using UnityEngine;

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
    }

    public void Open()
    {
        if (countdown != null && countdown.objectiveScreen != null)
            countdown.objectiveScreen.SetActive(false);

        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);

        if (selectHandCanvas != null)
            selectHandCanvas.SetActive(true);
    }

    public void ChooseLeftHand()
    {
        ChooseHand(false);
    }

    public void ChooseRightHand()
    {
        ChooseHand(true);
    }

    void ChooseHand(bool rightHand)
    {
        useRightHand = rightHand;

        Transform hand = PlayerHands.WritingController(rightHand);
        if (hand != null)
        {
            PlacePen(pencil, pencilPosition, pencilRotation, hand, pencilTip);
            PlacePen(ballpen, ballpenPosition, ballpenRotation, hand, ballpenTip);
            PlacePen(highlighter, highlighterPosition, highlighterRotation, hand, highlighterTip);
        }

        PlayerHands.SetWritingHand(rightHand);

        PenTrail trail = FindAnyObjectByType<PenTrail>(FindObjectsInactive.Include);
        if (trail != null)
            trail.UseRightHand(rightHand);

        if (selectHandCanvas != null)
            selectHandCanvas.SetActive(false);

        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(true);
    }

    public void ChoosePencil()
    {
        Choose(pencil, pencilTip);
    }

    public void ChooseBallpen()
    {
        Choose(ballpen, ballpenTip);
    }

    public void ChooseHighlighter()
    {
        Choose(highlighter, highlighterTip);
    }

    void Choose(GameObject pen, Vector3 tip)
    {
        ShowOnly(pen);

        if (penTip != null && pen != null)
        {
            penTip.SetParent(pen.transform, false);
            penTip.localPosition = tip;
            penTip.localRotation = Quaternion.identity;

            float scale = pen.transform.localScale.x;
            if (scale > 0.0001f)
                penTip.localScale = Vector3.one * (1f / scale);
        }

        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);

        if (countdown != null)
            countdown.StartCountdown();
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
