using UnityEngine;
using System.Collections;

public class BallDropSystem : MonoBehaviour
{
    public Transform transformAbove;

    public GameObject redBallPrefab;
    public GameObject blueBallPrefab;

    private Transform[] spawnPoints;

    void Start()
    {
        // Get all 10 spawn points
        spawnPoints = new Transform[transformAbove.childCount];

        for (int i = 0; i < transformAbove.childCount; i++)
        {
            spawnPoints[i] = transformAbove.GetChild(i);
        }

        Debug.Log("Found " + spawnPoints.Length + " spawn points.");

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
        // Pick a random spawn point
        int randomPoint = Random.Range(0, spawnPoints.Length);

        // Randomly choose red or blue
        GameObject ballPrefab;

        if (Random.value < 0.5f)
        {
            ballPrefab = redBallPrefab;
        }
        else
        {
            ballPrefab = blueBallPrefab;
        }

        // Spawn the ball
        Instantiate(
            ballPrefab,
            spawnPoints[randomPoint].position,
            Quaternion.identity
        );
    }
}