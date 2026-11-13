using UnityEngine;

/// <summary>
/// Builds stylized Phase I grocery props from Unity primitives (no external meshes).
/// </summary>
public static class ObjectPlacementGroceryFactory
{
    static Material fruitMat;
    static Material vegMat;
    static Material leafMat;
    static Material stemMat;

    public static GameObject Create(string id, ObjectPlacementGroceryKind kind)
    {
        EnsureMaterials();

        GameObject root = new GameObject(kind == ObjectPlacementGroceryKind.Fruit ? "Fruit_" + id : "Veg_" + id);
        Material body = kind == ObjectPlacementGroceryKind.Fruit ? fruitMat : vegMat;

        switch (id)
        {
            case "banana":
                BuildBanana(root.transform, body);
                break;
            case "lemon":
                BuildEllipsoid(root.transform, body, 0.055f, 0.04f, 0.04f);
                break;
            case "pineapple":
                BuildPineapple(root.transform, body);
                break;
            case "mango":
                BuildEllipsoid(root.transform, body, 0.06f, 0.045f, 0.04f);
                break;
            case "papaya":
                BuildEllipsoid(root.transform, body, 0.07f, 0.05f, 0.05f);
                break;
            case "yellow_apple":
            case "golden_tomato":
            case "apricot":
            case "peach":
            case "tomato":
            case "beet":
            case "red_onion":
            case "cherry_tomato":
            case "pomegranate":
                BuildSphere(root.transform, body, id.Contains("cherry") ? 0.028f : id == "pomegranate" || id == "red_onion" ? 0.055f : 0.045f);
                if (id == "peach" || id == "apricot")
                    Prim(root.transform, PrimitiveType.Sphere, "Blush", new Vector3(0.015f, 0f, 0.01f), Vector3.one * 0.035f, body);
                break;
            case "pear":
                BuildPear(root.transform, body);
                break;
            case "yellow_pepper":
            case "red_pepper":
                BuildPepper(root.transform, body);
                break;
            case "corn":
                BuildCorn(root.transform, body);
                break;
            case "melon":
            case "grapefruit":
            case "red_cabbage":
                BuildSphere(root.transform, body, id == "melon" ? 0.07f : 0.06f);
                break;
            case "starfruit":
                BuildStarfruit(root.transform, body);
                break;
            case "chili":
            case "red_okra":
                BuildChili(root.transform, body, id == "red_okra" ? 0.07f : 0.055f);
                break;
            case "radish":
                BuildRadish(root.transform, body);
                break;
            case "red_carrot":
                BuildCarrot(root.transform, body);
                break;
            case "red_potato":
                BuildEllipsoid(root.transform, body, 0.055f, 0.04f, 0.045f);
                break;
            case "red_bean_pod":
                Prim(root.transform, PrimitiveType.Capsule, "Pod", Vector3.zero, new Vector3(0.03f, 0.055f, 0.03f), body);
                break;
            case "red_lettuce":
                BuildLettuce(root.transform, body);
                break;
            case "chili_cluster":
                BuildChiliCluster(root.transform, body);
                break;
            default:
                BuildSphere(root.transform, body, 0.045f);
                break;
        }

        ObjectPlacementGroceryItem item = root.AddComponent<ObjectPlacementGroceryItem>();
        item.Configure(id, kind);
        return root;
    }

    /// <summary>Editor bake can inject project materials so prefabs keep durable asset refs.</summary>
    public static void UseSharedMaterials(Material fruit, Material veg, Material leaf, Material stem)
    {
        if (fruit != null)
            fruitMat = fruit;
        if (veg != null)
            vegMat = veg;
        if (leaf != null)
            leafMat = leaf;
        if (stem != null)
            stemMat = stem;
    }

    static void EnsureMaterials()
    {
        if (fruitMat == null)
            fruitMat = LoadOrMake("OP_FruitYellow", new Color(0.95f, 0.82f, 0.12f), 0.15f, 0.55f);
        if (vegMat == null)
            vegMat = LoadOrMake("OP_VegRed", new Color(0.82f, 0.12f, 0.10f), 0.12f, 0.5f);
        if (leafMat == null)
            leafMat = LoadOrMake("OP_LeafGreen", new Color(0.22f, 0.62f, 0.18f), 0.08f, 0.4f);
        if (stemMat == null)
            stemMat = LoadOrMake("OP_StemBrown", new Color(0.35f, 0.22f, 0.10f), 0.05f, 0.3f);
    }

    static Material LoadOrMake(string name, Color color, float metallic, float smooth)
    {
        Material loaded = Resources.Load<Material>("ObjectPlacement/" + name);
        if (loaded != null)
            return loaded;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.name = name;
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
        return mat;
    }

    static void BuildSphere(Transform parent, Material mat, float radius)
    {
        Prim(parent, PrimitiveType.Sphere, "Body", Vector3.zero, Vector3.one * (radius * 2f), mat);
    }

    static void BuildEllipsoid(Transform parent, Material mat, float sx, float sy, float sz)
    {
        Prim(parent, PrimitiveType.Sphere, "Body", Vector3.zero, new Vector3(sx * 2f, sy * 2f, sz * 2f), mat);
    }

