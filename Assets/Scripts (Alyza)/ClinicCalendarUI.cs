using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Interactive month calendar: per-profile schedule marks (planned / rest / done / early / missed).
/// Scene-placed world-space panel on Menu (see ClinicCalendarSceneBuilder).
/// </summary>
public class ClinicCalendarUI : MonoBehaviour
{
    public static ClinicCalendarUI Instance { get; private set; }

    public const string PanelName = "ClinicCalendarPanel";
    public const string CalendarButtonName = "Calendar";

    static readonly Color PanelBg = new Color(0.12f, 0.35f, 0.38f, 0.98f);
    static readonly Color CellBg = new Color(0.18f, 0.45f, 0.48f, 1f);
    static readonly Color TodayOutline = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color SessionMark = new Color(0.35f, 0.75f, 1f, 1f);
    static readonly Color RestMark = new Color(0.55f, 0.55f, 0.6f, 1f);
    static readonly Color DoneMark = new Color(0.3f, 0.85f, 0.45f, 1f);
    static readonly Color EarlyMark = new Color(1f, 0.65f, 0.2f, 1f);
    static readonly Color MissedMark = new Color(0.9f, 0.3f, 0.3f, 1f);
    static readonly Color TodayMark = new Color(1f, 0.9f, 0.35f, 1f);
    static readonly Color StartBtnColor = new Color(0.22f, 0.62f, 0.38f, 1f);
    static readonly Color DoneBtnColor = new Color(0.35f, 0.4f, 0.42f, 1f);

    [SerializeField] GameObject mainMenu;
    [SerializeField] Button calendarButton;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text detailText;
    [SerializeField] Transform gridRoot;
    [SerializeField] Transform moduleRowsRoot;
    [SerializeField] List<DayCell> cells = new List<DayCell>();
    [SerializeField] List<ModuleRow> moduleRows = new List<ModuleRow>();

    int viewYear;
    int viewMonth;
    string selectedDate;

    [Serializable]
    public class ModuleRow
    {
        public string moduleId;
        public GameObject go;
        public TMP_Text nameLabel;
        public Button actionButton;
        public TMP_Text actionLabel;
        public Image actionBg;
    }

    [Serializable]
    public class DayCell
    {
        public GameObject go;
        public Button button;
        public TMP_Text label;
        public Image mark;
        public Image bg;
        [NonSerialized] public string date;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // When the scene object exists but children were never baked, build them in edit mode.
        if (Application.isPlaying)
            return;
        if (transform.Find("Grid") != null)
            return;

        UnityEditor.EditorApplication.delayCall += BuildHierarchyInEditMode;
    }

