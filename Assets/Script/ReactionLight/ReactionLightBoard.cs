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

    [Tooltip("Console TMP named ResultLabel — Great / Wrong / Oops go here.")]
    public TMP_Text resultLabel;

    bool showingSetComplete;
    bool showingResultFeedback;

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

    /// <summary>Only ResultLabel — never StatusLabel (that stays for set-complete if assigned).</summary>
    public TMP_Text ResolveResultLabel()
    {
        if (resultLabel != null)
            return resultLabel;

        // Prefer the scene object named ResultLabel (even if inactive).
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != "ResultLabel")
                continue;

            TMP_Text tmp = transforms[i].GetComponent<TMP_Text>();
            if (tmp == null)
                tmp = transforms[i].GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                resultLabel = tmp;
                return resultLabel;
            }
        }

        return null;
    }

    public void SetTimeSeconds(float seconds)
    {
        // While Great/Wrong is up on ResultLabel, don't overwrite with TIME.
        if (showingResultFeedback || showingSetComplete)
            return;

        TMP_Text label = timeLabel != null ? timeLabel : ResolveResultLabel();
        if (label == null)
            return;

        int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
        label.gameObject.SetActive(true);
        label.color = Color.white;
        label.text = "TIME: " + whole;
    }

    /// <summary>Show Great / Wrong / Oops on ResultLabel only.</summary>
    public void ShowResultFeedback(string text)
    {
        TMP_Text label = ResolveResultLabel();
        if (label == null)
            return;

        showingSetComplete = false;
        showingResultFeedback = true;
        label.gameObject.SetActive(true);
        label.text = text;

        if (text == "Great")
            label.color = new Color(0.2f, 0.95f, 0.35f);
        else if (text == "Wrong" || text == "Oops")
            label.color = new Color(1f, 0.25f, 0.2f);
        else
            label.color = Color.white;
    }

    public void ClearResultFeedback()
    {
        if (showingSetComplete)
            return;

        ShowReady();
    }

    public void ShowReady()
    {
        TMP_Text label = ResolveResultLabel();
        if (label == null)
            return;

        showingResultFeedback = false;
        showingSetComplete = false;
        label.gameObject.SetActive(true);
        label.color = new Color(0.45f, 1f, 0.55f, 1f);
        label.text = "Ready";
    }

    public void ShowSetComplete(bool show)
    {
        TMP_Text label = statusLabel != null ? statusLabel : ResolveResultLabel();
        if (label == null)
            return;

        showingSetComplete = show;
        if (show)
            showingResultFeedback = false;
        label.gameObject.SetActive(show || label == resultLabel || label == timeLabel);
        if (show)
        {
            label.color = Color.white;
            label.text = "SET COMPLETE! PROCEED";
        }
        else if (!showingResultFeedback)
        {
            label.text = string.Empty;
        }
    }
}
