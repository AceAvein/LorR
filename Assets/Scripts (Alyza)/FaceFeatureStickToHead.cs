using UnityEngine;

/// <summary>
/// Forces nose/mouth to stay parented under the character Head bone.
/// Uses worldPositionStays so your Scene-view fit is preserved.
/// </summary>
[DisallowMultipleComponent]
public class FaceFeatureStickToHead : MonoBehaviour
{
    [SerializeField] Transform head;
    [Tooltip("Male Doctor (1) or character root that owns the Head bone.")]
    [SerializeField] Transform characterRoot;
    [SerializeField] bool stickOnStart = true;
    [SerializeField] bool stickEveryFrame = true;

    void Start()
    {
        if (stickOnStart)
            StickToHead();
    }

    void LateUpdate()
    {
        if (!stickEveryFrame || !Application.isPlaying)
            return;

        if (head == null)
            ResolveHead();

        if (head != null && transform.parent != head)
            StickToHead();
    }

    [ContextMenu("Stick To Head Now")]
    public void StickToHead()
    {
        ResolveHead();
        if (head == null)
        {
            Debug.LogWarning("FaceFeatureStickToHead: Head bone not found for " + name, this);
            return;
        }

        // Keep the pose you fitted in the Scene view.
        transform.SetParent(head, true);
    }

    void ResolveHead()
    {
        if (head != null)
            return;

        Transform root = characterRoot != null ? characterRoot : transform.root;

        // Prefer Head under the same character that has BaseBody (Doctor), not Old Man.
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.name != "BaseBody")
                continue;

            Transform t = smr.transform;
            while (t != null && t.parent != null && t.parent != root)
                t = t.parent;

            Transform searchRoot = t != null ? t : root;
            foreach (Transform bone in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Head")
                {
                    head = bone;
                    return;
                }
            }
        }

        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != "Head")
                continue;
            head = bone;
            return;
        }
    }
}
