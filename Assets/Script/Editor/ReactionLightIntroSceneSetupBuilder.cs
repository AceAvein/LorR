#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: LorR / NPC / Create Reaction Light Intro Cutscene Controls
/// </summary>
public static class ReactionLightIntroSceneSetupBuilder
{
    const string ScenePath = "Assets/Scenes/Scene Simulation/Reaction Light.unity";
    const string RootName = "IntroCutsceneControls";

    [MenuItem("LorR/NPC/Create Reaction Light Intro Cutscene Controls")]
    public static void CreateOrRefresh()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Reaction Light Intro", "Failed to open Reaction Light scene.", "OK");
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        ObjectPlacementIntroSceneSetup foreign = root.GetComponent<ObjectPlacementIntroSceneSetup>();
        if (foreign != null)
            Object.DestroyImmediate(foreign);
        BallDropIntroSceneSetup foreignBd = root.GetComponent<BallDropIntroSceneSetup>();
        if (foreignBd != null)
            Object.DestroyImmediate(foreignBd);

        ReactionLightIntroSceneSetup setup = root.GetComponent<ReactionLightIntroSceneSetup>();
        if (setup == null)
            setup = root.AddComponent<ReactionLightIntroSceneSetup>();

        Transform shotsRoot = root.transform.Find("IntroShots");
        if (shotsRoot == null)
        {
            GameObject shotsGo = new GameObject("IntroShots");
            shotsGo.transform.SetParent(root.transform, false);
            shotsRoot = shotsGo.transform;
        }

        Vector3 focus = new Vector3(0f, 1.4f, 1.6f);
        ReactionLightBoard board = Object.FindAnyObjectByType<ReactionLightBoard>();
        if (board != null && board.HasValidSlots())
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            for (int i = 0; i < board.slots.Length; i++)
            {
                if (board.slots[i] == null)
                    continue;
                sum += board.slots[i].position;
                n++;
            }
            if (n > 0)
                focus = sum / n;
        }

        setup.boardFocus = EnsureEmpty(shotsRoot, "BoardFocus", focus);
        setup.shotBoardOverview = EnsureShot(shotsRoot, "Shot_BoardOverview",
            focus + Vector3.back * 1.65f + Vector3.up * 0.3f, focus);

        BuildDialogueUi(root, setup);

        EditorUtility.SetDirty(setup);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog(
            "Reaction Light Intro",
            "Created/refreshed IntroCutsceneControls.\n\n" +
            "• Move/rotate IntroShots/Shot_BoardOverview\n" +
            "• Full transform is used as-is at runtime",
            "OK");
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

    static Transform EnsureShot(Transform parent, string name, Vector3 position, Vector3 lookAt)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);

        bool place = existing == null
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

    static void BuildDialogueUi(GameObject root, ReactionLightIntroSceneSetup setup)
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
            prt.anchorMin = new Vector2(0.08f, 0.14f);
            prt.anchorMax = new Vector2(0.92f, 0.3f);
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
            rt.anchorMax = new Vector2(1, 0.05f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
    }
}
#endif
