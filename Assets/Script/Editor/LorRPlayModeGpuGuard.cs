#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Permanent Editor TDR guard: Scene View dual-render + heavy URP FX is the usual cause of
/// "Failed to present D3D11 swapchain due to device reset/removed".
/// On Play: lighten Scene Views and temporarily drop AA; restore on Exit.
/// </summary>
[InitializeOnLoad]
public static class LorRPlayModeGpuGuard
{
    struct SceneViewSnapshot
    {
        public SceneView view;
        public bool drawGizmos;
        public bool fxEnabled;
        public bool showSkybox;
        public bool showFog;
        public bool showFlares;
        public bool showParticleSystems;
        public bool showImageEffects;
    }

    static readonly List<SceneViewSnapshot> snapshots = new List<SceneViewSnapshot>();
    static int savedAntiAliasing = -1;
    static float savedRenderScale = -1f;
    static bool guardActive;

    static LorRPlayModeGpuGuard()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            EnterPlayGuard();
        else if (state == PlayModeStateChange.ExitingPlayMode)
            ExitPlayGuard();
    }

    static void EnterPlayGuard()
    {
        if (guardActive)
            return;
        guardActive = true;
        snapshots.Clear();

        // Focus Game view so Scene View is less likely to keep a huge realtime URP path.
        System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
        {
            EditorWindow game = EditorWindow.GetWindow(gameViewType, false, null, false);
            if (game != null)
                game.Focus();
        }

        foreach (SceneView view in SceneView.sceneViews)
        {
            if (view == null)
                continue;

            SceneView.SceneViewState st = view.sceneViewState;
            snapshots.Add(new SceneViewSnapshot
            {
                view = view,
                drawGizmos = view.drawGizmos,
                fxEnabled = st.fxEnabled,
                showSkybox = st.showSkybox,
                showFog = st.showFog,
                showFlares = st.showFlares,
                showParticleSystems = st.showParticleSystems,
                showImageEffects = st.showImageEffects
            });

            view.drawGizmos = false;
            st.fxEnabled = false;
            st.showSkybox = false;
            st.showFog = false;
            st.showFlares = false;
            st.showParticleSystems = false;
            st.showImageEffects = false;
            view.sceneViewState = st;
            view.Repaint();
        }

        savedAntiAliasing = QualitySettings.antiAliasing;
        QualitySettings.antiAliasing = 0;

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
            urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            savedRenderScale = urp.renderScale;
            if (urp.renderScale > 0.85f)
                urp.renderScale = 0.85f;
        }
    }

    static void ExitPlayGuard()
    {
        if (!guardActive)
            return;
        guardActive = false;

        for (int i = 0; i < snapshots.Count; i++)
        {
            SceneViewSnapshot snap = snapshots[i];
            if (snap.view == null)
                continue;

            snap.view.drawGizmos = snap.drawGizmos;
            SceneView.SceneViewState st = snap.view.sceneViewState;
            st.fxEnabled = snap.fxEnabled;
            st.showSkybox = snap.showSkybox;
            st.showFog = snap.showFog;
            st.showFlares = snap.showFlares;
            st.showParticleSystems = snap.showParticleSystems;
            st.showImageEffects = snap.showImageEffects;
            snap.view.sceneViewState = st;
        }
        snapshots.Clear();

        if (savedAntiAliasing >= 0)
        {
            QualitySettings.antiAliasing = savedAntiAliasing;
            savedAntiAliasing = -1;
        }

        if (savedRenderScale > 0f)
        {
            UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
                urp.renderScale = savedRenderScale;
            savedRenderScale = -1f;
        }
    }

    [MenuItem("LorR/Performance/Apply TDR-Safe PC Graphics Now")]
    static void ApplyNow()
    {
        QualitySettings.antiAliasing = Mathf.Min(QualitySettings.antiAliasing, 2);
        UniversalRenderPipelineAsset urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.msaaSampleCount = Mathf.Min(urp.msaaSampleCount, 2);
            urp.shadowDistance = Mathf.Min(urp.shadowDistance, 25f);
            urp.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(urp);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "LorR TDR Guard",
            "Applied TDR-safe PC graphics tweaks.\n\n" +
            "Also auto-runs on every Play Mode enter:\n" +
            "• lightens Scene View FX\n" +
            "• focuses Game view\n" +
            "• drops AA / render scale temporarily",
            "OK");
    }
}
#endif
