#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds scene-editable cutscene shot markers + dialogue UI in Color Sort.
/// Menu: LorR / NPC / Create Intro Cutscene Scene Controls
/// </summary>
public static class ObjectPlacementIntroSceneSetupBuilder
{
    const string ColorSortScenePath = "Assets/Scenes/Scene Simulation/Color Sort.unity";
    const string RootName = "IntroCutsceneControls";

    [MenuItem("LorR/NPC/Create Intro Cutscene Scene Controls")]
    public static void CreateOrRefresh()
    {
        Scene scene = EditorSceneManager.OpenScene(ColorSortScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Intro Cutscene", "Failed to open Color Sort scene.", "OK");
            return;
        }

        ObjectPlacementStation station = Object.FindAnyObjectByType<ObjectPlacementStation>();
        if (station == null)
        {
            EditorUtility.DisplayDialog("Intro Cutscene", "ObjectPlacementStation not found in Color Sort.", "OK");
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        ObjectPlacementIntroSceneSetup setup = root.GetComponent<ObjectPlacementIntroSceneSetup>();
        if (setup == null)
            setup = root.AddComponent<ObjectPlacementIntroSceneSetup>();

        Transform shotsRoot = root.transform.Find("IntroShots");
        if (shotsRoot == null)
        {
            GameObject shotsGo = new GameObject("IntroShots");
            shotsGo.transform.SetParent(root.transform, false);
            shotsRoot = shotsGo.transform;
        }

        Vector3 boxesFocus = GetBoxesFocus(station);
        Vector3 beltFocus = GetBeltFocus(station);
        Vector3 wideFocus = (boxesFocus + beltFocus) * 0.5f;
        Vector3 viewDir = GuessViewDir(station, wideFocus);
        Vector3 right = Vector3.Cross(Vector3.up, viewDir).normalized;

        setup.shotWide = EnsureShot(shotsRoot, "Shot_Wide",
            wideFocus - viewDir * 3.1f + right * 0.95f + Vector3.up * 1.85f, wideFocus + Vector3.up * 0.35f);
        setup.shotBoxCloseUp = EnsureShot(shotsRoot, "Shot_BoxCloseUp",
            boxesFocus - viewDir * 1.55f + right * 0.35f + Vector3.up * 1.25f, boxesFocus + Vector3.up * 0.18f);
        setup.shotConveyor = EnsureShot(shotsRoot, "Shot_Conveyor",
            beltFocus - viewDir * 2.0f + right * -0.55f + Vector3.up * 1.45f, beltFocus + Vector3.up * 0.12f);
        setup.shotItemPickup = EnsureShot(shotsRoot, "Shot_ItemPickup",
            GetSpawnFocus(station) - viewDir * 1.25f + right * 0.25f + Vector3.up * 1.15f,
            GetSpawnFocus(station) + Vector3.up * 0.05f);
        setup.shotItemPlace = EnsureShot(shotsRoot, "Shot_ItemPlace",
            boxesFocus - viewDir * 1.35f + right * 0.4f + Vector3.up * 1.2f, boxesFocus + Vector3.up * 0.12f);
        setup.shotFinalBoxes = EnsureShot(shotsRoot, "Shot_FinalBoxes",
            beltFocus - viewDir * 1.9f + right * 0.15f + Vector3.up * 1.5f, beltFocus + Vector3.up * 0.15f);
        setup.shotFinalWide = EnsureShot(shotsRoot, "Shot_FinalWide",
            wideFocus - viewDir * 3.0f + right * 0.75f + Vector3.up * 1.8f, wideFocus + Vector3.up * 0.3f);

        BuildDialogueUi(root, setup);

        EditorUtility.SetDirty(setup);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog(
            "Intro Cutscene",
            "Created/refreshed IntroCutsceneControls in Color Sort.\n\n" +
            "• Move/rotate IntroShots/* markers in Scene View\n" +
            "• Edit IntroEffectsCanvas/GuidePanel RectTransform for dialogue height\n" +
            "• Gizmos show camera frustums when the root is visible",
            "OK");
    }

    static Transform EnsureShot(Transform parent, string name, Vector3 position, Vector3 lookAt)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);

