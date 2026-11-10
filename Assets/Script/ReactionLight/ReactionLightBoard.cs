using TMPro;
using UnityEngine;

/// <summary>
/// Marks the storyboard Reaction Light generator console and exposes the 12 slot anchors.
/// Lights must be named Ball / Ball (1) ... Ball (11) for ReactionLightRound.FindBalls().
/// </summary>
public class ReactionLightBoard : MonoBehaviour
{
    public const int Columns = 4;
    public const int Rows = 3;
    public const int SlotCount = Columns * Rows;

    [Tooltip("World-space light pads, row-major (left→right, top→bottom).")]
    public Transform[] slots = new Transform[SlotCount];

    public TMP_Text timeLabel;
    public TMP_Text statusLabel;

    public bool HasValidSlots()
    {
        if (slots == null || slots.Length < SlotCount)
            return false;

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null)
                return false;
        }

        return true;
    }

    public void SetTimeSeconds(float seconds)
    {
        if (timeLabel == null)
            return;

        int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
        timeLabel.text = "TIME: " + whole;
    }

    public void ShowSetComplete(bool show)
    {
        if (statusLabel == null)
            return;

        statusLabel.gameObject.SetActive(show);
        if (show)
            statusLabel.text = "SET COMPLETE! PROCEED";
    }
}
