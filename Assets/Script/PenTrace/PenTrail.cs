using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenTrail : MonoBehaviour
{
    public Transform penTip;
    public LineRenderer trail;

    public InputActionReference indexButton;
    public InputActionReference rightIndexButton;

    public static PenTrail Active { get; private set; }

    InputAction drawAction;
    bool useRightHand;
    bool wasPressed;
    bool strokeOpen;
    Color inkColor = Color.white;
    readonly List<GameObject> finishedStrokes = new List<GameObject>();
    const float PointSpacing = 0.004f;

    void OnEnable()
    {
        Active = this;
    }

    void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    void Start()
    {
        if (trail != null)
            trail.positionCount = 0;

        BindDrawTrigger();
    }

    public void UseRightHand(bool rightHand)
    {
        useRightHand = rightHand;
        BindDrawTrigger();
    }

    public bool IsDrawing()
    {
        return drawAction != null && drawAction.IsPressed();
    }

    public void SetInkColor(Color color)
    {
        inkColor = color;
        ApplyInkColor(trail);
    }

    public void ClearInk()
    {
        for (int i = 0; i < finishedStrokes.Count; i++)
        {
            if (finishedStrokes[i] != null)
                Destroy(finishedStrokes[i]);
        }

        finishedStrokes.Clear();
        strokeOpen = false;

        if (trail != null)
            trail.positionCount = 0;
    }

    void BindDrawTrigger()
    {
        drawAction?.Dispose();

        string hand = useRightHand ? "{RightHand}" : "{LeftHand}";
        drawAction = new InputAction(type: InputActionType.Button, binding: "<XRController>" + hand + "/{TriggerButton}");
        drawAction.Enable();
    }

    void OnDestroy()
    {
        drawAction?.Dispose();
    }

    void Update()
    {
        if (drawAction == null || trail == null || penTip == null)
            return;

        bool isPressed = drawAction.IsPressed();

        if (isPressed)
        {
            if (!wasPressed || !strokeOpen)
                BeginStroke();

            AddTrailPoint(penTip.position);
        }

        wasPressed = isPressed;
    }

    void BeginStroke()
    {
        if (trail.positionCount > 0)
            ArchiveStroke();

        trail.positionCount = 0;
        strokeOpen = true;
    }

    void ArchiveStroke()
    {
        GameObject strokeObject = new GameObject("Stroke");
        Transform parent = TracingBoardSurface.Current != null
            ? TracingBoardSurface.Current.transform
            : trail.transform.parent;
        strokeObject.transform.SetParent(parent, false);

        LineRenderer copy = strokeObject.AddComponent<LineRenderer>();
        copy.material = trail.material;
        copy.widthCurve = trail.widthCurve;
        copy.widthMultiplier = trail.widthMultiplier;
        copy.numCapVertices = trail.numCapVertices;
        copy.numCornerVertices = trail.numCornerVertices;
        copy.useWorldSpace = true;
        copy.startColor = inkColor;
        copy.endColor = inkColor;
        copy.positionCount = trail.positionCount;

        for (int i = 0; i < trail.positionCount; i++)
            copy.SetPosition(i, trail.GetPosition(i));

        finishedStrokes.Add(strokeObject);
    }

    void AddTrailPoint(Vector3 onBoard)
    {
        if (trail.positionCount > 0)
        {
            Vector3 last = trail.GetPosition(trail.positionCount - 1);
            if (Vector3.Distance(last, onBoard) < PointSpacing)
                return;
        }

        trail.positionCount++;
        trail.SetPosition(trail.positionCount - 1, onBoard);
    }

    void ApplyInkColor(LineRenderer line)
    {
        if (line == null)
            return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = inkColor;
            line.material = material;
        }

        line.startColor = inkColor;
        line.endColor = inkColor;
    }
}
