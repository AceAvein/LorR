using TMPro;
using UnityEngine;

/// <summary>
/// Marker for the Object Placement / Color Sort factory station.
/// Box GameObjects must stay named Red Box / Blue Box / Yellow Box for ColorSortRound.
/// Dual conveyors both flow +X (right). Infeed tunnel is on the right; left conveyor has its own mouth.
/// </summary>
public class ObjectPlacementStation : MonoBehaviour
{
    public Transform boxesRoot;
    public Transform redBox;
    public Transform blueBox;
    public Transform yellowBox;
    public Transform startButton;

    [Header("Infeed (right)")]
    public Transform conveyor;
    public Transform supplyBox;
    public Transform grocerySpawnPoint;
    public Transform beltSurface;

    [Header("Outbound / left")]
    public Transform conveyorLeft;
    public Transform supplyBoxLeft;
    public Transform grocerySpawnPointLeft;
    public Transform beltSurfaceLeft;
    [Tooltip("Phase I item stopper at the end of the left belt. Items stop here instead of falling off.")]
    public Transform beltStopperLeft;

    public TMP_Text setScreenLabel;

    void Awake()
    {
        if (GetComponent<ObjectPlacementConveyorController>() == null)
            gameObject.AddComponent<ObjectPlacementConveyorController>();
        ObjectPlacementRubberFlap.WireStationFlaps(this);
    }

    public void SetScreenText(string value)
    {
        if (setScreenLabel == null)
            return;
        setScreenLabel.text = value;
    }
}
