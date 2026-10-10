using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHands : MonoBehaviour
{
    static readonly HashSet<string> SceneNames = new HashSet<string>
    {
        "Splash_Screen",
        "ProfileSelect",
        "Menu",
        "TrainingRange",
        "PenControlTracing",
        "BallDrop_HalfwayTest",
        "ColorSort",
    };

    static PlayerHands instance;

    public GameObject leftHandPrefab;
    public GameObject rightHandPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (instance != null)
            return;

        PlayerHands prefab = Resources.Load<PlayerHands>("PlayerHands");
        if (prefab == null)
        {
            Debug.LogError("PlayerHands prefab is missing from Resources.");
            return;
        }

        instance = Instantiate(prefab);
        instance.name = "PlayerHands";
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToScene(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !SceneNames.Contains(scene.name))
            return;

        XROrigin[] origins = FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (XROrigin origin in origins)
        {
            if (origin.gameObject.scene != scene)
                continue;

            Apply(origin.transform);
        }
    }

    void Apply(Transform origin)
    {
        foreach (Transform child in origin.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Left Controller Visual" || child.name == "Right Controller Visual")
                child.gameObject.SetActive(false);
        }

        EnsureHand(FindNamedChild(origin, "Left Controller"), leftHandPrefab, true);
        EnsureHand(FindNamedChild(origin, "Right Controller"), rightHandPrefab, false);
    }

    static Transform FindNamedChild(Transform root, string exactName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == exactName)
                return child;
        }

        return null;
    }

    static void EnsureHand(Transform controller, GameObject prefab, bool leftHand)
    {
        if (controller == null || prefab == null)
            return;

        Transform existing = null;
        for (int i = 0; i < controller.childCount; i++)
        {
            Transform child = controller.GetChild(i);
            if (child.name.Contains("Hand Model"))
                existing = child;
        }

        GameObject hand;
        if (existing != null)
        {
            hand = existing.gameObject;
        }
        else
        {
            hand = Instantiate(prefab, controller, false);
            hand.name = prefab.name;
        }

        hand.SetActive(true);

        HandPoseDriver driver = hand.GetComponent<HandPoseDriver>();
        if (driver == null)
            driver = hand.AddComponent<HandPoseDriver>();

        driver.Configure(leftHand);
    }
}