    static void BuildBanana(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Capsule, "SegA", new Vector3(-0.025f, 0f, 0f), new Vector3(0.028f, 0.04f, 0.028f), mat);
        Prim(parent, PrimitiveType.Capsule, "SegB", new Vector3(0.01f, 0.012f, 0f), new Vector3(0.028f, 0.04f, 0.028f), mat);
        Prim(parent, PrimitiveType.Capsule, "SegC", new Vector3(0.04f, 0.005f, 0f), new Vector3(0.026f, 0.035f, 0.026f), mat);
        Prim(parent, PrimitiveType.Cylinder, "Stem", new Vector3(-0.05f, 0.01f, 0f), new Vector3(0.012f, 0.012f, 0.012f), stemMat);
    }

    static void BuildPineapple(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Cylinder, "Body", Vector3.zero, new Vector3(0.05f, 0.055f, 0.05f), mat);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            Prim(parent, PrimitiveType.Cube, "Leaf" + i,
                new Vector3(Mathf.Cos(a) * 0.01f, 0.07f, Mathf.Sin(a) * 0.01f),
                new Vector3(0.02f, 0.05f, 0.008f), leafMat);
        }
    }

    static void BuildPear(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Sphere, "Bottom", new Vector3(0f, -0.01f, 0f), Vector3.one * 0.08f, mat);
        Prim(parent, PrimitiveType.Sphere, "Top", new Vector3(0f, 0.03f, 0f), Vector3.one * 0.055f, mat);
        Prim(parent, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.055f, 0f), new Vector3(0.01f, 0.012f, 0.01f), stemMat);
    }

    static void BuildPepper(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.04f, 0.05f, 0.04f), mat);
        Prim(parent, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.055f, 0f), new Vector3(0.015f, 0.01f, 0.015f), stemMat);
        Prim(parent, PrimitiveType.Cube, "Leaf", new Vector3(0.015f, 0.06f, 0f), new Vector3(0.03f, 0.008f, 0.02f), leafMat);
    }

    static void BuildCorn(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Cylinder, "Cob", Vector3.zero, new Vector3(0.035f, 0.07f, 0.035f), mat);
        Prim(parent, PrimitiveType.Cube, "HuskA", new Vector3(0.02f, 0.02f, 0f), new Vector3(0.01f, 0.08f, 0.04f), leafMat);
        Prim(parent, PrimitiveType.Cube, "HuskB", new Vector3(-0.02f, 0.02f, 0f), new Vector3(0.01f, 0.08f, 0.04f), leafMat);
    }

    static void BuildStarfruit(Transform parent, Material mat)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            Prim(parent, PrimitiveType.Cube, "Point" + i,
                new Vector3(Mathf.Cos(a) * 0.025f, 0f, Mathf.Sin(a) * 0.025f),
                new Vector3(0.045f, 0.035f, 0.018f), mat);
        }
    }

    static void BuildChili(Transform parent, Material mat, float length)
    {
        Prim(parent, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.02f, length * 0.5f, 0.02f), mat);
        Prim(parent, PrimitiveType.Cylinder, "Stem", new Vector3(0f, length * 0.45f, 0f), new Vector3(0.01f, 0.01f, 0.01f), stemMat);
    }

    static void BuildRadish(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Sphere, "Body", Vector3.zero, Vector3.one * 0.05f, mat);
        Prim(parent, PrimitiveType.Cube, "Greens", new Vector3(0f, 0.04f, 0f), new Vector3(0.02f, 0.05f, 0.01f), leafMat);
    }

    static void BuildCarrot(Transform parent, Material mat)
    {
        GameObject cone = Prim(parent, PrimitiveType.Cylinder, "Body", Vector3.zero, new Vector3(0.035f, 0.06f, 0.035f), mat);
        cone.transform.localScale = new Vector3(0.035f, 0.06f, 0.018f);
        Prim(parent, PrimitiveType.Cube, "Tops", new Vector3(0f, 0.07f, 0f), new Vector3(0.025f, 0.04f, 0.01f), leafMat);
    }

    static void BuildLettuce(Transform parent, Material mat)
    {
        Prim(parent, PrimitiveType.Sphere, "Core", Vector3.zero, Vector3.one * 0.07f, mat);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Prim(parent, PrimitiveType.Sphere, "Leaf" + i,
                new Vector3(Mathf.Cos(a) * 0.02f, 0.01f, Mathf.Sin(a) * 0.02f),
                Vector3.one * 0.06f, mat);
        }
    }

    static void BuildChiliCluster(Transform parent, Material mat)
    {
        for (int i = 0; i < 3; i++)
        {
            float a = (i - 1) * 25f * Mathf.Deg2Rad;
            Prim(parent, PrimitiveType.Capsule, "Chili" + i,
                new Vector3(Mathf.Sin(a) * 0.02f, -0.01f, Mathf.Cos(a) * 0.01f),
                new Vector3(0.016f, 0.04f, 0.016f), mat);
        }
        Prim(parent, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.035f, 0f), new Vector3(0.012f, 0.012f, 0.012f), stemMat);
    }

    static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
        return go;
    }
}
