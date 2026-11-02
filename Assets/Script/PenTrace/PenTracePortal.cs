using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class PenTracePortal : MonoBehaviour
{
    public static PenTracePortal Instance { get; private set; }

    const string PortalName = "PORTAL";
    const string WhiteBoardName = "WHITE BOARD";

    [SerializeField] float enterRadius = 0.55f;
    [SerializeField] float standStaySeconds = 0.25f;

    Transform portal;
    Transform whiteBoard;
    Countdown countdown;
    XROrigin xrOrigin;
    Behaviour[] locomotion;
    bool[] locomotionWasEnabled;

    bool armed;
    bool locked;
    bool starting;
    float insideSeconds;
    Vector3 lockedPosition;
    Quaternion lockedRotation;

    public bool IsLocked
    {
        get { return locked; }
    }

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

        if (ModuleScenes.IsGuided(scene.name))
            return;

        if (FindAnyObjectByType<PenTracePortal>() != null)
            return;

        GameObject host = new GameObject("PenTracePortal");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PenTracePortal>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveRefs();
        PreparePortalCollider();
        CacheLocomotion();
        EnsureLocomotionEnabled();
    }

    void EnsureLocomotionEnabled()
    {
        if (locked)
            return;

        if (locomotion == null)
            CacheLocomotion();

        if (locomotion == null)
            return;

        for (int i = 0; i < locomotion.Length; i++)
        {
            if (locomotion[i] != null)
                locomotion[i].enabled = true;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ArmForCountdown(Countdown nextCountdown)
    {
        if (ModuleScenes.IsGuided(SceneManager.GetActiveScene().name))
            return;

        countdown = nextCountdown;
        armed = true;
        starting = false;
        insideSeconds = 0f;
        ResolveRefs();
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (locked)
        {
            HoldLock();
            return;
        }

        if (!armed || starting)
            return;

        if (!IsPlayerInsidePortal())
        {
            insideSeconds = 0f;
            return;
        }

        insideSeconds += Time.deltaTime;
        if (insideSeconds < standStaySeconds)
            return;

        BeginAtPortal();
    }

    void BeginAtPortal()
    {
        starting = true;
        armed = false;
        insideSeconds = 0f;

        SnapPlayerToPortal();
        LockLocomotion(true);

        if (countdown == null)
            countdown = FindFirstObjectByType<Countdown>(FindObjectsInactive.Include);

        if (countdown != null)
            countdown.StartCountdown();
        else
            Debug.LogWarning("PenTracePortal could not find Countdown");
    }

    void SnapPlayerToPortal()
    {
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Exclude);

        if (xrOrigin == null || portal == null)
            return;

        Camera headset = xrOrigin.Camera;
        if (headset == null)
            headset = Camera.main;
        if (headset == null)
            return;

        Vector3 stand = portal.position;
        stand.y = headset.transform.position.y;
        xrOrigin.MoveCameraToWorldLocation(stand);

        Vector3 face = FaceDirectionFromPortal();
        if (face.sqrMagnitude > 0.001f)
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, face);

        Transform originTransform = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
        lockedPosition = originTransform.position;
        lockedRotation = originTransform.rotation;
        locked = true;
    }

    Vector3 FaceDirectionFromPortal()
    {
        if (whiteBoard == null)
            return portal != null ? portal.forward : Vector3.forward;

        Vector3 target = WhiteBoardCenter();
        Vector3 from = portal != null ? portal.position : target;
        Vector3 flat = target - from;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
        {
            Vector3 normal = WhiteBoardNormal();
            flat = new Vector3(normal.x, 0f, normal.z);
        }

        return flat.sqrMagnitude > 0.001f ? flat.normalized : Vector3.forward;
    }

    void HoldLock()
    {
        if (xrOrigin == null)
            return;

        Transform originTransform = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
        originTransform.SetPositionAndRotation(lockedPosition, lockedRotation);
    }

    void LockLocomotion(bool on)
    {
        if (locomotion == null)
            CacheLocomotion();

        if (locomotion == null)
            return;

        for (int i = 0; i < locomotion.Length; i++)
        {
            if (locomotion[i] == null)
                continue;

            if (on)
            {
                if (locomotionWasEnabled != null && i < locomotionWasEnabled.Length)
                    locomotionWasEnabled[i] = locomotion[i].enabled;
                locomotion[i].enabled = false;
            }
            else if (locomotionWasEnabled != null && i < locomotionWasEnabled.Length)
            {
                locomotion[i].enabled = locomotionWasEnabled[i];
            }
        }
    }

    void CacheLocomotion()
    {
        LocomotionProvider[] providers = FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        locomotion = providers;
        locomotionWasEnabled = new bool[providers.Length];
        for (int i = 0; i < providers.Length; i++)
            locomotionWasEnabled[i] = providers[i] != null && providers[i].enabled;
    }

    bool IsPlayerInsidePortal()
    {
        if (portal == null)
            return false;

        Camera headset = xrOrigin != null ? xrOrigin.Camera : Camera.main;
        if (headset == null)
            return false;

        Vector3 feet = headset.transform.position;
        feet.y = portal.position.y;
        float radius = PortalRadius();
        return Vector3.Distance(feet, portal.position) <= radius;
    }

    float PortalRadius()
    {
        if (portal == null)
            return enterRadius;

        Vector3 scale = portal.lossyScale;
        float scaled = 0.5f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        return Mathf.Max(enterRadius, scaled + 0.05f);
    }

    void PreparePortalCollider()
    {
        if (portal == null)
            return;

        Collider col = portal.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void ResolveRefs()
    {
        if (portal == null)
            portal = FindNamed(PortalName);

        if (whiteBoard == null)
            whiteBoard = FindNamed(WhiteBoardName);

        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);

        if (countdown == null)
            countdown = FindFirstObjectByType<Countdown>(FindObjectsInactive.Include);
    }

    public static Transform WhiteBoardTransform()
    {
        if (Instance != null && Instance.whiteBoard != null)
            return Instance.whiteBoard;

        return FindNamed(WhiteBoardName);
    }

    public static Vector3 WhiteBoardCenter()
    {
        Transform board = WhiteBoardTransform();
        if (board == null)
            return Vector3.zero;

        Renderer renderer = board.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds.center;

        return board.position;
    }

    public static Vector3 WhiteBoardNormal()
    {
        Transform board = WhiteBoardTransform();
        if (board == null)
            return Vector3.forward;

        Transform axisSource = board.parent != null ? board.parent : board;
        Vector3 thin = ThinAxis(axisSource);
        Vector3 toPortal = Vector3.zero;
        Transform portalTransform = Instance != null ? Instance.portal : FindNamed(PortalName);
        if (portalTransform != null)
            toPortal = portalTransform.position - WhiteBoardCenter();

        if (toPortal.sqrMagnitude > 0.001f && Vector3.Dot(thin, toPortal) < 0f)
            thin = -thin;

        return thin.normalized;
    }

    static Vector3 ThinAxis(Transform source)
    {
        Vector3 lossy = source.lossyScale;
        float ax = Mathf.Abs(lossy.x);
        float ay = Mathf.Abs(lossy.y);
        float az = Mathf.Abs(lossy.z);

        if (ax <= ay && ax <= az)
            return source.right;
        if (ay <= ax && ay <= az)
            return source.up;
        return source.forward;
    }

    static Transform FindNamed(string objectName)
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
