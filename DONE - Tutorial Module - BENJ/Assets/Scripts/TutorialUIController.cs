using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives on the "Benj_Tutorial_WorldSpace" root.
/// Handles: idle stopwatch state on the RIGHT panel, and swapping in the
/// correct guideline JPG (loaded from Resources/) when a LEFT-panel button
/// is pressed.
///
/// Wiring (buttons -> ShowGuideline("<resource name>")) is done automatically
/// by BenjWorldSpaceTutorialBuilder.cs when you run:
///   Benj UI World Space -> Build Reference Tutorial
/// You normally never need to touch this script's Inspector fields by hand.
/// </summary>
public class TutorialUIController : MonoBehaviour
{
    [Header("Right panel references (auto-assigned by the builder)")]
    public GameObject idleStopwatchIcon;
    public Image guidelineImage;

    [Header("Resource name mapping (edit here later if needed)")]
    [Tooltip("Currently 'ColorSort' points at 'Object Placement Drill' " +
             "because 'Color Sort.jpg' does not exist yet. " +
             "Change the resourceName below to 'Color Sort' once the art lands.")]
    public GuidelineMapping[] mappings = new GuidelineMapping[]
    {
        new GuidelineMapping { buttonId = "BasicControls", resourceName = "Basic Controls" },
        new GuidelineMapping { buttonId = "BallDrop",       resourceName = "Ball Drop Reflex Drill" },
        new GuidelineMapping { buttonId = "ColorSort",      resourceName = "Object Placement Drill" }, // TODO: swap to "Color Sort" when the art exists
        new GuidelineMapping { buttonId = "ReactionLight",  resourceName = "Reaction Light Burst Drill" },
        new GuidelineMapping { buttonId = "PenTracing",     resourceName = "Pen Control Tracing" },
    };

    [System.Serializable]
    public class GuidelineMapping
    {
        public string buttonId;
        public string resourceName; // filename inside Assets/Resources/, without extension
    }

    void Awake()
    {
        ResetToIdle();
    }

    /// <summary>
    /// Called by a button's OnClick (wired up by the builder script).
    /// Loads Assets/Resources/&lt;resourceName&gt;.jpg (imported as a Sprite)
    /// and displays it, centered and aspect-preserved, in the right panel.
    /// </summary>
    public void ShowGuideline(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            Debug.LogWarning("[TutorialUIController] ShowGuideline called with empty resource name.");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(resourceName);

        if (sprite == null)
        {
            Debug.LogWarning($"[TutorialUIController] Could not find a Sprite named '{resourceName}' " +
                              $"in any Resources folder. Make sure 'Assets/Resources/{resourceName}.jpg' " +
                              $"exists and its Texture Type is set to 'Sprite (2D and UI)'.");
            return;
        }

        if (idleStopwatchIcon != null)
            idleStopwatchIcon.SetActive(false);

        if (guidelineImage != null)
        {
            guidelineImage.gameObject.SetActive(true);
            guidelineImage.sprite = sprite;
            guidelineImage.preserveAspect = true;
        }
    }

    /// <summary>
    /// Convenience overload: pass the buttonId from the mappings list above
    /// instead of the raw resource filename.
    /// </summary>
    public void ShowGuidelineById(string buttonId)
    {
        foreach (var m in mappings)
        {
            if (m.buttonId == buttonId)
            {
                ShowGuideline(m.resourceName);
                return;
            }
        }
        Debug.LogWarning($"[TutorialUIController] No mapping found for buttonId '{buttonId}'.");
    }

    /// <summary>
    /// Hides the guideline preview and shows the idle stopwatch again.
    /// Wired to the Back button by the builder script.
    /// </summary>
    public void ResetToIdle()
    {
        if (guidelineImage != null)
        {
            guidelineImage.sprite = null;
            guidelineImage.gameObject.SetActive(false);
        }
        if (idleStopwatchIcon != null)
            idleStopwatchIcon.SetActive(true);
    }
}
