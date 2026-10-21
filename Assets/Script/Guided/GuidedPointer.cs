using TMPro;
using UnityEngine;

public class GuidedPointer : MonoBehaviour
{
    Transform target;
    Transform glow;
    TextMeshPro label;
    float glowSize = 0.16f;
    float labelGap = 0.1f;
    string word;

    public static GuidedPointer Follow(Transform target, string word, float glowSize, float labelGap)
    {
        if (target == null)
            return null;

        GameObject root = new GameObject("GuidedPointer");
        GuidedPointer pointer = root.AddComponent<GuidedPointer>();
        pointer.target = target;
        pointer.glowSize = glowSize;
        pointer.labelGap = labelGap;
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

        Renderer glowRenderer = glowObject.GetComponent<Renderer>();
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
                material.color = new Color(1f, 0.85f, 0.15f, 0.4f);
                glowRenderer.material = material;
            }
        }

        glow = glowObject.transform;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        label = labelObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        label.font = font;
        label.fontSize = 0.14f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineWidth = 0.22f;
        label.outlineColor = Color.black;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(0.55f, 0.16f);
    }

    void LateUpdate()
    {
        Snap();
    }

    void Snap()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetPositionAndRotation(target.position, Quaternion.identity);
        transform.localScale = Vector3.one;

        float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f);
        if (glow != null)
            glow.localScale = Vector3.one * glowSize * pulse;

        if (label == null || !label.gameObject.activeSelf)
            return;

        Camera view = Camera.main;
        Vector3 toCam = Vector3.forward;
        if (view != null)
        {
            toCam = view.transform.position - target.position;
            if (toCam.sqrMagnitude < 0.0001f)
                toCam = view.transform.forward * -1f;
            toCam.Normalize();
        }

        label.transform.position = target.position + toCam * labelGap + Vector3.up * (glowSize * 0.35f);
        if (view != null)
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - view.transform.position);
    }
}
