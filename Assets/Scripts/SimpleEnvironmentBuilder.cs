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
    [SerializeField, Min(64f)] private float terrainSize = 8000f;
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
    [SerializeField] private GameObject[] customNPCPrefabs;

    [Header("Trees")]
    [SerializeField, Min(0)] private int treeCount = 10000;
    [SerializeField] private float treeSpawnRadius = 2500f;
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
        CreateGardenProps();
        SpawnLivingBirdsController();
        CreateAmbientAircraft(); // Yeni uçaklar eklendi
        SpawnFighterFlybys();    // Savaş uçakları ve yakın geçişler

        // Bulutlar tamamen devre dışı — PrototypeClouds oluşturulmayacak
        {
            Transform ec = transform.Find("PrototypeClouds");
            if (ec != null) DestroyImmediate(ec.gameObject);
        }
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
        // --- AGACLAR: Sadece dogal / gercekci renkli assetler ---
        var trees = new List<GameObject>();

        // Nature Starter Kit 2 ağaçları
        string[] nskTreePaths = new string[]
        {
            "Assets/NatureStarterKit2/Nature/tree01.prefab",
            "Assets/NatureStarterKit2/Nature/tree02.prefab",
            "Assets/NatureStarterKit2/Nature/tree03.prefab",
            "Assets/NatureStarterKit2/Nature/tree04.prefab"
        };
        foreach (var p in nskTreePaths) { var t = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (t) trees.Add(t); }

        if (trees.Count > 0) customTreePrefabs = trees.ToArray();

        // --- GOLET HAYVANLARI ---
        var butterfly = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Butterfly (Animated)/Prefab/Butterfly.prefab");
        var goat = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Goat_dark.prefab");
        var sheep = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UrsaAnimation/LOW POLY CUBIC - Goat and Sheep Pack/Prefabs_URP/SK_Sheep_white.prefab");
        var deer = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Backrock Studios/LowPoly-Animals/Prefabs/Deer/Deer_v1.prefab");
        var pa = new List<GameObject>();
        if (butterfly) pa.Add(butterfly); if (goat) pa.Add(goat); if (sheep) pa.Add(sheep);
        if (deer) pa.Add(deer);
        if (pa.Count > 0) customAnimalPrefabs = pa.ToArray();

        // --- ROLLER COASTER HAYVANLARI (ithappy Animals_FREE) ---
        string[] ithappyPaths = new string[]
        {
            "Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Deer_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Pinguin_001.prefab",
            "Assets/ithappy/Animals_FREE/Prefabs/Tiger_001.prefab"
        };
        var ca = new List<GameObject>();
        foreach (var p in ithappyPaths) { var a = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (a) ca.Add(a); }
        if (ca.Count > 0) customCoasterAnimalPrefabs = ca.ToArray();

        // --- NPC CHARACTERS (npc_casual_set_00) ---
        string[] npcPaths = new string[] {
            "Assets/npc_casual_set_00/Prefabs/npc_hmn_01m.prefab",
            "Assets/npc_casual_set_00/Prefabs/npc_hmn_01f.prefab"
        };
        var nl = new List<GameObject>();
        foreach (var p in npcPaths) { var n = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (n) nl.Add(n); }
        if (nl.Count > 0) customNPCPrefabs = nl.ToArray(); // Artık ayrı diziye atanıyor, hayvanları EZMİYOR

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

        // Su materyali: URP uyumlu su shader'ı bul
        if (waterMaterial == null)
        {
            waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Procedural Water Shader/Materials/Pool Water.mat");
            if (waterMaterial == null)
                waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Procedural Water Shader/Materials/Ocean Water.mat");
        }
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
        RenderSettings.fog = false;
        DynamicGI.UpdateEnvironment();

        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            c.farClipPlane = 60000f;
            // Sarsıntı efektini kameraya ekle
            if (c.gameObject.GetComponent<CoasterShakeEffect>() == null)
                c.gameObject.AddComponent<CoasterShakeEffect>();
        }

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
            "Assets/Low Poly Tree Mega Pack by MysticForge",
            "Assets/NatureStarterKit2", "Assets/MamkinEnthusiast"
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

                    if (mat.name.ToLower().Contains("leaf") || mat.name.ToLower().Contains("branch") ||
                        mat.name.ToLower().Contains("frond") || (mtx != null && !mat.name.ToLower().Contains("bark")))
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.SetFloat("_Cutoff", 0.35f);
                        mat.EnableKeyword("_ALPHATEST_ON");
                    }
                    UnityEditor.EditorUtility.SetDirty(mat);
                }
            }
        }

        // Ensure lb_groundTarget tag exists for birds
        UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool foundTag = false;
        for (int tj = 0; tj < tagsProp.arraySize; tj++)
        {
            if (tagsProp.GetArrayElementAtIndex(tj).stringValue == "lb_groundTarget") { foundTag = true; break; }
        }
        if (!foundTag)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "lb_groundTarget";
            tagManager.ApplyModifiedProperties();
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

        Texture2D gt = null;
        Texture2D nrm = null;
