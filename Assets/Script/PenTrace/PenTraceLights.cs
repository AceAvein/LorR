using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PenTraceLights : MonoBehaviour
{
    public static PenTraceLights Instance { get; private set; }

    const string LightObjectName = "LIGHT";

    [SerializeField] Color glowColor = Color.white;
    [SerializeField] float emissionStrength = 3.5f;
    [SerializeField] float lightIntensity = 10f;
    [SerializeField] float lightRange = 12f;
    [SerializeField] float ambientIntensity = 1.35f;
    [SerializeField] Vector3 lightLocalOffset = new Vector3(0f, -0.08f, 0f);

    Color savedAmbientSky;
    Color savedAmbientEquator;
    Color savedAmbientGround;
    float savedAmbientIntensity;
    AmbientMode savedAmbientMode;
    bool ambientSaved;

    class Lamp
    {
        public Transform transform;
        public Renderer renderer;
        public Material material;
        public Light light;
    }

    readonly List<Lamp> lamps = new List<Lamp>();
    bool lit = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    static void TryCreate(Scene scene)
    {
        if (!scene.IsValid() || !ModuleScenes.IsPenTrace(scene.name))
            return;

        if (FindAnyObjectByType<PenTraceLights>() != null)
            return;

        GameObject host = new GameObject("PenTraceLights");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PenTraceLights>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindLamps();
        SetLit(true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetLit(bool on)
    {
        lit = on;
        Color emission = on ? glowColor * emissionStrength : Color.black;

        for (int i = 0; i < lamps.Count; i++)
        {
            Lamp lamp = lamps[i];
            if (lamp.material != null)
            {
                if (lamp.material.HasProperty("_BaseColor"))
                    lamp.material.SetColor("_BaseColor", on ? glowColor : Color.gray);
                if (lamp.material.HasProperty("_Color"))
                    lamp.material.SetColor("_Color", on ? glowColor : Color.gray);
                if (lamp.material.HasProperty("_EmissionColor"))
                    lamp.material.SetColor("_EmissionColor", emission);
            }

            if (lamp.light != null)
            {
                lamp.light.color = glowColor;
                lamp.light.enabled = on;
                lamp.light.intensity = on ? lightIntensity : 0f;
                lamp.light.range = lightRange;
            }
        }

        ApplyRoomFill(on);
    }

    public void SetIntensity(float intensity)
    {
        lightIntensity = Mathf.Max(0f, intensity);
        if (!lit)
            return;

        for (int i = 0; i < lamps.Count; i++)
        {
            if (lamps[i].light != null)
                lamps[i].light.intensity = lightIntensity;
        }
    }

    public int LampCount
    {
        get { return lamps.Count; }
    }

    void FindLamps()
    {
        lamps.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate.name != LightObjectName)
                continue;

            Renderer renderer = candidate.GetComponent<Renderer>();
            if (renderer == null)
                renderer = candidate.GetComponentInChildren<Renderer>(true);

            Lamp lamp = new Lamp();
            lamp.transform = candidate;
            lamp.renderer = renderer;
            lamp.material = PrepareEmission(renderer);
            lamp.light = EnsurePointLight(candidate);
            lamps.Add(lamp);
        }

        if (lamps.Count == 0)
            Debug.LogWarning("PenTraceLights could not find any scene objects named " + LightObjectName);
    }

    Material PrepareEmission(Renderer renderer)
    {
        if (renderer == null)
            return null;

        Material material = renderer.material;
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", glowColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", glowColor);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", glowColor * emissionStrength);

        return material;
    }

    Light EnsurePointLight(Transform host)
    {
        Transform existing = host.Find("Point Light");
        GameObject lightObject;
        if (existing != null)
            lightObject = existing.gameObject;
        else
        {
            lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(host, false);
            lightObject.transform.localPosition = lightLocalOffset;
            lightObject.transform.localRotation = Quaternion.identity;
        }

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
            light = lightObject.AddComponent<Light>();

        light.type = LightType.Point;
        light.color = glowColor;
        light.intensity = lightIntensity;
        light.range = lightRange;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        return light;
    }

    void ApplyRoomFill(bool on)
    {
        if (!ambientSaved)
        {
            savedAmbientSky = RenderSettings.ambientSkyColor;
            savedAmbientEquator = RenderSettings.ambientEquatorColor;
            savedAmbientGround = RenderSettings.ambientGroundColor;
            savedAmbientIntensity = RenderSettings.ambientIntensity;
            savedAmbientMode = RenderSettings.ambientMode;
            ambientSaved = true;
        }

        if (on)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientEquatorColor = new Color(0.92f, 0.92f, 0.92f);
            RenderSettings.ambientGroundColor = new Color(0.75f, 0.75f, 0.75f);
            RenderSettings.ambientIntensity = ambientIntensity;
        }
        else
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedAmbientSky;
            RenderSettings.ambientEquatorColor = savedAmbientEquator;
            RenderSettings.ambientGroundColor = savedAmbientGround;
            RenderSettings.ambientIntensity = savedAmbientIntensity;
        }
    }
}
