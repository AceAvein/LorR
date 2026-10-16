using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BallDropSky : MonoBehaviour
{
    public Material easySky;
    public Material moderateSky;
    public Material hardSky;
    public Material easyGrid;
    public Material moderateGrid;
    public Material hardGrid;
    public Renderer floorRenderer;

    int appliedTier = -1;

    void Awake()
    {
        EnsureFloor();
        EnsureGlow();
        Apply(0);
    }

    public static void ApplyCurrent(int tier)
    {
        BallDropSky sky = FindFirstObjectByType<BallDropSky>(FindObjectsInactive.Include);
        if (sky != null)
            sky.Apply(tier);
    }

    public void Apply(int tier)
    {
        if (tier < 0)
            tier = 0;
        if (tier > 2)
            tier = 2;
        if (tier == appliedTier)
            return;

        appliedTier = tier;
        Material sky = SkyFor(tier);
        Material grid = GridFor(tier);
        if (sky != null)
        {
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }

        if (floorRenderer != null && grid != null)
            floorRenderer.sharedMaterial = grid;
    }

    void EnsureFloor()
    {
        if (floorRenderer != null)
            return;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Neon Floor";
        Collider collider = floor.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localRotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(100f, 1f, 100f);
        floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer != null)
        {
            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            floorRenderer.receiveShadows = false;
        }
    }

    void EnsureGlow()
    {
        Volume volume = GetComponent<Volume>();
        if (volume == null)
            volume = gameObject.AddComponent<Volume>();

        volume.isGlobal = true;
        volume.priority = 50f;

        Bloom bloom;
        if (volume.profile != null && volume.profile.TryGet(out bloom))
            return;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.5f);
        bloom.scatter.Override(0.45f);
        volume.profile = profile;
    }

    Material SkyFor(int tier)
    {
        if (tier <= 0)
            return easySky;
        if (tier == 1)
            return moderateSky;
        return hardSky;
    }

    Material GridFor(int tier)
    {
        if (tier <= 0)
            return easyGrid;
        if (tier == 1)
            return moderateGrid;
        return hardGrid;
    }
}