#if UNITY_EDITOR
        gt = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantasy Skybox FREE/Scenes/Textures (Terrain)/Texture_Grass_Diffuse.png");
        nrm = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantasy Skybox FREE/Scenes/Textures (Terrain)/Texture_Grass_Normal.png");

        if (gt == null) gt = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ADG_Textures/ground_vol1/ground3/ground3_Diffuse.tga");
        if (nrm == null) nrm = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ADG_Textures/ground_vol1/ground3/ground3_Normal.tga");
#endif
        if (gt == null) gt = CreateSolidColorTexture(64, new Color(0.15f, 0.35f, 0.12f, 1f));

        TerrainLayer gl = new TerrainLayer
        {
            diffuseTexture = gt,
            normalMapTexture = nrm,
            tileSize = new Vector2(15f, 15f),
            smoothness = 0f,
            metallic = 0f
        };
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

        float safeRadius = treeSpawnRadius + 100f;
        float targetPeakHeight = 200f;

        int ringCount = 18;
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (i * Mathf.PI * 2f / ringCount);

            GameObject prefab = customMountainPrefabs[Random.Range(0, customMountainPrefabs.Length)];
            if (prefab == null) continue;

            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds mb = mf.sharedMesh.bounds;
            float meshHeight = Mathf.Max(mb.size.y, 0.001f);
            float meshHalfWidth = Mathf.Max(mb.extents.x, mb.extents.z);

            targetPeakHeight = 500f;
            float scale = Mathf.Clamp(targetPeakHeight / meshHeight, 0.1f, 800f);

            float halfWidthWorld = meshHalfWidth * scale;
            float dist = safeRadius + halfWidthWorld + 150f;

            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            // Daha derine gömerek yerde yüzüyormuş veya boşluk varmış hissini yok et.
            pos.y = GetTerrainY(pos) - meshHeight * scale * 0.45f;

            GameObject mountain;
#if UNITY_EDITOR
            mountain = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
#else
            mountain = Instantiate(prefab, root.transform);
#endif
            mountain.transform.position = pos;
            mountain.transform.localScale = Vector3.one * scale;
            mountain.transform.rotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
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

        GameObject treesRoot = new GameObject("PrototypeTrees");
        treesRoot.transform.SetParent(transform, false);

        bool hasCustom = customTreePrefabs != null && customTreePrefabs.Length > 0;
        if (!hasCustom) return;

        List<Vector3> trackPoints = GetTrackExclusionPoints();
        Random.State saved = Random.state;
        Random.InitState(99);

        // Ormanlık Bölgeler (Forest Clusters) belirle
        int clusterCount = 12;
        Vector3[] clusters = new Vector3[clusterCount];
        for (int i = 0; i < clusterCount; i++)
        {
            float ca = Random.Range(0f, Mathf.PI * 2f);
            float cd = Random.Range(100f, treeSpawnRadius * 0.7f);
            clusters[i] = new Vector3(Mathf.Cos(ca) * cd, 0f, Mathf.Sin(ca) * cd);
        }

        // Ağaç boyutu gerçekçiliği için katsayı
        float globalTreeScale = 2.5f;

        // Hem rastgele hem de ormanlık bölgede ağaçlar
        int spawnCount = hasCustom ? 2500 : 0; // Gameobject olduğu için sayıyı sınırlı tuttuk, ancak boyutlarını büyük tutarak alanı dolduracağız.

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 bp;
            // %60 Ormanda, %40 Dağınık
            if (Random.value < 0.6f && clusterCount > 0)
            {
                Vector3 cc = clusters[Random.Range(0, clusterCount)];
                float ra = Random.Range(0f, Mathf.PI * 2f);
                float rd = Random.Range(0f, 150f); // Orman yarıçapı
                bp = cc + new Vector3(Mathf.Cos(ra) * rd, 0f, Mathf.Sin(ra) * rd);
            }
            else
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(55f, treeSpawnRadius);
                bp = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            }

            if (new Vector2(bp.x, bp.z).magnitude < stationExclusionRadius) continue;
            if (IsNearTrack(bp, trackPoints, trackExclusionRadius)) continue;

            bp.y = GetTerrainY(bp);

            GameObject prefab = customTreePrefabs[Random.Range(0, customTreePrefabs.Length)];
            if (prefab == null) continue;

            GameObject tree;
