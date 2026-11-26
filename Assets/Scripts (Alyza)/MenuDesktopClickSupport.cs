using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Makes Menu world-space UI clickable with mouse when not in VR (editor / desktop testing).
/// Binds event cameras, ensures GraphicRaycasters, and prefers mouse over ghost XR pointers.
/// </summary>
public static class MenuDesktopClickSupport
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyIfMenu(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyIfMenu(scene);
    }

    static void ApplyIfMenu(Scene scene)
    {
        if (scene.name != "Menu")
            return;

        Apply();
        EnsureRunner().StartCoroutine(ReapplyAfterCameraReady());
    }

    static IEnumerator ReapplyAfterCameraReady()
    {
        // XR Origin camera / EventSystem can initialize a frame or two late.
        yield return null;
        Apply();
        yield return new WaitForSecondsRealtime(0.25f);
        Apply();
    }

    static MenuDesktopClickRunner EnsureRunner()
    {
        MenuDesktopClickRunner runner = Object.FindFirstObjectByType<MenuDesktopClickRunner>();
        if (runner != null)
            return runner;

        GameObject go = new GameObject("MenuDesktopClickSupport");
        Object.DontDestroyOnLoad(go);
        return go.AddComponent<MenuDesktopClickRunner>();
    }

    public static void Apply()
    {
        bool xrRunning = IsXrDisplayRunning();
        Camera eventCam = ResolveEventCamera();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        EnsureEventSystemMouse(xrRunning);
        BindWorldCanvases(eventCam);
    }

    sealed class MenuDesktopClickRunner : MonoBehaviour { }

    static bool IsXrDisplayRunning()
    {
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        for (int i = 0; i < displays.Count; i++)
        {
            if (displays[i] != null && displays[i].running)
                return true;
        }

        return false;
    }

    static Camera ResolveEventCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.enabled && main.gameObject.activeInHierarchy)
            return main;

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                return cam;
        }

        return null;
    }

    static void EnsureEventSystemMouse(bool xrRunning)
    {
        EventSystem es = EventSystem.current;
        if (es == null)
            es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
            return;

        // XRUIInputModule (and compatible modules) expose these flags.
        Behaviour[] behaviours = es.GetComponents<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour b = behaviours[i];
            if (b == null)
                continue;

            string typeName = b.GetType().Name;
            if (typeName != "XRUIInputModule" && typeName != "InputSystemUIInputModule")
                continue;

            SetBoolProperty(b, "enableMouseInput", true);
            SetBoolProperty(b, "enableTouchInput", true);
            SetBoolProperty(b, "enableBuiltinActionsAsFallback", true);

            // Without a live headset, ghost XR pointers often eat UI events — prefer mouse.
            if (typeName == "XRUIInputModule")
                SetBoolProperty(b, "enableXRInput", xrRunning);
        }
    }

    static void BindWorldCanvases(Camera eventCam)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                continue;

            if (eventCam != null)
                canvas.worldCamera = eventCam;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Easier mouse hits on world boards that aren't perfectly camera-facing.
            raycaster.ignoreReversedGraphics = false;
            canvas.overrideSorting = canvas.overrideSorting;
        }
    }

    static void SetBoolProperty(object target, string propertyName, bool value)
    {
        var prop = target.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
        {
            prop.SetValue(target, value);
            return;
        }

        var field = target.GetType().GetField(propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(target, value);
    }
}
