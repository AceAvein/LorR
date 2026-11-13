using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads Phase I grocery meshes from Resources/ObjectPlacement/Items folders.
/// </summary>
public static class ObjectPlacementItemLibrary
{
    const string ResourcesRoot = "ObjectPlacement/Items";

    static GameObject[] fruitPrefabs;
    static GameObject[] vegPrefabs;

    public static void EnsureLoaded()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0)
            fruitPrefabs = LoadFolder(ObjectPlacementGroceryCatalog.CategoryFolders[0]);
        if (vegPrefabs == null || vegPrefabs.Length == 0)
            vegPrefabs = LoadFolder(ObjectPlacementGroceryCatalog.CategoryFolders[1]);
    }

    public static GameObject[] Fruits
    {
        get
        {
            EnsureLoaded();
            return fruitPrefabs;
        }
    }

    public static GameObject[] Vegetables
    {
        get
        {
            EnsureLoaded();
            return vegPrefabs;
        }
    }

    static GameObject[] LoadFolder(string folder)
    {
        string path = ResourcesRoot + "/" + folder;
        GameObject[] loaded = Resources.LoadAll<GameObject>(path);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogWarning("ObjectPlacementItemLibrary: no assets at Resources/" + path);
            return System.Array.Empty<GameObject>();
        }

        List<GameObject> unique = new List<GameObject>(loaded.Length);
        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < loaded.Length; i++)
        {
            GameObject go = loaded[i];
            if (go == null)
                continue;
            if (!seen.Add(go.name))
                continue;
            unique.Add(go);
        }

        return unique.ToArray();
    }

    public static GameObject Spawn(GameObject template, ObjectPlacementGroceryKind kind, Transform parent)
    {
        if (template == null)
            return null;

        GameObject instance = Object.Instantiate(template, parent);
        instance.name = template.name;
        instance.SetActive(true);

        // Hand-sized for VR player (~apple / tomato scale).
        const float TargetSize = 0.18f;
        Bounds b = GetBounds(instance.transform);
        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxDim > 0.001f)
        {
            float scale = TargetSize / maxDim;
            instance.transform.localScale *= scale;
        }

        ObjectPlacementGroceryItem item = instance.GetComponent<ObjectPlacementGroceryItem>();
        if (item == null)
            item = instance.AddComponent<ObjectPlacementGroceryItem>();
        item.Configure(template.name, kind);
        return instance;
    }

    static Bounds GetBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.08f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
