using System.Collections;
using UnityEngine;

public class BallDropSystem : MonoBehaviour
{
    public Transform transformAbove;
    public float targetDiameter = 0.15f;

    public const int BallTotal = 30;
    const int ballsPerLevel = 10;
    const float easyFallSpeed = 0.6f;
    const float moderateFallSpeed = 1.2f;
    const float hardFallSpeed = 2f;
    const float easySpawnInterval = 2.6f;
    const float moderateSpawnInterval = 2f;
    const float hardSpawnInterval = 1.4f;

    GameObject leftBallTemplate;
    GameObject rightBallTemplate;
    Transform[] spawnPoints;
    int spawnedBalls;
    bool spawning;
    Coroutine spawnLoop;

    public void Begin(GameObject leftTemplate, GameObject rightTemplate)
    {
        leftBallTemplate = leftTemplate;
        rightBallTemplate = rightTemplate;
        spawnedBalls = 0;
        spawning = true;

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

        if (spawnPoints != null && spawnPoints.Length == transformAbove.childCount)
            return;

        spawnPoints = new Transform[transformAbove.childCount];

        for (int i = 0; i < transformAbove.childCount; i++)
            spawnPoints[i] = transformAbove.GetChild(i);
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
        int level = ballIndex / ballsPerLevel;
        if (level <= 0)
            return easyFallSpeed;
        if (level == 1)
            return moderateFallSpeed;
        return hardFallSpeed;
    }

    static float SpawnIntervalForBall(int ballIndex)
    {
        int level = ballIndex / ballsPerLevel;
        if (level <= 0)
            return easySpawnInterval;
        if (level == 1)
            return moderateSpawnInterval;
        return hardSpawnInterval;
    }

    void SpawnBall(float fallSpeed)
    {
        if (!spawning || spawnPoints == null || spawnPoints.Length == 0 || leftBallTemplate == null || rightBallTemplate == null)
            return;

        int tier = spawnedBalls / ballsPerLevel;
        if (tier > 2)
            tier = 2;

        int randomPoint = Random.Range(0, spawnPoints.Length);
        bool forLeftHand = Random.value < 0.5f;
        GameObject template = forLeftHand ? leftBallTemplate : rightBallTemplate;

        SpawnWrapped(template, forLeftHand, tier, spawnPoints[randomPoint].position, fallSpeed);
        spawnedBalls++;

        if (BallDropRound.Instance != null)
            BallDropRound.Instance.BallSpawned(spawnedBalls, tier);

        if (spawnedBalls >= BallTotal)
            spawning = false;
    }

    void SpawnWrapped(GameObject template, bool forLeftHand, int tier, Vector3 position, float fallSpeed)
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
    }
}
