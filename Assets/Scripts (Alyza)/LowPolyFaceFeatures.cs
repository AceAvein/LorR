using System.Collections;
using UnityEngine;

/// <summary>
/// Adds simple low-poly nose + mouth props to a character Head bone.
/// Placement is based on BaseBody world bounds so CharCrafter armature scale (x100) still works.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class LowPolyFaceFeatures : MonoBehaviour
{
    [SerializeField] Transform headOverride;
    [SerializeField] Material skinMaterialOverride;
    [SerializeField] Material mouthMaterialOverride;
    [SerializeField] float featureScale = 1.35f;
    [Tooltip("On when the face points opposite transform.forward (back of head).")]
    [SerializeField] bool flipFaceForward = false;
    [SerializeField] bool rebuildOnEnable = true;
    [SerializeField] bool logDebug = true;

    const string NoseName = "LP_Nose";
    const string MouthName = "LP_Mouth";

    void OnEnable()
    {
        if (!rebuildOnEnable)
            return;

        if (Application.isPlaying)
            StartCoroutine(BuildNextFrame());
        else
            Build();
    }

    IEnumerator BuildNextFrame()
    {
        // Wait until skinned bounds are valid.
        yield return null;
        Build();
    }

    [ContextMenu("Build Face Features")]
    public void Build()
    {
        Transform head = ResolveHead();
        if (head == null)
        {
            Debug.LogWarning("LowPolyFaceFeatures: no Head bone found on " + name, this);
            return;
        }

        ClearExisting(head);

        SkinnedMeshRenderer body = ResolveBody();
        Bounds bounds = body != null ? body.bounds : new Bounds(head.position, Vector3.one * 0.4f);

        Material skin = ResolveSkinMaterial(body);
        Material mouthMat = mouthMaterialOverride != null
            ? mouthMaterialOverride
            : CreateMouthMaterial(skin);

        Vector3 faceForward = ResolveFaceForward();
        // Use world up — Head bone axes are unreliable after Blender export.
        Vector3 faceUp = Vector3.up;

        float bodyHeight = Mathf.Max(bounds.size.y, 0.5f);
        float headSize = bodyHeight * 0.14f * featureScale;
        Vector3 headCenter = new Vector3(bounds.center.x, bounds.min.y + bodyHeight * 0.90f, bounds.center.z);

        // Prefer Head bone height when it sits near the skull.
        if (Vector3.Distance(new Vector3(head.position.x, 0f, head.position.z),
                new Vector3(bounds.center.x, 0f, bounds.center.z)) < bodyHeight * 0.35f)
        {
            headCenter = new Vector3(bounds.center.x, head.position.y, bounds.center.z);
        }

        float stick = Mathf.Max(headSize * 0.55f, bounds.size.z * 0.55f, 0.06f);
        Vector3 noseWorld = headCenter + faceForward * stick - faceUp * (headSize * 0.08f);
        Vector3 mouthWorld = headCenter + faceForward * (stick * 0.92f) - faceUp * (headSize * 0.42f);

        float noseWorldSize = Mathf.Max(headSize * 0.45f, 0.05f);
        float mouthWorldSize = Mathf.Max(headSize * 0.55f, 0.06f);

        CreateFeature(NoseName, head, noseWorld, noseWorldSize, BuildNoseMesh(), skin);
        CreateFeature(MouthName, head, mouthWorld, mouthWorldSize, BuildMouthMesh(), mouthMat);

        if (logDebug)
        {
            Debug.Log(
                $"LowPolyFaceFeatures on '{name}': head={head.name}, bounds={bounds}, " +
                $"forward={faceForward}, nose={noseWorld}, mouth={mouthWorld}, " +
                $"noseSize={noseWorldSize:F3}, mouthSize={mouthWorldSize:F3}",
                this);
        }
    }

    [ContextMenu("Clear Face Features")]
    public void Clear()
    {
        Transform head = ResolveHead();
        if (head != null)
            ClearExisting(head);
    }

    [ContextMenu("Toggle Flip Face Forward + Rebuild")]
    public void ToggleFlipAndRebuild()
    {
        flipFaceForward = !flipFaceForward;
        Build();
    }

    Transform ResolveHead()
    {
        if (headOverride != null)
            return headOverride;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Head")
                return t;
        }

        return null;
    }

    SkinnedMeshRenderer ResolveBody()
    {
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.name == "BaseBody")
                return smr;
        }

        SkinnedMeshRenderer best = null;
        float bestVolume = 0f;
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Vector3 e = smr.bounds.size;
            float volume = e.x * e.y * e.z;
            if (volume > bestVolume)
            {
                bestVolume = volume;
                best = smr;
            }
        }

        return best;
    }

    Material ResolveSkinMaterial(SkinnedMeshRenderer body)
    {
        if (skinMaterialOverride != null)
            return skinMaterialOverride;

        if (body != null && body.sharedMaterial != null)
            return body.sharedMaterial;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMaterial != null)
                return smr.sharedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("HDRP/Lit")
                        ?? Shader.Find("Standard");
        return new Material(shader)
        {
            color = new Color(0.93f, 0.86f, 0.64f, 1f)
        };
    }

    static Material CreateMouthMaterial(Material skin)
    {
        Shader shader = skin != null && skin.shader != null
            ? skin.shader
            : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = "LP_Mouth_Mat" };

        Color skinColor = new Color(0.93f, 0.86f, 0.64f, 1f);
        if (skin != null)
        {
            if (skin.HasProperty("_BaseColor"))
                skinColor = skin.GetColor("_BaseColor");
            else if (skin.HasProperty("_Color"))
                skinColor = skin.GetColor("_Color");
        }

        Color mouth = Color.Lerp(skinColor, new Color(0.25f, 0.08f, 0.08f, 1f), 0.8f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", mouth);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", mouth);
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f); // both sides

        return mat;
    }

    Vector3 ResolveFaceForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();
        return flipFaceForward ? -forward : forward;
    }

    void ClearExisting(Transform head)
    {
        // Also clear orphans under this character (from previous wrong parents).
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == NoseName || t.name == MouthName)
                DestroyImmediateSafe(t.gameObject);
        }
    }

    void CreateFeature(string featureName, Transform head, Vector3 worldPos, float worldSize, Mesh mesh, Material material)
    {
        var go = new GameObject(featureName);
        go.layer = gameObject.layer;
        go.transform.SetParent(head, false);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.LookRotation(ResolveFaceForward(), Vector3.up);

        float lossy = Mathf.Max(
            0.0001f,
            (Mathf.Abs(head.lossyScale.x) + Mathf.Abs(head.lossyScale.y) + Mathf.Abs(head.lossyScale.z)) / 3f);
        go.transform.localScale = Vector3.one * (worldSize / lossy);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    static Mesh BuildNoseMesh()
    {
        var mesh = new Mesh { name = "LP_NoseMesh" };
        Vector3[] v =
        {
            new Vector3(0f, 0.45f, -0.2f),
            new Vector3(-0.38f, -0.1f, -0.15f),
            new Vector3(0.38f, -0.1f, -0.15f),
            new Vector3(0f, -0.05f, 0.85f),
            new Vector3(0f, -0.45f, -0.05f),
        };
        int[] t =
        {
            0, 1, 3,
            0, 3, 2,
            1, 4, 3,
            2, 3, 4,
            0, 2, 4,
            0, 4, 1,
        };
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildMouthMesh()
    {
        var mesh = new Mesh { name = "LP_MouthMesh" };
        float w = 0.7f;
        float h = 0.16f;
        float d = 0.18f;
        Vector3[] v =
        {
            new Vector3(-w, 0.1f, 0f),
            new Vector3(-w * 0.4f, -0.05f, d * 0.5f),
            new Vector3(0f, -h, d),
            new Vector3(w * 0.4f, -0.05f, d * 0.5f),
            new Vector3(w, 0.1f, 0f),
            new Vector3(-w, 0.22f, -0.05f),
            new Vector3(0f, 0.12f, d * 0.45f),
            new Vector3(w, 0.22f, -0.05f),
        };
        int[] tris =
        {
            0, 1, 5,
            1, 6, 5,
            1, 2, 6,
            2, 3, 6,
            3, 7, 6,
            3, 4, 7,
            5, 6, 7,
            0, 5, 7,
            0, 7, 4,
            0, 2, 1,
            0, 4, 2,
            4, 3, 2,
        };
        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void DestroyImmediateSafe(Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
