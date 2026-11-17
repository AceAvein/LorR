#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: LorR / NPC / Create Pen Trace Intro Cutscene Controls
/// Does not create Dialogue circle or Dr. Rivera — those already exist in the scene.
/// </summary>
public static class PenTraceIntroSceneSetupBuilder
{
    const string ScenePath = "Assets/Scenes/Scene Simulation/Pen Control Tracing.unity";
    const string RootName = "IntroCutsceneControls";

    [MenuItem("LorR/NPC/Create Pen Trace Intro Cutscene Controls")]
    public static void CreateOrRefresh()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Pen Trace Intro", "Failed to open Pen Control Tracing scene.", "OK");
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        ObjectPlacementIntroSceneSetup foreignOp = root.GetComponent<ObjectPlacementIntroSceneSetup>();
        if (foreignOp != null)
            Object.DestroyImmediate(foreignOp);
        BallDropIntroSceneSetup foreignBd = root.GetComponent<BallDropIntroSceneSetup>();
        if (foreignBd != null)
            Object.DestroyImmediate(foreignBd);
        ReactionLightIntroSceneSetup foreignRl = root.GetComponent<ReactionLightIntroSceneSetup>();
        if (foreignRl != null)
            Object.DestroyImmediate(foreignRl);

        PenTraceIntroSceneSetup setup = root.GetComponent<PenTraceIntroSceneSetup>();
        if (setup == null)
            setup = root.AddComponent<PenTraceIntroSceneSetup>();

        Transform shotsRoot = root.transform.Find("IntroShots");
        if (shotsRoot == null)
        {
            GameObject shotsGo = new GameObject("IntroShots");
            shotsGo.transform.SetParent(root.transform, false);
            shotsRoot = shotsGo.transform;
        }

        Vector3 tableFocus = ResolveTableFocus();
        Vector3 penFocus = ResolvePenSelectFocus();

        setup.tableFocus = EnsureEmpty(shotsRoot, "TableFocus", tableFocus);
        setup.penSelectFocus = EnsureEmpty(shotsRoot, "PenSelectFocus", penFocus);

        // Front-centered (no left/right offset) — matches desk/station facing.
        Vector3 deskApproach = FrontApproach(GameObject.Find("TraceDesk Easy"));
        Vector3 penApproach = FrontApproach(GameObject.Find("PenSelectStation"));
        setup.shotTableOverview = EnsureShot(shotsRoot, "Shot_TableOverview",
            tableFocus + deskApproach * 1.2f + Vector3.up * 1.05f, tableFocus, force: true);
        setup.shotPenSelect = EnsureShot(shotsRoot, "Shot_PenSelect",
            penFocus + penApproach * 1.4f + Vector3.up * 0.55f, penFocus, force: true);

        // Presenter mark beside TraceDesk Easy so Rivera stays in the Set1 paper shot.
        Vector3 riveraPos = tableFocus + Vector3.right * 0.75f + Vector3.back * 0.15f;
        riveraPos.y = ResolveRiveraFloorY();
        Transform riveraMark = EnsureEmpty(shotsRoot, "RiveraPresenterMark", riveraPos);
        Vector3 lookDesk = tableFocus - riveraPos;
        lookDesk.y = 0f;
        if (lookDesk.sqrMagnitude > 0.001f)
            riveraMark.rotation = Quaternion.LookRotation(lookDesk.normalized, Vector3.up);
        setup.riveraPresenterMark = riveraMark;

        BuildDialogueUi(root, setup);

