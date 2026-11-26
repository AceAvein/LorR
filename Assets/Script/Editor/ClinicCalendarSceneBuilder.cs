#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bakes Clinic Calendar into Menu hierarchy (button + world panel).
/// Menu: LorR / Clinic Calendar / Build Into Menu Scene
/// </summary>
public static class ClinicCalendarSceneBuilder
{
    const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("LorR/Clinic Calendar/Build Into Menu Scene")]
    public static void CreateOrRefresh()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Clinic Calendar", "Failed to open Menu scene.", "OK");
            return;
        }

        Button calendarButton = EnsureCalendarButton();
        ClinicCalendarUI panel = EnsurePanel(scene, calendarButton);

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(panel.gameObject);
        if (calendarButton != null)
            EditorUtility.SetDirty(calendarButton.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = panel.gameObject;
        EditorGUIUtility.PingObject(panel.gameObject);
        Debug.Log("[ClinicCalendar] Built into Menu hierarchy: ClinicCalendarPanel + MainMenu_Buttons/Calendar");
    }

    /// <summary>Batchmode entry: Unity -executeMethod ClinicCalendarSceneBuilder.BuildBatch</summary>
    public static void BuildBatch()
    {
        CreateOrRefresh();
        EditorApplication.Exit(0);
    }

    static Button EnsureCalendarButton()
    {
        GameObject buttonsGo = GameObject.Find("MainMenu_Buttons");
        if (buttonsGo == null)
        {
            Debug.LogWarning("[ClinicCalendar] MainMenu_Buttons not found.");
            return null;
        }

        Transform buttons = buttonsGo.transform;
        Transform existing = buttons.Find(ClinicCalendarUI.CalendarButtonName);
        GameObject calendarGo;
        if (existing != null)
            calendarGo = existing.gameObject;
        else
        {
            Transform profile = buttons.Find("Profile");
            if (profile != null)
            {
                calendarGo = Object.Instantiate(profile.gameObject, buttons);
                calendarGo.name = ClinicCalendarUI.CalendarButtonName;
                RectTransform rt = calendarGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(52f, -52f);
                    Vector3 lp = rt.localPosition;
                    rt.localPosition = new Vector3(lp.x, lp.y, -16f);
                }
            }
            else
            {
                calendarGo = new GameObject(ClinicCalendarUI.CalendarButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
                calendarGo.transform.SetParent(buttons, false);
            }
        }

        TMP_Text label = calendarGo.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Calendar";

        Button button = calendarGo.GetComponent<Button>();
        if (button == null)
            button = calendarGo.AddComponent<Button>();

        button.onClick.RemoveAllListeners();
        return button;
    }

    static ClinicCalendarUI EnsurePanel(Scene scene, Button calendarButton)
    {
        ClinicCalendarUI existing = Object.FindFirstObjectByType<ClinicCalendarUI>(FindObjectsInactive.Include);
        GameObject panelGo;
        if (existing != null)
            panelGo = existing.gameObject;
        else
        {
            panelGo = new GameObject(ClinicCalendarUI.PanelName);
            SceneManager.MoveGameObjectToScene(panelGo, scene);
        }

        panelGo.name = ClinicCalendarUI.PanelName;
        ClinicCalendarUI.ConfigureWorldCanvas(panelGo);

        ClinicCalendarUI ui = panelGo.GetComponent<ClinicCalendarUI>();
        if (ui == null)
            ui = panelGo.AddComponent<ClinicCalendarUI>();

        ui.BuildHierarchy();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("mainMenu").objectReferenceValue = GameObject.Find("MainMenu_Buttons");
        so.FindProperty("calendarButton").objectReferenceValue = calendarButton;
        so.FindProperty("titleText").objectReferenceValue = panelGo.transform.Find("Title") != null
            ? panelGo.transform.Find("Title").GetComponent<TMP_Text>()
            : null;
        so.FindProperty("detailText").objectReferenceValue = panelGo.transform.Find("Detail") != null
            ? panelGo.transform.Find("Detail").GetComponent<TMP_Text>()
            : null;
        so.FindProperty("gridRoot").objectReferenceValue = panelGo.transform.Find("Grid");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (calendarButton != null)
        {
            calendarButton.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(calendarButton.onClick, ui.Open);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(calendarButton.onClick, ui.Open);
            EditorUtility.SetDirty(calendarButton);
        }

        // Closed by default; still visible in Hierarchy when expanded.
        panelGo.SetActive(false);
        return ui;
    }
}
#endif
