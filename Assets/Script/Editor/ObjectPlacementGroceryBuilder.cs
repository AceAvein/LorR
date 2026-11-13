#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes Phase I stylized grocery prefabs with durable URP material asset refs
/// (avoids magenta Missing Material in Project thumbnails).
/// Menu: LorR / Object Placement / Build Grocery Prefabs
/// </summary>
public static class ObjectPlacementGroceryBuilder
{
    const string PrefabFolder = "Assets/Scenes/Scene Simulation/Object Placement/Prefabs/Groceries";
    const string MaterialsFolder = "Assets/Scenes/Scene Simulation/Object Placement/Materials";
    const string ResourcesFolder = "Assets/Resources/ObjectPlacement";

    [MenuItem("LorR/Object Placement/Build Grocery Prefabs")]
    public static void BuildPrefabs()
    {
        EnsureFolders();

        Material fruit = EnsureMat("OP_FruitYellow", new Color(0.95f, 0.82f, 0.12f), 0.15f, 0.55f);
        Material veg = EnsureMat("OP_VegRed", new Color(0.82f, 0.12f, 0.10f), 0.12f, 0.5f);
        Material leaf = EnsureMat("OP_LeafGreen", new Color(0.22f, 0.62f, 0.18f), 0.08f, 0.4f);
        Material stem = EnsureMat("OP_StemBrown", new Color(0.35f, 0.22f, 0.10f), 0.05f, 0.3f);

        // Keep Resources copies in sync for runtime Resources.Load.
        MirrorToResources(fruit);
        MirrorToResources(veg);
        MirrorToResources(leaf);
        MirrorToResources(stem);

        ObjectPlacementGroceryFactory.UseSharedMaterials(fruit, veg, leaf, stem);

        BuildList(ObjectPlacementGroceryCatalog.FruitIds, ObjectPlacementGroceryKind.Fruit);
        BuildList(ObjectPlacementGroceryCatalog.VegetableIds, ObjectPlacementGroceryKind.Vegetable);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Object Placement: rebuilt grocery prefabs with URP materials in " + PrefabFolder);
    }

    static void BuildList(string[] ids, ObjectPlacementGroceryKind kind)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            GameObject go = ObjectPlacementGroceryFactory.Create(id, kind);
            string path = PrefabFolder + "/" + go.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Scene Simulation/Object Placement"))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Object Placement");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Scene Simulation/Object Placement/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation/Object Placement", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation/Object Placement/Prefabs", "Groceries");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation/Object Placement", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "ObjectPlacement");
    }

    static Material EnsureMat(string name, Color color, float metallic, float smooth)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            Apply(mat, color, metallic, smooth);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        if (mat.shader == null || mat.shader.name.Contains("Error") || mat.shader.name.Contains("Hidden/InternalErrorShader"))
            mat.shader = shader;

        Apply(mat, color, metallic, smooth);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Apply(Material mat, Color color, float metallic, float smooth)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", smooth);
    }

    static void MirrorToResources(Material source)
    {
        if (source == null)
            return;

        string path = ResourcesFolder + "/" + source.name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null)
        {
            Material copy = new Material(source);
            copy.name = source.name;
            AssetDatabase.CreateAsset(copy, path);
            return;
        }

        existing.shader = source.shader;
        existing.CopyPropertiesFromMaterial(source);
        EditorUtility.SetDirty(existing);
    }
}
#endif
