#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a Catch-the-Stick style overhead arc for Ball Drop.
/// Menu: LorR / Ball Drop / Build Overhead Arc Prefab
/// </summary>
public static class BallDropArcBuilder
{
    const string RootFolder = "Assets/Scenes/Scene Simulation/Ball Drop";
    const string MaterialsFolder = RootFolder + "/Materials";
    const string PrefabsFolder = RootFolder + "/Prefabs";
    const string PrefabPath = PrefabsFolder + "/BallDropOverheadArc.prefab";

    const int SpawnCount = 10;
    const float ArcRadius = 0.55f;
    const float ArcHeight = 2.15f;

    static readonly Color FrameWhite = new Color(0.92f, 0.93f, 0.95f);
    static readonly Color AccentDark = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color RimBlue = new Color(0.25f, 0.55f, 0.95f);
    static readonly Color ButtonRed = new Color(0.85f, 0.12f, 0.12f);

    [MenuItem("LorR/Ball Drop/Build Overhead Arc Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material frameMat = GetOrCreateMaterial("BD_ArcFrame", FrameWhite, 0.05f, 0.55f, false);
        Material darkMat = GetOrCreateMaterial("BD_ArcDark", AccentDark, 0.2f, 0.4f, false);
        Material rimMat = GetOrCreateMaterial("BD_ArcRim", RimBlue, 0.1f, 0.7f, true, RimBlue * 0.45f);
        Material buttonMat = GetOrCreateMaterial("BD_ArcButton", ButtonRed, 0.05f, 0.6f, true, ButtonRed * 0.25f);

        GameObject root = new GameObject("BallDropOverheadArc");
        BallDropOverheadArc marker = root.AddComponent<BallDropOverheadArc>();

        BuildBase(root.transform, frameMat);
        BuildPillar(root.transform, frameMat, darkMat, buttonMat);
        Transform arc = BuildArc(root.transform, frameMat, rimMat);
        Transform spawnRoot = BuildSpawnPoints(root.transform, arc, darkMat);
        marker.spawnRoot = spawnRoot;
        marker.RefreshSpawnPointCache();

        // Convenient default height for half-court play zone
        root.transform.position = new Vector3(0f, 0f, 0.6f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Ball Drop Overhead Arc",
            "Built prefab:\n" + PrefabPath +
            "\n\n1) Drag into Ball Drop.unity\n" +
            "2) On BallDropSystem, set Transform Above = child Transform_Above\n" +
            "3) You can keep using your existing ball templates.",
            "OK");

        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
        if (prefab != null)
            Selection.activeObject = prefab;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Scene Simulation"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Scene Simulation");
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets/Scenes/Scene Simulation", "Ball Drop");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");
    }

    static void BuildBase(Transform parent, Material mat)
    {
        GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseRing.name = "BaseRing";
        baseRing.transform.SetParent(parent, false);
        baseRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        baseRing.transform.localScale = new Vector3(0.95f, 0.03f, 0.95f);
        ApplyMat(baseRing, mat);
        Object.DestroyImmediate(baseRing.GetComponent<Collider>());

        GameObject baseInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseInner.name = "BasePad";
        baseInner.transform.SetParent(parent, false);
        baseInner.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        baseInner.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);
        ApplyMat(baseInner, mat);
        Object.DestroyImmediate(baseInner.GetComponent<Collider>());
    }

    static void BuildPillar(Transform parent, Material frameMat, Material darkMat, Material buttonMat)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Pillar";
        pillar.transform.SetParent(parent, false);
        pillar.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        pillar.transform.localScale = new Vector3(0.16f, 2.1f, 0.16f);
        ApplyMat(pillar, frameMat);
        Object.DestroyImmediate(pillar.GetComponent<Collider>());

        GameObject sensor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sensor.name = "Sensor";
        sensor.transform.SetParent(parent, false);
        sensor.transform.localPosition = new Vector3(0f, 1.55f, -0.09f);
        sensor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        sensor.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
        ApplyMat(sensor, darkMat);
        Object.DestroyImmediate(sensor.GetComponent<Collider>());

        GameObject display = GameObject.CreatePrimitive(PrimitiveType.Cube);
        display.name = "Display";
        display.transform.SetParent(parent, false);
        display.transform.localPosition = new Vector3(0f, 1.35f, -0.09f);
        display.transform.localScale = new Vector3(0.1f, 0.06f, 0.02f);
        ApplyMat(display, darkMat);
        Object.DestroyImmediate(display.GetComponent<Collider>());

        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        button.name = "StartButton";
        button.transform.SetParent(parent, false);
        button.transform.localPosition = new Vector3(0f, 1.12f, -0.1f);
        button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        button.transform.localScale = new Vector3(0.1f, 0.025f, 0.1f);
        ApplyMat(button, buttonMat);
        Object.DestroyImmediate(button.GetComponent<Collider>());

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "KeyPlate";
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(0f, 0.92f, -0.09f);
        plate.transform.localScale = new Vector3(0.08f, 0.04f, 0.015f);
        ApplyMat(plate, darkMat);
        Object.DestroyImmediate(plate.GetComponent<Collider>());
    }

    static Transform BuildArc(Transform parent, Material frameMat, Material rimMat)
    {
        GameObject arcRoot = new GameObject("Arc");
        arcRoot.transform.SetParent(parent, false);
        arcRoot.transform.localPosition = new Vector3(0f, ArcHeight, 0f);

        // Approximate C-arc with segment cubes + rim cylinders
        const int segments = 16;
        float startDeg = 200f;
        float endDeg = 340f;

        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float a0 = Mathf.Lerp(startDeg, endDeg, t0) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(startDeg, endDeg, t1) * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * ArcRadius, 0f, Mathf.Sin(a0) * ArcRadius);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * ArcRadius, 0f, Mathf.Sin(a1) * ArcRadius);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 dir = p1 - p0;
            float len = dir.magnitude;

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "ArcBeam_" + i;
            beam.transform.SetParent(arcRoot.transform, false);
            beam.transform.localPosition = mid;
            beam.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            beam.transform.localScale = new Vector3(0.08f, 0.07f, len);
            ApplyMat(beam, frameMat);
            Object.DestroyImmediate(beam.GetComponent<Collider>());

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "ArcRim_" + i;
            rim.transform.SetParent(arcRoot.transform, false);
            rim.transform.localPosition = mid + Vector3.down * 0.035f;
            rim.transform.localRotation = beam.transform.localRotation;
            rim.transform.localScale = new Vector3(0.06f, 0.015f, len * 0.98f);
            ApplyMat(rim, rimMat);
            Object.DestroyImmediate(rim.GetComponent<Collider>());
        }

        // Caps on arc tips
        PlaceCap(arcRoot.transform, startDeg, frameMat);
        PlaceCap(arcRoot.transform, endDeg, frameMat);

        return arcRoot.transform;
    }

    static void PlaceCap(Transform arcRoot, float deg, Material mat)
    {
        float rad = deg * Mathf.Deg2Rad;
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "ArcCap";
        cap.transform.SetParent(arcRoot, false);
        cap.transform.localPosition = new Vector3(Mathf.Cos(rad) * ArcRadius, 0f, Mathf.Sin(rad) * ArcRadius);
        cap.transform.localScale = Vector3.one * 0.09f;
        ApplyMat(cap, mat);
        Object.DestroyImmediate(cap.GetComponent<Collider>());
    }

    static Transform BuildSpawnPoints(Transform parent, Transform arc, Material hangerMat)
    {
        GameObject spawnRoot = new GameObject("Transform_Above");
        spawnRoot.transform.SetParent(parent, false);
        spawnRoot.transform.localPosition = Vector3.zero;

        float startDeg = 210f;
        float endDeg = 330f;

        for (int i = 0; i < SpawnCount; i++)
        {
            float t = SpawnCount == 1 ? 0.5f : i / (float)(SpawnCount - 1);
            float deg = Mathf.Lerp(startDeg, endDeg, t);
            float rad = deg * Mathf.Deg2Rad;
            Vector3 onArc = new Vector3(Mathf.Cos(rad) * ArcRadius, ArcHeight, Mathf.Sin(rad) * ArcRadius);

            GameObject hanger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hanger.name = "Hanger_" + i.ToString("00");
            hanger.transform.SetParent(parent, false);
            hanger.transform.localPosition = onArc + Vector3.down * 0.05f;
            hanger.transform.localScale = new Vector3(0.035f, 0.04f, 0.035f);
            ApplyMat(hanger, hangerMat);
            Object.DestroyImmediate(hanger.GetComponent<Collider>());

            // Keep naming compatible with existing scene style
            string pointName = i == 0 ? "SpawnPoint" : "SpawnPoint_" + i.ToString("00");
            // Existing scene used SpawnPoint_03 style — use SpawnPoint_00..09
            pointName = "SpawnPoint_" + i.ToString("00");

            GameObject point = new GameObject(pointName);
            point.transform.SetParent(spawnRoot.transform, false);
            // Release slightly below hanger so ball clears the arc
            point.transform.position = parent.TransformPoint(onArc + Vector3.down * 0.14f);
        }

        return spawnRoot.transform;
    }

    static Material GetOrCreateMaterial(string name, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor = default)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyMaterialSettings(existing, baseColor, metallic, smooth, emission, emissionColor);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        ApplyMaterialSettings(mat, baseColor, metallic, smooth, emission, emissionColor);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyMaterialSettings(Material mat, Color baseColor, float metallic, float smooth, bool emission, Color emissionColor)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", smooth);

        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissionColor == default ? baseColor * 0.3f : emissionColor);
        }
    }

    static void ApplyMat(GameObject go, Material mat)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
    }
}
#endif