#if UNITY_EDITOR
            tree = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, treesRoot.transform);
#else
            tree = Instantiate(prefab, treesRoot.transform);
#endif
            tree.transform.position = bp;
            float indScale = Random.Range(0.8f, 1.5f) * globalTreeScale;
            tree.transform.localScale = prefab.transform.localScale * indScale;
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        Random.state = saved;
    }

    // ================================================================
    //  PONDS AND FAUNA
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
            ScatterPondFlowers(pondsRoot.transform, center, radius);
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
    public float megaLakeRadius = 310f;
    private Vector3 GetMegaLakeCenter()
    {
        List<Vector3> pts = GetTrackExclusionPoints();
        if (pts == null || pts.Count == 0) return new Vector3(80f, 0f, 150f);
        Vector3 sum = Vector3.zero;
        foreach (var p in pts) sum += p;
        return sum / pts.Count;
    }

    // ================================================================
    //  CENTRAL FEATURE POND  –  Pistin icini tamamen kaplayan tam devasa göl
    // ================================================================
    private void CreateCentralFeaturePond()
    {
        Transform existing = transform.Find("CentralFeaturePond");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("CentralFeaturePond");
        root.transform.SetParent(transform, false);

        Vector3 center = GetMegaLakeCenter();
        center.y = GetTerrainY(center) + pondYOffset;
        float radius = megaLakeRadius;

        root.transform.position = center;

        // Su yuzeyi (Devasa)
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
        rim.transform.localScale = new Vector3(radius * 2.05f, 0.03f, radius * 2.05f);
        SetColor(rim, new Color(0.12f, 0.36f, 0.11f));
        Object.DestroyImmediate(rim.GetComponent<Collider>());

        // ── Alstra Infinite FishV1-4 Büyük Balıklar ──────────────────────
#if UNITY_EDITOR
        string[] polyFishPaths = {
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/FishV1.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/FishV2.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/FishV3.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/FishV4.prefab"
        };
        var polyFishPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (var pp in polyFishPaths)
        {
            var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pp);
            if (pf) polyFishPrefabs.Add(pf);
        }
        // Yedek olarak eski balık paketini de kullan
        bool hasPolyFish = polyFishPrefabs.Count > 0;
        bool hasFallbackFish = customFishPrefabs != null && customFishPrefabs.Length > 0;
        GameObject fishSplashPrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_Multiple.prefab") ??
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_A.prefab") ??
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Hit_01.prefab");
#endif
        for (int i = 0; i < 12; i++)
        {
            float fa = Random.Range(0f, Mathf.PI * 2f);
            float fd = radius * Random.Range(0.2f, 0.75f);
            Vector3 fp = center + new Vector3(Mathf.Cos(fa) * fd, 0f, Mathf.Sin(fa) * fd);
            fp.y = GetTerrainY(fp) + 0.05f;

            GameObject fish = null;
#if UNITY_EDITOR
            if (hasPolyFish)
            {
                var prefab = polyFishPrefabs[Random.Range(0, polyFishPrefabs.Count)];
                fish = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
                // Büyük boyut: 3-5x
                fish.transform.localScale = prefab.transform.localScale * Random.Range(3f, 5f);
                fish.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                FixPinkMaterials(fish);
            }
            else if (hasFallbackFish)
            {
                var prefab = customFishPrefabs[Random.Range(0, customFishPrefabs.Length)];
                fish = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform);
                fish.transform.localScale = prefab.transform.localScale * Random.Range(2.5f, 4f);
                fish.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
#else
            if (hasFallbackFish)
            {
                var prefab = customFishPrefabs[Random.Range(0, customFishPrefabs.Length)];
                fish = Instantiate(prefab, root.transform);
                fish.transform.localScale = prefab.transform.localScale * Random.Range(2.5f, 4f);
            }
#endif
            if (fish == null)
            {
                fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fish.name = "FishPrimitive";
                fish.transform.SetParent(root.transform, true);
                fish.transform.localScale = new Vector3(1.2f, 0.7f, 2.5f);
                SetColor(fish, new Color(0.2f, 0.6f, 0.9f));
                Object.DestroyImmediate(fish.GetComponent<Collider>());
            }
            fish.transform.position = fp;

            var jumper = fish.AddComponent<JumpingFish>();
            jumper.jumpHeight = Random.Range(6.5f, 11f); 
            jumper.jumpDistance = Random.Range(10f, 15f);
            jumper.jumpIntervalMin = Random.Range(5f, 8f);
            jumper.jumpIntervalMax = Random.Range(9f, 15f);
            jumper.cinematicHeightMultiplier = 3.5f;
            jumper.cinematicDistanceMultiplier = 2.5f;
            jumper.coasterTriggerDistance = 85f;
            jumper.cameraPassDistance = 5.0f; // Kameranın tam önünden geçmesi için (Profesyonel ayar)
            jumper.burstJumpCount = 5;
            jumper.burstJumpDelay = 0.45f;
            jumper.burstCooldown = 12f;
#if UNITY_EDITOR
            jumper.splashFXPrefab = fishSplashPrefab;
#endif
        }

        Random.State saved = Random.state;
        Random.InitState(77);

