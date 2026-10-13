using System.Collections;
using UnityEngine;

public class BallDropSystem : MonoBehaviour
{
    public Transform transformAbove;
    public float targetDiameter = 0.15f;

    GameObject leftBallTemplate;
    GameObject rightBallTemplate;
    Transform[] spawnPoints;

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
            SpawnBall();
            yield return new WaitForSeconds(2f);
        }
    }

    void SpawnBall()
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || leftBallTemplate == null || rightBallTemplate == null)
            return;

        int randomPoint = Random.Range(0, spawnPoints.Length);
        bool forLeftHand = Random.value < 0.5f;
        GameObject template = forLeftHand ? leftBallTemplate : rightBallTemplate;

        SpawnWrapped(template, forLeftHand, spawnPoints[randomPoint].position);
    }

    void SpawnWrapped(GameObject template, bool forLeftHand, Vector3 position)
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

        root.AddComponent<BallFall>();
        root.AddComponent<BallCollision>();
    }
}
