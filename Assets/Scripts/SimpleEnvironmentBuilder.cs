using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleEnvironmentBuilder : MonoBehaviour
{
    [Header("Feature Flags")]
    [SerializeField] private bool buildTrees = true;
    [SerializeField] private bool buildPonds = true;
    [SerializeField] private bool buildClouds = false; // Bulutlar kapatildi
    [SerializeField] private bool buildAnimals = true;
    [SerializeField] private bool buildMountains = true;
    [SerializeField] private bool setupAtmosphere = true;

    [Header("Terrain")]
    [SerializeField, Min(64f)] private float terrainSize = 1200f;
    [SerializeField, Min(10f)] private float terrainHeight = 200f;
    [SerializeField] private int heightmapResolution = 513;

    [Header("Custom Assets (Optional)")]
    [Tooltip("Leave empty to use procedurally generated primitives.")]
    [SerializeField] private GameObject[] customTreePrefabs;
    [SerializeField] private GameObject[] customCloudPrefabs;
    [SerializeField] private GameObject[] customPondPrefabs;
    [SerializeField] private GameObject[] customAnimalPrefabs;
    [SerializeField] private GameObject[] customFishPrefabs;
    [SerializeField] private GameObject customMountainPrefab;

    [Header("Trees")]
    [SerializeField, Min(0)] private int treeCount = 1200;
    [SerializeField] private float treeSpawnRadius = 500f;
    [SerializeField] private Vector2 treeHeightRange = new Vector2(5f, 10f);
    [SerializeField] private float trackExclusionRadius = 18f;
    [SerializeField] private float stationExclusionRadius = 35f;

    [Header("Ponds")]
    [SerializeField, Min(1)] private int pondCount = 7;
    [SerializeField] private float pondMinRadius = 10f;
    [SerializeField] private float pondMaxRadius = 28f;
    [SerializeField] private float pondSpawnRadius = 350f;
    [SerializeField] private float pondYLevel = -0.2f;
    [SerializeField] private Material waterMaterial;

    [Header("Clouds")]
    [SerializeField, Min(5)] private int cloudCount = 18;
    [SerializeField] private float cloudMinAltitude = 80f;
    [SerializeField] private float cloudMaxAltitude = 140f;
    [SerializeField] private float cloudSpawnRadius = 500f;
    [SerializeField] private float cloudDriftSpeed = 2f;
    [SerializeField] private Material cloudMaterial;

    [Header("Sky & Atmosphere")]
    [SerializeField] private Material skyboxMaterial;

    public void SetSkyboxMaterial(Material material) { skyboxMaterial = material; }
    public void SetCloudMaterial(Material material) { cloudMaterial = material; }

    [ContextMenu("Build Environment")]
    public void BuildEnvironment()
    {
#if UNITY_EDITOR
        AutoAssignAssets();
#endif
        CreateFlatTerrain();
        if (buildMountains) CreateMountains();
        if (buildTrees) CreateProfessionalTrees();
        if (buildPonds) CreatePondsAndFauna();
        if (buildClouds) CreateRealisticClouds();
        
        // Skybox uygula
        ApplySkyboxOnly();
        
        // Force Recovery of Broken Lighting & Materials
        #if UNITY_EDITOR
        ForceRecoverEnvironment();
        #endif
    }

#if UNITY_EDITOR
    private void AutoAssignAssets()
    {
        List<GameObject> trees = new List<GameObject>();
        
        // Nicrom LPW Trees (ruzgar animasyonlu low-poly agaclar)
        string[] nicromPaths = new string[] {
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_A1_6.5m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_B1_9m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_B1_9m_02.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_C1_11m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_C1_9m_01.prefab"
        };
        foreach (var p in nicromPaths) {
            var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (t) trees.Add(t);
        }

        // Tree9 varyantlari
        string[] tree9Paths = new string[] {
            "Assets/Tree9/Tree9_2.prefab",
            "Assets/Tree9/Tree9_3.prefab",
            "Assets/Tree9/Tree9_4.prefab",
            "Assets/Tree9/Tree9_5.prefab"
        };
        foreach (var p in tree9Paths) {
            var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (t) trees.Add(t);
        }
        
        if (trees.Count > 0) customTreePrefabs = trees.ToArray();

        string butterflyPath = "Assets/Butterfly (Animated)/Prefab/Butterfly.prefab";
        var butterfly = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(butterflyPath);
        
        string goatPath = "Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Goat_dark.prefab";
        var goat = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(goatPath);
        
        string sheepPath = "Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Sheep_white.prefab";
        var sheep = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(sheepPath);
        
        List<GameObject> animalsList = new List<GameObject>();
        if (butterfly) animalsList.Add(butterfly);
        if (goat) animalsList.Add(goat);
        if (sheep) animalsList.Add(sheep);
        if (animalsList.Count > 0) customAnimalPrefabs = animalsList.ToArray();

        string mountainPath = "Assets/Forest Pack/Prefabs/Mountain.prefab";
        var mountain = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(mountainPath);
        if (mountain) customMountainPrefab = mountain;

        // Su materyali bos birakilarak primitive URP materyali olusturulacak
        waterMaterial = null;

        // Fantasy Skybox - Bulutlu gunduz gokyuzu
        if (skyboxMaterial == null)
        {
            string skyboxPath = "Assets/Fantasy Skybox FREE/Cubemaps/Classic/FS000_Day_01.mat";
            var skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
            if (skyMat) skyboxMaterial = skyMat;
        }
    }
    
    // Pembe Materyalleri ve Siyah Gokyuzunu Zorla Duzeltir
    private void ForceRecoverEnvironment()
    {
        // 1. AtmosphereManager varsa sahne objesini yok et (gece dongusu durdurulsun)
        var atmoObj = GameObject.Find("AtmosphereManager");
        if (atmoObj != null) DestroyImmediate(atmoObj);
        
        // AtmosphereManager bilesenlerini de sil
        var atmoComps = FindObjectsByType<AtmosphereManager>(FindObjectsSortMode.None);
        foreach (var a in atmoComps) DestroyImmediate(a);

        // 2. Skybox - Fantasy Skybox varsa onu uygula, yoksa URP default
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        DynamicGI.UpdateEnvironment();

        // Isigi sabitle - gunduz
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.color = new Color(1f, 0.96f, 0.88f);
                l.intensity = 1.5f;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        // 3. Tum pembe materyalleri URP/Lit shader ile onar
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return; // URP yoksa bir sey yapma
        
        string[] materialFolders = new string[] {
            "Assets/BEDRILL",
            "Assets/ALP_Assets",
            "Assets/Forest Pack",
            "Assets/Butterfly (Animated)",
            "Assets/Symphonie",
            "Assets/Nicrom",
            "Assets/Tree9"
        };
        
        foreach (string folder in materialFolders)
        {
            string[] matGuids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (string guid in matGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != urpLit)
                {
                    // Mevcut rengi koru
                    Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    
                    mat.shader = urpLit;
                    
                    // URP property isimlerine tasi
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainColor);
                    if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    
                    UnityEditor.EditorUtility.SetDirty(mat);
                }
            }
        }
        
        UnityEditor.AssetDatabase.SaveAssets();
        
        // 4. Dag materyallerini gri/yesil yap
        string[] mountainMatGuids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/Forest Pack/Materials" });
        foreach (string guid in mountainMatGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                // Dag gri/yesil renkleri
                string matName = mat.name.ToLower();
                Color mountainColor;
                
                // Ust kisimlar yesil, alt kisimlar gri ton
                if (matName.Contains("001") || matName.Contains("002") || matName.Contains("003"))
                    mountainColor = new Color(0.55f, 0.65f, 0.45f); // Yesil ust
                else if (matName.Contains("004") || matName.Contains("005") || matName.Contains("006"))
                    mountainColor = new Color(0.50f, 0.58f, 0.42f); // Acik yesil
                else if (matName.Contains("007") || matName.Contains("008") || matName.Contains("009"))
                    mountainColor = new Color(0.55f, 0.55f, 0.55f); // Gri
                else
                    mountainColor = new Color(0.50f, 0.50f, 0.52f); // Koyu gri
                
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mountainColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", mountainColor);
                
                UnityEditor.EditorUtility.SetDirty(mat);
            }
        }
        
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
#endif

    private void CreateFlatTerrain()
    {
        Transform existing = transform.Find("PrototypeTerrain");
        if (existing != null) DestroyImmediate(existing.gameObject);

        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1,
            size = new Vector3(terrainSize, terrainHeight, terrainSize)
        };

        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        float flatNoiseScale = 0.008f;
        float flatNoiseStrength = 0.003f;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                heights[y, x] = Mathf.PerlinNoise(x * flatNoiseScale + 50f, y * flatNoiseScale + 50f) * flatNoiseStrength;

        terrainData.SetHeights(0, 0, heights);

        Texture2D grassTex = CreateSolidColorTexture(64, new Color(0.28f, 0.55f, 0.22f));
        TerrainLayer grassLayer = new TerrainLayer { diffuseTexture = grassTex, tileSize = new Vector2(10f, 10f) };
        terrainData.terrainLayers = new TerrainLayer[] { grassLayer };

        float[,,] alphamaps = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 1];
        for (int y = 0; y < terrainData.alphamapResolution; y++)
            for (int x = 0; x < terrainData.alphamapResolution; x++)
                alphamaps[y, x, 0] = 1f;
        terrainData.SetAlphamaps(0, 0, alphamaps);

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "PrototypeTerrain";
        terrainObj.transform.SetParent(transform, false);
        terrainObj.transform.position = new Vector3(-terrainSize * 0.5f, -2f, -terrainSize * 0.5f);
    }

    private void CreateMountains()
    {
        Transform existing = transform.Find("PrototypeMountains");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("PrototypeMountains");
        root.transform.SetParent(transform, false);

        if (customMountainPrefab == null) return;

        // Merkezde Kucuk Daglarkir (Pistin ortasina yakin)
        GameObject centerMountain = null;
        #if UNITY_EDITOR
        centerMountain = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(customMountainPrefab, root.transform);
        #else
        centerMountain = Instantiate(customMountainPrefab, root.transform);
        #endif
        
        Vector3 centerPos = new Vector3(80f, 0f, 80f); // Pistten uzakta, carpismamasi icin yana kaydirma
        if (Terrain.activeTerrain != null)
            centerPos.y = Terrain.activeTerrain.SampleHeight(centerPos) + Terrain.activeTerrain.transform.position.y;
        
        centerMountain.transform.position = centerPos;
        centerMountain.transform.localScale = Vector3.one * Random.Range(1.5f, 2.5f);
        centerMountain.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Dis Halka Buyuk Daglar (Ring) - Daha kucuk boyutlarda
        int mountainCount = 28; 
        float radius = 500f; 

        for (int i = 0; i < mountainCount; i++)
        {
            float angle = (i * Mathf.PI * 2f) / mountainCount;
            float r = radius + Random.Range(-20f, 30f); 
            
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, -5f, Mathf.Sin(angle) * r);
            
            GameObject ringMountain = null;
            #if UNITY_EDITOR
            ringMountain = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(customMountainPrefab, root.transform);
            #else
            ringMountain = Instantiate(customMountainPrefab, root.transform);
            #endif

            ringMountain.transform.position = pos;
            ringMountain.transform.localScale = Vector3.one * Random.Range(4f, 7f); // Daglar kucultuldu
            ringMountain.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    private void CreateProfessionalTrees()
    {
        Transform existing = transform.Find("PrototypeTrees");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("PrototypeTrees");
        root.transform.SetParent(transform, false);

        List<Vector3> trackPoints = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(99);

        int placed = 0;
        int attempts = 0;
        int maxAttempts = treeCount * 5;
        bool hasCustomPrefs = customTreePrefabs != null && customTreePrefabs.Length > 0;

        // Ormanlik kume sektorleri (acilari radyan cinsinden)
        // Bu sektorlerde agaclar 3x daha yogun yerlestirilir
        float[] forestSectors = { 0.3f, 1.2f, 2.5f, 3.8f, 5.0f };
        float forestSectorWidth = 0.6f; // Her sektorun genisligi (radyan)

        while (placed < treeCount && attempts < maxAttempts)
        {
            attempts++;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            
            // Ormanlik sektorde mi kontrol et - sektordeyse daha yakin mesafede de spawn olabilir
            bool inForestSector = false;
            foreach (float sector in forestSectors)
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, sector * Mathf.Rad2Deg));
                if (diff < forestSectorWidth * Mathf.Rad2Deg * 0.5f)
                {
                    inForestSector = true;
                    break;
                }
            }
            
            // Ormanlik sektorde daha kisa mesafelerden baslat (yogun orman)
            float minDist = inForestSector ? 25f : 40f;
            float maxDist = inForestSector ? treeSpawnRadius * 0.8f : treeSpawnRadius;
            float dist = Random.Range(minDist, maxDist);
            
            // Ormanlik olmayan sektorde %40 atlama sansi (seyreklestirme)
            if (!inForestSector && Random.value < 0.4f) continue;
            
            Vector3 basePos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            if (Terrain.activeTerrain != null)
            {
                basePos.y = Terrain.activeTerrain.SampleHeight(basePos) + Terrain.activeTerrain.transform.position.y;
            }
            else
            {
                basePos.y = -2f;
            }

            Vector2 pos2D = new Vector2(basePos.x, basePos.z);
            if (pos2D.magnitude < stationExclusionRadius) continue;
            if (IsNearTrack(basePos, trackPoints, trackExclusionRadius)) continue;

            GameObject tree = null;

            if (hasCustomPrefs)
            {
                GameObject prefab = customTreePrefabs[Random.Range(0, customTreePrefabs.Length)];
                if (prefab != null)
                {
                    #if UNITY_EDITOR
                    tree = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    #else
                    tree = Instantiate(prefab, root.transform);
                    #endif
                    tree.transform.position = basePos;
                    // Rastgele dondur ve boyutlandir
                    tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    
                    // Boyutlandirmayi assetin ismine gore ayarla
                    float scale = Random.Range(0.4f, 0.7f); // Kucuk agaclar - pisti kaybettirmemeli
                    
                    tree.transform.localScale = prefab.transform.localScale * scale;
                }
            }
            
            // Custom prefab basarisiz olduysa veya set edilmediyse fallback (primitive) kullan
            if (tree == null)
            {
                float treeHeight = Random.Range(treeHeightRange.x, treeHeightRange.y);
                int treeType = Random.Range(0, 3);
                switch (treeType)
                {
                    case 0: tree = CreatePineTree(basePos, treeHeight, placed); break;
                    case 1: tree = CreateOakTree(basePos, treeHeight, placed); break;
                    default: tree = CreateBirchTree(basePos, treeHeight, placed); break;
                }
                tree.transform.SetParent(root.transform, true);
            }

            placed++;
        }

        Random.state = saved;
    }

    private void CreatePondsAndFauna()
    {
        Transform existing = transform.Find("PrototypePonds");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject pondsRoot = new GameObject("PrototypePonds");
        pondsRoot.transform.SetParent(transform, false);

        List<Vector3> trackPts = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(42);

        int placed = 0;
        int attempts = 0;
        bool hasCustomPonds = customPondPrefabs != null && customPondPrefabs.Length > 0;

        while (placed < pondCount && attempts < pondCount * 10)
        {
            attempts++;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(50f, pondSpawnRadius);
            float pondRadius = Random.Range(pondMinRadius, pondMaxRadius);

            Vector3 center = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            if (Terrain.activeTerrain != null)
            {
                center.y = Terrain.activeTerrain.SampleHeight(center) + Terrain.activeTerrain.transform.position.y - 0.05f;
            }
            else
            {
                center.y = pondYLevel;
            }

            Vector2 pondPos2D = new Vector2(center.x, center.z);
            if (IsNearTrack(center, trackPts, trackExclusionRadius + pondRadius)) continue;
            if (pondPos2D.magnitude < stationExclusionRadius + pondRadius) continue;

            GameObject pondRoot = new GameObject($"Pond_{placed:00}");
            pondRoot.transform.SetParent(pondsRoot.transform, true);
            pondRoot.transform.position = center;
            pondRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject pondMesh = null;

            if (hasCustomPonds)
            {
                GameObject prefab = customPondPrefabs[Random.Range(0, customPondPrefabs.Length)];
                if (prefab != null)
                {
                    #if UNITY_EDITOR
                    pondMesh = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot.transform);
                    #else
                    pondMesh = Instantiate(prefab, pondRoot.transform);
                    #endif
                    pondMesh.transform.localPosition = Vector3.zero;
                    float scale = pondRadius / 10f; // Varsayilan bir olcek
                    pondMesh.transform.localScale *= scale;
                }
            }

            // Primitive Fallback
            if (pondMesh == null)
            {
                GameObject pondPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pondPrimitive.name = "WaterVolume";
                pondPrimitive.transform.SetParent(pondRoot.transform, false);
                pondPrimitive.transform.localPosition = Vector3.zero;
                pondPrimitive.transform.localScale = new Vector3(pondRadius * 2f, 0.05f, pondRadius * 2f);

                // Primitive su (prosedurel patladigi icin normal saydam mavi su yapiyoruz)
                SetColor(pondPrimitive, new Color(0.12f, 0.42f, 0.78f, 0.85f), true);

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "PondRim";
                rim.transform.SetParent(pondRoot.transform, false);
                rim.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                rim.transform.localScale = new Vector3(pondRadius * 2.2f, 0.02f, pondRadius * 2.2f);
                SetColor(rim, new Color(0.18f, 0.40f, 0.15f));
            }

            if (buildAnimals) SpawnFauna(pondRoot.transform, center, pondRadius);

            placed++;
        }

        Random.state = saved;
    }

    private void SpawnFauna(Transform pondRoot, Vector3 pondCenter, float pondRadius)
    {
        bool hasAnimals = customAnimalPrefabs != null && customAnimalPrefabs.Length > 0;
        bool hasFish = customFishPrefabs != null && customFishPrefabs.Length > 0;

        // 1. Gole Atlayan Baliklar
        int fishCount = Random.Range(2, 6);
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 fishPos = pondCenter + new Vector3(
                Random.Range(-pondRadius * 0.7f, pondRadius * 0.7f),
                0f,
                Random.Range(-pondRadius * 0.7f, pondRadius * 0.7f)
            );

            GameObject fish = null;
            if (hasFish)
            {
                GameObject prefab = customFishPrefabs[Random.Range(0, customFishPrefabs.Length)];
                if (prefab != null)
                {
                    #if UNITY_EDITOR
                    fish = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot);
                    #else
                    fish = Instantiate(prefab, pondRoot);
                    #endif
                }
            }

            if (fish == null)
            {
                fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fish.name = "FishPrimitive";
                fish.transform.SetParent(pondRoot, true);
                fish.transform.localScale = new Vector3(0.5f, 0.4f, 1.2f);
                SetColor(fish, new Color(0.85f, 0.4f, 0.1f)); // Koi turuncusu
                
                // Kuyruk (Primitive)
                GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tail.transform.SetParent(fish.transform, false);
                tail.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                tail.transform.localPosition = new Vector3(0f, 0.1f, -0.6f);
                tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                SetColor(tail, new Color(0.85f, 0.4f, 0.1f));
                Object.DestroyImmediate(tail.GetComponent<Collider>());
            }

            fish.transform.position = fishPos;
            var jumper = fish.AddComponent<JumpingFish>();
            jumper.jumpHeight = Random.Range(1.5f, 3.5f);
            jumper.jumpDistance = Random.Range(2f, 5f);
            jumper.jumpIntervalMin = Random.Range(1f, 3f);
            jumper.jumpIntervalMax = Random.Range(5f, 8f);
            Object.DestroyImmediate(fish.GetComponent<Collider>());
        }

        // 2. Gol Kenarindaki Hayvanlar (kelebekler, keciler, koyunlar)
        int animalCount = Random.Range(4, 10);
        for (int i = 0; i < animalCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = pondRadius + Random.Range(2f, 15f); // Daha genis dagilim
            Vector3 animalPos = pondCenter + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            // Zemin yuksekligini net olarak al
            if (Terrain.activeTerrain != null)
                animalPos.y = Terrain.activeTerrain.SampleHeight(animalPos) + Terrain.activeTerrain.transform.position.y;

            GameObject animal = null;
            if (hasAnimals)
            {
                // Kelebek cikma ihtimali daha yuksek
                int prefabIdx = Random.Range(0, customAnimalPrefabs.Length);
                if (Random.value < 0.6f) // %60 ihtimalle her zaman kelebek (0. indexi)
                {
                    for (int p = 0; p < customAnimalPrefabs.Length; p++)
                    {
                        if (customAnimalPrefabs[p] != null && customAnimalPrefabs[p].name.Contains("Butterfly"))
                        {
                            prefabIdx = p; break;
                        }
                    }
                }
                
                GameObject prefab = customAnimalPrefabs[prefabIdx];
                if (prefab != null)
                {
                    #if UNITY_EDITOR
                    animal = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot);
                    #else
                    animal = Instantiate(prefab, pondRoot);
                    #endif
                    
                    bool isButterfly = prefab.name.Contains("Butterfly") || prefab.name.Contains("fly");
                    
                    if (isButterfly)
                    {
                        // Daha da kucuk kelebekler
                        float butterflyScale = Random.Range(0.03f, 0.06f);
                        animal.transform.localScale = Vector3.one * butterflyScale;
                        animalPos.y += Random.Range(0.5f, 2.5f); // Ucsunlar
                        animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    }
                    else
                    {
                        // Keci/Koyun ayari - zeminin altina girmemesini sagla
                        animal.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                        // Kesisim sorunlarini engellemek icin birazcik yukari kaydir (0.1f offset)
                        animalPos.y += 0.1f; 
                        
                        // Gole baksin
                        Vector3 lookDir = pondCenter - animalPos;
                        lookDir.y = 0f;
                        if (lookDir != Vector3.zero) animal.transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }

            if (animal == null)
            {
                animal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                animal.name = "AnimalPrimitive";
                animal.transform.SetParent(pondRoot, true);
                animal.transform.localScale = new Vector3(1.2f, 1f, 2f);
                SetColor(animal, new Color(0.6f, 0.4f, 0.2f)); 
                animalPos.y += 0.5f; // Primitive cube merkez pivotlu, yukari kaydir
            }

            animal.transform.position = animalPos;
        }
    }

    private void CreateRealisticClouds()
    {
        Transform existing = transform.Find("PrototypeClouds");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject cloudsRoot = new GameObject("PrototypeClouds");
        cloudsRoot.transform.SetParent(transform, false);

        CloudDrifter drifter = cloudsRoot.gameObject.AddComponent<CloudDrifter>();
        drifter.driftSpeed = cloudDriftSpeed;

        Random.State saved = Random.state;
        Random.InitState(77);

        bool hasCustomClouds = customCloudPrefabs != null && customCloudPrefabs.Length > 0;

        for (int i = 0; i < cloudCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(50f, cloudSpawnRadius);
            float altitude = Random.Range(cloudMinAltitude, cloudMaxAltitude);
            Vector3 center = new Vector3(Mathf.Cos(angle) * dist, altitude, Mathf.Sin(angle) * dist);

            GameObject cloudGroup = null;

            if (hasCustomClouds)
            {
                GameObject prefab = customCloudPrefabs[Random.Range(0, customCloudPrefabs.Length)];
                if (prefab != null)
                {
                    #if UNITY_EDITOR
                    cloudGroup = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, cloudsRoot.transform);
                    #else
                    cloudGroup = Instantiate(prefab, cloudsRoot.transform);
                    #endif
                    cloudGroup.transform.position = center;
                    float scale = Random.Range(0.8f, 1.5f);
                    cloudGroup.transform.localScale *= scale;
                }
            }

            if (cloudGroup == null)
            {
                cloudGroup = new GameObject($"Cloud_{i:00}");
                cloudGroup.transform.SetParent(cloudsRoot.transform, false);
                cloudGroup.transform.localPosition = center;

                int puffCount = Random.Range(4, 8);
                for (int p = 0; p < puffCount; p++)
                {
                    float puffSize = Random.Range(8f, 22f);
                    Vector3 offset = new Vector3(Random.Range(-12f, 12f), Random.Range(-2f, 4f), Random.Range(-10f, 10f));

                    GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = $"Puff_{p}";
                    puff.transform.SetParent(cloudGroup.transform, false);
                    puff.transform.localPosition = offset;
                    puff.transform.localScale = new Vector3(puffSize, puffSize * 0.5f, puffSize * 0.8f);

                    Object.DestroyImmediate(puff.GetComponent<Collider>());

                    if (cloudMaterial != null) puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
                    else SetColor(puff, new Color(0.95f, 0.96f, 0.98f, 0.75f), true);
                }
            }
            
            drifter.driftSpeed = cloudDriftSpeed;
        }

        Random.state = saved;
    }

    private void ApplyAtmosphere()
    {
        ApplySkyboxOnly();
        AtmosphereManager[] existing = FindObjectsByType<AtmosphereManager>(FindObjectsSortMode.None);
        if (existing.Length == 0)
        {
            GameObject atmManager = new GameObject("AtmosphereManager");
            atmManager.AddComponent<AtmosphereManager>();
        }
    }

    private void ApplySkyboxOnly()
    {
        if (skyboxMaterial != null) RenderSettings.skybox = skyboxMaterial;
        // Atmosphere layer should take over the ambientLight parameters if active
    }

    // ==== Primitive Agac Yedekleri ====
    private GameObject CreatePineTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject($"Pine_{index:000}");
        root.transform.localPosition = pos;
        float trunkH = height * 0.65f;
        float trunkW = 0.3f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(trunkW, trunkH * 0.5f, trunkW);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.35f, 0.22f, 0.12f));
        for (int layer = 0; layer < 3; layer++)
        {
            float layerY = trunkH * 0.5f + layer * (height * 0.18f);
            float layerRadius = (height * 0.25f) * (1f - layer * 0.25f);
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.transform.SetParent(root.transform, false);
            cone.transform.localPosition = new Vector3(0f, layerY, 0f);
            cone.transform.localScale = new Vector3(layerRadius * 2f, layerRadius * 1.6f, layerRadius * 2f);
            SetColor(cone, new Color(0.12f + layer * 0.04f, 0.38f + layer * 0.06f, 0.14f));
        }
        return root;
    }

    private GameObject CreateOakTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject($"Oak_{index:000}");
        root.transform.localPosition = pos;
        float trunkH = height * 0.45f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.5f, trunkH * 0.5f, 0.5f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.32f, 0.20f, 0.10f));
        float crownR = height * 0.35f;
        GameObject mainCrown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mainCrown.transform.SetParent(root.transform, false);
        mainCrown.transform.localPosition = new Vector3(0f, trunkH + crownR * 0.4f, 0f);
        mainCrown.transform.localScale = new Vector3(crownR * 2.2f, crownR * 1.6f, crownR * 2.2f);
        SetColor(mainCrown, new Color(0.20f, 0.45f, 0.18f));
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            float offR = crownR * 0.5f;
            GameObject sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sub.transform.SetParent(root.transform, false);
            sub.transform.localPosition = new Vector3(Mathf.Cos(a) * offR, trunkH + crownR * 0.15f, Mathf.Sin(a) * offR);
            sub.transform.localScale = Vector3.one * crownR * 1.1f;
            SetColor(sub, new Color(0.18f, 0.42f + i * 0.02f, 0.16f));
        }
        return root;
    }

    private GameObject CreateBirchTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject($"Birch_{index:000}");
        root.transform.localPosition = pos;
        float trunkH = height * 0.7f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.2f, trunkH * 0.5f, 0.2f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.82f, 0.78f, 0.72f));
        for (int layer = 0; layer < 2; layer++)
        {
            float layerY = trunkH * 0.6f + layer * (height * 0.22f);
            float r = height * (0.22f - layer * 0.06f);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, layerY, 0f);
            crown.transform.localScale = new Vector3(r * 2f, r * 1.4f, r * 2f);
            SetColor(crown, new Color(0.30f, 0.55f, 0.22f));
        }
        return root;
    }

    private bool IsNearTrack(Vector3 pos, List<Vector3> trackPoints, float minDist)
    {
        float minDistSqr = minDist * minDist;
        for (int i = 0; i < trackPoints.Count; i++)
        {
            Vector3 tp = trackPoints[i];
            float dx = pos.x - tp.x;
            float dz = pos.z - tp.z;
            if (dx * dx + dz * dz < minDistSqr) return true;
        }
        return false;
    }

    private List<Vector3> GetTrackExclusionPoints()
    {
        SplineTrackGenerator gen = FindAnyObjectByType<SplineTrackGenerator>();
        if (gen != null) return gen.GetTrackPoints();
        return new List<Vector3>();
    }

    private static Texture2D CreateSolidColorTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void SetColor(GameObject obj, Color color, bool transparent = false)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat == null) mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        if (transparent)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        renderer.sharedMaterial = mat;
    }
}
