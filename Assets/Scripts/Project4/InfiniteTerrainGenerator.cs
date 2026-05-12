using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainGenerator : MonoBehaviour
{

    #region -- 設定 --

    [Header("References")]
    public Transform player;

    // 順序：0=第一層(最低), 1=第二層, 2=第三層, 3=第四層(含陡坡), 4=第五層(最高)
    public TerrainLayer[] terrainLayers;

    [Header("Chunk Settings")]
    public int chunkSize = 256;
    public int heightmapResolution = 257; // 必須是 2^n + 1
    public float maxHeight = 80f;
    public int viewDistanceInChunks = 2;

    [Header("Pool Settings")]
    public bool prewarmPoolOnStart = true;
    public int extraPooledChunks = 2;
    public Transform chunkParent;

    [Header("Noise (Terrain Shape)")]
    public int seed = 12345;
    public float noiseScale = 0.0025f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightPower = 1f;

    [Header("Texture Blend Weights")]
    public float layer0Weight = 0.30f;
    public float layer1Weight = 0.20f;
    public float layer2Weight = 0.20f;
    public float layer3Weight = 0.20f;
    public float layer4Weight = 0.10f;

    [Header("Texture Noise Blend")]
    public bool enableTextureNoiseBlend = true;

    // 大尺度變化：控制大片區域偏向哪種材質
    public float textureMacroNoiseScale = 0.006f;

    // 小尺度變化：製造局部斑駁、破碎感
    public float textureDetailNoiseScale = 0.045f;

    // 擾動強度，越大越不均勻
    [Range(0f, 3f)]
    public float textureNoiseStrength = 1.8f;

    // 細節擾動強度
    [Range(0f, 3f)]
    public float textureDetailStrength = 1.2f;

    // 權重銳利度，越高越容易讓某些 Layer 局部變明顯
    [Range(0.25f, 4f)]
    public float textureContrast = 2.2f;

    [Header("Startup")]
    public bool snapPlayerToTerrainOnStart = true;
    public float playerSpawnYOffset = 2f;

    [Header("Diagonal Canyon Settings")]
    public bool enableDiagonalCanyon = true;

    // 通過原點的對角線方向。
    // (1, 1) 代表峽谷沿著 Z = X 的方向通過原點。
    // (1, -1) 則代表沿著 Z = -X。
    public Vector2 diagonalCanyonDirection = new Vector2(1f, 1f);

    // 谷底「平坦區」的完整寬度。
    // 需求是至少 100。
    public float canyonFlatBottomWidth = 100f;

    // 峽谷兩側斜坡過渡寬度。
    // 越大代表峽谷越寬、邊坡越平緩。
    public float canyonSideSlopeWidth = 180f;

    // 峽谷底部高度，這裡設為 0。
    [Range(0f, 1f)]
    public float canyonBottomHeight01 = 0f;

    // 峽谷切割強度。
    // 1 代表完全切到 canyonBottomHeight01。
    [Range(0f, 1f)]
    public float canyonCarveStrength = 1f;

    [Header("Plateau / Height Clamp Settings")]
    public bool enableHeightClamp = true;

    // 超過這個世界高度就會被切平。
    // 例如 maxHeight = 800，clampHeight = 400，代表高於 400 的地形都會變成 400。
    public float clampHeight = 400f;

    // 是否讓截平邊緣稍微平滑。
    // false = 硬切平，山頂會很明顯被削掉。
    // true = 接近 clampHeight 時慢慢壓平。
    public bool smoothClampEdge = false;

    // 平滑過渡範圍，只有 smoothClampEdge = true 時有效。
    // 例如 30 代表 370~400 之間逐漸壓平。
    public float clampSmoothRange = 30f;

    #endregion

    // Active chunks
    private Dictionary<Vector2Int, Terrain> chunks = new Dictionary<Vector2Int, Terrain>();

    // Inactive pooled terrains
    private Queue<Terrain> chunkPool = new Queue<Terrain>();

    private Vector2Int lastPlayerChunk;

    // Seed offsets
    private float seedOffsetX;
    private float seedOffsetZ;

    private int pooledChunkIndex = 0;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Assign Player Transform in InfiniteTerrainGenerator.");
            enabled = false;
            return;
        }

        if (chunkParent == null)
        {
            GameObject parentObj = new GameObject("TerrainChunkPool");
            chunkParent = parentObj.transform;
        }

        System.Random rng = new System.Random(seed);
        seedOffsetX = (float)(rng.NextDouble() * 10000.0);
        seedOffsetZ = (float)(rng.NextDouble() * 10000.0);

        if (prewarmPoolOnStart)
        {
            int visibleChunkCount = GetVisibleChunkCount();
            int poolCount = visibleChunkCount + Mathf.Max(0, extraPooledChunks);

            PrewarmPool(poolCount);
        }

        lastPlayerChunk = GetPlayerChunkCoord();

        if (!chunks.ContainsKey(lastPlayerChunk))
            ActivateChunk(lastPlayerChunk);

        if (snapPlayerToTerrainOnStart)
            SnapPlayerToCurrentChunk();

        UpdateChunks();
    }

    void Update()
    {
        if (player == null) return;

        Vector2Int currentChunk = GetPlayerChunkCoord();

        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    // ---------- Pool Management ----------

    int GetVisibleChunkCount()
    {
        int diameter = viewDistanceInChunks * 2 + 1;
        return diameter * diameter;
    }

    void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Terrain terrain = CreatePooledTerrain();
            ReturnChunkToPool(terrain);
        }
    }

    Terrain CreatePooledTerrain()
    {
        TerrainData data = CreateTerrainData();

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = $"PooledTerrain_{pooledChunkIndex++}";
        go.transform.SetParent(chunkParent);

        Terrain terrain = go.GetComponent<Terrain>();
        terrain.drawInstanced = true;

        TerrainCollider collider = go.GetComponent<TerrainCollider>();
        if (collider != null)
            collider.terrainData = data;

        go.SetActive(false);

        return terrain;
    }

    TerrainData CreateTerrainData()
    {
        TerrainData data = new TerrainData();

        data.heightmapResolution = heightmapResolution;
        data.size = new Vector3(chunkSize, maxHeight, chunkSize);
        data.alphamapResolution = Mathf.Clamp(heightmapResolution, 64, 512);

        if (terrainLayers != null && terrainLayers.Length > 0)
            data.terrainLayers = terrainLayers;

        return data;
    }

    Terrain GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            return chunkPool.Dequeue();
        }

        // 若玩家移動太快或 viewDistance 被調大，池不夠時才補一個。
        // 這不是每次移動都生成，而是池容量不足時才擴充。
        return CreatePooledTerrain();
    }

    void ReturnChunkToPool(Terrain terrain)
    {
        if (terrain == null) return;

        terrain.gameObject.SetActive(false);
        terrain.transform.position = Vector3.zero;
        terrain.transform.SetParent(chunkParent);

        chunkPool.Enqueue(terrain);
    }

    // ---------- Chunk Management ----------

    Vector2Int GetPlayerChunkCoord()
    {
        Vector3 p = player.position;

        int cx = Mathf.FloorToInt(p.x / chunkSize);
        int cz = Mathf.FloorToInt(p.z / chunkSize);

        return new Vector2Int(cx, cz);
    }

    void UpdateChunks()
    {
        Vector2Int center = lastPlayerChunk;

        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();

        for (int dz = -viewDistanceInChunks; dz <= viewDistanceInChunks; dz++)
        {
            for (int dx = -viewDistanceInChunks; dx <= viewDistanceInChunks; dx++)
            {
                needed.Add(new Vector2Int(center.x + dx, center.y + dz));
            }
        }

        foreach (Vector2Int coord in needed)
        {
            if (!chunks.ContainsKey(coord))
                ActivateChunk(coord);
        }

        List<Vector2Int> toRecycle = new List<Vector2Int>();

        foreach (var kv in chunks)
        {
            if (!needed.Contains(kv.Key))
                toRecycle.Add(kv.Key);
        }

        foreach (Vector2Int coord in toRecycle)
        {
            Terrain terrain = chunks[coord];

            chunks.Remove(coord);

            ReturnChunkToPool(terrain);
        }
    }

    void ActivateChunk(Vector2Int coord)
    {
        Terrain terrain = GetChunkFromPool();

        terrain.name = $"Terrain_{coord.x}_{coord.y}";
        terrain.transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

        TerrainData data = terrain.terrainData;

        EnsureTerrainDataSettings(data);

        GenerateHeights(data, coord);
        ApplyTextures(data, coord);

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null)
            collider.terrainData = data;

        terrain.gameObject.SetActive(true);

        chunks[coord] = terrain;
    }

    void EnsureTerrainDataSettings(TerrainData data)
    {
        if (data == null) return;

        if (data.heightmapResolution != heightmapResolution)
            data.heightmapResolution = heightmapResolution;

        data.size = new Vector3(chunkSize, maxHeight, chunkSize);
        data.alphamapResolution = Mathf.Clamp(heightmapResolution, 64, 512);

        if (terrainLayers != null && terrainLayers.Length > 0)
            data.terrainLayers = terrainLayers;
    }

    // ---------- Player Spawn Safety ----------

    void SnapPlayerToCurrentChunk()
    {
        Vector2Int c = GetPlayerChunkCoord();

        if (!chunks.TryGetValue(c, out Terrain terrain) || terrain == null)
            return;

        Vector3 p = player.position;
        float groundY = terrain.SampleHeight(p) + terrain.transform.position.y;

        CharacterController cc = player.GetComponent<CharacterController>();

        bool reEnable = false;

        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            reEnable = true;
        }

        player.position = new Vector3(p.x, groundY + playerSpawnYOffset, p.z);

        if (reEnable && cc != null)
            cc.enabled = true;
    }

    // ---------- Height Generation ----------

    void GenerateHeights(TerrainData data, Vector2Int coord)
    {
        int res = data.heightmapResolution;

        float[,] heights = new float[res, res];

        Vector2 origin = new Vector2(coord.x * chunkSize, coord.y * chunkSize);

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float wx = origin.x + (x / (res - 1f)) * chunkSize;
                float wz = origin.y + (z / (res - 1f)) * chunkSize;

                heights[z, x] = SampleHeight01(wx, wz);
            }
        }

        data.SetHeights(0, 0, heights);
    }

    float SampleHeight01(float worldX, float worldZ)
    {
        float h = FBM(worldX, worldZ, noiseScale, octaves, persistence, lacunarity);

        h = Mathf.Pow(h, Mathf.Max(0.01f, heightPower));

        if (enableDiagonalCanyon)
        {
            h = ApplyDiagonalCanyon(h, worldX, worldZ);
        }

        if (enableHeightClamp)
        {
            h = ApplyHeightClamp(h);
        }

        return Mathf.Clamp01(h);
    }

    float ApplyDiagonalCanyon(float baseHeight, float worldX, float worldZ)
    {
        Vector2 dir = diagonalCanyonDirection;

        if (dir.sqrMagnitude < 0.0001f)
            dir = new Vector2(1f, 1f);

        dir.Normalize();

        // normal 是垂直於峽谷方向的向量。
        // 用它可以計算某一點到峽谷中心線的距離。
        Vector2 normal = new Vector2(-dir.y, dir.x);

        Vector2 p = new Vector2(worldX, worldZ);

        // 因為中心線通過原點，所以不需要 offset。
        // distance 越小，代表越接近峽谷中心線。
        float distanceToCenterLine = Mathf.Abs(Vector2.Dot(p, normal));

        float halfFlatWidth = canyonFlatBottomWidth * 0.5f;
        float outerWidth = halfFlatWidth + canyonSideSlopeWidth;

        // 1. 谷底平坦區：高度直接切到 0
        if (distanceToCenterLine <= halfFlatWidth)
        {
            return canyonBottomHeight01;
        }

        // 2. 峽谷外側：不受影響，維持原地形
        if (distanceToCenterLine >= outerWidth)
        {
            return baseHeight;
        }

        // 3. 峽谷邊坡：從 0 平滑過渡到原本地形
        float t = Mathf.InverseLerp(halfFlatWidth, outerWidth, distanceToCenterLine);

        // SmoothStep 讓邊坡過渡比較自然，不會是生硬直線。
        t = t * t * (3f - 2f * t);

        float canyonHeight = Mathf.Lerp(canyonBottomHeight01, baseHeight, t);

        return Mathf.Lerp(baseHeight, canyonHeight, canyonCarveStrength);
    }

    float ApplyHeightClamp(float height01)
    {
        if (maxHeight <= 0f)
            return height01;

        float clampHeight01 = Mathf.Clamp01(clampHeight / maxHeight);

        if (!smoothClampEdge)
        {
            return Mathf.Min(height01, clampHeight01);
        }

        float smoothRange01 = Mathf.Max(0.0001f, clampSmoothRange / maxHeight);

        float start01 = Mathf.Clamp01(clampHeight01 - smoothRange01);
        float end01 = clampHeight01;

        // 低於平滑區域：不處理
        if (height01 <= start01)
            return height01;

        // 高於截平高度：直接切到 clampHeight
        if (height01 >= end01)
            return clampHeight01;

        // 平滑區域：逐漸壓向 clampHeight
        float t = Mathf.InverseLerp(start01, end01, height01);
        t = t * t * (3f - 2f * t);

        return Mathf.Lerp(height01, clampHeight01, t);
    }

    float FBM(float worldX, float worldZ, float scale, int oct, float pers, float lac)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            float nx = worldX * scale * frequency + seedOffsetX;
            float nz = worldZ * scale * frequency + seedOffsetZ;

            float n = Mathf.PerlinNoise(nx, nz);

            sum += n * amplitude;
            norm += amplitude;

            amplitude *= pers;
            frequency *= lac;
        }

        return sum / Mathf.Max(0.0001f, norm);
    }

    // ---------- Terrain Texturing ----------

    void ApplyTextures(TerrainData data, Vector2Int coord)
    {
        if (data.terrainLayers == null || data.terrainLayers.Length == 0)
            return;

        int aRes = data.alphamapResolution;
        int layerCount = data.terrainLayers.Length;

        float[,,] maps = new float[aRes, aRes, layerCount];

        for (int z = 0; z < aRes; z++)
        {
            for (int x = 0; x < aRes; x++)
            {
                float u = x / (aRes - 1f);
                float v = z / (aRes - 1f);

                float worldX = coord.x * chunkSize + u * chunkSize;
                float worldZ = coord.y * chunkSize + v * chunkSize;

                float[] weights = new float[layerCount];

                if (enableTextureNoiseBlend)
                {
                    FillNoiseTextureWeights(weights, worldX, worldZ);
                }
                else
                {
                    FillBaseTextureWeights(weights);
                }

                NormalizeWeights(weights);

                for (int l = 0; l < layerCount; l++)
                {
                    maps[z, x, l] = weights[l];
                }
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    void FillBaseTextureWeights(float[] weights)
    {
        int layerCount = weights.Length;

        if (layerCount >= 5)
        {
            weights[0] = layer0Weight;
            weights[1] = layer1Weight;
            weights[2] = layer2Weight;
            weights[3] = layer3Weight;
            weights[4] = layer4Weight;

            for (int i = 5; i < layerCount; i++)
                weights[i] = 0.05f;
        }
        else
        {
            float equalWeight = 1f / layerCount;

            for (int i = 0; i < layerCount; i++)
                weights[i] = equalWeight;
        }
    }

    void FillNoiseTextureWeights(float[] weights, float worldX, float worldZ)
    {
        FillBaseTextureWeights(weights);

        for (int i = 0; i < weights.Length; i++)
        {
            // 每個 Layer 使用不同 offset，避免所有 Layer 同步變化
            float layerOffsetX = seedOffsetX + i * 137.31f;
            float layerOffsetZ = seedOffsetZ + i * 291.73f;

            // 大尺度 noise：決定大範圍材質分布
            float macro = Mathf.PerlinNoise(
                worldX * textureMacroNoiseScale + layerOffsetX,
                worldZ * textureMacroNoiseScale + layerOffsetZ
            );

            // 小尺度 noise：讓貼圖更破碎、不規則
            float detail = Mathf.PerlinNoise(
                worldX * textureDetailNoiseScale + layerOffsetX * 1.7f,
                worldZ * textureDetailNoiseScale + layerOffsetZ * 1.7f
            );

            // 轉成 -1 ~ 1
            float macroSigned = (macro - 0.5f) * 2f;
            float detailSigned = (detail - 0.5f) * 2f;

            float noiseFactor =
                1f
                + macroSigned * textureNoiseStrength
                + detailSigned * textureDetailStrength;

            weights[i] *= Mathf.Max(0.001f, noiseFactor);

            // 對比強化：讓某些區域更明顯偏向特定 Layer
            weights[i] = Mathf.Pow(weights[i], textureContrast);
        }

        // 額外加入一個 winner boost：
        // 找出目前最強的 Layer，再把它加強，避免所有材質混得太平均。
        int strongestLayer = 0;
        float strongestValue = weights[0];

        for (int i = 1; i < weights.Length; i++)
        {
            if (weights[i] > strongestValue)
            {
                strongestValue = weights[i];
                strongestLayer = i;
            }
        }

        weights[strongestLayer] *= 1.8f;
    }

    void NormalizeWeights(float[] weights)
    {
        float sum = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Mathf.Max(0f, weights[i]);
            sum += weights[i];
        }

        if (sum <= 0.0001f)
        {
            float equalWeight = 1f / weights.Length;

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = equalWeight;
            }

            return;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= sum;
        }
    }

}