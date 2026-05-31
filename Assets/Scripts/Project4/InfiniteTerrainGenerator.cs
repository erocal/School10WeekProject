using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainGenerator : MonoBehaviour
{
    #region -- 設定 --

    [Header("References")]
    public Transform player;
    public TerrainLayer[] terrainLayers;

    [Header("Water Settings")]
    public GameObject waterPrefab;
    public bool waterFollowPlayer = true;        // 水面是否跟隨 Player 的 XZ 位置
    public float fixedWaterY = 0f;               // 水面固定高度,不會跟著 Player 的 Y 改變
    public Vector3 waterScale = Vector3.one;     // 水面生成時的縮放倍率

    [Header("Chunk Settings")]
    public int chunkSize = 256;
    public int heightmapResolution = 257;        // 必須是 2^n + 1
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

    [Header("Rugged Rock Terrain")]
    public bool enableRuggedTerrain = true;
    [Range(0f, 1f)] public float ruggedStrength = 0.65f;     // 岩石嶙峋強度,越高越破碎
    public float ridgeNoiseScale = 0.012f;                   // 山脊細節比例
    public int ridgeOctaves = 5;
    public float ridgePersistence = 0.55f;
    public float ridgeLacunarity = 2.25f;
    public float warpScale = 0.004f;                         // 扭曲坐標,避免地形太規則
    public float warpStrength = 85f;
    public bool enableRockTerraces = true;                   // 岩層階梯效果
    public int terraceSteps = 18;
    [Range(0f, 1f)] public float terraceStrength = 0.45f;
    [Range(0.5f, 4f)] public float cliffSharpness = 1.8f;    // 讓岩壁更銳利

    [Header("Texture Blend Weights")]
    public float layer0Weight = 0.30f;
    public float layer1Weight = 0.20f;
    public float layer2Weight = 0.20f;
    public float layer3Weight = 0.20f;
    public float layer4Weight = 0.10f;

    [Header("Texture Noise Blend")]
    public bool enableTextureNoiseBlend = true;
    public float textureMacroNoiseScale = 0.006f;            // 大尺度變化:控制大片區域偏向哪種材質
    public float textureDetailNoiseScale = 0.045f;           // 小尺度變化:製造局部斑駁、破碎感
    [Range(0f, 3f)] public float textureNoiseStrength = 1.8f;
    [Range(0f, 3f)] public float textureDetailStrength = 1.2f;
    [Range(0.25f, 4f)] public float textureContrast = 2.2f;  // 權重銳利度,越高越會讓某些 Layer 局部變明顯

    [Header("Tree Generation")]
    public bool enableTrees = true;
    public GameObject[] treePrefabs;                         // 放入樹木 Prefab,例如 Pine、DeadTree、Cactus 等
    public int treeAttemptsPerChunk = 250;                   // 每個 Chunk 嘗試生成幾次樹
    [Range(0f, 1f)] public float treeSpawnChance = 0.18f;    // 生成機率,數值越高樹越密
    public float minTreeHeight = 5f;                         // 樹木可出現的高度範圍 (使用世界高度)
    public float maxTreeHeightForTrees = 380f;
    [Range(0f, 90f)] public float maxTreeSlope = 28f;        // 避免樹長在太陡的斜坡上
    public float minTreeScale = 0.8f;                        // 樹木大小隨機範圍
    public float maxTreeScale = 1.6f;
    public float treeNoiseScale = 0.008f;                    // 樹木分布 Noise,讓樹林成片出現,不要平均撒滿
    [Range(0f, 1f)] public float treeNoiseThreshold = 0.45f;
    public float minDistanceFromWaterHeight = 2f;            // 避免樹長在峽谷谷底或水底附近

    [Header("Startup")]
    public bool snapPlayerToTerrainOnStart = true;
    public float playerSpawnYOffset = 2f;

    [Header("Diagonal Canyon Settings")]
    public bool enableDiagonalCanyon = true;
    // 通過原點的對角線方向。(1, 1) 代表峽谷沿著 Z = X 方向通過原點。(1, -1) 則代表沿著 Z = -X。
    public Vector2 diagonalCanyonDirection = new Vector2(1f, 1f);
    public float canyonFlatBottomWidth = 100f;               // 谷底「平坦區」的完整寬度,建議至少 100
    public float canyonSideSlopeWidth = 180f;                // 峽谷兩側斜坡過渡寬度,越大代表峽谷越寬、邊坡越平緩
    [Range(0f, 1f)] public float canyonBottomHeight01 = 0f;  // 峽谷底部高度,這裡設為 0
    [Range(0f, 1f)] public float canyonCarveStrength = 1f;   // 峽谷切割強度,1 代表完全切到 canyonBottomHeight01

    [Header("Plateau / Height Clamp Settings")]
    public bool enableHeightClamp = true;
    // 超過這個世界高度就會被切平。例如 maxHeight = 800, clampHeight = 400,代表高於 400 的地形都會變成 400。
    public float clampHeight = 400f;
    // false = 硬切平,山頂會很明顯被砍掉;true = 接近 clampHeight 時慢慢壓平。
    public bool smoothClampEdge = false;
    public float clampSmoothRange = 30f;                     // 平滑過渡範圍,只有 smoothClampEdge = true 時有效

    #endregion

    // ---- Runtime ----
    private Transform waterInstance;

    // Active chunks
    private readonly Dictionary<Vector2Int, Terrain> chunks = new Dictionary<Vector2Int, Terrain>();
    // Inactive pooled terrains
    private readonly Queue<Terrain> chunkPool = new Queue<Terrain>();
    // 重複使用的暫存集合,避免每幀 GC
    private readonly HashSet<Vector2Int> neededBuffer = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> recycleBuffer = new List<Vector2Int>();

    private Vector2Int lastPlayerChunk;
    private float seedOffsetX;
    private float seedOffsetZ;
    private int pooledChunkIndex = 0;

    // ---------- Unity Callbacks ----------

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Assign Player Transform in InfiniteTerrainGenerator.");
            enabled = false;
            return;
        }

        CreateWaterIfNeeded();

        if (chunkParent == null)
            chunkParent = new GameObject("TerrainChunkPool").transform;

        var rng = new System.Random(seed);
        seedOffsetX = (float)(rng.NextDouble() * 10000.0);
        seedOffsetZ = (float)(rng.NextDouble() * 10000.0);

        if (prewarmPoolOnStart)
        {
            int poolCount = GetVisibleChunkCount() + Mathf.Max(0, extraPooledChunks);
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

        UpdateWaterFollow();

        Vector2Int currentChunk = GetPlayerChunkCoord();
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    // ---------- Water ----------

    void CreateWaterIfNeeded()
    {
        if (waterPrefab == null || waterInstance != null) return;

        Vector3 spawnPosition = new Vector3(player.position.x, fixedWaterY, player.position.z);
        GameObject waterObj = Instantiate(waterPrefab, spawnPosition, Quaternion.identity);
        waterObj.name = "Following_Water";
        waterObj.transform.localScale = waterScale;
        waterInstance = waterObj.transform;
    }

    void UpdateWaterFollow()
    {
        if (!waterFollowPlayer || waterInstance == null) return;

        Vector3 p = player.position;
        waterInstance.position = new Vector3(p.x, fixedWaterY, p.z);
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
            ReturnChunkToPool(CreatePooledTerrain());
    }

    Terrain CreatePooledTerrain()
    {
        TerrainData data = CreateTerrainData();

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = $"PooledTerrain_{pooledChunkIndex++}";
        go.transform.SetParent(chunkParent);

        Terrain terrain = go.GetComponent<Terrain>();
        terrain.drawInstanced = true;

        TerrainCollider tc = go.GetComponent<TerrainCollider>();
        if (tc != null) tc.terrainData = data;

        go.SetActive(false);
        return terrain;
    }

    TerrainData CreateTerrainData()
    {
        TerrainData data = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            size = new Vector3(chunkSize, maxHeight, chunkSize),
            alphamapResolution = Mathf.Clamp(heightmapResolution, 64, 512)
        };

        if (terrainLayers != null && terrainLayers.Length > 0)
            data.terrainLayers = terrainLayers;

        SetupTreePrototypes(data);
        return data;
    }

    void SetupTreePrototypes(TerrainData data)
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;

        var prototypes = new TreePrototype[treePrefabs.Length];
        for (int i = 0; i < treePrefabs.Length; i++)
        {
            prototypes[i] = new TreePrototype { prefab = treePrefabs[i], bendFactor = 0.2f };
        }

        data.treePrototypes = prototypes;
        data.RefreshPrototypes();
    }

    // 若玩家移動太快或 viewDistance 被調大,池不夠時才補一個。
    // 不是每次移動都生成,而是池容量不足時才擴充。
    Terrain GetChunkFromPool()
    {
        return chunkPool.Count > 0 ? chunkPool.Dequeue() : CreatePooledTerrain();
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
        return new Vector2Int(
            Mathf.FloorToInt(p.x / chunkSize),
            Mathf.FloorToInt(p.z / chunkSize));
    }

    void UpdateChunks()
    {
        Vector2Int center = lastPlayerChunk;

        neededBuffer.Clear();
        for (int dz = -viewDistanceInChunks; dz <= viewDistanceInChunks; dz++)
        {
            for (int dx = -viewDistanceInChunks; dx <= viewDistanceInChunks; dx++)
                neededBuffer.Add(new Vector2Int(center.x + dx, center.y + dz));
        }

        foreach (Vector2Int coord in neededBuffer)
        {
            if (!chunks.ContainsKey(coord))
                ActivateChunk(coord);
        }

        recycleBuffer.Clear();
        foreach (var kv in chunks)
        {
            if (!neededBuffer.Contains(kv.Key))
                recycleBuffer.Add(kv.Key);
        }

        for (int i = 0; i < recycleBuffer.Count; i++)
        {
            Vector2Int coord = recycleBuffer[i];
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
        GenerateTrees(data, coord);

        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null) tc.terrainData = data;

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

        if (!chunks.TryGetValue(c, out Terrain terrain) || terrain == null) return;

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

        if (reEnable && cc != null) cc.enabled = true;
    }

    // ---------- Height Generation ----------

    void GenerateHeights(TerrainData data, Vector2Int coord)
    {
        int res = data.heightmapResolution;
        float[,] heights = new float[res, res];

        float originX = coord.x * chunkSize;
        float originZ = coord.y * chunkSize;
        float invResMinus1 = 1f / (res - 1f);
        float step = chunkSize * invResMinus1;

        for (int z = 0; z < res; z++)
        {
            float wz = originZ + z * step;
            for (int x = 0; x < res; x++)
            {
                float wx = originX + x * step;
                heights[z, x] = SampleHeight01(wx, wz);
            }
        }

        data.SetHeights(0, 0, heights);
    }

    float SampleHeight01(float worldX, float worldZ)
    {
        float h = FBM(worldX, worldZ, noiseScale, octaves, persistence, lacunarity);
        h = Mathf.Pow(h, Mathf.Max(0.01f, heightPower));

        if (enableRuggedTerrain) h = ApplyRuggedTerrain(h, worldX, worldZ);
        if (enableDiagonalCanyon) h = ApplyDiagonalCanyon(h, worldX, worldZ);
        if (enableHeightClamp) h = ApplyHeightClamp(h);

        return Mathf.Clamp01(h);
    }

    float ApplyRuggedTerrain(float baseHeight, float worldX, float worldZ)
    {
        // 1. Domain Warping:扭曲取樣座標,讓岩石不會太規則
        float warpX = (Mathf.PerlinNoise(
            worldX * warpScale + seedOffsetX,
            worldZ * warpScale + seedOffsetZ) - 0.5f) * 2f * warpStrength;

        float warpZ = (Mathf.PerlinNoise(
            worldX * warpScale + seedOffsetX + 91.7f,
            worldZ * warpScale + seedOffsetZ + 43.3f) - 0.5f) * 2f * warpStrength;

        float wx = worldX + warpX;
        float wz = worldZ + warpZ;

        // 2. Ridged Noise:產生尖銳脊線與破碎岩壁
        float ridge = RidgedFBM(wx, wz, ridgeNoiseScale, ridgeOctaves, ridgePersistence, ridgeLacunarity);

        // 3. 強化高頻破碎感
        ridge = Mathf.Pow(ridge, cliffSharpness);

        // 4. 把基礎地形與岩石脊線混合
        float ruggedHeight = Mathf.Lerp(baseHeight, ridge, ruggedStrength);

        // 5. 岩層階梯,做出沉積岩、峽谷岩層次
        if (enableRockTerraces && terraceSteps > 1)
            ruggedHeight = ApplyTerrace(ruggedHeight, terraceSteps, terraceStrength);

        return Mathf.Clamp01(ruggedHeight);
    }

    float RidgedFBM(float worldX, float worldZ, float scale, int oct, float pers, float lac)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            float nx = worldX * scale * frequency + seedOffsetX + i * 37.1f;
            float nz = worldZ * scale * frequency + seedOffsetZ + i * 19.7f;

            float n = Mathf.PerlinNoise(nx, nz);
            // Perlin 0~1 轉成 ridged:0.5 附近低,接近 0 或 1 形成高脊線
            n = 1f - Mathf.Abs(n * 2f - 1f);
            // 再反轉一次,讓脊線更突出
            n = 1f - n;

            sum += n * amplitude;
            norm += amplitude;

            amplitude *= pers;
            frequency *= lac;
        }

        return sum / Mathf.Max(0.0001f, norm);
    }

    float ApplyTerrace(float height01, int steps, float strength)
    {
        float stepped = Mathf.Floor(height01 * steps) / steps;
        // 保留一點原始高度,避免完全變成 Minecraft 式階梯
        return Mathf.Lerp(height01, stepped, strength);
    }

    float ApplyDiagonalCanyon(float baseHeight, float worldX, float worldZ)
    {
        Vector2 dir = diagonalCanyonDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 1f);
        dir.Normalize();

        // normal 是垂直於峽谷方向的向量,用它可以計算某一點到峽谷中心線的距離
        Vector2 normal = new Vector2(-dir.y, dir.x);

        // 因為中心線通過原點,所以不需要 offset。distance 越小,代表越接近峽谷中心線。
        float distanceToCenterLine = Mathf.Abs(worldX * normal.x + worldZ * normal.y);

        float halfFlatWidth = canyonFlatBottomWidth * 0.5f;
        float outerWidth = halfFlatWidth + canyonSideSlopeWidth;

        // 1. 谷底平坦區:高度直接切到 0
        if (distanceToCenterLine <= halfFlatWidth)
            return canyonBottomHeight01;

        // 2. 峽谷外側:不受影響,維持原地形
        if (distanceToCenterLine >= outerWidth)
            return baseHeight;

        // 3. 峽谷邊坡:從 0 平滑過渡到原本地形
        float t = Mathf.InverseLerp(halfFlatWidth, outerWidth, distanceToCenterLine);
        // SmoothStep 讓邊坡過渡比較自然,不會是生硬直線
        t = t * t * (3f - 2f * t);

        float canyonHeight = Mathf.Lerp(canyonBottomHeight01, baseHeight, t);
        return Mathf.Lerp(baseHeight, canyonHeight, canyonCarveStrength);
    }

    float ApplyHeightClamp(float height01)
    {
        if (maxHeight <= 0f) return height01;

        float clampHeight01 = Mathf.Clamp01(clampHeight / maxHeight);

        if (!smoothClampEdge)
            return Mathf.Min(height01, clampHeight01);

        float smoothRange01 = Mathf.Max(0.0001f, clampSmoothRange / maxHeight);
        float start01 = Mathf.Clamp01(clampHeight01 - smoothRange01);
        float end01 = clampHeight01;

        // 低於平滑區域:不處理
        if (height01 <= start01) return height01;
        // 高於切平高度:直接切到 clampHeight
        if (height01 >= end01) return clampHeight01;

        // 平滑區域:逐漸壓向 clampHeight
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

            sum += Mathf.PerlinNoise(nx, nz) * amplitude;
            norm += amplitude;

            amplitude *= pers;
            frequency *= lac;
        }

        return sum / Mathf.Max(0.0001f, norm);
    }

    // ---------- Terrain Texturing ----------

    void ApplyTextures(TerrainData data, Vector2Int coord)
    {
        if (data.terrainLayers == null || data.terrainLayers.Length == 0) return;

        int aRes = data.alphamapResolution;
        int layerCount = data.terrainLayers.Length;

        float[,,] maps = new float[aRes, aRes, layerCount];
        // 內層 buffer 重複使用,不要每個像素都 new
        float[] weights = new float[layerCount];

        float originX = coord.x * chunkSize;
        float originZ = coord.y * chunkSize;
        float invResMinus1 = 1f / (aRes - 1f);
        float step = chunkSize * invResMinus1;

        for (int z = 0; z < aRes; z++)
        {
            float worldZ = originZ + z * step;
            for (int x = 0; x < aRes; x++)
            {
                float worldX = originX + x * step;

                if (enableTextureNoiseBlend)
                    FillNoiseTextureWeights(weights, worldX, worldZ);
                else
                    FillBaseTextureWeights(weights);

                NormalizeWeights(weights);

                for (int l = 0; l < layerCount; l++)
                    maps[z, x, l] = weights[l];
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    void GenerateTrees(TerrainData data, Vector2Int coord)
    {
        if (!enableTrees || treePrefabs == null || treePrefabs.Length == 0)
        {
            data.treeInstances = new TreeInstance[0];
            return;
        }

        if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            SetupTreePrototypes(data);

        // 預估容量 (treeSpawnChance * treeNoise 通過率 ≈ 一半左右),減少 List 擴容
        var trees = new List<TreeInstance>(Mathf.CeilToInt(treeAttemptsPerChunk * treeSpawnChance) + 8);
        var rng = new System.Random(GetChunkSeed(coord, 9173));

        float waterLimit = fixedWaterY + minDistanceFromWaterHeight;
        int prototypeCount = data.treePrototypes.Length;

        for (int i = 0; i < treeAttemptsPerChunk; i++)
        {
            float localX01 = (float)rng.NextDouble();
            float localZ01 = (float)rng.NextDouble();

            float worldX = coord.x * chunkSize + localX01 * chunkSize;
            float worldZ = coord.y * chunkSize + localZ01 * chunkSize;

            float noise = Mathf.PerlinNoise(
                worldX * treeNoiseScale + seedOffsetX,
                worldZ * treeNoiseScale + seedOffsetZ);
            if (noise < treeNoiseThreshold) continue;

            if ((float)rng.NextDouble() > treeSpawnChance) continue;

            float height01 = SampleHeight01(worldX, worldZ);
            float worldHeight = height01 * maxHeight;
            if (worldHeight < minTreeHeight || worldHeight > maxTreeHeightForTrees) continue;
            if (worldHeight <= waterLimit) continue;

            float slope = data.GetSteepness(localX01, localZ01);
            if (slope > maxTreeSlope) continue;

            int prototypeIndex = rng.Next(0, prototypeCount);
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)rng.NextDouble());

            trees.Add(new TreeInstance
            {
                position = new Vector3(localX01, height01, localZ01),
                prototypeIndex = prototypeIndex,
                widthScale = scale,
                heightScale = scale,
                color = Color.white,
                lightmapColor = Color.white
            });
        }

        data.treeInstances = trees.ToArray();
    }

    int GetChunkSeed(Vector2Int coord, int salt)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 73856093 ^ coord.x;
            hash = hash * 19349663 ^ coord.y;
            hash = hash * 83492791 ^ salt;
            return hash;
        }
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
            for (int i = 5; i < layerCount; i++) weights[i] = 0.05f;
        }
        else
        {
            float equalWeight = 1f / layerCount;
            for (int i = 0; i < layerCount; i++) weights[i] = equalWeight;
        }
    }

    void FillNoiseTextureWeights(float[] weights, float worldX, float worldZ)
    {
        FillBaseTextureWeights(weights);

        int strongestLayer = 0;
        float strongestValue = float.MinValue;

        for (int i = 0; i < weights.Length; i++)
        {
            // 每個 Layer 使用不同 offset,避免所有 Layer 同步變化
            float layerOffsetX = seedOffsetX + i * 137.31f;
            float layerOffsetZ = seedOffsetZ + i * 291.73f;

            // 大尺度 noise:決定大範圍材質分布
            float macro = Mathf.PerlinNoise(
                worldX * textureMacroNoiseScale + layerOffsetX,
                worldZ * textureMacroNoiseScale + layerOffsetZ);

            // 小尺度 noise:讓貼圖更破碎、不規則
            float detail = Mathf.PerlinNoise(
                worldX * textureDetailNoiseScale + layerOffsetX * 1.7f,
                worldZ * textureDetailNoiseScale + layerOffsetZ * 1.7f);

            // 轉成 -1 ~ 1
            float macroSigned = (macro - 0.5f) * 2f;
            float detailSigned = (detail - 0.5f) * 2f;

            float noiseFactor = 1f + macroSigned * textureNoiseStrength + detailSigned * textureDetailStrength;

            float w = weights[i] * Mathf.Max(0.001f, noiseFactor);
            // 對比強化:讓某些區域更明顯偏向特定 Layer
            w = Mathf.Pow(w, textureContrast);
            weights[i] = w;

            // 同時找出最強 Layer,免得稍後再跑一次迴圈
            if (w > strongestValue)
            {
                strongestValue = w;
                strongestLayer = i;
            }
        }

        // Winner boost:加強最強 Layer,避免所有權重混得太平均
        weights[strongestLayer] *= 1.8f;
    }

    void NormalizeWeights(float[] weights)
    {
        float sum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] < 0f) weights[i] = 0f;
            sum += weights[i];
        }

        if (sum <= 0.0001f)
        {
            float equalWeight = 1f / weights.Length;
            for (int i = 0; i < weights.Length; i++) weights[i] = equalWeight;
            return;
        }

        float invSum = 1f / sum;
        for (int i = 0; i < weights.Length; i++) weights[i] *= invSum;
    }
}