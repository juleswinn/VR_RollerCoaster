using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleEnvironmentBuilder : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField, Min(64f)] private float terrainSize = 1200f;
    [SerializeField, Min(10f)] private float terrainHeight = 200f;
    [SerializeField] private int heightmapResolution = 513;

    [Header("Trees")]
    [SerializeField, Min(0)] private int treeCount = 350;
    [SerializeField] private float treeSpawnRadius = 500f;
    [SerializeField] private Vector2 treeHeightRange = new Vector2(5f, 10f);
    [SerializeField] private float trackExclusionRadius = 18f;
    [SerializeField] private float stationExclusionRadius = 35f;

    [Header("Ponds")]
    [SerializeField, Min(1)] private int pondCount = 7;
    [SerializeField] private float pondMinRadius = 10f;
    [SerializeField] private float pondMaxRadius = 28f;
    [SerializeField] private float pondSpawnRadius = 350f;
    [SerializeField] private float pondYLevel = -0.25f;
    [SerializeField] private Material waterMaterial;

    [Header("Clouds")]
    [SerializeField, Min(5)] private int cloudCount = 18;
    [SerializeField] private float cloudMinAltitude = 80f;
    [SerializeField] private float cloudMaxAltitude = 140f;
    [SerializeField] private float cloudSpawnRadius = 500f;
    [SerializeField] private float cloudDriftSpeed = 2f;
    [SerializeField] private Material cloudMaterial;

    [Header("Sky")]
    [SerializeField] private Material skyboxMaterial;

    public void SetSkyboxMaterial(Material material)
    {
        skyboxMaterial = material;
    }

    public void SetCloudMaterial(Material material)
    {
        cloudMaterial = material;
    }

    [ContextMenu("Build Environment")]
    public void BuildEnvironment()
    {
        CreateFlatTerrain();
        CreateProfessionalTrees();
        CreatePonds();
        CreateRealisticClouds();
        ApplySky();
    }

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

        while (placed < treeCount && attempts < maxAttempts)
        {
            attempts++;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(25f, treeSpawnRadius);
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

            float treeHeight = Random.Range(treeHeightRange.x, treeHeightRange.y);
            int treeType = Random.Range(0, 3);

            GameObject tree;
            switch (treeType)
            {
                case 0: tree = CreatePineTree(basePos, treeHeight, placed); break;
                case 1: tree = CreateOakTree(basePos, treeHeight, placed); break;
                default: tree = CreateBirchTree(basePos, treeHeight, placed); break;
            }

            tree.transform.SetParent(root.transform, false);
            placed++;
        }

        Random.state = saved;
    }

    private GameObject CreatePineTree(Vector3 pos, float height, int index)
    {
        GameObject root = new GameObject($"Pine_{index:000}");
        root.transform.localPosition = pos;

        float trunkH = height * 0.65f;
        float trunkW = 0.3f;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(trunkW, trunkH * 0.5f, trunkW);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.35f, 0.22f, 0.12f));

        for (int layer = 0; layer < 3; layer++)
        {
            float layerY = trunkH * 0.5f + layer * (height * 0.18f);
            float layerRadius = (height * 0.25f) * (1f - layer * 0.25f);

            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.name = $"Crown_{layer}";
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
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.5f, trunkH * 0.5f, 0.5f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.32f, 0.20f, 0.10f));

        float crownR = height * 0.35f;
        GameObject mainCrown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mainCrown.name = "MainCrown";
        mainCrown.transform.SetParent(root.transform, false);
        mainCrown.transform.localPosition = new Vector3(0f, trunkH + crownR * 0.4f, 0f);
        mainCrown.transform.localScale = new Vector3(crownR * 2.2f, crownR * 1.6f, crownR * 2.2f);
        SetColor(mainCrown, new Color(0.20f, 0.45f, 0.18f));

        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            float offR = crownR * 0.5f;
            GameObject sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sub.name = $"SubCrown_{i}";
            sub.transform.SetParent(root.transform, false);
            sub.transform.localPosition = new Vector3(
                Mathf.Cos(a) * offR,
                trunkH + crownR * 0.15f,
                Mathf.Sin(a) * offR
            );
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
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localScale = new Vector3(0.2f, trunkH * 0.5f, 0.2f);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        SetColor(trunk, new Color(0.82f, 0.78f, 0.72f));

        for (int layer = 0; layer < 2; layer++)
        {
            float layerY = trunkH * 0.6f + layer * (height * 0.22f);
            float r = height * (0.22f - layer * 0.06f);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = $"Crown_{layer}";
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

    private void CreatePonds()
    {
        Transform existing = transform.Find("PrototypePonds");
        if (existing != null) DestroyImmediate(existing.gameObject);
        Transform oldLake = transform.Find("PrototypeLake");
        if (oldLake != null) DestroyImmediate(oldLake.gameObject);

        GameObject pondsRoot = new GameObject("PrototypePonds");
        pondsRoot.transform.SetParent(transform, false);

        List<Vector3> trackPts = GetTrackExclusionPoints();

        Random.State saved = Random.state;
        Random.InitState(42);

        int placed = 0;
        int attempts = 0;
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

            GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pond.name = $"Pond_{placed:00}";
            pond.transform.SetParent(pondsRoot.transform, true);
            pond.transform.position = center;
            pond.transform.localScale = new Vector3(pondRadius * 2f, 0.05f, pondRadius * 2f);

            if (waterMaterial != null)
            {
                pond.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            }
            else
            {
                SetColor(pond, new Color(0.12f, 0.42f, 0.78f, 0.9f), true);
            }

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = $"PondRim_{placed:00}";
            rim.transform.SetParent(pondsRoot.transform, true);
            rim.transform.position = new Vector3(center.x, center.y - 0.01f, center.z);
            rim.transform.localScale = new Vector3(pondRadius * 2.2f, 0.02f, pondRadius * 2.2f);
            SetColor(rim, new Color(0.18f, 0.40f, 0.15f));

            placed++;
        }

        Random.state = saved;
    }

    private void CreateRealisticClouds()
    {
        Transform existing = transform.Find("PrototypeClouds");
        if (existing != null) DestroyImmediate(existing.gameObject);

        Transform oldClouds = transform.Find("Clouds");
        if (oldClouds != null) DestroyImmediate(oldClouds.gameObject);

        GameObject cloudsRoot = new GameObject("PrototypeClouds");
        cloudsRoot.transform.SetParent(transform, false);

        CloudDrifter drifter = cloudsRoot.GetComponent<CloudDrifter>();
        if (drifter == null) drifter = cloudsRoot.AddComponent<CloudDrifter>();
        drifter.driftSpeed = cloudDriftSpeed;

        Random.State saved = Random.state;
        Random.InitState(77);

        for (int i = 0; i < cloudCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(50f, cloudSpawnRadius);
            float altitude = Random.Range(cloudMinAltitude, cloudMaxAltitude);

            Vector3 center = new Vector3(
                Mathf.Cos(angle) * dist,
                altitude,
                Mathf.Sin(angle) * dist
            );

            int puffCount = Random.Range(4, 8);
            GameObject cloudGroup = new GameObject($"Cloud_{i:00}");
            cloudGroup.transform.SetParent(cloudsRoot.transform, false);
            cloudGroup.transform.localPosition = center;

            for (int p = 0; p < puffCount; p++)
            {
                float puffSize = Random.Range(8f, 22f);
                Vector3 offset = new Vector3(
                    Random.Range(-12f, 12f),
                    Random.Range(-2f, 4f),
                    Random.Range(-10f, 10f)
                );

                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = $"Puff_{p}";
                puff.transform.SetParent(cloudGroup.transform, false);
                puff.transform.localPosition = offset;
                puff.transform.localScale = new Vector3(puffSize, puffSize * 0.5f, puffSize * 0.8f);

                Object.DestroyImmediate(puff.GetComponent<Collider>());

                if (cloudMaterial != null)
                {
                    puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
                }
                else
                {
                    SetColor(puff, new Color(0.95f, 0.96f, 0.98f, 0.75f), true);
                }
            }
        }

        Random.state = saved;
    }

    private void ApplySky()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.74f);
    }

    private static Texture2D CreateSolidColorTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = "GeneratedGrass";
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
