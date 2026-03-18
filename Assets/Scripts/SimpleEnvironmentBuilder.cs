using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleEnvironmentBuilder : MonoBehaviour
{
    [Header("Feature Flags")]
    [SerializeField] private bool buildTrees = true;
    [SerializeField] private bool buildPonds = true;
    [SerializeField] private bool buildNearTrackPonds = true;
    [SerializeField] private bool buildClouds = false;
    [SerializeField] private bool buildAnimals = true;
    [SerializeField] private bool buildMountains = true;
    [SerializeField] private bool buildRollerCoasterAnimals = true;

    [Header("Terrain")]
    [SerializeField, Min(64f)] private float terrainSize = 1200f;
    [SerializeField, Min(10f)] private float terrainHeight = 200f;
    [SerializeField] private int heightmapResolution = 513;

    [Header("Custom Assets")]
    [SerializeField] private GameObject[] customTreePrefabs;
    [SerializeField] private GameObject[] customCloudPrefabs;
    [SerializeField] private GameObject[] customPondPrefabs;
    [SerializeField] private GameObject[] customAnimalPrefabs;
    [SerializeField] private GameObject[] customFishPrefabs;
    [SerializeField] private GameObject[] customMountainPrefabs;
    [SerializeField] private int customMountainBGCount = 0; // kac prefab BackgroundMountainFree
    [SerializeField] private GameObject[] customRockPrefabs;
    [SerializeField] private GameObject[] customBushPrefabs;
    [SerializeField] private GameObject[] customCoasterAnimalPrefabs;

    [Header("Trees")]
    [SerializeField, Min(0)] private int treeCount = 3000;
    [SerializeField] private float treeSpawnRadius = 500f;
    [SerializeField] private Vector2 treeHeightRange = new Vector2(5f, 10f);
    [SerializeField] private float trackExclusionRadius = 18f;
    [SerializeField] private float stationExclusionRadius = 35f;

    [Header("Ponds")]
    [SerializeField, Min(1)] private int pondCount = 7;
    [SerializeField] private float pondMinRadius = 10f;
    [SerializeField] private float pondMaxRadius = 28f;
    [SerializeField] private float pondSpawnRadius = 350f;
    [SerializeField] private float pondYOffset = 0.08f;
    [SerializeField] private Material waterMaterial;

    [Header("Near-Track Ponds")]
    [SerializeField, Min(1)] private int nearTrackPondCount = 4;
    [SerializeField] private float nearTrackPondMinDist = 22f;
    [SerializeField] private float nearTrackPondMaxDist = 45f;

    [Header("Clouds")]
    [SerializeField, Min(5)] private int cloudCount = 18;
    [SerializeField] private float cloudMinAltitude = 80f;
    [SerializeField] private float cloudMaxAltitude = 140f;
    [SerializeField] private float cloudSpawnRadius = 500f;
    [SerializeField] private float cloudDriftSpeed = 2f;
    [SerializeField] private Material cloudMaterial;

    [Header("Sky")]
    [SerializeField] private Material skyboxMaterial;

    public void SetSkyboxMaterial(Material m) { skyboxMaterial = m; }
    public void SetCloudMaterial(Material m) { cloudMaterial = m; }

    // ================================================================
    //  BUILD
    // ================================================================
    [ContextMenu("BuildEnvironment")]
    public void BuildEnvironment()
    {
#if UNITY_EDITOR
        AutoAssignAssets();
#endif
        CreateFlatTerrain();
        if (buildMountains) CreateMountains();
        if (buildTrees) CreateProfessionalTrees();
        if (buildPonds) CreatePondsAndFauna();
        if (buildNearTrackPonds) CreateNearTrackPonds();
        CreateCentralFeaturePond(); // Pistin yanindaki buyuk ozel golet
        if (buildRollerCoasterAnimals) CreateRollerCoasterAnimals();

        // Bulutlar KAPALI - mevcut varsa temizle
        {
            Transform ec = transform.Find("PrototypeClouds");
            if (ec != null) DestroyImmediate(ec.gameObject);
        }
        if (buildClouds) CreateRealisticClouds();
        ApplySkyboxOnly();
#if UNITY_EDITOR
        ForceRecoverEnvironment();
#endif
    }

#if UNITY_EDITOR
    // ================================================================
    //  AUTO ASSIGN ASSETS
    // ================================================================
    private void AutoAssignAssets()
    {
        // --- AGACLAR (MysticForge URP Pine + Woodland + Forest Pack) ---
        var trees = new List<GameObject>();

        // Forest Pack agaclari – PRIMARY (yuvarlak, gur yaprakli, en iyi orman gorunumu)
        string[] forestTreePaths = new string[]
        {
            "Assets/Forest Pack/Prefabs/Tree001_V1.prefab",
            "Assets/Forest Pack/Prefabs/Tree001_V2.prefab",
            "Assets/Forest Pack/Prefabs/Tree002_V1.prefab",
            "Assets/Forest Pack/Prefabs/Tree002_V2.prefab",
            "Assets/Forest Pack/Prefabs/Tree003_V1.prefab",
            "Assets/Forest Pack/Prefabs/Tree003_V2.prefab",
            "Assets/Forest Pack/Prefabs/Tree004_V1.prefab",
            "Assets/Forest Pack/Prefabs/Tree004_V2.prefab",
            "Assets/Forest Pack/Prefabs/Tree004_V3.prefab",
            "Assets/Forest Pack/Prefabs/Tree004_V4.prefab",
            "Assets/Forest Pack/Prefabs/Tree005_V1.prefab",
            "Assets/Forest Pack/Prefabs/Tree005_V2.prefab",
        };
        foreach (var p in forestTreePaths) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (t) trees.Add(t); }

        // Nicrom LPW Trees – SECONDARY (ruzgar animasyonlu, URP dostu)
        string[] nicromPaths = new string[]
        {
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_A1_6.5m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_B1_9m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_B1_9m_02.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_C1_11m_01.prefab",
            "Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_C1_9m_01.prefab"
        };
        foreach (var p in nicromPaths) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (t) trees.Add(t); }

        // MysticForge Woodland BasicTrees – SECONDARY (sadece genis yaprakli tipler)
        int[] woodNums = new int[] { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 };
        foreach (int n in woodNums)
        {
            string wp = "Assets/Low Poly Tree Mega Pack by MysticForge/Prefabs/URP/Woodland/BasicTree " + n + ".prefab";
            var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(wp);
            if (t) trees.Add(t);
        }

        // Tree9 – FALLBACK
        string[] tree9Paths = new string[]
        {
            "Assets/Tree9/Tree9_2.prefab", "Assets/Tree9/Tree9_3.prefab",
            "Assets/Tree9/Tree9_4.prefab", "Assets/Tree9/Tree9_5.prefab"
        };
        foreach (var p in tree9Paths) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (t) trees.Add(t); }

        if (trees.Count > 0) customTreePrefabs = trees.ToArray();

        // --- GOLET HAYVANLARI ---
        var butterfly = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Butterfly (Animated)/Prefab/Butterfly.prefab");
        var goat = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Goat_dark.prefab");
        var sheep = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Sheep_white.prefab");
        var deer = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Backrock Studios/LowPoly-Animals/Prefabs/Deer/Deer_v1.prefab");
        var horse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Backrock Studios/LowPoly-Animals/Prefabs/Horse/Horse_v1.prefab");
        var pa = new List<GameObject>();
        if (butterfly) pa.Add(butterfly); if (goat) pa.Add(goat); if (sheep) pa.Add(sheep);
        if (deer) pa.Add(deer); if (horse) pa.Add(horse);
        if (pa.Count > 0) customAnimalPrefabs = pa.ToArray();

        // --- ROLLER COASTER HAYVANLARI (ithappy Animals_FREE) ---
        string[] ithappyPaths = new string[]
        {
            "Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Deer_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Horse_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Pinguin_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Tiger_001.prefab"
        };
        var ca = new List<GameObject>();
        foreach (var p in ithappyPaths) { var a = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (a) ca.Add(a); }
        if (ca.Count > 0) customCoasterAnimalPrefabs = ca.ToArray();

        // --- LOW POLY FISH (Floreswa) ---
        string[] fishPaths = new string[]
        {
            "Assets/Floreswa/Prefabs/fish01.prefab",
            "Assets/Floreswa/Prefabs/fish02.prefab",
            "Assets/Floreswa/Prefabs/fish03.prefab"
        };
        var fl = new List<GameObject>();
        foreach (var p in fishPaths) { var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (f) fl.Add(f); }
        if (fl.Count > 0) customFishPrefabs = fl.ToArray();

        // --- DAGLAR ---
        // backgroundMountainPrefabs: kucuk, backdrop icin tasarlanmis -> olcek 3-6x guvenli
        var bgMountains = new List<GameObject>();
        string[] bgMtPaths = new string[]
        {
            "Assets/BackgroundMountainFree/Prefabs/LowPolyMountain.prefab",
            "Assets/BackgroundMountainFree/Prefabs/MediumPolyMountain.prefab",
            "Assets/BackgroundMountainFree/Prefabs/ExtremeLowPolyMountain.prefab"
        };
        foreach (var p in bgMtPaths) { var m = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (m) bgMountains.Add(m); }

        // hqpMountainPrefabs: cok buyuk terrain parcalari -> sadece olcek 1-1.5x + cok derin gomme
        var hqpMountains = new List<GameObject>();
        string[] hqpMtPaths = new string[]
        {
            "Assets/HQP STUDIOS/Rocks and Terrains Pack - Low Poly/Prefabs/Terrains/Mountains/NoLOD/Mountain_L_01.prefab",
            "Assets/HQP STUDIOS/Rocks and Terrains Pack - Low Poly/Prefabs/Terrains/Mountains/NoLOD/Mountain_L_05.prefab",
            "Assets/HQP STUDIOS/Rocks and Terrains Pack - Low Poly/Prefabs/Terrains/Mountains/NoLOD/Mountain_L_10.prefab",
            "Assets/HQP STUDIOS/Rocks and Terrains Pack - Low Poly/Prefabs/Terrains/Mountains/NoLOD/Mountain_L_15.prefab",
            "Assets/HQP STUDIOS/Rocks and Terrains Pack - Low Poly/Prefabs/Terrains/Mountains/NoLOD/Mountain_L_20.prefab"
        };
        foreach (var p in hqpMtPaths) { var m = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (m) hqpMountains.Add(m); }

        // customMountainPrefabs[0..bgCount-1]  = BackgroundMountainFree (guvenli olcek)
        // customMountainPrefabs[bgCount..]     = HQP (kucuk olcek + derin gomme)
        var ml = new List<GameObject>(bgMountains);
        ml.AddRange(hqpMountains);
        if (ml.Count > 0) customMountainPrefabs = ml.ToArray();
        // kaclari BG oldugunu kaydet (olcek ayrimi icin)
        // BG count: bgMountains.Count  -> CustomMountainBGCount field'i kullanilacak
        customMountainBGCount = bgMountains.Count;

        // --- KAYALAR (Low Poly Stones) ---
        string[] rockPaths = new string[]
        {
            "Assets/Low Poly Stones/Prefabs/ST_Stone1.prefab",
            "Assets/Low Poly Stones/Prefabs/ST_Stone2.prefab",
            "Assets/Low Poly Stones/Prefabs/ST_Stone3.prefab",
            "Assets/Low Poly Stones/Prefabs/ST_Stone4.prefab",
            "Assets/Low Poly Stones/Prefabs/ST_Stone5.prefab"
        };
        var rl = new List<GameObject>();
        foreach (var p in rockPaths) { var r = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (r) rl.Add(r); }
        if (rl.Count > 0) customRockPrefabs = rl.ToArray();

        // --- CALILAR (YughuesFreeBushes2018) ---
        string[] bushPaths = new string[]
        {
            "Assets/YughuesFreeBushes2018/Prefabs/P_Bush01.prefab",
            "Assets/YughuesFreeBushes2018/Prefabs/P_Bush02.prefab",
            "Assets/YughuesFreeBushes2018/Prefabs/P_Bush03.prefab",
            "Assets/YughuesFreeBushes2018/Prefabs/P_Bush04.prefab",
            "Assets/YughuesFreeBushes2018/Prefabs/P_Bush05.prefab"
        };
        var bl = new List<GameObject>();
        foreach (var p in bushPaths) { var b = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (b) bl.Add(b); }
        if (bl.Count > 0) customBushPrefabs = bl.ToArray();

        waterMaterial = null;
        if (skyboxMaterial == null)
        {
            var skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Cubemaps/Classic/FS000_Day_01.mat");
            if (skyMat) skyboxMaterial = skyMat;
        }
    }

    private void ForceRecoverEnvironment()
    {
        var atmoObj = GameObject.Find("AtmosphereManager");
        if (atmoObj != null) DestroyImmediate(atmoObj);
        var atmoComps = FindObjectsByType<AtmosphereManager>(FindObjectsSortMode.None);
        foreach (var a in atmoComps) DestroyImmediate(a);

        if (skyboxMaterial != null) RenderSettings.skybox = skyboxMaterial;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        DynamicGI.UpdateEnvironment();

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

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        string[] matFolders = new string[]
        {
            "Assets/BEDRILL", "Assets/ALP_Assets", "Assets/Forest Pack",
            "Assets/Butterfly (Animated)", "Assets/Symphonie", "Assets/Nicrom", "Assets/Tree9",
            "Assets/Floreswa", "Assets/ithappy", "Assets/HQP STUDIOS",
            "Assets/Low Poly Stones", "Assets/YughuesFreeBushes2018",
            "Assets/BackgroundMountainFree",
            "Assets/Low Poly Tree Mega Pack by MysticForge"
        };
        foreach (string folder in matFolders)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (string guid in guids)
            {
                string mp = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (mat != null && mat.shader != urpLit)
                {
                    Color mc = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture mtx = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    mat.shader = urpLit;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mc);
                    if (mat.HasProperty("_BaseMap") && mtx != null) mat.SetTexture("_BaseMap", mtx);
                    UnityEditor.EditorUtility.SetDirty(mat);
                }
            }
        }
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
#endif

    // ================================================================
    //  TERRAIN
    // ================================================================
    private void CreateFlatTerrain()
    {
        Transform existing = transform.Find("PrototypeTerrain");
        if (existing != null) DestroyImmediate(existing.gameObject);

        TerrainData td = new TerrainData
        {
            heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1,
            size = new Vector3(terrainSize, terrainHeight, terrainSize)
        };
        int res = td.heightmapResolution;
        float[,] heights = new float[res, res];
        float fns = 0.008f, fnStr = 0.003f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                heights[y, x] = Mathf.PerlinNoise(x * fns + 50f, y * fns + 50f) * fnStr;
        td.SetHeights(0, 0, heights);

        Texture2D gt = CreateSolidColorTexture(64, new Color(0.28f, 0.55f, 0.22f));
        TerrainLayer gl = new TerrainLayer { diffuseTexture = gt, tileSize = new Vector2(10f, 10f) };
        td.terrainLayers = new TerrainLayer[] { gl };

        float[,,] am = new float[td.alphamapResolution, td.alphamapResolution, 1];
        for (int y = 0; y < td.alphamapResolution; y++)
            for (int x = 0; x < td.alphamapResolution; x++)
                am[y, x, 0] = 1f;
        td.SetAlphamaps(0, 0, am);

        GameObject terrainObj = Terrain.CreateTerrainGameObject(td);
        terrainObj.name = "PrototypeTerrain";
        terrainObj.transform.SetParent(transform, false);
        terrainObj.transform.position = new Vector3(-terrainSize * 0.5f, -2f, -terrainSize * 0.5f);
    }

    // ================================================================
    //  MOUNTAINS
    //  Mesh bounds OLCULEREK guvenli mesafe ve olcek hesaplanir.
    //  Her prefabin gercek boyutunu alir -> hic tahminde bulunmaz.
    //  Hedef: daglarin tabaninin gameplay alanina (treeSpawnRadius+80m)
    //  kesinlikle girmemesi.
    // ================================================================
    private void CreateMountains()
    {
        Transform existing = transform.Find("PrototypeMountains");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject root = new GameObject("PrototypeMountains");
        root.transform.SetParent(transform, false);
        if (customMountainPrefabs == null || customMountainPrefabs.Length == 0) return;

        Random.State saved = Random.state;
        Random.InitState(7);

        // Gameplay alaninin otesinde tampon: agaclar 500m, +100m guvenlik bandi
        float safeRadius = treeSpawnRadius + 100f;

        // Hedef gorus yuksekligi: daglarin yukari ne kadar cikacagi (Unity metres)
        float targetPeakHeight = 200f;

        int ringCount = 26;
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (i * Mathf.PI * 2f / ringCount) + Random.Range(-0.10f, 0.10f);

            GameObject prefab = customMountainPrefabs[Random.Range(0, customMountainPrefabs.Length)];
            if (prefab == null) continue;

            // Prefabin gercek mesh boyutlarini al
            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds mb = mf.sharedMesh.bounds;
            float meshHeight    = Mathf.Max(mb.size.y, 0.001f);
            float meshHalfWidth = Mathf.Max(mb.extents.x, mb.extents.z);

            // Istenen gorunur yukseklige gore olcegi hesapla
            float scale = Mathf.Clamp(targetPeakHeight / meshHeight, 0.01f, 50f);

            // Guvenli minimum mesafe: taban gameplay alanini gecmesin
            float halfWidthWorld = meshHalfWidth * scale;
            float minDist = safeRadius + halfWidthWorld + 50f; // 50m ekstra tampon
            float dist = minDist + Random.Range(0f, 80f);

            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            // Tabanin %15'ini topraga göm: dogal gorunum
            pos.y = GetTerrainY(pos) - meshHeight * scale * 0.15f;

            GameObject mountain;
#if UNITY_EDITOR
            mountain = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
            mountain = Instantiate(prefab, root.transform);
#endif
            mountain.transform.position = pos;
            mountain.transform.localScale = Vector3.one * scale;
            mountain.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            FixPinkMaterials(mountain);
        }

        // Arka plan derinligi: daha buyuk, daha uzak
        for (int i = 0; i < 12; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);

            GameObject prefab = customMountainPrefabs[Random.Range(0, customMountainPrefabs.Length)];
            if (prefab == null) continue;

            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds mb = mf.sharedMesh.bounds;
            float meshHeight    = Mathf.Max(mb.size.y, 0.001f);
            float meshHalfWidth = Mathf.Max(mb.extents.x, mb.extents.z);

            // Arka plan: %50 daha buyuk, daha uzakta
            float scale = Mathf.Clamp(targetPeakHeight * 1.5f / meshHeight, 0.01f, 50f);
            float halfWidthWorld = meshHalfWidth * scale;
            float minDist = safeRadius + halfWidthWorld + 100f;
            float dist = minDist + Random.Range(50f, 150f);

            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            pos.y = GetTerrainY(pos) - meshHeight * scale * 0.20f;

            GameObject mountain;
