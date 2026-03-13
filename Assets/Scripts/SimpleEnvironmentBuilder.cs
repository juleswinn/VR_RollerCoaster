using UnityEngine;
using UnityEngine.Rendering;

public class SimpleEnvironmentBuilder : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField, Min(64f)] private float terrainSize = 300f;
    [SerializeField, Min(10f)] private float terrainHeight = 30f;
    [SerializeField] private int heightmapResolution = 257;
    [SerializeField] private float noiseScale = 0.03f;
    [SerializeField] private float noiseStrength = 0.08f;

    [Header("Trees")]
    [SerializeField, Min(0)] private int treeCount = 24;
    [SerializeField] private float treeSpawnRadius = 120f;
    [SerializeField] private Vector2 treeHeightRange = new Vector2(5f, 9f);

    [Header("Sky")]
    [SerializeField] private Material skyboxMaterial;

    public void SetSkyboxMaterial(Material material)
    {
        skyboxMaterial = material;
    }

    [ContextMenu("Build Environment")]
    public void BuildEnvironment()
    {
        CreateTerrain();
        CreateTrees();
        ApplySky();
    }

    private void CreateTerrain()
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
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x * noiseScale;
                float ny = y * noiseScale;
                heights[y, x] = Mathf.PerlinNoise(nx, ny) * noiseStrength;
            }
        }

        terrainData.SetHeights(0, 0, heights);
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "PrototypeTerrain";
        terrainObject.transform.SetParent(transform, false);
        terrainObject.transform.position = new Vector3(-terrainSize * 0.5f, -2f, -terrainSize * 0.5f);
    }

    private void CreateTrees()
    {
        Transform existing = transform.Find("PrototypeTrees");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("PrototypeTrees");
        root.transform.SetParent(transform, false);

        for (int i = 0; i < treeCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(15f, treeSpawnRadius);
            Vector3 basePos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            float treeHeight = Random.Range(treeHeightRange.x, treeHeightRange.y);
            float trunkHeight = treeHeight * 0.5f;
            float crownRadius = treeHeight * 0.22f;

            GameObject treeRoot = new GameObject($"Tree_{i:00}");
            treeRoot.transform.SetParent(root.transform, false);
            treeRoot.transform.localPosition = basePos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeRoot.transform, false);
            trunk.transform.localScale = new Vector3(0.45f, trunkHeight * 0.5f, 0.45f);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(treeRoot.transform, false);
            crown.transform.localScale = Vector3.one * (crownRadius * 2f);
            crown.transform.localPosition = new Vector3(0f, trunkHeight + crownRadius * 0.6f, 0f);

            var trunkRenderer = trunk.GetComponent<Renderer>();
            if (trunkRenderer != null) trunkRenderer.sharedMaterial.color = new Color(0.37f, 0.24f, 0.16f);

            var crownRenderer = crown.GetComponent<Renderer>();
            if (crownRenderer != null) crownRenderer.sharedMaterial.color = new Color(0.22f, 0.48f, 0.24f);
        }
    }

    private void ApplySky()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.68f, 0.72f, 0.74f);
    }
}