    void BuildHierarchyInEditMode()
    {
        if (this == null || Application.isPlaying)
            return;
        if (transform.Find("Grid") != null)
            return;

        ConfigureWorldCanvas(gameObject);
        BuildHierarchy();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    void Awake()
    {
        EnsureWired();
    }

    /// <summary>Safe to call while inactive (Menu bootstrap / Calendar button).</summary>
    public void EnsureWired()
    {
        if (Instance != null && Instance != this && Instance.name == PanelName && name != PanelName)
            return;

        Instance = this;
        DateTime manila = ProgressionConfig.NowManila();
        viewYear = manila.Year;
        viewMonth = manila.Month;
        CacheRefs();
        WireButtons();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open()
    {
        string profileId = ProgressionService.CurrentProfileId();
        if (!string.IsNullOrEmpty(profileId))
            ClinicScheduleService.EnsureSchedule(profileId);

        DateTime manila = ProgressionConfig.NowManila();
        viewYear = manila.Year;
        viewMonth = manila.Month;
        CacheRefs();

        if (mainMenu == null)
        {
            Transform buttons = FindNamedTransform("MainMenu_Buttons");
            if (buttons != null)
                mainMenu = buttons.gameObject;
        }

        if (mainMenu != null)
            mainMenu.SetActive(false);

        EnsureModuleRows();
        if (detailText != null)
            detailText.text = "Tap a day for schedule.";
        HideModuleRows();

        Refresh();
        gameObject.SetActive(true);
        ShowDetail(ProgressionConfig.TodayManila());
        MenuDesktopClickSupport.Apply();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    void PrevMonth()
    {
        viewMonth--;
        if (viewMonth < 1)
        {
            viewMonth = 12;
            viewYear--;
        }

        Refresh();
    }

    void NextMonth()
    {
        viewMonth++;
        if (viewMonth > 12)
        {
            viewMonth = 1;
            viewYear++;
        }

        Refresh();
    }

    void GoToday()
    {
        DateTime manila = ProgressionConfig.NowManila();
        viewYear = manila.Year;
        viewMonth = manila.Month;
        Refresh();
        ShowDetail(ProgressionConfig.TodayManila());
    }

    void Refresh()
    {
        if (titleText != null)
            titleText.text = new DateTime(viewYear, viewMonth, 1).ToString("MMMM yyyy");

        if (cells == null || cells.Count == 0)
            CacheRefs();

        string profileId = ProgressionService.CurrentProfileId();
        HashSet<string> playDates = string.IsNullOrEmpty(profileId)
            ? new HashSet<string>()
            : ClinicScheduleService.CollectAllPlayDates(profileId);

        DateTime first = new DateTime(viewYear, viewMonth, 1);
        int startOffset = ((int)first.DayOfWeek + 6) % 7; // Monday-first
        int daysInMonth = DateTime.DaysInMonth(viewYear, viewMonth);
        string today = ProgressionConfig.TodayManila();

        for (int i = 0; i < cells.Count; i++)
        {
            DayCell cell = cells[i];
            if (cell == null || cell.go == null)
                continue;

            int dayNum = i - startOffset + 1;
            bool inMonth = dayNum >= 1 && dayNum <= daysInMonth;
            cell.go.SetActive(true);
            if (cell.button != null)
                cell.button.interactable = inMonth;

            if (!inMonth)
            {
                if (cell.label != null)
                    cell.label.text = string.Empty;
                if (cell.mark != null)
                    cell.mark.color = Color.clear;
                if (cell.bg != null)
                    cell.bg.color = CellBg * 0.5f;
                cell.date = null;
                continue;
            }

            string date = new DateTime(viewYear, viewMonth, dayNum).ToString("yyyy-MM-dd");
            cell.date = date;
            if (cell.label != null)
                cell.label.text = dayNum.ToString();
            if (cell.bg != null)
                cell.bg.color = date == today ? CellBg * 1.15f : CellBg;

            ClinicDayMark mark = string.IsNullOrEmpty(profileId)
                ? ClinicDayMark.None
                : ClinicScheduleService.GetMark(profileId, date, playDates);

            if (cell.mark != null)
                cell.mark.color = ColorForMark(mark);

            Outline outline = cell.bg != null ? cell.bg.GetComponent<Outline>() : null;
            if (outline != null)
                outline.effectColor = date == today ? TodayOutline : Color.clear;
        }
    }

    void ShowDetail(string date)
    {
        selectedDate = date;
        EnsureModuleRows();

        if (detailText == null || string.IsNullOrEmpty(date))
            return;

        string profileId = ProgressionService.CurrentProfileId();
        if (string.IsNullOrEmpty(profileId))
        {
            detailText.text = date + "\nSelect a profile first.";
            HideModuleRows();
            return;
        }

        // Date only — phase/session live on each module row (modules can diverge).
        detailText.text = date;
        RefreshModuleRows(profileId, date);
        MenuDesktopClickSupport.Apply();
    }

    void RefreshModuleRows(string profileId, string date)
    {
        string today = ProgressionConfig.TodayManila();
        bool isToday = string.Equals(date, today, StringComparison.Ordinal);

        for (int i = 0; i < moduleRows.Count; i++)
        {
            ModuleRow row = moduleRows[i];
            if (row == null || row.go == null)
                continue;

            row.go.SetActive(true);

            ModuleProgress module = ProgressionService.GetModule(profileId, row.moduleId);
            ClinicDayPlan plan = ClinicScheduleService.GetModuleDayPlan(profileId, row.moduleId, date);
            int phase = plan != null
                ? ProgressionConfig.ClampPhase(plan.phase)
                : ResolveModulePlayPhase(module);
            bool unlocked = ProgressionService.IsPhaseUnlocked(profileId, row.moduleId, phase);
            int completedSlots = CountCompletedSessionsInPhase(module, phase);
            bool playedThisDate = ModuleFinishedOnDate(profileId, row.moduleId, phase, date);
            bool sessionSlotDone = plan != null &&
                                   plan.type == (int)ClinicDayPlanType.Session &&
                                   plan.slot > 0 &&
                                   completedSlots >= plan.slot;
            // Done on the credited session day (may be later than early play-on-rest).
            bool showDone = sessionSlotDone || (playedThisDate && isToday &&
                                               (plan == null || plan.type == (int)ClinicDayPlanType.Session));

            if (row.nameLabel != null)
                row.nameLabel.text = BuildModuleRowLabel(row.moduleId, plan, module, phase, unlocked);

            // Soft rest: Start still allowed today if gate says CanPlay (replan credits next session day).
            bool canStart = isToday && unlocked && !playedThisDate &&
                            ProgressionService.CanPlay(profileId, row.moduleId, phase, out _, out _);

            if (row.actionButton == null || row.actionLabel == null)
                continue;

            if (showDone)
            {
                row.actionButton.gameObject.SetActive(true);
                row.actionButton.interactable = false;
                row.actionLabel.text = "Done";
                if (row.actionBg != null)
                    row.actionBg.color = DoneBtnColor;
            }
            else if (canStart)
            {
                row.actionButton.gameObject.SetActive(true);
                row.actionButton.interactable = true;
                row.actionLabel.text = "Start Session";
                if (row.actionBg != null)
                    row.actionBg.color = StartBtnColor;
            }
            else
            {
                row.actionButton.gameObject.SetActive(false);
            }
        }
    }

    static string BuildModuleRowLabel(
        string moduleId,
        ClinicDayPlan plan,
        ModuleProgress module,
        int phase,
        bool unlocked)
    {
        string name = ModuleDisplayName(moduleId);

        if (plan != null && plan.type == (int)ClinicDayPlanType.Rest)
        {
            string rest = name + " Rest Day";
            if (!unlocked)
                rest += " (locked)";
            return rest;
        }

        int session;
        if (plan != null && plan.type == (int)ClinicDayPlanType.Session && plan.slot > 0)
            session = plan.slot;
        else
            session = ResolveModuleSessionNumber(module, phase, plan != null ? plan.date : null);

        string label = name + " Phase " + phase + " Session " + session;
        if (!unlocked)
            label += " (locked)";
        return label;
    }

    void HideModuleRows()
    {
        for (int i = 0; i < moduleRows.Count; i++)
        {
            if (moduleRows[i] != null && moduleRows[i].go != null)
                moduleRows[i].go.SetActive(false);
        }
    }

    void StartModuleSession(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(selectedDate))
            return;

        string today = ProgressionConfig.TodayManila();
        if (!string.Equals(selectedDate, today, StringComparison.Ordinal))
            return;

        string profileId = ProgressionService.CurrentProfileId();
        if (string.IsNullOrEmpty(profileId))
            return;

        ModuleProgress module = ProgressionService.GetModule(profileId, moduleId);
        int phase = ResolveModulePlayPhase(module);

        if (!ProgressionService.CanPlay(profileId, moduleId, phase, out _, out string message))
        {
            if (detailText != null && !string.IsNullOrEmpty(message))
                detailText.text = selectedDate + "\n" + message;
            RefreshModuleRows(profileId, selectedDate);
            return;
        }

        string sceneName = SceneNameForModule(moduleId);
        if (string.IsNullOrEmpty(sceneName))
            return;

        ProgressionSessionContext.Set(moduleId, ProgressionConfig.ModeUnguided, phase);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Phase this module should play next (per-module; may differ across modules).</summary>
    static int ResolveModulePlayPhase(ModuleProgress module)
    {
        if (module == null)
            return 1;

        int unlocked = Math.Max(1, module.unlockedThroughPhase);
        int phase = ProgressionConfig.ClampPhase(module.currentPhase);
        if (phase < 1)
            phase = 1;
        if (phase > unlocked)
            phase = unlocked;

        // If current phase week already passed, advance to next unlocked phase when available.
        if (module.phaseWeeks != null)
        {
            for (int i = 0; i < module.phaseWeeks.Count; i++)
            {
                PhaseWeekProgress week = module.phaseWeeks[i];
                if (week == null || week.phase != phase)
                    continue;
                if (week.weekPassed && phase < unlocked)
                    phase = ProgressionConfig.ClampPhase(phase + 1);
                break;
            }
        }

        return ProgressionConfig.ClampPhase(phase);
    }

    /// <summary>1..4 session index within the module's active phase week.</summary>
    static int ResolveModuleSessionNumber(ModuleProgress module, int phase, string date)
    {
        phase = ProgressionConfig.ClampPhase(phase);
        if (module == null || module.phaseWeeks == null)
            return 1;

        PhaseWeekProgress week = null;
        for (int i = 0; i < module.phaseWeeks.Count; i++)
        {
            PhaseWeekProgress candidate = module.phaseWeeks[i];
            if (candidate != null && candidate.phase == phase)
                week = candidate;
        }

        if (week == null || week.playDates == null || week.playDates.Count == 0)
            return 1;

        if (!string.IsNullOrEmpty(date))
        {
            for (int i = 0; i < week.playDates.Count; i++)
            {
                if (string.Equals(week.playDates[i], date, StringComparison.Ordinal))
                    return Mathf.Clamp(i + 1, 1, ProgressionConfig.MaxPlayDaysPerWeek);
            }
        }

        int next = week.playDates.Count + 1;
        return Mathf.Clamp(next, 1, ProgressionConfig.MaxPlayDaysPerWeek);
    }

    static bool ModuleFinishedOnDate(string profileId, string moduleId, int phase, string date)
    {
        ModuleProgress module = ProgressionService.GetModule(profileId, moduleId);
        if (module == null || module.phaseWeeks == null)
            return false;

        phase = ProgressionConfig.ClampPhase(phase);
        for (int i = 0; i < module.phaseWeeks.Count; i++)
        {
            PhaseWeekProgress week = module.phaseWeeks[i];
            if (week == null || week.phase != phase || week.playDates == null)
                continue;
            if (week.playDates.Contains(date))
                return true;
        }

        return false;
    }

    static int CountCompletedSessionsInPhase(ModuleProgress module, int phase)
    {
        if (module == null || module.phaseWeeks == null)
            return 0;

        phase = ProgressionConfig.ClampPhase(phase);
        int count = 0;
        for (int i = 0; i < module.phaseWeeks.Count; i++)
        {
            PhaseWeekProgress week = module.phaseWeeks[i];
            if (week == null || week.phase != phase || week.playDates == null)
                continue;
            count += week.playDates.Count;
        }

        return Mathf.Min(count, ProgressionConfig.MaxPlayDaysPerWeek);
    }

    static string ModuleDisplayName(string moduleId)
    {
        if (moduleId == ProgressionConfig.ReactionLight) return "Reaction Light";
        if (moduleId == ProgressionConfig.BallDrop) return "Ball Drop";
        if (moduleId == ProgressionConfig.ObjectPlacement) return "Object Placement";
        if (moduleId == ProgressionConfig.PenTrace) return "Pen Trace";
        return moduleId;
    }

    static string SceneNameForModule(string moduleId)
    {
        if (moduleId == ProgressionConfig.ReactionLight) return "Reaction Light";
        if (moduleId == ProgressionConfig.BallDrop) return "Ball Drop";
        if (moduleId == ProgressionConfig.ObjectPlacement) return "Color Sort";
        if (moduleId == ProgressionConfig.PenTrace) return "Pen Control Tracing";
        return null;
    }

    static Color ColorForMark(ClinicDayMark mark)
    {
        switch (mark)
        {
            case ClinicDayMark.PlannedSession: return SessionMark;
            case ClinicDayMark.PlannedRest: return RestMark;
            case ClinicDayMark.CompletedOnPlan: return DoneMark;
            case ClinicDayMark.CompletedEarly: return EarlyMark;
            case ClinicDayMark.Missed: return MissedMark;
            case ClinicDayMark.Today: return TodayMark;
            default: return Color.clear;
        }
    }

    void WireButtons()
    {
        if (calendarButton != null)
        {
            calendarButton.onClick.RemoveListener(Open);
            calendarButton.onClick.AddListener(Open);
        }

        HookChildButton("Prev", PrevMonth);
        HookChildButton("Next", NextMonth);
        HookChildButton("Today", GoToday);
        HookChildButton("Close", Close);

        for (int i = 0; i < cells.Count; i++)
        {
            DayCell cell = cells[i];
            if (cell == null || cell.button == null)
                continue;

            DayCell captured = cell;
            cell.button.onClick.RemoveAllListeners();
            cell.button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(captured.date))
                    ShowDetail(captured.date);
            });
        }
    }