#if UNITY_EDITOR
        // ── Tekne Splash + Wake prefabları ─────────────────────────────
        GameObject boatSplashPrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_B.prefab") ??
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_A.prefab");
        GameObject boatWakePrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubbles_Vertical_Loop.prefab");

        // ── Alstra Infinite Tekneler ──────────────────────────────────
        var boatPrefabList = new System.Collections.Generic.List<GameObject>();
        string[] boatPaths = {
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/Speed_Boat.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/Scout_Boat.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/Fisher_Boat.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/1.2 Version/Speed_Boat.prefab",
            "Assets/Alstra Infinite/Boats LowPoly/Prefabs/1.2 Version/Scout_Boat.prefab"
        }; foreach (var bp in boatPaths) { var b = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bp); if (b) boatPrefabList.Add(b); }

        // NPC'leri Nolant assetlerine zorla (Eski assetleri tamamen devredışı bırakır)
        var npcPrefabList = new List<GameObject>();
        string[] nps = {
            "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Blue(Free Version).prefab",
            "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Brown(Free Version).prefab",
            "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Green(Free Version).prefab",
            "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Yellow(Free Version).prefab"
        };
        foreach (var p in nps) { var pref = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p); if (pref) npcPrefabList.Add(pref); }

        Debug.Log($"Generating Lake Environment: {boatPrefabList.Count} boats, {npcPrefabList.Count} NPC variants available.");

        if (boatPrefabList.Count > 0)
        {
            int boatCount = 15;
            for (int i = 0; i < boatCount; i++)
            {
                float ba = Random.Range(0f, Mathf.PI * 2f);
                float bd = Random.Range(20f, radius * 0.85f);
                Vector3 boatPos = center + new Vector3(Mathf.Cos(ba) * bd, 0f, Mathf.Sin(ba) * bd);
                boatPos.y = GetTerrainY(boatPos) + 0.15f;

                GameObject boatPrefab = boatPrefabList[Random.Range(0, boatPrefabList.Count)];
                GameObject boatObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(boatPrefab, root.transform);
                boatObj.transform.position = boatPos;
                boatObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                float boatScale = Random.Range(2.5f, 3.5f);
                boatObj.transform.localScale = boatPrefab.transform.localScale * boatScale;
                FixPinkMaterials(boatObj);

                // --- RollerCoasterBoatAI Bağla ---
                var boatAI = boatObj.AddComponent<RollerCoasterBoatAI>();
                boatAI.lakeCenter = center;
                boatAI.patrolRadius = radius * 0.82f;
                boatAI.normalSpeed = Random.Range(3f, 6f);
                boatAI.burstSpeed = Random.Range(14f, 22f);
                boatAI.coasterReactDistance = 90f;
                boatAI.splashPrefab = boatSplashPrefab;
                boatAI.wakePrefab = boatWakePrefab;
                boatAI.waterY = center.y + 0.15f;

                // --- TEKNE İÇİ NPC'LER (Nolant Entegrasyonu) ---
                if (npcPrefabList.Count > 0)
                {
                    float boatDeckY = 0.05f;
                    Renderer boatRend = boatObj.GetComponentInChildren<Renderer>();
                    if (boatRend != null) boatDeckY = boatRend.localBounds.center.y + boatRend.localBounds.extents.y * 0.2f;

                    int npcCount = Random.Range(1, 3);
                    for (int n = 0; n < npcCount; n++)
                    {
                        GameObject npcPrefab = npcPrefabList[Random.Range(0, npcPrefabList.Count)];
                        GameObject npcObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(npcPrefab, boatObj.transform);

                        // İnsan boyutunu yaklaşık 2.5 katına çıkardık (Daha gerçekçi yetişkin boyutu)
                        float humanScale = (1.0f / boatScale) * 2.65f;
                        npcObj.transform.localScale = Vector3.one * humanScale;

                        // Birbirlerine veya tekneye değmemeleri için aralıkları genişlettik (0.15 -> 0.55)
                        float npcOffsetX = (n - (npcCount - 1) * 0.5f) * 0.55f;
                        float npcOffsetZ = (npcCount > 1) ? (n * 0.3f - 0.15f) : 0f;

                        Vector3 rayStart = new Vector3(npcOffsetX, 1.5f, npcOffsetZ);
                        Vector3 localPos = rayStart;
                        if (Physics.Raycast(boatObj.transform.TransformPoint(rayStart), -boatObj.transform.up, out RaycastHit hit, 2.5f))
                        {
                            localPos = boatObj.transform.InverseTransformPoint(hit.point);
                            localPos.y += 0.05f; // Hafifçe yukarı (Clipping önleme)
                        }
                        else
                        {
                            localPos = new Vector3(npcOffsetX, boatDeckY, npcOffsetZ);
                        }

                        npcObj.transform.localPosition = localPos;
                        npcObj.transform.localRotation = Quaternion.Euler(0, Random.Range(-30f, 30f), 0);

                        Animator npcAnim = npcObj.GetComponentInChildren<Animator>();
                        if (npcAnim != null)
                        {
                            npcAnim.applyRootMotion = false;
                            npcAnim.enabled = true;
                        }
                        FixPinkMaterials(npcObj);
                    }
                }
            }
        }
