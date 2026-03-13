using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class CoasterStationQueueBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Station")]
    [SerializeField, Range(0f, 1f)] private float stationT = 0.01f;
    [SerializeField] private Vector3 stationPlatformSize = new Vector3(18f, 0.6f, 10f);
    [SerializeField] private float stationOffsetToSide = 7f;

    [Header("Queue")]
    [SerializeField, Min(2)] private int laneCount = 4;
    [SerializeField, Min(4f)] private float laneLength = 24f;
    [SerializeField, Min(1.5f)] private float laneSpacing = 2.6f;
    [SerializeField, Min(0.2f)] private float bollardHeight = 1.1f;
    [SerializeField, Min(0.02f)] private float bollardRadius = 0.09f;
    [SerializeField, Min(0.02f)] private float ropeRadius = 0.03f;

    [Header("Materials")]
    [SerializeField] private Material platformMaterial;
    [SerializeField] private Material bollardMaterial;
    [SerializeField] private Material ropeMaterial;

    public void SetSpline(SplineContainer container)
    {
        splineContainer = container;
    }

    public void SetMaterials(Material platform, Material bollard, Material rope)
    {
        platformMaterial = platform;
        bollardMaterial = bollard;
        ropeMaterial = rope;
    }

    [ContextMenu("Build Station And Queue")]
    public void BuildStationAndQueue()
    {
        if (splineContainer == null)
        {
            Debug.LogWarning("CoasterStationQueueBuilder: SplineContainer atanmadi.");
            return;
        }

        Transform stationRoot = FindOrCreateChild("StationArea");
        ClearChildren(stationRoot);

        EvaluateFrame(out Vector3 stationPos, out Vector3 stationForward, out Vector3 stationUp, out Vector3 stationRight);
        Vector3 sideOffset = stationRight * stationOffsetToSide;

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "BoardingPlatform";
        platform.transform.SetParent(stationRoot, true);
        platform.transform.position = stationPos + sideOffset - stationUp * 0.35f;
        platform.transform.rotation = Quaternion.LookRotation(stationForward, stationUp);
        platform.transform.localScale = stationPlatformSize;
        ApplyMaterial(platform, platformMaterial);

        GameObject queueRoot = new GameObject("QueueLanes");
        queueRoot.transform.SetParent(stationRoot, true);
        queueRoot.transform.position = stationPos + sideOffset + stationForward * (stationPlatformSize.z * 0.5f + 2f);
        queueRoot.transform.rotation = Quaternion.LookRotation(stationForward, stationUp);

        BuildQueueLanes(queueRoot.transform);

        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "StationGate";
        gate.transform.SetParent(stationRoot, true);
        gate.transform.position = stationPos + sideOffset - stationForward * (stationPlatformSize.z * 0.25f) + Vector3.up * 1f;
        gate.transform.rotation = Quaternion.LookRotation(stationForward, stationUp);
        gate.transform.localScale = new Vector3(2.4f, 2f, 0.12f);
        ApplyMaterial(gate, bollardMaterial);
    }

    private void BuildQueueLanes(Transform root)
    {
        float queueWidth = (laneCount - 1) * laneSpacing;
        float startX = -queueWidth * 0.5f;

        for (int lane = 0; lane < laneCount; lane++)
        {
            float x = startX + lane * laneSpacing;
            Vector3 laneStart = new Vector3(x, 0f, 0f);
            Vector3 laneEnd = new Vector3(x, 0f, laneLength);

            Transform laneRoot = new GameObject($"Lane_{lane:00}").transform;
            laneRoot.SetParent(root, false);

            GameObject bollardA = CreateBollard(laneRoot, laneStart, $"Bollard_{lane:00}_A");
            GameObject bollardB = CreateBollard(laneRoot, laneEnd, $"Bollard_{lane:00}_B");

            if (lane < laneCount - 1)
            {
                float nextX = startX + (lane + 1) * laneSpacing;
                Vector3 crossA = new Vector3(nextX, 0f, 0f);
                Vector3 crossB = new Vector3(nextX, 0f, laneLength);

                GameObject nextStart = CreateBollard(laneRoot, crossA, $"Bollard_{lane:00}_C");
                GameObject nextEnd = CreateBollard(laneRoot, crossB, $"Bollard_{lane:00}_D");

                CreateRope(laneRoot, bollardA.transform.localPosition, nextStart.transform.localPosition, $"Rope_{lane:00}_Front");
                CreateRope(laneRoot, bollardB.transform.localPosition, nextEnd.transform.localPosition, $"Rope_{lane:00}_Back");
            }

            if (lane > 0)
            {
                CreateRope(laneRoot,
                    laneStart + Vector3.up * (bollardHeight * 0.8f),
                    laneEnd + Vector3.up * (bollardHeight * 0.8f),
                    $"Rope_{lane:00}_Inner");
            }
        }
    }

    private GameObject CreateBollard(Transform parent, Vector3 localPos, string name)
    {
        GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bollard.name = name;
        bollard.transform.SetParent(parent, false);
        bollard.transform.localPosition = localPos + Vector3.up * (bollardHeight * 0.5f);
        bollard.transform.localScale = new Vector3(bollardRadius * 2f, bollardHeight * 0.5f, bollardRadius * 2f);
        ApplyMaterial(bollard, bollardMaterial);
        return bollard;
    }

    private void CreateRope(Transform parent, Vector3 start, Vector3 end, string name)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.001f) return;

        GameObject rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rope.name = name;
        rope.transform.SetParent(parent, false);
        rope.transform.localPosition = (start + end) * 0.5f;
        rope.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        rope.transform.localScale = new Vector3(ropeRadius * 2f, distance * 0.5f, ropeRadius * 2f);
        ApplyMaterial(rope, ropeMaterial);
    }

    private void EvaluateFrame(out Vector3 position, out Vector3 forward, out Vector3 up, out Vector3 right)
    {
        SplineUtility.Evaluate(
            splineContainer.Spline,
            stationT,
            out float3 localPosition,
            out float3 localTangent,
            out float3 localUp
        );

        position = splineContainer.transform.TransformPoint((Vector3)localPosition);
        forward = splineContainer.transform.TransformDirection((Vector3)localTangent).normalized;
        up = splineContainer.transform.TransformDirection((Vector3)localUp).normalized;
        right = Vector3.Cross(up, forward).normalized;
    }

    private Transform FindOrCreateChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null) return child;

        GameObject obj = new GameObject(childName);
        obj.transform.SetParent(transform, false);
        return obj.transform;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Object.Destroy(root.GetChild(i).gameObject);
            else Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        if (material == null) return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }
}
