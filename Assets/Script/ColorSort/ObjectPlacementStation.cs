using TMPro;
using UnityEngine;

/// <summary>
/// Marker for the Object Placement / Color Sort factory station.
/// Box GameObjects must stay named Red Box / Blue Box / Yellow Box for ColorSortRound.
/// </summary>
public class ObjectPlacementStation : MonoBehaviour
{
    public Transform boxesRoot;
    public Transform redBox;
    public Transform blueBox;
    public Transform yellowBox;
    public Transform startButton;
    public Transform conveyor;
    public Transform supplyBox;
    public Transform grocerySpawnPoint;
    public TMP_Text setScreenLabel;

    public void SetScreenText(string value)
    {
        if (setScreenLabel == null)
            return;
        setScreenLabel.text = value;
    }
}
