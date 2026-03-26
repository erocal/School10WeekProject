using System.Collections.Generic;
using UnityEngine;
public class EnemyList : MonoBehaviour
{

    [Header("玩家"), SerializeField] private GameObject player;

    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float spacing = 2f;
    private List<GameObject> enemies = new List<GameObject>();

    [Header("生成範圍"), SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minZ = -10f;
    [SerializeField] private float maxZ = 10f;

    private void Start()
    {
        SpawnRandomEnemies();
        PrintEnemies();
    }
    private void SpawnRandomEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            PrimitiveType type = (PrimitiveType)Random.Range(0, 3); // 0=Sphere, 1=Capsule, 2=Cylinder 
            GameObject enemy = GameObject.CreatePrimitive(type);
            enemy.name = $"Enemy_{i}_{type}";
            enemy.transform.position = new Vector3(15f + i * spacing, 0f, 15f);
            enemies.Add(enemy);
        }
    }
    private void PrintEnemies()
    {
        int index = 0;
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                Debug.Log($"{index}: {enemy.name}");
            }
            else
            {
                Debug.Log($"{index}: null");
            }
            index++;
        }
    }

    private void SpawnEnemies()
    {

        PrimitiveType[] enemyTypes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule };
        PrimitiveType randomType = enemyTypes[Random.Range(0, enemyTypes.Length)];

        GameObject enemy = GameObject.CreatePrimitive(randomType);
        enemy.transform.SetParent(this.transform);
        enemy.name = $"Enemy_{randomType}";

        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        enemy.transform.position = this.GetComponent<Transform>().position + new Vector3(randomX, 0f, randomZ);

        enemies.Add(enemy);

    }

    private void RemoveOneEnemy()
    {

        if (enemies.Count == 0)
        {
            Debug.Log("No enemies to remove.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("Player not found. Please tag the player as 'Player'.");
            return;
        }

        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(player.transform.position, enemies[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex >= 0)
        {
            GameObject closestEnemy = enemies[closestIndex];
            enemies.RemoveAt(closestIndex);
            Destroy(closestEnemy);
        }

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SpawnEnemies();
            PrintEnemies();
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveOneEnemy();
            PrintEnemies();
        }
    }
}