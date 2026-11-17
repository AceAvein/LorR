using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PenTraceFloor : MonoBehaviour
{
    const string FloorName = "FLOOR";
    const string NeonFloorName = "Neon Floor";

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

        if (FindAnyObjectByType<PenTraceFloor>() != null)
            return;

        GameObject host = new GameObject("PenTraceFloor");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PenTraceFloor>();
    }

    void Awake()
    {
        Transform floor = EnsureFloor();
        EnsureCollider(floor);
        PlacePlayerOnFloor(floor);
    }

    Transform EnsureFloor()
    {
        Transform floor = FindNamed(FloorName);
        if (floor != null)
            return floor;

        Transform neon = FindNamed(NeonFloorName);
        if (neon != null)
        {
            neon.name = FloorName;
            return neon;
        }

        GameObject created = GameObject.CreatePrimitive(PrimitiveType.Plane);
        created.name = FloorName;
        created.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        created.transform.localScale = new Vector3(100f, 1f, 100f);
        SceneManager.MoveGameObjectToScene(created, gameObject.scene);

        Renderer renderer = created.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        return created.transform;
    }

    static void EnsureCollider(Transform floor)
    {
        if (floor == null)
            return;

        Collider[] colliders = floor.GetComponents<Collider>();
        bool hasSolid = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            colliders[i].isTrigger = false;
            hasSolid = true;
        }

        if (hasSolid)
            return;

        MeshFilter filter = floor.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            MeshCollider meshCollider = floor.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            return;
        }

        BoxCollider box = floor.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = new Vector3(10f, 0.05f, 10f);
        box.center = new Vector3(0f, -0.025f, 0f);
    }

    static void PlacePlayerOnFloor(Transform floor)
    {
        if (floor == null)
            return;

        XROrigin origin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Exclude);
        if (origin == null)
            return;

        float floorY = FloorSurfaceY(floor);
        Transform originTransform = origin.Origin != null ? origin.Origin.transform : origin.transform;
        Vector3 position = originTransform.position;
        if (position.y < floorY + 0.01f)
        {
            position.y = floorY;
            originTransform.position = position;
        }

        CharacterController controller = origin.GetComponent<CharacterController>();
        if (controller == null && origin.Origin != null)
            controller = origin.Origin.GetComponent<CharacterController>();
        if (controller == null)
            controller = origin.GetComponentInChildren<CharacterController>();

        if (controller != null && !controller.enabled)
            controller.enabled = true;
    }

    public static float GetFloorSurfaceY()
    {
        Transform floor = FindNamed(FloorName);
        if (floor == null)
            floor = FindNamed(NeonFloorName);
        if (floor == null)
            return 0f;

        return FloorSurfaceY(floor);
    }

    static float FloorSurfaceY(Transform floor)
    {
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds.max.y;

        Collider collider = floor.GetComponent<Collider>();
        if (collider != null)
            return collider.bounds.max.y;

        return floor.position.y;
    }

    public static Transform FindNamed(string objectName)
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
