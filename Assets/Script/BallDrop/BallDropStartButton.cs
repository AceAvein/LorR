using System.Collections;
using UnityEngine;

/// <summary>
/// Index-finger poke on pole StartButton → press anim → Ball Drop starts.
/// Mirrors Object Placement world-button poke (tip distance, not XR grab).
/// </summary>
[DisallowMultipleComponent]
public class BallDropStartButton : MonoBehaviour
{
    public float pokeRadius = 0.06f;
    public float buttonPressDepth = 0.018f;
    public float buttonPressSeconds = 0.07f;
    public float buttonReleaseSeconds = 0.14f;

    BallAssignController assign;
    Transform button;
    Vector3 buttonRestLocal;
    bool pressing;
    bool tipWasInsideLeft;
    bool tipWasInsideRight;
    Coroutine pressRoutine;

    public void Bind(BallAssignController controller)
    {
        assign = controller;
        ResolveButton();
    }

    void Awake()
    {
        if (assign == null)
            assign = FindFirstObjectByType<BallAssignController>(FindObjectsInactive.Include);
        ResolveButton();
    }

    void Update()
    {
        if (pressing || button == null || assign == null || !assign.enabled)
            return;

        tipWasInsideRight = CheckTipEdge(FindIndexTip(true), tipWasInsideRight);
        tipWasInsideLeft = CheckTipEdge(FindIndexTip(false), tipWasInsideLeft);
    }

    void ResolveButton()
    {
        if (button != null)
            return;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name != "StartButton")
                continue;

            button = all[i];
            break;
        }

        if (button == null)
            return;

        buttonRestLocal = button.localPosition;

        Collider col = button.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = button.gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = 0.55f;
            capsule.height = 1.2f;
            capsule.direction = 1;
        }

        var simple = button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (simple != null)
            Destroy(simple);

        var grab = button.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            Destroy(grab);

        BallDropStartButtonProxy proxy = button.GetComponent<BallDropStartButtonProxy>();
        if (proxy == null)
            proxy = button.gameObject.AddComponent<BallDropStartButtonProxy>();
        proxy.Bind(this);
    }

    bool CheckTipEdge(Transform tip, bool wasInside)
    {
        if (tip == null)
            return false;

        float dist = Vector3.Distance(tip.position, button.position);
        bool inside = dist <= pokeRadius;
        if (inside && !wasInside)
            TryPress();
        return inside;
    }

    static Transform FindIndexTip(bool rightHand)
    {
        Transform controller = PlayerHands.WritingController(rightHand);
        if (controller == null)
        {
            string want = rightHand ? "Right Controller" : "Left Controller";
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == want)
                {
                    controller = all[i];
                    break;
                }
            }
        }

        if (controller == null)
            return null;

        Transform tip = null;
        Transform[] children = controller.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string n = children[i].name;
            if (n.Contains("index3") || n.Contains("Index Distal") || n.Contains("INDEX_DISTAL"))
                tip = children[i];
        }

        return tip != null ? tip : controller;
    }

    public void TryPress()
    {
        if (pressing || assign == null || !assign.enabled)
            return;

        if (pressRoutine != null)
            StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(PressRoutine());
    }

    IEnumerator PressRoutine()
    {
        pressing = true;

        if (button != null)
        {
            // Parent +Z = into the pole face (wall-mounted press, not vertical).
            Vector3 pressed = buttonRestLocal + Vector3.forward * buttonPressDepth;
            float t = 0f;
            while (t < buttonPressSeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.01f, buttonPressSeconds));
                button.localPosition = Vector3.Lerp(buttonRestLocal, pressed, u);
                yield return null;
            }

            button.localPosition = pressed;
            t = 0f;
            while (t < buttonReleaseSeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.01f, buttonReleaseSeconds));
                float e = 1f - (1f - u) * (1f - u);
                button.localPosition = Vector3.Lerp(pressed, buttonRestLocal, e);
                yield return null;
            }

            button.localPosition = buttonRestLocal;
        }

        assign.StartFromPoleButton();
        pressing = false;
        pressRoutine = null;
    }
}

/// <summary>
/// Editor click fallback on StartButton. VR uses index-finger tip poke.
/// </summary>
public class BallDropStartButtonProxy : MonoBehaviour
{
    BallDropStartButton owner;

    public void Bind(BallDropStartButton controller)
    {
        owner = controller;
    }

    void OnMouseDown()
    {
        if (owner != null)
            owner.TryPress();
    }
}