#endif

        // Remove old stones, trees and animals around the mega lake. They will spawn on the outside instead utilizing IsNearTrack automatically.

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
                        animal.transform.localScale = Vector3.one * Random.Range(0.015f, 0.035f);
                        ap.y += Random.Range(0.5f, 2.5f);
                        animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    }
                    else
                    {
                        animal.transform.localScale = prefab.transform.localScale * Random.Range(1.8f, 2.8f);
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
            bool isB = animal.name.Contains("Butterfly") || animal.name.Contains("fly");
            if (!isB) animal.AddComponent<AnimalWander>();
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

            Vector3 dirToLake = (pos - GetMegaLakeCenter()).normalized;
            pos += dirToLake * 10f;
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
            animal.transform.localScale = prefab.transform.localScale * Random.Range(1.8f, 2.8f); // Boyutlar çok daha büyük!
            animal.AddComponent<AnimalWander>();
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
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string sn = (mats[i].shader != null) ? mats[i].shader.name : "";
                // Zaten URP ise dokunma
                if (sn.StartsWith("Universal Render Pipeline")) continue;

                // Renk ve texture bilgisini shader degismeden once al
                Color col = Color.white;
                if (mats[i].HasProperty("_Color")) col = mats[i].GetColor("_Color");
                if (mats[i].HasProperty("_BaseColor")) col = mats[i].GetColor("_BaseColor");

                Texture tex = null;
                if (mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");
                if (tex == null && mats[i].HasProperty("_BaseMap")) tex = mats[i].GetTexture("_BaseMap");
                if (tex == null && mats[i].HasProperty("_BaseColorMap")) tex = mats[i].GetTexture("_BaseColorMap");

                // In-place shader değiştir
                mats[i].shader = urpLit;

                // URP Lit property'lerini zorla ayarla (Keywordler dahil)
                if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", col);
                if (tex != null)
                {
                    if (mats[i].HasProperty("_BaseMap")) mats[i].SetTexture("_BaseMap", tex);
                    mats[i].EnableKeyword("_BASEMAP"); // Keyword zorlaması (bazı versiyonlar için)
                }

                // Tint zorlaması
                if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", col);

                // Opaklık/Transparanlık kontrolü
                if (sn.Contains("Transparent") || sn.Contains("Cutout"))
                {
                    mats[i].SetFloat("_Surface", 1); // 1 = Transparent
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mats[i].SetInt("_ZWrite", 0);
                    mats[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mats[i].renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }

                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
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
        Vector3 lakeCenter = GetMegaLakeCenter();
        if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(lakeCenter.x, lakeCenter.z)) < megaLakeRadius)
            return true; // Devasa göletin alanına denk geliyorsa "piste çok yakınmış gibi" engelleyerek hiçbir prop'un doğmamasını sağla.

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

    private void SpawnLivingBirdsController()
    {
        Transform existing = transform.Find("PrototypeBirds");
        if (existing != null) DestroyImmediate(existing.gameObject);

        // Remove any old global controllers left over
        GameObject oldCtl = GameObject.Find("_livingBirdsController");
        if (oldCtl != null) DestroyImmediate(oldCtl);

        GameObject birdsRoot = new GameObject("PrototypeBirds");
        birdsRoot.transform.SetParent(transform, false);

        GameObject lbController = new GameObject("_livingBirdsController");
        lbController.transform.SetParent(birdsRoot.transform, false);

        lb_BirdController bc = lbController.AddComponent<lb_BirdController>();
        bc.idealNumberOfBirds = 35;
        bc.maximumNumberOfBirds = 50;
        bc.birdScale = 2f; // Kuslarin VR'da rahat gorunmesi icin gozle karar bir buyutme
        bc.unspawnDistance = 5000f; // Onemli: mesafe kisa olursa kuslar ciktigi gibi silinir
        bc.collideWithObjects = true;
        bc.highQuality = true;

        // Script will automatically handle Instantiation at Start() during Play Mode.
    }

    private void ScatterPondFlowers(Transform parent, Vector3 center, float radius)
    {
#if UNITY_EDITOR
        string flowerPath = "Assets/Patchmesh/Free Sample Stylized Hand-Painted Plant & Flower Pack/Prefabs";
        if (!UnityEditor.AssetDatabase.IsValidFolder(flowerPath)) return;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { flowerPath });
        if (guids.Length == 0) return;

        List<GameObject> prefabs = new List<GameObject>();
        foreach (string guid in guids)
        {
            GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (p != null) prefabs.Add(p);
        }
        if (prefabs.Count == 0) return;

        int flowerCount = Random.Range(15, 30);
        for (int i = 0; i < flowerCount; i++)
        {
            float fa = Random.Range(0f, Mathf.PI * 2f);
            float fd = radius + Random.Range(2f, 15f);
            Vector3 fpos = center + new Vector3(Mathf.Cos(fa) * fd, 0, Mathf.Sin(fa) * fd);
            fpos.y = GetTerrainY(fpos);
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            f.transform.position = fpos;
            f.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            f.transform.localScale = Vector3.one * Random.Range(1.8f, 3.5f);
        }
#endif
    }

    private void CreateGardenProps()
    {
#if UNITY_EDITOR
        string propPath = "Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs";
        if (!UnityEditor.AssetDatabase.IsValidFolder(propPath)) return;

        GameObject GetPrefab(string name)
        {
            string tp = propPath + "/" + name + ".prefab";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(tp);
        }

        GameObject fencePrefab = GetPrefab("Fence");
        if (fencePrefab == null) return;

        Transform existing = transform.Find("GardenProps");
        if (existing != null) DestroyImmediate(existing.gameObject);
        GameObject root = new GameObject("GardenProps");
        root.transform.SetParent(transform, false);

        // 3 farklı bahçe alanı oluşturalım (Gölet dışında rastgele dağıtarak)
        Vector3[] gardenCenters = new Vector3[6];
        Vector3 lakeCenter = GetMegaLakeCenter();
        for (int idx = 0; idx < gardenCenters.Length; ++idx)
        {
            float gAng = Random.Range(0f, Mathf.PI * 2f);
            float gDist = megaLakeRadius + Random.Range(40f, 180f);
            gardenCenters[idx] = lakeCenter + new Vector3(Mathf.Cos(gAng) * gDist, 0, Mathf.Sin(gAng) * gDist);
        }

        foreach (var centerPos in gardenCenters)
        {
            Vector3 center = centerPos;
            center.y = GetTerrainY(center) + 0.1f;

            GameObject gardenRoot = new GameObject("MiniGarden");
            gardenRoot.transform.SetParent(root.transform, false);
            gardenRoot.transform.position = center;
            gardenRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            float pScale = 2.0f;
            float fenceLen = 1.9f * pScale;

            // Çitler
            for (int i = 0; i < 4; i++)
            {
                var f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fencePrefab, gardenRoot.transform);
                f.transform.localPosition = new Vector3(-1.5f * fenceLen + i * fenceLen, 0, -1.5f * fenceLen);
                f.transform.localScale = Vector3.one * pScale;
            }
            for (int i = 0; i < 3; i++)
            {
                var f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fencePrefab, gardenRoot.transform);
                f.transform.localPosition = new Vector3(-1.5f * fenceLen + i * fenceLen, 0, 1.5f * fenceLen);
                f.transform.localScale = Vector3.one * pScale;
            }
            for (int i = 0; i < 3; i++)
            {
                var f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fencePrefab, gardenRoot.transform);
                f.transform.localPosition = new Vector3(-2f * fenceLen, 0, -0.5f * fenceLen + i * fenceLen);
                f.transform.localRotation = Quaternion.Euler(0, -90f, 0);
                f.transform.localScale = Vector3.one * pScale;
            }
            for (int i = 0; i < 3; i++)
            {
                var f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fencePrefab, gardenRoot.transform);
                f.transform.localPosition = new Vector3(2f * fenceLen, 0, -0.5f * fenceLen + i * fenceLen);
                f.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                f.transform.localScale = Vector3.one * pScale;
            }

            // Hydrangea Çiçekleri (Sol bölüm)
            var hyd = GetPrefab("Hydrangea");
            if (hyd)
            {
                for (int i = 0; i < 3; i++)
                {
                    var h = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(hyd, gardenRoot.transform);
                    h.transform.localPosition = new Vector3(-1f * fenceLen + Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f) * fenceLen);
                    h.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                    h.transform.localScale = Vector3.one * pScale * 1.5f;
                }
            }

            // Toprak zemin ve Laleler (Orta bölüm)
            var earth = GetPrefab("EarthHill");
            var tred = GetPrefab("TulipRed");
            var tyel = GetPrefab("TulipYellow");
            if (earth)
            {
                var e = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(earth, gardenRoot.transform);
                e.transform.localPosition = new Vector3(0.5f * fenceLen, 0, 0.5f * fenceLen);
                e.transform.localScale = new Vector3(pScale * 1.5f, pScale * 0.8f, pScale * 1.5f);
                if (tred && tyel)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var tf = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(Random.value > 0.5f ? tred : tyel, gardenRoot.transform);
                        tf.transform.localPosition = new Vector3(0.5f * fenceLen + Random.Range(-0.6f, 0.6f) * fenceLen, 0, 0.5f * fenceLen + Random.Range(-0.4f, 0.4f) * fenceLen);
                        tf.transform.localScale = Vector3.one * pScale * 1.2f;
                    }
                }
            }

            // Alet Edevat ve Saksılar (Sağ bölüm)
            GameObject[] props = { GetPrefab("WateringCup"), GetPrefab("PotSmall"), GetPrefab("PotBig"), GetPrefab("Shovel"), GetPrefab("PotRectangle") };
            foreach (var pr in props)
            {
                if (pr == null) continue;
                var pObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pr, gardenRoot.transform);
                pObj.transform.localPosition = new Vector3(1.2f * fenceLen + Random.Range(-0.5f, 0.5f), 0, -0.5f * fenceLen + Random.Range(-0.5f, 0.5f));
                pObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                pObj.transform.localScale = Vector3.one * pScale;
            }
        }
