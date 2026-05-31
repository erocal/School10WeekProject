using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;

    private ObjectPool<Enemy> pool;
    private int activeEnemyCount = 0;

    public int ActiveEnemyCount => activeEnemyCount;

    private void Awake()
    {
        pool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                Enemy enemy = Instantiate(enemyPrefab);
                enemy.SetPool(this);
                return enemy;
            },
            actionOnGet: enemy =>
            {
                enemy.gameObject.SetActive(true);
                activeEnemyCount++;
                Debug.Log("Active Enemy Count: " + activeEnemyCount);
            },
            actionOnRelease: enemy =>
            {
                activeEnemyCount--;
                Debug.Log("Active Enemy Count: " + activeEnemyCount);
                enemy.gameObject.SetActive(false);
            },
            actionOnDestroy: enemy =>
            {
                Destroy(enemy.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    public Enemy SpawnEnemy(Vector3 position)
    {
        Enemy enemy = pool.Get();
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;
        enemy.BeginLife();

        return enemy;
    }

    public void ReturnEnemy(Enemy enemy)
    {
        pool.Release(enemy);
    }
}