#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

    // Arc opens toward -Z (player/front); closed back connects into the pole
    const float ArcStartDeg = 200f;
    const float ArcEndDeg = 340f;

    static readonly Color FrameWhite = new Color(0.92f, 0.93f, 0.95f);
    static readonly Color AccentDark = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color RimBlue = new Color(0.25f, 0.55f, 0.95f);
    static readonly Color ButtonRed = new Color(0.85f, 0.12f, 0.12f);
    static readonly Color ScreenGlow = new Color(0.15f, 0.75f, 0.95f);

    [MenuItem("LorR/Ball Drop/Build Overhead Arc Prefab")]
    public static void BuildPrefab()
    {
        EnsureFolders();

        Material frameMat = GetOrCreateMaterial("BD_ArcFrame", FrameWhite, 0.05f, 0.55f, false);
        Material darkMat = GetOrCreateMaterial("BD_ArcDark", AccentDark, 0.2f, 0.4f, false);
        Material rimMat = GetOrCreateMaterial("BD_ArcRim", RimBlue, 0.1f, 0.7f, true, RimBlue * 0.45f);
        Material buttonMat = GetOrCreateMaterial("BD_ArcButton", ButtonRed, 0.05f, 0.6f, true, ButtonRed * 0.25f);
        Material screenMat = GetOrCreateMaterial("BD_PoleScreen", AccentDark, 0.05f, 0.25f, true, ScreenGlow * 0.35f);

        GameObject root = new GameObject("BallDropOverheadArc");
        BallDropOverheadArc marker = root.AddComponent<BallDropOverheadArc>();

        BuildBase(root.transform, frameMat);
        BuildPillar(root.transform, frameMat, darkMat, buttonMat, screenMat);
        Transform arc = BuildArc(root.transform, frameMat, rimMat);
        BuildArcToPoleConnection(root.transform, frameMat, darkMat);
        Transform spawnRoot = BuildSpawnPoints(root.transform, arc, darkMat);
        marker.spawnRoot = spawnRoot;
        marker.RefreshSpawnPointCache();

        root.transform.position = new Vector3(0f, 0f, 0.6f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Ball Drop Overhead Arc",
            "Rebuilt:\n" + PrefabPath +
            "\n\n• Arc connected to pole (hub + arms)\n" +
            "• Screen facing user above pole buttons\n\n" +
            "Re-drag into Ball Drop.unity and set Transform Above.",
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

    static void BuildPillar(Transform parent, Material frameMat, Material darkMat, Material buttonMat, Material screenMat)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "Pillar";
        pillar.transform.SetParent(parent, false);
        pillar.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        pillar.transform.localScale = new Vector3(0.18f, 2.1f, 0.18f);
        ApplyMat(pillar, frameMat);
        Object.DestroyImmediate(pillar.GetComponent<Collider>());

        // Control face toward player (-Z = "tapat")
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "ControlPanel";
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = new Vector3(0f, 1.05f, -0.11f);
        panel.transform.localScale = new Vector3(0.14f, 0.55f, 0.03f);
        ApplyMat(panel, darkMat);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        // Screen ABOVE buttons, facing user
        GameObject screenRoot = new GameObject("PoleScreen");
        screenRoot.transform.SetParent(parent, false);
        screenRoot.transform.localPosition = new Vector3(0f, 1.42f, -0.13f);
        screenRoot.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

        GameObject screenFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screenFrame.name = "ScreenFrame";
        screenFrame.transform.SetParent(screenRoot.transform, false);
        screenFrame.transform.localScale = new Vector3(0.2f, 0.14f, 0.03f);
        ApplyMat(screenFrame, darkMat);
        Object.DestroyImmediate(screenFrame.GetComponent<Collider>());

        GameObject screenPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screenPanel.name = "ScreenPanel";
        screenPanel.transform.SetParent(screenRoot.transform, false);
        screenPanel.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        screenPanel.transform.localScale = new Vector3(0.17f, 0.11f, 0.015f);
        ApplyMat(screenPanel, screenMat);
        Object.DestroyImmediate(screenPanel.GetComponent<Collider>());

        GameObject canvasGo = new GameObject("ScreenCanvas");
        canvasGo.transform.SetParent(screenRoot.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.022f);
        canvasGo.transform.localScale = Vector3.one * 0.001f;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 100f);

        GameObject textGo = new GameObject("ScreenLabel");
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "READY";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ScreenGlow;
        tmp.rectTransform.sizeDelta = new Vector2(150f, 90f);

        // Button cluster under the screen
        float btnY = 1.18f;
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                btn.name = "Btn_" + row + "_" + col;
                btn.transform.SetParent(parent, false);
                btn.transform.localPosition = new Vector3((col - 1) * 0.035f, btnY - row * 0.05f, -0.13f);
                btn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                btn.transform.localScale = new Vector3(0.028f, 0.012f, 0.028f);
                ApplyMat(btn, buttonMat);
                Object.DestroyImmediate(btn.GetComponent<Collider>());
            }
        }

        // Big start button below cluster
        GameObject start = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        start.name = "StartButton";
        start.transform.SetParent(parent, false);
        start.transform.localPosition = new Vector3(0f, 0.98f, -0.13f);
        start.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        start.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
        ApplyMat(start, buttonMat);
        Object.DestroyImmediate(start.GetComponent<Collider>());

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "KeyPlate";
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = new Vector3(0f, 0.86f, -0.12f);
        plate.transform.localScale = new Vector3(0.09f, 0.04f, 0.015f);
        ApplyMat(plate, darkMat);
        Object.DestroyImmediate(plate.GetComponent<Collider>());
    }

    static void BuildArcToPoleConnection(Transform parent, Material frameMat, Material darkMat)
    {
        // Hub collar on top of pole
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "ArcHub";
        hub.transform.SetParent(parent, false);
        hub.transform.localPosition = new Vector3(0f, ArcHeight, 0f);
        hub.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);
        ApplyMat(hub, darkMat);
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        // Main spine: pole hub → arc back midpoint (270°)
        float midRad = 270f * Mathf.Deg2Rad;
        Vector3 arcBack = new Vector3(Mathf.Cos(midRad) * ArcRadius, ArcHeight, Mathf.Sin(midRad) * ArcRadius);
        // midRad 270 → (0, ArcHeight, -ArcRadius)
        Vector3 hubPos = new Vector3(0f, ArcHeight, 0f);
        Vector3 mid = (hubPos + arcBack) * 0.5f;
        Vector3 dir = arcBack - hubPos;

        GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spine.name = "ArcConnectSpine";
        spine.transform.SetParent(parent, false);
        spine.transform.localPosition = mid;
        spine.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        spine.transform.localScale = new Vector3(0.09f, 0.07f, dir.magnitude);
        ApplyMat(spine, frameMat);
        Object.DestroyImmediate(spine.GetComponent<Collider>());

        // Left / right brace arms into the arc
        AddBrace(parent, "ArcBraceL", hubPos, ArcPoint(230f), frameMat);
        AddBrace(parent, "ArcBraceR", hubPos, ArcPoint(310f), frameMat);

        // Small joint spheres where braces meet arc
        PlaceJoint(parent, ArcPoint(230f), frameMat);
        PlaceJoint(parent, ArcPoint(310f), frameMat);
        PlaceJoint(parent, arcBack, darkMat);
    }

    static Vector3 ArcPoint(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * ArcRadius, ArcHeight, Mathf.Sin(rad) * ArcRadius);
    }

    static void AddBrace(Transform parent, string name, Vector3 from, Vector3 to, Material mat)
    {
        Vector3 mid = (from + to) * 0.5f;
        Vector3 dir = to - from;
        GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brace.name = name;
        brace.transform.SetParent(parent, false);
        brace.transform.localPosition = mid;
        brace.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        brace.transform.localScale = new Vector3(0.06f, 0.05f, dir.magnitude);
        ApplyMat(brace, mat);
        Object.DestroyImmediate(brace.GetComponent<Collider>());
    }

    static void PlaceJoint(Transform parent, Vector3 pos, Material mat)
    {
        GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        joint.name = "ConnectJoint";
        joint.transform.SetParent(parent, false);
        joint.transform.localPosition = pos;
        joint.transform.localScale = Vector3.one * 0.08f;
        ApplyMat(joint, mat);
        Object.DestroyImmediate(joint.GetComponent<Collider>());
    }

    static Transform BuildArc(Transform parent, Material frameMat, Material rimMat)
    {
        GameObject arcRoot = new GameObject("Arc");
        arcRoot.transform.SetParent(parent, false);
        arcRoot.transform.localPosition = new Vector3(0f, ArcHeight, 0f);

        const int segments = 18;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float a0 = Mathf.Lerp(ArcStartDeg, ArcEndDeg, t0) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(ArcStartDeg, ArcEndDeg, t1) * Mathf.Deg2Rad;
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

        PlaceCap(arcRoot.transform, ArcStartDeg, frameMat);
        PlaceCap(arcRoot.transform, ArcEndDeg, frameMat);

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

        float startDeg = 215f;
        float endDeg = 325f;

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

            GameObject point = new GameObject("SpawnPoint_" + i.ToString("00"));
            point.transform.SetParent(spawnRoot.transform, false);
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
