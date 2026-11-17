using UnityEngine;

/// <summary>
/// Ensures LP_Nose / LP_Mouth always have a visible low-poly mesh in Edit + Play mode.
/// Does not move the transform — safe for manual face fitting in the Scene view.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FaceFeatureProceduralMesh : MonoBehaviour
{
    public enum FeatureKind
    {
        Nose,
        Mouth
    }

    [SerializeField] FeatureKind kind = FeatureKind.Nose;
    [SerializeField] Material materialOverride;

    MeshFilter filter;
    MeshRenderer meshRenderer;
    Mesh runtimeMesh;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    [ContextMenu("Rebuild Mesh")]
    public void Apply()
    {
        if (filter == null)
            filter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (runtimeMesh != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMesh);
            else
                DestroyImmediate(runtimeMesh);
            runtimeMesh = null;
        }

        runtimeMesh = kind == FeatureKind.Nose ? BuildNoseMesh() : BuildMouthMesh();
        runtimeMesh.hideFlags = HideFlags.DontSave;
        filter.sharedMesh = runtimeMesh;

        if (materialOverride != null)
            meshRenderer.sharedMaterial = materialOverride;
        else if (meshRenderer.sharedMaterial == null)
            meshRenderer.sharedMaterial = CreateFallbackMaterial(kind);

        meshRenderer.forceRenderingOff = false;
        meshRenderer.enabled = true;
    }

    void OnDestroy()
    {
        if (runtimeMesh == null)
            return;
        if (Application.isPlaying)
            Destroy(runtimeMesh);
        else
            DestroyImmediate(runtimeMesh);
    }

    static Material CreateFallbackMaterial(FeatureKind featureKind)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader)
        {
            name = featureKind == FeatureKind.Nose ? "LP_Nose_Fallback" : "LP_Mouth_Fallback",
            hideFlags = HideFlags.DontSave
        };
        Color color = featureKind == FeatureKind.Nose
            ? new Color(0.93f, 0.86f, 0.64f, 1f)
            : new Color(0.42f, 0.18f, 0.14f, 1f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        return mat;
    }

    static Mesh BuildNoseMesh()
    {
        var mesh = new Mesh { name = "LP_NoseMesh" };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0.45f, -0.2f),
            new Vector3(-0.45f, -0.1f, -0.15f),
            new Vector3(0.45f, -0.1f, -0.15f),
            new Vector3(0f, -0.05f, 1.0f),
            new Vector3(0f, -0.5f, -0.05f),
        };
        mesh.triangles = new[]
        {
            0, 1, 3, 0, 3, 2,
            1, 4, 3, 2, 3, 4,
            0, 2, 4, 0, 4, 1,
            // backfaces so it stays visible from both sides
            3, 1, 0, 2, 3, 0,
            3, 4, 1, 4, 3, 2,
            4, 2, 0, 1, 4, 0,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildMouthMesh()
    {
        var mesh = new Mesh { name = "LP_MouthMesh" };
        float w = 0.85f;
        float h = 0.2f;
        float d = 0.25f;
        mesh.vertices = new[]
        {
            new Vector3(-w, 0.12f, 0f),
            new Vector3(-w * 0.4f, -0.05f, d * 0.5f),
            new Vector3(0f, -h, d),
            new Vector3(w * 0.4f, -0.05f, d * 0.5f),
            new Vector3(w, 0.12f, 0f),
            new Vector3(-w, 0.28f, -0.05f),
            new Vector3(0f, 0.14f, d * 0.45f),
            new Vector3(w, 0.28f, -0.05f),
        };
        mesh.triangles = new[]
        {
            0, 1, 5, 1, 6, 5, 1, 2, 6, 2, 3, 6, 3, 7, 6, 3, 4, 7,
            5, 6, 7, 0, 5, 7, 0, 7, 4,
            0, 2, 1, 0, 4, 2, 4, 3, 2,
            // reverse
            5, 1, 0, 5, 6, 1, 6, 2, 1, 6, 3, 2, 6, 7, 3, 7, 4, 3,
            7, 6, 5, 7, 5, 0, 4, 7, 0,
            1, 2, 0, 2, 4, 0, 2, 3, 4,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