        // Only place if newly created or at origin (don't clobber user edits on refresh).
        bool place = existing == null
                     || (go.transform.localPosition == Vector3.zero
                         && go.transform.position.sqrMagnitude < 0.01f);
        if (place || existing == null)
        {
            go.transform.position = position;
            Vector3 forward = lookAt - position;
            if (forward.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        if (go.GetComponent<ObjectPlacementIntroShotMarker>() == null)
            go.AddComponent<ObjectPlacementIntroShotMarker>();

        return go.transform;
    }

    static void BuildDialogueUi(GameObject root, ObjectPlacementIntroSceneSetup setup)
    {
        Transform existingCanvas = root.transform.Find("IntroEffectsCanvas");
        GameObject canvasGo = existingCanvas != null
            ? existingCanvas.gameObject
            : GameObject.Find("IntroEffectsCanvas");

        if (canvasGo == null)
            canvasGo = new GameObject("IntroEffectsCanvas");

        canvasGo.name = "IntroEffectsCanvas";
        canvasGo.transform.SetParent(root.transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 0.45f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2048f, 1536f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasGo.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        if (canvasRt == null)
            canvasRt = canvasGo.AddComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;
        canvasRt.localScale = Vector3.one;

        EnsureFullImage(canvasGo.transform, "Fade", new Color(0f, 0f, 0f, 0f), 0);
        EnsureFullImage(canvasGo.transform, "Vignette", new Color(0f, 0f, 0f, 0f), 1);
        EnsureBar(canvasGo.transform, "LetterboxTop", true);
        EnsureBar(canvasGo.transform, "LetterboxBottom", false);

        Transform panelT = canvasGo.transform.Find("GuidePanel");
        GameObject panelGo;
        RectTransform prt;
        if (panelT == null)
        {
            panelGo = new GameObject("GuidePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            prt = panelGo.AddComponent<RectTransform>();
            // Default lower-middle band — user can drag anchors afterward.
            prt.anchorMin = new Vector2(0.06f, 0.16f);
            prt.anchorMax = new Vector2(0.94f, 0.32f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            Image img = panelGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.78f);
            img.raycastTarget = false;
        }
        else
        {
            panelGo = panelT.gameObject;
            prt = panelGo.GetComponent<RectTransform>();
        }

        Image plate = panelGo.GetComponent<Image>();
        Transform textT = panelGo.transform.Find("GuideText");
        TMP_Text tmp;
        if (textT == null)
        {
            GameObject textGo = new GameObject("GuideText");
            textGo.transform.SetParent(panelGo.transform, false);
            RectTransform grt = textGo.AddComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(18f, 10f);
            grt.offsetMax = new Vector2(-18f, -10f);
            TextMeshProUGUI ugui = textGo.AddComponent<TextMeshProUGUI>();
            ugui.alignment = TextAlignmentOptions.Center;
            ugui.enableAutoSizing = false;
            ugui.fontSize = 32f;
            ugui.color = Color.white;
            ugui.text = "CUTSCENE DIALOGUE";
            ugui.textWrappingMode = TextWrappingModes.Normal;
            ugui.overflowMode = TextOverflowModes.Truncate;
            ugui.fontStyle = FontStyles.Bold;
            tmp = ugui;
        }
        else
        {
            tmp = textT.GetComponent<TMP_Text>();
        }

        setup.effectsCanvas = canvas;
        setup.guidePanel = prt;
        setup.guidePlate = plate;
        setup.guideText = tmp;

        // Hide sample text in edit mode until Play; keep panel visible for layout.
        if (!Application.isPlaying && tmp != null)
            tmp.text = string.Empty;
    }

    static Image EnsureFullImage(Transform parent, string name, Color color, int sibling)
    {
        Transform t = parent.Find(name);
        if (t != null)
            return t.GetComponent<Image>();

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(sibling);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image EnsureBar(Transform parent, string name, bool top)
    {
        Transform t = parent.Find(name);
        if (t != null)
            return t.GetComponent<Image>();

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        if (top)
        {
            rt.anchorMin = new Vector2(0f, 0.90f);
            rt.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.035f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        return img;
    }

    static Vector3 GetBoxesFocus(ObjectPlacementStation station)
    {
        if (station.yellowBox != null && station.redBox != null)
            return (station.yellowBox.position + station.redBox.position) * 0.5f + Vector3.up * 0.2f;
        if (station.boxesRoot != null)
            return station.boxesRoot.position + Vector3.up * 0.25f;
        return station.transform.position + Vector3.up * 1f;
    }

    static Vector3 GetBeltFocus(ObjectPlacementStation station)
    {
        Transform belt = station.beltSurface != null ? station.beltSurface : station.conveyor;
        if (belt != null)
            return belt.position + Vector3.up * 0.15f;
        return GetBoxesFocus(station);
    }

    static Vector3 GetSpawnFocus(ObjectPlacementStation station)
    {
        Transform spawn = station.grocerySpawnPointLeft != null
            ? station.grocerySpawnPointLeft
            : station.grocerySpawnPoint;
        if (spawn != null)
            return spawn.position + Vector3.up * 0.08f;
        return GetBeltFocus(station);
    }

    static Vector3 GuessViewDir(ObjectPlacementStation station, Vector3 focus)
    {
        var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null)
        {
            Transform t = origin.Origin != null ? origin.Origin.transform : origin.transform;
            Vector3 toPlayer = t.position - focus;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                return toPlayer.normalized;
        }

        Vector3 dir = -station.transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector3.back;
        return dir.normalized;
    }
}

/// <summary>Marks a cutscene camera shot empty for Scene View selection.</summary>
public class ObjectPlacementIntroShotMarker : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.08f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
    }
}
#endif