#endif
    }

    private void CreateAmbientAircraft()
    {
#if UNITY_EDITOR
        string aircraftRootPath = "Assets/Generic Aircraft Models/Prefabs/Aircrafts";
        string p1Path = aircraftRootPath + "/aircraft-d.prefab"; // Smaller Jet
        string p2Path = aircraftRootPath + "/aircraft-e.prefab"; // Smaller Alt Jet

        GameObject prefab1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p1Path);
        GameObject prefab2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p2Path);

        if (prefab1 == null || prefab2 == null) return;

        Transform root = transform.Find("AmbientAircraft");
        if (root != null) DestroyImmediate(root.gameObject);
        GameObject aircraftRoot = new GameObject("AmbientAircraft");
        aircraftRoot.transform.SetParent(transform, false);

        // --- BEYAZ UÇAK (750m İrtifa, Çok Uzak) ---
        GameObject whitePlane = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab1, aircraftRoot.transform);
        whitePlane.name = "Aircraft_White_Far";
        whitePlane.transform.position = new Vector3(-1800f, 750f, 400f);
        whitePlane.transform.rotation = Quaternion.Euler(0, 90f, 0);
        whitePlane.transform.localScale = Vector3.one * 1.5f;
        SetColor(whitePlane, Color.white);
        var ai1 = whitePlane.AddComponent<AmbientAircraftAI>();
        ai1.speed = 90f;
        ai1.loopDistance = 4500f;
        FixPinkMaterials(whitePlane);

        // --- SİYAH UÇAK ---
        GameObject greenPlane = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab2, aircraftRoot.transform);
        greenPlane.name = "Aircraft_Black_Far";
        greenPlane.transform.position = new Vector3(1800f, 780f, -250f);
        greenPlane.transform.rotation = Quaternion.Euler(0, -90f, 0);
        greenPlane.transform.localScale = Vector3.one * 1.6f;
        SetColor(greenPlane, new Color(0.15f, 0.15f, 0.15f));
        var ai2 = greenPlane.AddComponent<AmbientAircraftAI>();
        ai2.speed = 85f;
        ai2.loopDistance = 4500f;
        FixPinkMaterials(greenPlane);