        EditorUtility.SetDirty(setup);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog(
            "Pen Trace Intro",
            "Created/refreshed IntroCutsceneControls.\n\n" +
            "• Shot_PenSelect → PenSelectStation\n" +
            "• Shot_TableOverview → TraceDesk Easy / TracingBoard_Set1\n" +
            "• RiveraPresenterMark beside Easy desk\n" +
            "• Reuses existing Dialogue circle + Dr. Rivera",
            "OK");
    }

    static Vector3 ResolveTableFocus()
    {
        Transform paper = PenTraceBoards.FindSet(0);
        if (paper != null)
            return PenTraceBoards.BoardCenter(paper);

        GameObject desk = GameObject.Find("TraceDesk Easy");
        if (desk != null)
            return desk.transform.position + Vector3.up * 0.95f;

        return new Vector3(0f, 1.0f, 1.2f);
    }

    static float ResolveRiveraFloorY()
    {
        GameObject rivera = GameObject.Find("Dr. Rivera");
        if (rivera != null)
            return rivera.transform.position.y;
        return 0f;
    }

    static Vector3 ResolvePenSelectFocus()
    {
        GameObject station = GameObject.Find("PenSelectStation");
        if (station != null)
            return station.transform.position + Vector3.up * 0.9f;
        GameObject pencil = GameObject.Find("Pencil");
        if (pencil != null)
            return pencil.transform.position;
        return new Vector3(0.2f, 0.95f, -2.2f);
    }

    static Transform EnsureEmpty(Transform parent, string name, Vector3 worldPos)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
            t.position = worldPos;
        }
        return t;
    }

    static Vector3 FrontApproach(GameObject subject)
    {
        if (subject == null)
            return Vector3.back;
        Vector3 facing = Vector3.ProjectOnPlane(subject.transform.forward, Vector3.up);
        if (facing.sqrMagnitude < 0.001f)
            facing = Vector3.ProjectOnPlane(subject.transform.right, Vector3.up);
        if (facing.sqrMagnitude < 0.001f)
            return Vector3.back;
        return -facing.normalized;
    }

    static Transform EnsureShot(Transform parent, string name, Vector3 position, Vector3 lookAt, bool force = false)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);

        bool place = force || existing == null
                     || (go.transform.localPosition == Vector3.zero && go.transform.position.sqrMagnitude < 0.01f);
        if (place)
        {
            go.transform.position = position;
            Vector3 forward = lookAt - position;
            if (forward.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        return go.transform;
    }

    static void BuildDialogueUi(GameObject root, PenTraceIntroSceneSetup setup)
    {
        Transform existingCanvas = root.transform.Find("IntroEffectsCanvas");
        GameObject canvasGo = existingCanvas != null ? existingCanvas.gameObject : new GameObject("IntroEffectsCanvas");
        canvasGo.name = "IntroEffectsCanvas";
        canvasGo.transform.SetParent(root.transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 0.45f;
        canvas.sortingOrder = 5000;

        if (canvasGo.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2048, 1536);
        }

        EnsureFull(canvasGo.transform, "Fade", new Color(0, 0, 0, 0));
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
            prt.anchorMin = new Vector2(0.08f, 0.12f);
            prt.anchorMax = new Vector2(0.92f, 0.28f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            Image img = panelGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f);
        }
        else
        {
            panelGo = panelT.gameObject;
            prt = panelGo.GetComponent<RectTransform>();
        }

        Transform textT = panelGo.transform.Find("GuideText");
        TMP_Text tmp;
        if (textT == null)
        {
            GameObject textGo = new GameObject("GuideText");
            textGo.transform.SetParent(panelGo.transform, false);
            RectTransform grt = textGo.AddComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(16, 10);
            grt.offsetMax = new Vector2(-16, -10);
            TextMeshProUGUI ugui = textGo.AddComponent<TextMeshProUGUI>();
            ugui.alignment = TextAlignmentOptions.Center;
            ugui.fontSize = 30f;
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
        setup.guidePlate = panelGo.GetComponent<Image>();
        setup.guideText = tmp;
    }

    static void EnsureFull(Transform parent, string name, Color color)
    {
        if (parent.Find(name) != null)
            return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    static void EnsureBar(Transform parent, string name, bool top)
    {
        if (parent.Find(name) != null)
            return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        if (top)
        {
            rt.anchorMin = new Vector2(0, 0.9f);
            rt.anchorMax = Vector2.one;
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1, 0.08f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }
}
#endif