#if UNITY_EDITOR
            mountain = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
            mountain = Instantiate(prefab, root.transform);
#endif
            mountain.transform.position = pos;
            mountain.transform.localScale = Vector3.one * scale;
            mountain.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            FixPinkMaterials(mountain);
        }

        Random.state = saved;
    }

    // ================================================================
    //  TREES

    // ================================================================
    private void CreateProfessionalTrees()
    {
        Transform existing = transform.Find("PrototypeTrees");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject root = new GameObject("PrototypeTrees");
        root.transform.SetParent(transform, false);

        List<Vector3> trackPoints = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(99);
        bool hasCustom = customTreePrefabs != null && customTreePrefabs.Length > 0;
        int totalPlaced = 0;

        // ===== YOGUN ORMAN KUMELERI =====
        // Her kume: Gaussian dagilimla yogun merkez, seyrelen kenar
        Vector3[] clusterCenters = new Vector3[]
        {
            new Vector3(-180f, 0f,  130f),
            new Vector3( 165f, 0f,  210f),
            new Vector3(-230f, 0f, -160f),
            new Vector3( 260f, 0f, -190f),
            new Vector3(  55f, 0f,  300f),
        };
        float[] clusterRadii  = new float[] { 95f,  85f, 105f,  90f,  80f };
        int[]   clusterCounts = new int[]   { 520, 460,  560, 490, 420 };

        for (int c = 0; c < clusterCenters.Length; c++)
        {
            Vector3 cc  = clusterCenters[c];
            float   cr  = clusterRadii[c];
            int     cnt = clusterCounts[c];
            int     att = 0;

            while (att < cnt * 8 && totalPlaced < treeCount)
            {
                att++;
                float u1 = Mathf.Max(0.0001f, 1f - Random.value);
                float u2 = 1f - Random.value;
                float gauss = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
                float dist  = Mathf.Abs(gauss * cr * 0.45f);
                if (dist > cr) continue;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 bp  = cc + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                bp.y = GetTerrainY(bp);

                if (new Vector2(bp.x, bp.z).magnitude < stationExclusionRadius) continue;
                if (IsNearTrack(bp, trackPoints, trackExclusionRadius)) continue;

                PlaceTree(bp, root.transform, hasCustom, totalPlaced);
                totalPlaced++;
            }
        }

        // ===== DAGITIK ARKA PLAN AGACLARI =====
        int scattered = treeCount - totalPlaced;
        int scAtt = 0;
        while (scAtt < scattered * 7 && totalPlaced < treeCount)
        {
            scAtt++;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(55f, treeSpawnRadius);
            if (dist < 150f && Random.value < 0.55f) continue;

            Vector3 bp = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            bp.y = GetTerrainY(bp);

            if (new Vector2(bp.x, bp.z).magnitude < stationExclusionRadius) continue;
            if (IsNearTrack(bp, trackPoints, trackExclusionRadius)) continue;

            PlaceTree(bp, root.transform, hasCustom, totalPlaced);
            totalPlaced++;
        }

        Random.state = saved;
    }

    private void PlaceTree(Vector3 pos, Transform parent, bool hasCustom, int index)
    {
        GameObject tree = null;
        if (hasCustom)
        {
            GameObject prefab = customTreePrefabs[Random.Range(0, customTreePrefabs.Length)];
            if (prefab != null)
            {
#if UNITY_EDITOR
                tree = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#else
                tree = Instantiate(prefab, parent);
#endif
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                tree.transform.localScale = prefab.transform.localScale * Random.Range(1.5f, 2.2f);
            }
        }
        if (tree == null)
        {
            float h = Random.Range(treeHeightRange.x, treeHeightRange.y);
            switch (Random.Range(0, 3))
            {
                case 0:  tree = CreatePineTree(pos, h, index);  break;
                case 1:  tree = CreateOakTree(pos, h, index);   break;
                default: tree = CreateBirchTree(pos, h, index); break;
            }
            tree.transform.SetParent(parent, true);
        }
    }

    // ================================================================
    //  PONDS AND FAUNA  (uzak goletler)
    // ================================================================
    private void CreatePondsAndFauna()
    {
        Transform existing = transform.Find("PrototypePonds");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject pondsRoot = new GameObject("PrototypePonds");
        pondsRoot.transform.SetParent(transform, false);

        List<Vector3> trackPts = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(42);
        int placed = 0, attempts = 0;

        while (placed < pondCount && attempts < pondCount * 15)
        {
            attempts++;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(60f, pondSpawnRadius);
            float radius = Random.Range(pondMinRadius, pondMaxRadius);
            Vector3 center = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            center.y = GetTerrainY(center) + pondYOffset;
            Vector2 pos2D = new Vector2(center.x, center.z);
            if (IsNearTrack(center, trackPts, trackExclusionRadius + radius)) continue;
            if (pos2D.magnitude < stationExclusionRadius + radius) continue;
            SpawnPond(pondsRoot.transform, center, radius, placed);
            placed++;
        }
        Random.state = saved;
    }

    // ================================================================
    //  NEAR-TRACK PONDS  (piste yakin yeni goletler)
    // ================================================================
    private void CreateNearTrackPonds()
    {
        Transform existing = transform.Find("NearTrackPonds");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject pondsRoot = new GameObject("NearTrackPonds");
        pondsRoot.transform.SetParent(transform, false);

        List<Vector3> trackPts = GetTrackExclusionPoints();
        if (trackPts.Count == 0) return;

        Random.State saved = Random.state;
        Random.InitState(88);
        int placed = 0, attempts = 0;

        while (placed < nearTrackPondCount && attempts < nearTrackPondCount * 30)
        {
            attempts++;
            Vector3 trackPt = trackPts[Random.Range(0, trackPts.Count)];
            float sideAngle = Random.Range(0f, Mathf.PI * 2f);
            float sideDist = Random.Range(nearTrackPondMinDist, nearTrackPondMaxDist);
            Vector3 center = trackPt + new Vector3(Mathf.Cos(sideAngle) * sideDist, 0f, Mathf.Sin(sideAngle) * sideDist);
            float radius = Random.Range(pondMinRadius * 0.5f, pondMinRadius * 1.3f);
            center.y = GetTerrainY(center) + pondYOffset;
            if (IsNearTrack(center, trackPts, trackExclusionRadius + radius)) continue;
            if (new Vector2(center.x, center.z).magnitude < stationExclusionRadius) continue;
            SpawnPond(pondsRoot.transform, center, radius, placed + 100);
            placed++;
        }
        Random.state = saved;
    }

    // ================================================================
    //  SPAWN POND  (ortak golet olusturucu)
    // ================================================================
    private void SpawnPond(Transform parent, Vector3 center, float pondRadius, int index)
    {
        GameObject pondRoot = new GameObject(string.Format("Pond_{0:00}", index));
        pondRoot.transform.SetParent(parent, true);
        pondRoot.transform.position = center;
        pondRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        bool hasCustomPonds = customPondPrefabs != null && customPondPrefabs.Length > 0;
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
                pondMesh.transform.localScale *= pondRadius / 10f;
            }
        }

        if (pondMesh == null)
        {
            // Su yuzeyi - terrain ustunde, z-fighting olmamasi icin offset
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "WaterSurface";
            water.transform.SetParent(pondRoot.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            water.transform.localScale = new Vector3(pondRadius * 2f, 0.04f, pondRadius * 2f);
            SetColor(water, new Color(0.10f, 0.38f, 0.80f, 0.88f), true);
            Object.DestroyImmediate(water.GetComponent<Collider>());

            // Golet kenari
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pondRoot.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            rim.transform.localScale = new Vector3(pondRadius * 2.3f, 0.02f, pondRadius * 2.3f);
            SetColor(rim, new Color(0.15f, 0.38f, 0.13f));
            Object.DestroyImmediate(rim.GetComponent<Collider>());
        }

        if (buildAnimals) SpawnFauna(pondRoot.transform, center, pondRadius);
    }

    // ================================================================
    //  CENTRAL FEATURE POND  –  Pistin hemen yanindaki buyuk golet
    //  Cok sayida balik, kaya kumesi, cali kumesi, karisik hayvanlar
    // ================================================================
    private void CreateCentralFeaturePond()
    {
        Transform existing = transform.Find("CentralFeaturePond");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("CentralFeaturePond");
        root.transform.SetParent(transform, false);

        // Merkez: pistten 50-70m uzaklikta, sag tarafa
        Vector3 center = new Vector3(55f, 0f, 40f);
        center.y = GetTerrainY(center) + pondYOffset;
        float radius = 45f; // Buyuk golet

        root.transform.position = center;

        // Su yuzeyi
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "CentralWaterSurface";
        water.transform.SetParent(root.transform, false);
        water.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        water.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        SetColor(water, new Color(0.08f, 0.35f, 0.75f, 0.90f), true);
        Object.DestroyImmediate(water.GetComponent<Collider>());

        // Golet kenari (yesilimsi zemin halkasi)
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "CentralPondRim";
        rim.transform.SetParent(root.transform, false);
        rim.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        rim.transform.localScale = new Vector3(radius * 2.4f, 0.03f, radius * 2.4f);
        SetColor(rim, new Color(0.12f, 0.36f, 0.11f));
        Object.DestroyImmediate(rim.GetComponent<Collider>());

        // Baliklar (bol miktarda)
        bool hasFish = customFishPrefabs != null && customFishPrefabs.Length > 0;
        Random.State saved = Random.state;
        Random.InitState(77);

        for (int i = 0; i < 12; i++)
        {
            float fa = Random.Range(0f, Mathf.PI * 2f);
            float fd = radius * Random.Range(0.2f, 0.75f);
            Vector3 fp = center + new Vector3(Mathf.Cos(fa) * fd, 0f, Mathf.Sin(fa) * fd);
            fp.y = GetTerrainY(fp) + 0.05f;

            GameObject fish = null;
            if (hasFish)
            {
                var prefab = customFishPrefabs[Random.Range(0, customFishPrefabs.Length)];
                if (prefab != null)
                {
#if UNITY_EDITOR
                    fish = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
                    fish = Instantiate(prefab, root.transform);
#endif
                    fish.transform.localScale = prefab.transform.localScale * Random.Range(0.3f, 0.65f);
                    fish.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
            if (fish == null)
            {
                fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fish.name = "FishPrimitive";
                fish.transform.SetParent(root.transform, true);
                fish.transform.localScale = new Vector3(0.5f, 0.3f, 1.1f);
                SetColor(fish, new Color(0.2f, 0.6f, 0.9f));
                Object.DestroyImmediate(fish.GetComponent<Collider>());
            }
            fish.transform.position = fp;
            var jumper = fish.AddComponent<JumpingFish>();
            jumper.jumpHeight = Random.Range(1.5f, 3.5f);
            jumper.jumpDistance = Random.Range(2f, 6f);
            jumper.jumpIntervalMin = Random.Range(0.8f, 2.5f);
            jumper.jumpIntervalMax = Random.Range(3f, 7f);
        }

        // Kaya kumeleri (3 kume, golet etrafinda)
        bool hasRocks = customRockPrefabs != null && customRockPrefabs.Length > 0;
        if (hasRocks)
        {
            for (int c = 0; c < 4; c++)
            {
                float ca = (c * Mathf.PI * 0.5f) + Random.Range(-0.3f, 0.3f);
                float cd = radius * Random.Range(0.95f, 1.4f);
                Vector3 cc = center + new Vector3(Mathf.Cos(ca) * cd, 0f, Mathf.Sin(ca) * cd);
                for (int r = 0; r < Random.Range(4, 8); r++)
                {
                    float ra = Random.Range(0f, Mathf.PI * 2f);
                    float rd = Random.Range(0.5f, 5f);
                    Vector3 rp = cc + new Vector3(Mathf.Cos(ra) * rd, 0f, Mathf.Sin(ra) * rd);
                    rp.y = GetTerrainY(rp) + 0.02f;
                    var prefab = customRockPrefabs[Random.Range(0, customRockPrefabs.Length)];
                    if (prefab == null) continue;
                    GameObject rock;
#if UNITY_EDITOR
                    rock = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
                    rock = Instantiate(prefab, root.transform);
#endif
                    rock.transform.position = rp;
                    rock.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                    float rockScale = (r == 0) ? Random.Range(1.2f, 2.0f) : Random.Range(0.3f, 1.1f);
                    rock.transform.localScale = prefab.transform.localScale * rockScale;
                }
            }
        }

        // Cali kumeleri (golet etrafinda)
        bool hasBushes = customBushPrefabs != null && customBushPrefabs.Length > 0;
        if (hasBushes)
        {
            for (int c = 0; c < 5; c++)
            {
                float ba = Random.Range(0f, Mathf.PI * 2f);
                float bd = radius * Random.Range(1.05f, 1.8f);
                Vector3 bc = center + new Vector3(Mathf.Cos(ba) * bd, 0f, Mathf.Sin(ba) * bd);
                for (int b = 0; b < Random.Range(3, 6); b++)
                {
                    float bra = Random.Range(0f, Mathf.PI * 2f);
                    float brd = Random.Range(0.3f, 3.5f);
                    Vector3 bp = bc + new Vector3(Mathf.Cos(bra) * brd, 0f, Mathf.Sin(bra) * brd);
                    bp.y = GetTerrainY(bp) + 0.02f;
                    var prefab = customBushPrefabs[Random.Range(0, customBushPrefabs.Length)];
                    if (prefab == null) continue;
                    GameObject bush;
#if UNITY_EDITOR
                    bush = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
                    bush = Instantiate(prefab, root.transform);
#endif
                    bush.transform.position = bp;
                    bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    bush.transform.localScale = prefab.transform.localScale * Random.Range(0.8f, 1.6f);
                }
            }
        }

        // Karisik hayvanlar golet etrafinda (farkli turler farkli yerlerde)
        bool hasAnimals = customAnimalPrefabs != null && customAnimalPrefabs.Length > 0;
        if (hasAnimals)
        {
            int animalCount = 14;
            for (int i = 0; i < animalCount; i++)
            {
                float aa = (i * Mathf.PI * 2f / animalCount) + Random.Range(-0.2f, 0.2f);
                float ad = radius + Random.Range(2f, 18f);
                Vector3 ap = center + new Vector3(Mathf.Cos(aa) * ad, 0f, Mathf.Sin(aa) * ad);
                ap.y = GetTerrainY(ap);

                // Her 4 hayvanda bir kelebek, geri kalani buyuk hayvan
                bool butterfly = (i % 4 == 0);
                int idx = Random.Range(0, customAnimalPrefabs.Length);
                if (butterfly)
                {
                    for (int pp = 0; pp < customAnimalPrefabs.Length; pp++)
                        if (customAnimalPrefabs[pp] != null && customAnimalPrefabs[pp].name.Contains("Butterfly"))
                        { idx = pp; break; }
                }
                else
                {
                    for (int tries = 0; tries < 5; tries++)
                    {
                        int cand = Random.Range(0, customAnimalPrefabs.Length);
                        if (customAnimalPrefabs[cand] != null && !customAnimalPrefabs[cand].name.Contains("Butterfly"))
                        { idx = cand; break; }
                    }
                }

                var prefab = customAnimalPrefabs[idx];
                if (prefab == null) continue;
                GameObject animal;
#if UNITY_EDITOR
                animal = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
                animal = Instantiate(prefab, root.transform);
#endif
                bool isBfly = prefab.name.Contains("Butterfly") || prefab.name.Contains("fly");
                if (isBfly)
                {
                    animal.transform.localScale = Vector3.one * Random.Range(0.006f, 0.018f);
                    ap.y += Random.Range(0.5f, 2.5f);
                    animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
                else
                {
                    animal.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                    ap.y += 0.1f;
                    Vector3 look = center - ap; look.y = 0f;
                    if (look != Vector3.zero) animal.transform.rotation = Quaternion.LookRotation(look);
                }
                animal.transform.position = ap;
            }
        }

        Random.state = saved;
    }

    // ================================================================
    //  SPAWN FAUNA  (baliklar + hayvanlar + kayalar + calilar)
    // ================================================================
    private void SpawnFauna(Transform pondRoot, Vector3 pondCenter, float pondRadius)
    {
        bool hasFish = customFishPrefabs != null && customFishPrefabs.Length > 0;
        bool hasAnimals = customAnimalPrefabs != null && customAnimalPrefabs.Length > 0;
        bool hasRocks = customRockPrefabs != null && customRockPrefabs.Length > 0;
        bool hasBushes = customBushPrefabs != null && customBushPrefabs.Length > 0;

        // 1. LOW POLY FISH (Floreswa fish01/02/03)
        int fishCount = Random.Range(2, 5);
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 fishPos = pondCenter + new Vector3(
                Random.Range(-pondRadius * 0.65f, pondRadius * 0.65f), 0f,
                Random.Range(-pondRadius * 0.65f, pondRadius * 0.65f));
            fishPos.y = GetTerrainY(fishPos) + 0.05f;

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
                    fish.transform.localScale = prefab.transform.localScale * Random.Range(0.3f, 0.7f);
                    fish.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
            if (fish == null)
            {
                fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fish.name = "FishPrimitive";
                fish.transform.SetParent(pondRoot, true);
                fish.transform.localScale = new Vector3(0.5f, 0.3f, 1.1f);
                SetColor(fish, new Color(0.2f, 0.6f, 0.9f));
                Object.DestroyImmediate(fish.GetComponent<Collider>());
            }
            fish.transform.position = fishPos;
            var jumper = fish.AddComponent<JumpingFish>();
            jumper.jumpHeight = Random.Range(1.2f, 3.0f);
            jumper.jumpDistance = Random.Range(1.5f, 4.0f);
            jumper.jumpIntervalMin = Random.Range(1f, 3f);
            jumper.jumpIntervalMax = Random.Range(4f, 8f);
        }

        // 2. GOLET HAYVANLARI – dogal dagitim
        //    Kelebekler: yuksekte, cicek etrafinda
        //    Buyuk hayvanlar: birbirinden uzak, suya dogru bakan
        int animalCount = Random.Range(4, 9);
        float baseAngle = Random.Range(0f, Mathf.PI * 2f); // baslangic acisi (kumelesin)
        for (int i = 0; i < animalCount; i++)
        {
            // Her hayvan govdeyi 45-90 derece ilerler (halka dugumler)
            float aa = baseAngle + i * (Mathf.PI * 2f / animalCount) + Random.Range(-0.3f, 0.3f);
            // Kelebekler: daha genis dagilim
            // Buyuk hayvanlar: golet kenarinda
            bool forButterfly = (i % 3 == 0); // 3'te 1 kelebek yeri
            float minD = forButterfly ? pondRadius * 0.5f : pondRadius + 1.5f;
            float maxD = forButterfly ? pondRadius + 4f : pondRadius + 16f;
            float ad = Random.Range(minD, maxD);

            Vector3 ap = pondCenter + new Vector3(Mathf.Cos(aa) * ad, 0f, Mathf.Sin(aa) * ad);
            ap.y = GetTerrainY(ap);

            GameObject animal = null;
            if (hasAnimals)
            {
                int idx;
                if (forButterfly)
                {
                    // Kelebek sec (varsa)
                    idx = Random.Range(0, customAnimalPrefabs.Length);
                    for (int pp = 0; pp < customAnimalPrefabs.Length; pp++)
                        if (customAnimalPrefabs[pp] != null && customAnimalPrefabs[pp].name.Contains("Butterfly"))
                        { idx = pp; break; }
                }
                else
                {
                    // Buyuk hayvan sec (Butterfly DEGIL)
                    idx = Random.Range(0, customAnimalPrefabs.Length);
                    // Kelebek cikmamasi icin tekrar sec
                    for (int tries = 0; tries < 5; tries++)
                    {
                        int cand = Random.Range(0, customAnimalPrefabs.Length);
                        if (customAnimalPrefabs[cand] != null && !customAnimalPrefabs[cand].name.Contains("Butterfly"))
                        { idx = cand; break; }
                    }
                }

                GameObject prefab = customAnimalPrefabs[idx];
                if (prefab != null)
                {
#if UNITY_EDITOR
                    animal = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot);
#else
                    animal = Instantiate(prefab, pondRoot);
#endif
                    bool isBfly = prefab.name.Contains("Butterfly") || prefab.name.Contains("fly");
                    if (isBfly)
                    {
                        animal.transform.localScale = Vector3.one * Random.Range(0.006f, 0.018f);
                        ap.y += Random.Range(0.5f, 2.5f);
                        animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    }
                    else
                    {
                        animal.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                        ap.y += 0.1f;
                        Vector3 look = pondCenter - ap; look.y = 0f;
                        if (look != Vector3.zero) animal.transform.rotation = Quaternion.LookRotation(look);
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
                ap.y += 0.5f;
            }
            animal.transform.position = ap;
        }

        // 3. KAYALAR – KUME halinde (2-3 kume, her kumede 3-6 tas)
        if (hasRocks)
        {
            int clusterCount = Random.Range(2, 4);
            for (int c = 0; c < clusterCount; c++)
            {
                float ca = Random.Range(0f, Mathf.PI * 2f);
                float cd = pondRadius * Random.Range(0.85f, 1.5f);
                Vector3 clusterCenter = pondCenter + new Vector3(Mathf.Cos(ca) * cd, 0f, Mathf.Sin(ca) * cd);

                int rocksInCluster = Random.Range(3, 7);
                for (int r = 0; r < rocksInCluster; r++)
                {
                    // Kume icinde kucuk dagitim (3-6m yaricap)
                    float ra = Random.Range(0f, Mathf.PI * 2f);
                    float rd = Random.Range(0.5f, 4.5f);
                    Vector3 rp = clusterCenter + new Vector3(Mathf.Cos(ra) * rd, 0f, Mathf.Sin(ra) * rd);
                    rp.y = GetTerrainY(rp) + 0.02f;

                    GameObject prefab = customRockPrefabs[Random.Range(0, customRockPrefabs.Length)];
                    if (prefab == null) continue;
                    GameObject rock;
#if UNITY_EDITOR
                    rock = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot);
#else
                    rock = Instantiate(prefab, pondRoot);
#endif
                    rock.transform.position = rp;
                    rock.transform.rotation = Quaternion.Euler(
                        Random.Range(-5f, 5f),
                        Random.Range(0f, 360f),
                        Random.Range(-10f, 10f));
                    // Kume icinde cesitli boyutlar: buyukten kucuge
                    float sizeBase = (r == 0) ? Random.Range(1.0f, 1.8f) : Random.Range(0.3f, 1.0f);
                    rock.transform.localScale = prefab.transform.localScale * sizeBase;
                }
            }
        }

        // 4. CALILAR – KUME halinde (2-3 kume, her kumede 3-5 cali)
        if (hasBushes)
        {
            int bushClusterCount = Random.Range(2, 4);
            for (int c = 0; c < bushClusterCount; c++)
            {
                float ba = Random.Range(0f, Mathf.PI * 2f);
                float bd = pondRadius * Random.Range(1.1f, 2.0f);
                Vector3 bushClusterCenter = pondCenter + new Vector3(Mathf.Cos(ba) * bd, 0f, Mathf.Sin(ba) * bd);

                int bushesInCluster = Random.Range(3, 6);
                for (int b = 0; b < bushesInCluster; b++)
                {
                    float bra = Random.Range(0f, Mathf.PI * 2f);
                    float brd = Random.Range(0.3f, 3.5f);
                    Vector3 bp2 = bushClusterCenter + new Vector3(Mathf.Cos(bra) * brd, 0f, Mathf.Sin(bra) * brd);
                    bp2.y = GetTerrainY(bp2) + 0.02f;

                    GameObject prefab = customBushPrefabs[Random.Range(0, customBushPrefabs.Length)];
                    if (prefab == null) continue;
                    GameObject bush;
#if UNITY_EDITOR
                    bush = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, pondRoot);
#else
                    bush = Instantiate(prefab, pondRoot);
#endif
                    bush.transform.position = bp2;
                    bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    bush.transform.localScale = prefab.transform.localScale * Random.Range(0.7f, 1.5f);
                }
            }
        }
    }

    // ================================================================
    //  ROLLER COASTER ANIMALS  (ithappy - pist etrafinda)
    // ================================================================
    private void CreateRollerCoasterAnimals()
    {
        Transform existing = transform.Find("RollerCoasterAnimals");
        if (existing != null) DestroyImmediate(existing.gameObject);
        if (customCoasterAnimalPrefabs == null || customCoasterAnimalPrefabs.Length == 0) return;

        GameObject root = new GameObject("RollerCoasterAnimals");
        root.transform.SetParent(transform, false);

        List<Vector3> trackPts = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(55);

        int totalAnimals = 30, placed = 0, attempts = 0;
        while (placed < totalAnimals && attempts < totalAnimals * 20)
        {
            attempts++;
            Vector3 trackPt = trackPts.Count > 0 ? trackPts[Random.Range(0, trackPts.Count)]
                                                 : new Vector3(Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f));
            float sideAngle = Random.Range(0f, Mathf.PI * 2f);
            float sideDist = Random.Range(12f, 30f);
            Vector3 pos = trackPt + new Vector3(Mathf.Cos(sideAngle) * sideDist, 0f, Mathf.Sin(sideAngle) * sideDist);
            pos.y = GetTerrainY(pos);
            if (IsNearTrack(pos, trackPts, trackExclusionRadius)) continue;
            if (new Vector2(pos.x, pos.z).magnitude < stationExclusionRadius) continue;

            GameObject prefab = customCoasterAnimalPrefabs[Random.Range(0, customCoasterAnimalPrefabs.Length)];
            if (prefab == null) continue;
            GameObject animal;
#if UNITY_EDITOR
            animal = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
            animal = Instantiate(prefab, root.transform);
#endif
            animal.transform.position = pos + Vector3.up * 0.1f;
            animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            animal.transform.localScale = prefab.transform.localScale * Random.Range(0.8f, 1.3f);
            placed++;
        }
        Random.state = saved;
    }

    // ================================================================
    //  CLOUDS
    // ================================================================
    private void CreateRealisticClouds()
    {
        Transform existing = transform.Find("PrototypeClouds");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject cloudsRoot = new GameObject("PrototypeClouds");
        cloudsRoot.transform.SetParent(transform, false);
        CloudDrifter drifter = cloudsRoot.AddComponent<CloudDrifter>();
        drifter.driftSpeed = cloudDriftSpeed;

        Random.State saved = Random.state;
        Random.InitState(77);
        bool hasCC = customCloudPrefabs != null && customCloudPrefabs.Length > 0;

        for (int i = 0; i < cloudCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(50f, cloudSpawnRadius);
            float altitude = Random.Range(cloudMinAltitude, cloudMaxAltitude);
            Vector3 center = new Vector3(Mathf.Cos(angle) * dist, altitude, Mathf.Sin(angle) * dist);
            GameObject cg = null;
            if (hasCC)
            {
                GameObject prefab = customCloudPrefabs[Random.Range(0, customCloudPrefabs.Length)];
                if (prefab != null)
                {
#if UNITY_EDITOR
                    cg = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, cloudsRoot.transform);
#else
                    cg = Instantiate(prefab, cloudsRoot.transform);
#endif
                    cg.transform.position = center;
                    cg.transform.localScale *= Random.Range(0.8f, 1.5f);
                }
            }
            if (cg == null)
            {
                cg = new GameObject(string.Format("Cloud_{0:00}", i));
                cg.transform.SetParent(cloudsRoot.transform, false);
                cg.transform.localPosition = center;
                for (int p = 0; p < Random.Range(4, 8); p++)
                {
                    float puffSize = Random.Range(8f, 22f);
                    Vector3 offset = new Vector3(Random.Range(-12f, 12f), Random.Range(-2f, 4f), Random.Range(-10f, 10f));
                    GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = string.Format("Puff_{0}", p);
                    puff.transform.SetParent(cg.transform, false);
                    puff.transform.localPosition = offset;
                    puff.transform.localScale = new Vector3(puffSize, puffSize * 0.5f, puffSize * 0.8f);
                    Object.DestroyImmediate(puff.GetComponent<Collider>());
                    if (cloudMaterial != null) puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
                    else SetColor(puff, new Color(0.95f, 0.96f, 0.98f, 0.75f), true);
                }
            }
        }
        Random.state = saved;
    }

    private void ApplySkyboxOnly()
    {
        if (skyboxMaterial != null) RenderSettings.skybox = skyboxMaterial;
    }

    // ================================================================
    //  PRIMITIVE AGAC YEDEKLERI
    // ================================================================
    private GameObject CreatePineTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject(string.Format("Pine_{0:000}", index));
        root.transform.localPosition = pos;
        float trunkH = height * 0.65f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.3f, trunkH * 0.5f, 0.3f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.35f, 0.22f, 0.12f));
        for (int layer = 0; layer < 3; layer++)
        {
            float ly = trunkH * 0.5f + layer * (height * 0.18f);
            float lr = (height * 0.25f) * (1f - layer * 0.25f);
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.transform.SetParent(root.transform, false);
            cone.transform.localPosition = new Vector3(0f, ly, 0f);
            cone.transform.localScale = new Vector3(lr * 2f, lr * 1.6f, lr * 2f);
            SetColor(cone, new Color(0.12f + layer * 0.04f, 0.38f + layer * 0.06f, 0.14f));
        }
        return root;
    }

    private GameObject CreateOakTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject(string.Format("Oak_{0:000}", index));
        root.transform.localPosition = pos;
        float trunkH = height * 0.45f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.5f, trunkH * 0.5f, 0.5f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.32f, 0.20f, 0.10f));
        float cr = height * 0.35f;
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, trunkH + cr * 0.4f, 0f);
        crown.transform.localScale = new Vector3(cr * 2.2f, cr * 1.6f, cr * 2.2f);
        SetColor(crown, new Color(0.20f, 0.45f, 0.18f));
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            GameObject sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sub.transform.SetParent(root.transform, false);
            sub.transform.localPosition = new Vector3(Mathf.Cos(a) * cr * 0.5f, trunkH + cr * 0.15f, Mathf.Sin(a) * cr * 0.5f);
            sub.transform.localScale = Vector3.one * cr * 1.1f;
            SetColor(sub, new Color(0.18f, 0.42f + i * 0.02f, 0.16f));
        }
        return root;
    }

    private GameObject CreateBirchTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject(string.Format("Birch_{0:000}", index));
        root.transform.localPosition = pos;
        float trunkH = height * 0.7f;
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.2f, trunkH * 0.5f, 0.2f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.82f, 0.78f, 0.72f));
        for (int layer = 0; layer < 2; layer++)
        {
            float ly = trunkH * 0.6f + layer * (height * 0.22f);
            float r = height * (0.22f - layer * 0.06f);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, ly, 0f);
            crown.transform.localScale = new Vector3(r * 2f, r * 1.4f, r * 2f);
            SetColor(crown, new Color(0.30f, 0.55f, 0.22f));
        }
        return root;
    }

    // ================================================================
    //  HELPER METHODS
    // ================================================================
    // ================================================================
    //  FIX PINK MATERIALS
    //  Instantiate edilen objede URP olmayan (pembe) tum materyalleri
    //  URP/Lit shader ile degistir; rengi muhafaza eder.
    // ================================================================
    private static void FixPinkMaterials(GameObject obj)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            Material[] mats = r.materials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string sn = (mats[i].shader != null) ? mats[i].shader.name : "";
                // Zaten URP ise dokunma
                if (sn.StartsWith("Universal Render Pipeline")) continue;

                // Renk ve texture bilgisini shader degismeden once al
                Color col = Color.white;
                if (mats[i].HasProperty("_Color"))     col = mats[i].GetColor("_Color");
                if (mats[i].HasProperty("_BaseColor")) col = mats[i].GetColor("_BaseColor");

                Texture tex = null;
                if (mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");
                if (tex == null && mats[i].HasProperty("_BaseMap")) tex = mats[i].GetTexture("_BaseMap");

                Material fm = new Material(urpLit);
                if (fm.HasProperty("_BaseColor")) fm.SetColor("_BaseColor", col);
                if (tex != null && fm.HasProperty("_BaseMap")) fm.SetTexture("_BaseMap", tex);
                mats[i] = fm;
                changed = true;
            }
            if (changed) r.materials = mats;
        }
    }

    private float GetTerrainY(Vector3 worldPos)
    {
        if (Terrain.activeTerrain != null)
            return Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y;
        return -2f;
    }

    private bool IsNearTrack(Vector3 pos, List<Vector3> trackPoints, float minDist)
    {
        float minSqr = minDist * minDist;
        for (int i = 0; i < trackPoints.Count; i++)
        {
            float dx = pos.x - trackPoints[i].x;
            float dz = pos.z - trackPoints[i].z;
            if (dx * dx + dz * dz < minSqr) return true;
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
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;
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
        rend.sharedMaterial = mat;
    }
}
