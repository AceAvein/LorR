#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds scene-editable cutscene shot markers + dialogue UI in Ball Drop.
/// Menu: LorR / NPC / Create Ball Drop Intro Cutscene Controls
/// </summary>
public static class BallDropIntroSceneSetupBuilder
{
    const string BallDropScenePath = "Assets/Scenes/Scene Simulation/Ball Drop.unity";
    const string RootName = "IntroCutsceneControls";

    [MenuItem("LorR/NPC/Create Ball Drop Intro Cutscene Controls")]
    public static void CreateOrRefresh()
    {
        Scene scene = EditorSceneManager.OpenScene(BallDropScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Ball Drop Intro", "Failed to open Ball Drop scene.", "OK");
            return;
        }

        GameObject poleGo = GameObject.Find("PoleScreen");
        Transform pole = poleGo != null ? poleGo.transform : null;

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        // Prefer Ball Drop setup; remove Color Sort setup if someone copied the root name only.
        ObjectPlacementIntroSceneSetup foreign = root.GetComponent<ObjectPlacementIntroSceneSetup>();
        if (foreign != null)
            Object.DestroyImmediate(foreign);

        BallDropIntroSceneSetup setup = root.GetComponent<BallDropIntroSceneSetup>();
        if (setup == null)
            setup = root.AddComponent<BallDropIntroSceneSetup>();

        Transform shotsRoot = root.transform.Find("IntroShots");
        if (shotsRoot == null)
        {
            GameObject shotsGo = new GameObject("IntroShots");
            shotsGo.transform.SetParent(root.transform, false);
            shotsRoot = shotsGo.transform;
        }

        Vector3 focus = pole != null ? pole.position : new Vector3(0f, 1.45f, 0f);
        Vector3 viewDir = Vector3.back;
        if (pole != null)
        {
            viewDir = -pole.forward;
            viewDir.y = 0f;
            if (viewDir.sqrMagnitude < 0.001f)
                viewDir = Vector3.back;
            viewDir.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, viewDir).normalized;

        setup.shotPoleOverview = EnsureShot(shotsRoot, "Shot_PoleOverview",
            focus - viewDir * 1.9f + Vector3.up * 0.2f, focus + Vector3.up * -0.05f);
        setup.shotLeftCatch = EnsureShot(shotsRoot, "Shot_LeftCatch",
            focus - viewDir * 1.75f + right * -0.28f + Vector3.up * 0.18f, focus + right * -0.15f);
        setup.shotRightCatch = EnsureShot(shotsRoot, "Shot_RightCatch",
            focus - viewDir * 1.75f + right * 0.28f + Vector3.up * 0.18f, focus + right * 0.15f);
        setup.shotMiss = EnsureShot(shotsRoot, "Shot_Miss",
            focus - viewDir * 1.85f + right * 0.08f + Vector3.up * 0.12f, focus + viewDir * 0.2f + Vector3.up * -0.2f);

        Transform anchors = root.transform.Find("DemoAnchors");
        if (anchors == null)
        {
            GameObject a = new GameObject("DemoAnchors");
            a.transform.SetParent(root.transform, false);
            anchors = a.transform;
        }

        setup.poleScreen = pole;
        setup.leftCatchAnchor = EnsureAnchor(anchors, "LeftCatch",
            focus + viewDir * 0.55f + right * -0.22f + Vector3.up * -0.15f);
        setup.rightCatchAnchor = EnsureAnchor(anchors, "RightCatch",
            focus + viewDir * 0.55f + right * 0.22f + Vector3.up * -0.15f);
        setup.ballSpawnLeft = EnsureAnchor(anchors, "SpawnLeft",
            focus + viewDir * 0.08f + right * -0.12f + Vector3.up * 0.35f);
        setup.ballSpawnRight = EnsureAnchor(anchors, "SpawnRight",
            focus + viewDir * 0.08f + right * 0.12f + Vector3.up * 0.35f);
        setup.missEnd = EnsureAnchor(anchors, "MissEnd",
            focus + viewDir * 0.95f + Vector3.up * -0.85f);

        BuildDialogueUi(root, setup);

        EditorUtility.SetDirty(setup);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog(
            "Ball Drop Intro",
            "Created/refreshed IntroCutsceneControls in Ball Drop.\n\n" +
            "• Move/rotate IntroShots/* (PoleOverview / LeftCatch / RightCatch / Miss)\n" +
            "• Adjust DemoAnchors for spawn/catch/miss points\n" +
            "• Edit IntroEffectsCanvas/GuidePanel for subtitle layout\n" +
            "Keep Shot_PoleOverview as the stable comfort framing.",
            "OK");
    }

    static Transform EnsureShot(Transform parent, string name, Vector3 position, Vector3 lookAt)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);

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

        if (go.GetComponent<BallDropIntroShotMarker>() == null)
            go.AddComponent<BallDropIntroShotMarker>();

        return go.transform;
    }

    static Transform EnsureAnchor(Transform parent, string name, Vector3 worldPos)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);

        bool place = existing == null
                     || (go.transform.localPosition == Vector3.zero
                         && go.transform.position.sqrMagnitude < 0.01f);
        if (place)
            go.transform.position = worldPos;

        return go.transform;
    }

    static void BuildDialogueUi(GameObject root, BallDropIntroSceneSetup setup)
    {
        Transform existingCanvas = root.transform.Find("IntroEffectsCanvas");
        GameObject canvasGo = existingCanvas != null
            ? existingCanvas.gameObject
            : null;

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

        if (canvasGo.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

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
            prt.anchorMin = new Vector2(0.06f, 0.16f);
            prt.anchorMax = new Vector2(0.94f, 0.32f);
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
            ugui.fontSize = 32f;
            ugui.color = Color.white;
            ugui.text = string.Empty;
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
}

/// <summary>Editor gizmo tag for Ball Drop intro shot empties.</summary>
public class BallDropIntroShotMarker : MonoBehaviour
{
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.35f);
    }
#endif
}
#endif
