using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallDropSystem : MonoBehaviour
{
    public Transform transformAbove;
    public float targetDiameter = 0.15f;

    // TEMP OVERRIDE (not bible Phase I — Ball Drop has no formal Phase I).
    // Bible Phase II = 3×10 = 30 with progression. Revert when wiring real Phase II.
    const bool TemporaryEasyOnly = true;
    public const int BallTotal = TemporaryEasyOnly ? 10 : 30;
    const int ballsPerLevel = 10;
    const float easyFallSpeed = 0.6f;
    const float moderateFallSpeed = 1.2f;
    const float hardFallSpeed = 2f;
    const float easySpawnInterval = 2.6f;
    const float moderateSpawnInterval = 2f;
    const float hardSpawnInterval = 1.4f;

    readonly List<GameObject> ballTemplates = new List<GameObject>();
    BallAssignController assign;
    Transform[] spawnPoints;
    int spawnedBalls;
    bool spawning;
    Coroutine spawnLoop;

    public void Begin(BallAssignController assignController, IList<GameObject> templates)
    {
        assign = assignController;
        ballTemplates.Clear();
        if (templates != null)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                if (templates[i] != null)
                    ballTemplates.Add(templates[i]);
            }
        }

        spawnedBalls = 0;
        spawning = ballTemplates.Count > 0;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        CacheSpawnPoints();

        if (spawnLoop != null)
            StopCoroutine(spawnLoop);

        spawnLoop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    void OnDisable()
    {
        spawnLoop = null;
    }

    void OnEnable()
    {
        if (spawning && spawnedBalls < BallTotal && spawnLoop == null)
            spawnLoop = StartCoroutine(SpawnLoop());
    }

    void Start()
    {
        CacheSpawnPoints();
    }

    void CacheSpawnPoints()
    {
        if (transformAbove == null)
            return;

        List<Transform> found = new List<Transform>();
        for (int i = 0; i < transformAbove.childCount; i++)
        {
            Transform child = transformAbove.GetChild(i);
            if (child == null)
                continue;

            string n = child.name;
            if (n == "SpawnPoint" || n.StartsWith("SpawnPoint"))
            {
                found.Add(child);
                continue;
            }

            if (n.StartsWith("Hanger_"))
            {
                Transform nested = FindSpawnPointChild(child);
                found.Add(nested != null ? nested : child);
            }
        }

        // Fallback: treat direct children as spawn points (legacy flat layout).
        if (found.Count == 0)
        {
            for (int i = 0; i < transformAbove.childCount; i++)
                found.Add(transformAbove.GetChild(i));
        }

        if (spawnPoints != null && spawnPoints.Length == found.Count)
        {
            bool same = true;
            for (int i = 0; i < found.Count; i++)
            {
                if (spawnPoints[i] != found[i])
                {
                    same = false;
                    break;
                }
            }

            if (same)
                return;
        }

        spawnPoints = found.ToArray();
    }

    static Transform FindSpawnPointChild(Transform hanger)
    {
        for (int i = 0; i < hanger.childCount; i++)
        {
            Transform c = hanger.GetChild(i);
            if (c != null && (c.name == "SpawnPoint" || c.name.StartsWith("SpawnPoint")))
                return c;
        }

        return hanger.childCount > 0 ? hanger.GetChild(0) : null;
    }

    IEnumerator SpawnLoop()
    {
        while (spawning && spawnedBalls < BallTotal)
        {
            float fallSpeed = FallSpeedForBall(spawnedBalls);
            float interval = SpawnIntervalForBall(spawnedBalls);
            SpawnBall(fallSpeed);

            float waited = 0f;
            while (waited < interval)
            {
                if (!GamePauseMenu.IsFrozen)
                    waited += Time.deltaTime;

                yield return null;
            }
        }
    }

    static float FallSpeedForBall(int ballIndex)
    {
        if (TemporaryEasyOnly)
            return easyFallSpeed;

        int level = ballIndex / ballsPerLevel;
        if (level <= 0)
            return easyFallSpeed;
        if (level == 1)
            return moderateFallSpeed;
        return hardFallSpeed;
    }

    static float SpawnIntervalForBall(int ballIndex)
    {
        if (TemporaryEasyOnly)
            return easySpawnInterval;

        int level = ballIndex / ballsPerLevel;
        if (level <= 0)
            return easySpawnInterval;
        if (level == 1)
            return moderateSpawnInterval;
        return hardSpawnInterval;
    }

    void SpawnBall(float fallSpeed)
    {
        if (!spawning || spawnPoints == null || spawnPoints.Length == 0 || ballTemplates.Count == 0)
            return;

        int tier = TemporaryEasyOnly ? 0 : spawnedBalls / ballsPerLevel;
        if (tier > 2)
            tier = 2;

        GameObject template = ballTemplates[Random.Range(0, ballTemplates.Count)];
        if (assign != null)
            assign.ShowUpcomingBall(template);

        int randomPoint = Random.Range(0, spawnPoints.Length);
        bool forNdhLeft = ProfileManager.IsNonDominantLeft();
        if (BallDropRound.Instance != null)
            forNdhLeft = BallDropRound.Instance.nonDominantIsLeft;

        SpawnWrapped(template, forNdhLeft, tier, spawnPoints[randomPoint].position, fallSpeed);
        spawnedBalls++;

        if (BallDropRound.Instance != null)
            BallDropRound.Instance.BallSpawned(spawnedBalls, tier);

        if (spawnedBalls >= BallTotal)
            spawning = false;
    }

    public GameObject SpawnOne(GameObject template, bool forLeftHand, float fallSpeed)
    {
        if (template == null)
            return null;

        CacheSpawnPoints();
        Vector3 position = PickSpawnPosition();
        return SpawnWrapped(template, forLeftHand, 0, position, fallSpeed);
    }

    Vector3 PickSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return new Vector3(0f, 1.9f, 0.35f);

        Camera view = Camera.main;
        if (view == null)
            return spawnPoints[spawnPoints.Length / 2].position;

        Vector3 look = view.transform.position + view.transform.forward * 0.45f;
        look.y = spawnPoints[0].position.y;

        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                continue;

            float dist = Vector3.Distance(spawnPoints[i].position, look);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = i;
        }

        return spawnPoints[best].position;
    }

    GameObject SpawnWrapped(GameObject template, bool forLeftHand, int tier, Vector3 position, float fallSpeed)
    {
        GameObject root = new GameObject(template.name);
        root.transform.SetPositionAndRotation(position, Quaternion.identity);

        SphereCollider ballCollider = root.AddComponent<SphereCollider>();
        ballCollider.radius = targetDiameter * 0.5f;
        ballCollider.isTrigger = false;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        GameObject visual = Instantiate(template);
        Collider[] extraColliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < extraColliders.Length; i++)
            DestroyImmediate(extraColliders[i]);

        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.SetActive(true);

        FitBallVisual fit = visual.AddComponent<FitBallVisual>();
        fit.targetDiameter = targetDiameter;
        fit.Fit();

        DropBall identity = root.AddComponent<DropBall>();
        identity.forLeftHand = forLeftHand;
        identity.tier = tier;
        identity.spawnedAt = Time.time;

        BallFall fall = root.AddComponent<BallFall>();
        fall.fallSpeed = fallSpeed;
        root.AddComponent<BallCollision>();
        return root;
    }
}
