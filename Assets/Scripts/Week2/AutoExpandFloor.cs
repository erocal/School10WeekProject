using System.Collections.Generic;
using UnityEngine;

public class AutoExpandFloor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject floorPrefab;

    [Header("Floor Settings")]
    [SerializeField] private Vector2 tileSize = new Vector2(10f, 10f);
    [SerializeField] private int visibleRangeX = 2;   // 左右各幾格
    [SerializeField] private int visibleRangeZ = 2;   // 前後各幾格
    [SerializeField] private float floorY = 0f;       // 地板高度

    // 用格子座標記錄已生成的地板
    private Dictionary<Vector2Int, GameObject> spawnedFloors = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("[AutoExpandFloor] player 沒有指定。");
            enabled = false;
            return;
        }

        if (floorPrefab == null)
        {
            Debug.LogError("[AutoExpandFloor] floorPrefab 沒有指定。");
            enabled = false;
            return;
        }

        UpdateFloors();
    }

    private void Update()
    {
        UpdateFloors();
    }

    private void UpdateFloors()
    {
        Vector2Int playerTile = GetTileCoord(player.position);

        HashSet<Vector2Int> neededTiles = new HashSet<Vector2Int>();

        for (int x = -visibleRangeX; x <= visibleRangeX; x++)
        {
            for (int z = -visibleRangeZ; z <= visibleRangeZ; z++)
            {
                Vector2Int coord = new Vector2Int(playerTile.x + x, playerTile.y + z);
                neededTiles.Add(coord);

                if (!spawnedFloors.ContainsKey(coord))
                {
                    SpawnFloor(coord);
                }
            }
        }

        // 刪除超出範圍的地板
        List<Vector2Int> removeList = new List<Vector2Int>();

        foreach (var kvp in spawnedFloors)
        {
            if (!neededTiles.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                removeList.Add(kvp.Key);
            }
        }

        foreach (var coord in removeList)
        {
            spawnedFloors.Remove(coord);
        }
    }

    private void SpawnFloor(Vector2Int coord)
    {
        Vector3 worldPos = new Vector3(
            coord.x * tileSize.x,
            floorY,
            coord.y * tileSize.y
        );

        GameObject floor = Instantiate(floorPrefab, worldPos, Quaternion.identity, transform);
        floor.name = $"Floor_{coord.x}_{coord.y}";
        spawnedFloors.Add(coord, floor);
    }

    private Vector2Int GetTileCoord(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / tileSize.x);
        int z = Mathf.RoundToInt(position.z / tileSize.y);
        return new Vector2Int(x, z);
    }
}