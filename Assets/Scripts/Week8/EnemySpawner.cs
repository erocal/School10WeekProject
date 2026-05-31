using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyPool enemyPool;

    [Header("Spawn Time")]
    [SerializeField] private float minSpawnTime = 1f;
    [SerializeField] private float maxSpawnTime = 5f;

    [Header("Random Spawn Range")]
    [SerializeField] private Vector2 randomX = new Vector2(-5f, 5f);
    [SerializeField] private Vector2 randomZ = new Vector2(-5f, 5f);
    [SerializeField] private float spawnY = 0f;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            Vector3 randomPosition = new Vector3(
                Random.Range(randomX.x, randomX.y),
                spawnY,
                Random.Range(randomZ.x, randomZ.y)
            );

            enemyPool.SpawnEnemy(randomPosition);
        }
    }
}