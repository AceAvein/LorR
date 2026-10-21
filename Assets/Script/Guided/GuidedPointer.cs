using TMPro;
using UnityEngine;

public class GuidedPointer : MonoBehaviour
{
    const float MinLabelDistance = 0.55f;

    Transform target;
    Transform glow;
    TextMeshPro label;
    Renderer glowRenderer;
    float glowSize = 0.16f;
    bool wrapBounds;
    string word;

    public static GuidedPointer Follow(Transform target, string word, float glowSize)
    {
        return Create(target, word, glowSize, false);
    }

    public static GuidedPointer FollowMesh(Transform target, string word)
    {
        return Create(target, word, 0.12f, true);
    }

    public static GuidedPointer FollowHand(Transform hand, string word)
    {
        return Create(hand, word, 0.2f, true);
    }

    static GuidedPointer Create(Transform target, string word, float glowSize, bool wrapBounds)
    {
        if (target == null)
            return null;

        GameObject root = new GameObject("GuidedPointer");
        GuidedPointer pointer = root.AddComponent<GuidedPointer>();
        pointer.target = target;
        pointer.glowSize = glowSize;
        pointer.wrapBounds = wrapBounds;
        pointer.Build();
        pointer.SetWord(word);
        pointer.Snap();
        return pointer;
    }

    public static void ClearAll()
    {
        GuidedPointer[] pointers = FindObjectsByType<GuidedPointer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < pointers.Length; i++)
        {
            if (pointers[i] != null)
                Destroy(pointers[i].gameObject);
        }
    }

    public void SetWord(string nextWord)
    {
        word = nextWord;
        if (label == null)
            return;

        bool show = !string.IsNullOrEmpty(word);
        label.gameObject.SetActive(show);
        if (show)
            label.text = word;
    }

    void Build()
    {
        GameObject glowObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObject.name = "Glow";
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = Vector3.zero;
        Collider glowCollider = glowObject.GetComponent<Collider>();
        if (glowCollider != null)
            Destroy(glowCollider);

        glowRenderer = glowObject.GetComponent<Renderer>();
        if (glowRenderer != null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null && glowRenderer.sharedMaterial != null)
                shader = glowRenderer.sharedMaterial.shader;

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(1f, 0.85f, 0.15f, wrapBounds ? 0.32f : 0.4f);
                glowRenderer.material = material;
            }
        }

        glow = glowObject.transform;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(null);
        label = labelObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        label.font = font;
        label.fontSize = 0.85f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineWidth = 0.22f;
        label.outlineColor = Color.black;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(1.1f, 0.3f);
    }

    void LateUpdate()
    {
        Snap();
    }

    void OnDestroy()
    {
        if (label != null)
            Destroy(label.gameObject);
    }

    void Snap()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 center = target.position;
        Vector3 size = Vector3.one * glowSize;
        if (wrapBounds && TryHandBounds(target, out Bounds bounds))
        {
            center = bounds.center;
            size = bounds.size * 1.15f;
            float minAxis = 0.08f;
            size.x = Mathf.Max(size.x, minAxis);
            size.y = Mathf.Max(size.y, minAxis);
            size.z = Mathf.Max(size.z, minAxis);
        }

        transform.SetPositionAndRotation(center, Quaternion.identity);
        transform.localScale = Vector3.one;

        float pulse = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 5f);
        if (glow != null)
            glow.localScale = size * pulse;

        if (label == null || !label.gameObject.activeSelf)
            return;

        PlaceLabel(center, Mathf.Max(size.y, glowSize) * 0.5f);
    }

    void PlaceLabel(Vector3 objectCenter, float objectRadius)
    {
        Camera view = Camera.main;
        Vector3 up = Vector3.up * (objectRadius + 0.07f);
        Vector3 pos = objectCenter + up;

        if (view != null)
        {
            Vector3 away = objectCenter - view.transform.position;
            if (away.sqrMagnitude > 0.0001f)
                pos += away.normalized * 0.05f;

            Vector3 fromEye = pos - view.transform.position;
            float dist = fromEye.magnitude;
            if (dist < MinLabelDistance && dist > 0.0001f)
                pos = view.transform.position + fromEye / dist * MinLabelDistance;

            label.transform.rotation = Quaternion.LookRotation(pos - view.transform.position, Vector3.up);
        }

        label.transform.position = pos;
        label.transform.localScale = Vector3.one;
    }

    static bool TryHandBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;
            if (renderer is LineRenderer || renderer is TrailRenderer)
                continue;
            if (IsUnderHandCanvas(renderer.transform))
                continue;
            if (renderer.GetComponentInParent<GuidedPointer>() != null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static bool IsUnderHandCanvas(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name.Contains("Hand Canvas"))
                return true;

            current = current.parent;
        }

        return false;
    }
}
