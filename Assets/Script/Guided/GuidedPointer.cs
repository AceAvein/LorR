using TMPro;
using UnityEngine;

public class GuidedPointer : MonoBehaviour
{
    Transform glow;
    TextMeshPro label;
    Vector3 glowBase = Vector3.one * 0.2f;

    public static GuidedPointer Attach(Transform target, string word, Vector3 localOffset)
    {
        if (target == null)
            return null;

        GuidedPointer pointer = target.GetComponentInChildren<GuidedPointer>();
        if (pointer == null)
        {
            GameObject root = new GameObject("GuidedPointer");
            root.transform.SetParent(target, false);
            pointer = root.AddComponent<GuidedPointer>();
            pointer.Build();
        }

        pointer.transform.localPosition = localOffset;
        pointer.SetWord(word);
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

    public void SetWord(string word)
    {
        if (label != null)
            label.text = word;
    }

    void Build()
    {
        GameObject glowObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObject.name = "Glow";
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localScale = glowBase;
        Collider glowCollider = glowObject.GetComponent<Collider>();
        if (glowCollider != null)
            Destroy(glowCollider);

        Renderer glowRenderer = glowObject.GetComponent<Renderer>();
        if (glowRenderer != null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = glowRenderer.sharedMaterial != null ? glowRenderer.sharedMaterial.shader : null;

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(1f, 0.85f, 0.15f, 0.45f);
                glowRenderer.material = material;
            }
        }

        glow = glowObject.transform;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        label = labelObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        label.font = font;
        label.fontSize = 2.4f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineWidth = 0.25f;
        label.outlineColor = Color.black;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);
    }

    void LateUpdate()
    {
        if (glow != null)
        {
            float pulse = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 5f);
            glow.localScale = glowBase * pulse;
        }

        if (label == null)
            return;

        Camera view = Camera.main;
        if (view == null)
            return;

        label.transform.rotation = Quaternion.LookRotation(label.transform.position - view.transform.position);
    }
}