#endif
    }

    private void SpawnFighterFlybys()
    {
#if UNITY_EDITOR
        var sc = FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
        if (sc == null) return;

        Transform root = transform.Find("FighterFlybys");
        if (root != null) DestroyImmediate(root.gameObject);
        GameObject fighterRoot = new GameObject("FighterFlybys");
        fighterRoot.transform.SetParent(transform, false);

        string fighterPath = "Assets/Generic Aircraft Models/Prefabs/Aircrafts/aircraft-f.prefab";
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fighterPath);
        if (prefab == null) return;

        // --- SIRALI SEKANSLAR: Başlangıç (0.1 T), Orta (0.5 T) ve Bitiş (0.9 T) ---
        float[] sequences = { 0.12f, 0.52f, 0.90f };
        int[] jetCounts = { 3, 1, 2 }; // Start: 3, Mid: 1, End: 2

        for (int s = 0; s < sequences.Length; s++)
        {
            float seqT = sequences[s];
            int count = jetCounts[s];

            for (int i = 0; i < count; i++)
            {
                GameObject f = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, fighterRoot.transform);
                f.name = string.Format("FighterJet_Overtake_{0}_{1}", s, i);
                
                // --- ARKADAN GELİŞ HESABI ---
                float backT = (seqT - 0.08f + 1f) % 1f; 
                Vector3 spawnPos = sc.EvaluatePosition(backT);
                Vector3 spawnTangent = Vector3.Normalize(sc.EvaluateTangent(backT));
                Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.up, spawnTangent));

                // Gecikmeli Başlangıç (Start: 8s, Mid: 22s, End: 42s civarı)
                float baseDelay = (s == 0) ? 8.0f : (s == 1 ? 22.0f : 42.0f);
                float jetDelay = baseDelay + (i * 2.5f); 

                // Kameranın görüş açısında daha net görünmeleri için mesafeyi 48-52 metreye çektik
                float sideOffset = (i == 1) ? 48f : (i == 0 ? -50f : 52f); 
                Vector3 flyPos = spawnPos + Vector3.up * 16f + right * sideOffset;

                f.transform.position = flyPos;
                f.transform.rotation = Quaternion.LookRotation(spawnTangent);
                f.transform.localScale = Vector3.one * 5.8f;

                var ai = f.AddComponent<FighterOvertakeAI>();
                ai.speed = 310f; 
                ai.startDelay = jetDelay;
                ai.shakeIntensity = 0.42f; // Daha tok bir sarsıntı
                
                SetColor(f, new Color(0.2f, 0.22f, 0.25f)); 
                FixPinkMaterials(f);
            }
        }
