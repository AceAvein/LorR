using System.Collections;
using UnityEngine;

public class BallDropSystem : MonoBehaviour
{
    public Transform transformAbove;
    public float targetDiameter = 0.15f;

    const int ballsPerLevel = 10;
    const float easyFallSpeed = 1.2f;
    const float moderateFallSpeed = 2f;
    const float hardFallSpeed = 3f;
    const float easySpawnInterval = 2.6f;
    const float moderateSpawnInterval = 2f;
    const float hardSpawnInterval = 1.4f;

    GameObject leftBallTemplate;
    GameObject rightBallTemplate;
    Transform[] spawnPoints;
    int spawnedBalls;

    public void Begin(GameObject leftTemplate, GameObject rightTemplate)
    {
        leftBallTemplate = leftTemplate;
        rightBallTemplate = rightTemplate;
        gameObject.SetActive(true);
    }

    void Start()
    {
        spawnPoints = new Transform[transformAbove.childCount];

        for (int i = 0; i < transformAbove.childCount; i++)
            spawnPoints[i] = transformAbove.GetChild(i);

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float fallSpeed = FallSpeedForBall(spawnedBalls);
            float interval = SpawnIntervalForBall(spawnedBalls);
            SpawnBall(fallSpeed);
            yield return new WaitForSeconds(interval);
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
        if (spawnPoints == null || spawnPoints.Length == 0 || leftBallTemplate == null || rightBallTemplate == null)
            return;

        int randomPoint = Random.Range(0, spawnPoints.Length);
        bool forLeftHand = Random.value < 0.5f;
        GameObject template = forLeftHand ? leftBallTemplate : rightBallTemplate;

        SpawnWrapped(template, forLeftHand, spawnPoints[randomPoint].position, fallSpeed);
        spawnedBalls++;
    }

    void SpawnWrapped(GameObject template, bool forLeftHand, Vector3 position, float fallSpeed)
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

        BallFall fall = root.AddComponent<BallFall>();
        fall.fallSpeed = fallSpeed;
        root.AddComponent<BallCollision>();
    }
}