    void HookChildButton(string childName, Action action)
    {
        Transform t = transform.Find(childName);
        if (t == null)
            return;

        Button button = t.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action());
    }

    void CacheRefs()
    {
        if (titleText == null)
        {
            Transform t = transform.Find("Title");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (detailText == null)
        {
            Transform t = transform.Find("Detail");
            if (t != null)
                detailText = t.GetComponent<TMP_Text>();
        }

        if (gridRoot == null)
        {
            Transform t = transform.Find("Grid");
            if (t != null)
                gridRoot = t;
        }

        if (moduleRowsRoot == null)
        {
            Transform t = transform.Find("ModuleRows");
            if (t != null)
                moduleRowsRoot = t;
        }

        EnsureModuleRows();

        if ((cells == null || cells.Count == 0) && gridRoot != null)
        {
            cells = new List<DayCell>();
            for (int i = 0; i < gridRoot.childCount; i++)
            {
                Transform day = gridRoot.GetChild(i);
                DayCell cell = new DayCell
                {
                    go = day.gameObject,
                    button = day.GetComponent<Button>(),
                    bg = day.GetComponent<Image>(),
                    label = day.Find("Num") != null ? day.Find("Num").GetComponent<TMP_Text>() : null,
                    mark = day.Find("Mark") != null ? day.Find("Mark").GetComponent<Image>() : null
                };
                cells.Add(cell);
            }
        }

        if (mainMenu == null)
        {
            Transform buttons = FindNamedTransform("MainMenu_Buttons");
            if (buttons != null)
                mainMenu = buttons.gameObject;
        }

        if (calendarButton == null)
        {
            Transform buttons = FindNamedTransform("MainMenu_Buttons");
            if (buttons != null)
            {
                Transform cal = buttons.Find(CalendarButtonName);
                if (cal != null)
                    calendarButton = cal.GetComponent<Button>();
            }
        }
    }

    /// <summary>
    /// Builds / refreshes the full panel hierarchy under this GameObject (edit mode + play).
    /// </summary>
    public void BuildHierarchy()
    {
        Image panelImage = GetComponent<Image>();
        if (panelImage == null)
            panelImage = gameObject.AddComponent<Image>();
        panelImage.color = PanelBg;

        titleText = EnsureText(transform, "Title", "Month Year", 48, TextAlignmentOptions.Center,
            new Vector2(0.2f, 0.88f), new Vector2(0.8f, 0.98f));

        EnsureNavButton(transform, "Prev", "‹", new Vector2(0.04f, 0.88f), new Vector2(0.14f, 0.98f));
        EnsureNavButton(transform, "Next", "›", new Vector2(0.86f, 0.88f), new Vector2(0.96f, 0.98f));
        EnsureNavButton(transform, "Today", "Today", new Vector2(0.68f, 0.02f), new Vector2(0.84f, 0.1f));
        EnsureNavButton(transform, "Close", "Close", new Vector2(0.86f, 0.02f), new Vector2(0.98f, 0.1f));

        string[] headers = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        for (int i = 0; i < 7; i++)
        {
            float x0 = 0.05f + i * 0.13f;
            EnsureText(transform, "H" + i, headers[i], 26, TextAlignmentOptions.Center,
                new Vector2(x0, 0.8f), new Vector2(x0 + 0.12f, 0.86f));
        }

        Transform grid = transform.Find("Grid");
        if (grid == null)
        {
            GameObject gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(transform, false);
            grid = gridGo.transform;
        }

        gridRoot = grid;
        RectTransform gridRt = grid.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.05f, 0.38f);
        gridRt.anchorMax = new Vector2(0.95f, 0.78f);
        gridRt.offsetMin = Vector2.zero;
        gridRt.offsetMax = Vector2.zero;

        cells = new List<DayCell>();
        for (int i = 0; i < 42; i++)
        {
            DayCell cell = EnsureDayCell(grid, i);
            int row = i / 7;
            int col = i % 7;
            float x0 = col / 7f;
            float x1 = (col + 1) / 7f;
            float y1 = 1f - row / 6f;
            float y0 = 1f - (row + 1) / 6f;
            RectTransform rt = cell.go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0 + 0.01f, y0 + 0.02f);
            rt.anchorMax = new Vector2(x1 - 0.01f, y1 - 0.02f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            cells.Add(cell);
        }

        detailText = EnsureText(transform, "Detail", "Tap a day for schedule.", 26, TextAlignmentOptions.TopLeft,
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.37f));

        EnsureModuleRows();
        EnsureLegend(transform);
    }

    void EnsureModuleRows()
    {
        // Keep room for module rows even if hierarchy was baked with the old layout.
        if (gridRoot != null)
        {
            RectTransform gridRt = gridRoot as RectTransform;
            if (gridRt != null)
            {
                gridRt.anchorMin = new Vector2(0.05f, 0.38f);
                gridRt.anchorMax = new Vector2(0.95f, 0.78f);
                gridRt.offsetMin = Vector2.zero;
                gridRt.offsetMax = Vector2.zero;
            }
        }

        if (detailText != null)
        {
            RectTransform dRt = detailText.rectTransform;
            dRt.anchorMin = new Vector2(0.05f, 0.30f);
            dRt.anchorMax = new Vector2(0.95f, 0.37f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;
        }

        if (moduleRowsRoot == null)
        {
            Transform existing = transform.Find("ModuleRows");
            if (existing != null)
                moduleRowsRoot = existing;
            else
            {
                GameObject go = new GameObject("ModuleRows", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                moduleRowsRoot = go.transform;
            }
        }

        RectTransform rowsRt = moduleRowsRoot.GetComponent<RectTransform>();
        rowsRt.anchorMin = new Vector2(0.05f, 0.10f);
        rowsRt.anchorMax = new Vector2(0.95f, 0.29f);
        rowsRt.offsetMin = Vector2.zero;
        rowsRt.offsetMax = Vector2.zero;

        if (moduleRows == null)
            moduleRows = new List<ModuleRow>();

        // Rebuild list from children if needed.
        if (moduleRows.Count != ProgressionConfig.AllModules.Length)
        {
            moduleRows.Clear();
            for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
                moduleRows.Add(EnsureModuleRow(moduleRowsRoot, ProgressionConfig.AllModules[i], i));
        }
        else
        {
            for (int i = 0; i < ProgressionConfig.AllModules.Length; i++)
            {
                if (moduleRows[i] == null || moduleRows[i].go == null)
                    moduleRows[i] = EnsureModuleRow(moduleRowsRoot, ProgressionConfig.AllModules[i], i);
            }
        }
    }

    ModuleRow EnsureModuleRow(Transform parent, string moduleId, int index)
    {
        string rowName = "Module_" + moduleId;
        Transform existing = parent.Find(rowName);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(rowName, typeof(RectTransform), typeof(Image));

        go.transform.SetParent(parent, false);
        Image bg = go.GetComponent<Image>();
        if (bg == null)
            bg = go.AddComponent<Image>();
        bg.color = new Color(CellBg.r, CellBg.g, CellBg.b, 0.55f);
        bg.raycastTarget = false;

        float y1 = 1f - index * 0.25f;
        float y0 = y1 - 0.23f;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, y0);
        rt.anchorMax = new Vector2(1f, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TMP_Text nameLabel = EnsureText(go.transform, "Name", ModuleDisplayName(moduleId), 24,
            TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.1f), new Vector2(0.55f, 0.9f));

        Transform btnT = go.transform.Find("Action");
        GameObject btnGo;
        if (btnT != null)
            btnGo = btnT.gameObject;
        else
            btnGo = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button));

        btnGo.transform.SetParent(go.transform, false);
        Image btnImg = btnGo.GetComponent<Image>();
        if (btnImg == null)
            btnImg = btnGo.AddComponent<Image>();
        btnImg.color = StartBtnColor;

        Button button = btnGo.GetComponent<Button>();
        if (button == null)
            button = btnGo.AddComponent<Button>();

        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.58f, 0.12f);
        btnRt.anchorMax = new Vector2(0.98f, 0.88f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        TMP_Text actionLabel = EnsureText(btnGo.transform, "Label", "Start Session", 22,
            TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f));

        string capturedId = moduleId;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => StartModuleSession(capturedId));

        go.SetActive(false);
        return new ModuleRow
        {
            moduleId = moduleId,
            go = go,
            nameLabel = nameLabel,
            actionButton = button,
            actionLabel = actionLabel,
            actionBg = btnImg
        };
    }

    DayCell EnsureDayCell(Transform parent, int index)
    {
        string name = "Day" + index;
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));

        go.transform.SetParent(parent, false);
        Image bg = go.GetComponent<Image>();
        if (bg == null)
            bg = go.AddComponent<Image>();
        bg.color = CellBg;

        Outline outline = go.GetComponent<Outline>();
        if (outline == null)
            outline = go.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(3f, 3f);

        if (go.GetComponent<Button>() == null)
            go.AddComponent<Button>();

        TMP_Text label = EnsureText(go.transform, "Num", "", 28, TextAlignmentOptions.Center,
            new Vector2(0f, 0.25f), new Vector2(1f, 1f));

        Transform markT = go.transform.Find("Mark");
        Image mark;
        if (markT == null)
        {
            mark = new GameObject("Mark", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            mark.transform.SetParent(go.transform, false);
        }
        else
            mark = markT.GetComponent<Image>();

        mark.color = Color.clear;
        RectTransform mRt = mark.rectTransform;
        mRt.anchorMin = new Vector2(0.3f, 0.08f);
        mRt.anchorMax = new Vector2(0.7f, 0.28f);
        mRt.offsetMin = Vector2.zero;
        mRt.offsetMax = Vector2.zero;

        return new DayCell
        {
            go = go,
            button = go.GetComponent<Button>(),
            label = label,
            mark = mark,
            bg = bg
        };
    }

    static void EnsureNavButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();
        img.color = CellBg;

        if (go.GetComponent<Button>() == null)
            go.AddComponent<Button>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EnsureText(go.transform, "Label", label, 30, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
    }

    static TMP_Text EnsureText(Transform parent, string name, string value, float size, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(name, typeof(RectTransform));

        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = go.AddComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.enableAutoSizing = false;
        text.raycastTarget = false;

        RectTransform rt = text.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return text;
    }

    static void EnsureLegend(Transform parent)
    {
        EnsureLegendItem(parent, 0, "Session", SessionMark);
        EnsureLegendItem(parent, 1, "Rest", RestMark);
        EnsureLegendItem(parent, 2, "Done", DoneMark);
        EnsureLegendItem(parent, 3, "Early", EarlyMark);
        EnsureLegendItem(parent, 4, "Missed", MissedMark);
    }

    static void EnsureLegendItem(Transform parent, int index, string label, Color color)
    {
        float x0 = 0.05f + index * 0.12f;
        string swatchName = "Leg" + label;
        Transform swatchT = parent.Find(swatchName);
        Image swatch;
        if (swatchT == null)
        {
            swatch = new GameObject(swatchName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            swatch.transform.SetParent(parent, false);
        }
        else
            swatch = swatchT.GetComponent<Image>();

        swatch.color = color;
        RectTransform sRt = swatch.rectTransform;
        sRt.anchorMin = new Vector2(x0, 0.03f);
        sRt.anchorMax = new Vector2(x0 + 0.03f, 0.08f);
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;

        EnsureText(parent, "LegT" + label, label, 20, TextAlignmentOptions.Left,
            new Vector2(x0 + 0.035f, 0.03f), new Vector2(x0 + 0.12f, 0.08f));
    }

    public static void ConfigureWorldCanvas(GameObject panel)
    {
        panel.layer = 5;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null)
            rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1100f, 900f);

        Transform profileWindow = FindNamedTransform("Profile_Window");
        if (profileWindow != null)
        {
            panel.transform.position = profileWindow.position + new Vector3(0.02f, 0f, 0.02f);
            panel.transform.rotation = profileWindow.rotation;
            panel.transform.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
        }
        else
        {
            panel.transform.position = new Vector3(0f, 1.2f, 0.95f);
            panel.transform.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
        }

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 25;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = panel.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;
        scaler.referencePixelsPerUnit = 100f;

        if (panel.GetComponent<GraphicRaycaster>() == null)
            panel.AddComponent<GraphicRaycaster>();

        Type tracked = Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (tracked != null && panel.GetComponent(tracked) == null)
            panel.AddComponent(tracked);
    }

    static Transform FindNamedTransform(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }
}