#endif
    }
}

    public class AnimalWander : MonoBehaviour
    {
        private Vector3 startPos;
        public float wanderRadius = 15f;
        public float moveSpeed = 1.0f;
        public float turnSpeed = 45f;
        public bool isBird = false;

        private float changeDirTimer;
        private float targetYRot;

        void Start()
        {
            startPos = transform.position;
            targetYRot = transform.eulerAngles.y;

            // Hayvanların yerinde saymasına neden olan "Root Motion" kapatılır.
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        void Update()
        {
            changeDirTimer -= Time.deltaTime;

            Vector3 offset = transform.position - startPos;
            offset.y = 0;

            if (offset.magnitude > wanderRadius)
            {
                Vector3 dirToCenter = -offset.normalized;
                targetYRot = Mathf.Atan2(dirToCenter.x, dirToCenter.z) * Mathf.Rad2Deg;
                changeDirTimer = 3f;
            }
            else if (changeDirTimer <= 0)
            {
                targetYRot += Random.Range(-90f, 90f);
                changeDirTimer = Random.Range(2f, 5f);
            }

            float currentY = transform.eulerAngles.y;
            float newY = Mathf.MoveTowardsAngle(currentY, targetYRot, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, newY, 0);

            // Skala büyüdüğü için hızı artırdık ve root motion kapalıyken ilerlemesini garanti ediyoruz
            float moveAmount = moveSpeed * 2.5f * Time.deltaTime;
            transform.Translate(Vector3.forward * moveAmount, Space.Self);

            if (!isBird && Terrain.activeTerrain != null)
            {
                Vector3 p = transform.position;
                p.y = Terrain.activeTerrain.SampleHeight(p) + Terrain.activeTerrain.transform.position.y;
                transform.position = p;
            }
        }
    }



