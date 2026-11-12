using UnityEngine;

/// <summary>
/// Keeps a face prop editable under the character root in the Scene view,
/// then parents it to the Head bone at play (world pose preserved).
/// </summary>
public class FaceFeatureAnchor : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] bool parentOnPlay = true;

    void Start()
    {
        if (!parentOnPlay)
            return;

        if (head == null)
        {
            foreach (Transform t in transform.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Head")
                {
                    head = t;
                    break;
                }
            }
        }

        if (head != null)
            transform.SetParent(head, true);
    }
}
