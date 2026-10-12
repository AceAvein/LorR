using UnityEngine;

public class PenSelect : MonoBehaviour
{
    public GameObject selectPenCanvas;
    public Countdown countdown;

    public GameObject pencil;
    public GameObject ballpen;
    public GameObject highlighter;

    public Transform penTip;
    public Vector3 pencilTip = new Vector3(0f, 3.035f, -0.267f);
    public Vector3 ballpenTip = new Vector3(-0.006f, 1.014f, 0.013f);
    public Vector3 highlighterTip = new Vector3(-1.292f, 0f, 0.04f);

    void Awake()
    {
        if (selectPenCanvas != null)
            selectPenCanvas.SetActive(false);

        ShowOnly(null);
    }

    public void Open()
    {
        if (countdown != null && countdown.objectiveScreen != null)
            countdown.objectiveScreen.SetActive(false);

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
