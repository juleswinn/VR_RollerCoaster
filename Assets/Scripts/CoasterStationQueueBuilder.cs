using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class CoasterStationQueueBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Station")]
    [SerializeField, Range(0f, 1f)] private float stationT = 0.0f;
    [SerializeField] private Vector3 stationPlatformSize = new Vector3(28f, 0.6f, 18f);
    [SerializeField] private float stationOffsetToSide = 8f;
    [SerializeField] private float stationRoofHeight = 5f;

    [Header("Queue")]
    [SerializeField, Min(2)] private int laneCount = 4;
    [SerializeField, Min(4f)] private float laneLength = 28f;
    [SerializeField, Min(1.5f)] private float laneSpacing = 2.6f;
    [SerializeField, Min(0.2f)] private float bollardHeight = 0.95f;
    [SerializeField, Min(0.02f)] private float bollardRadius = 0.15f;
    [SerializeField, Min(0.02f)] private float ropeRadius = 0.05f;

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
        SetColor(platform, new Color(0.55f, 0.55f, 0.55f));

        BuildStationRoof(stationRoot, stationPos + sideOffset, stationForward, stationUp);

        BuildStationWalls(stationRoot, stationPos + sideOffset, stationForward, stationRight, stationUp);

        Vector3 queueOrigin = stationPos + sideOffset + stationForward * (stationPlatformSize.z * 0.5f + 2f);
        Quaternion queueRotation = Quaternion.LookRotation(stationForward, stationUp);

        GameObject queueRoot = new GameObject("QueueLanes");
        queueRoot.transform.SetParent(stationRoot, true);
        queueRoot.transform.position = queueOrigin;
        queueRoot.transform.rotation = queueRotation;

        BuildQueueFloor(queueRoot.transform);

        BuildQueueLanes(queueRoot.transform);

        BuildTurnstiles(stationRoot, queueOrigin, stationForward, stationRight, stationUp);

        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "StationGate";
        gate.transform.SetParent(stationRoot, true);
        gate.transform.position = stationPos + sideOffset - stationForward * (stationPlatformSize.z * 0.25f) + Vector3.up * 1f;
        gate.transform.rotation = Quaternion.LookRotation(stationForward, stationUp);
        gate.transform.localScale = new Vector3(3.2f, 2.5f, 0.15f);
        SetColor(gate, new Color(0.6f, 0.15f, 0.15f));
    }

    private void BuildStationRoof(Transform root, Vector3 center, Vector3 forward, Vector3 up)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "StationRoof";
        roof.transform.SetParent(root, true);
        roof.transform.position = center + up * stationRoofHeight;
        roof.transform.rotation = Quaternion.LookRotation(forward, up);
        roof.transform.localScale = new Vector3(stationPlatformSize.x + 2f, 0.3f, stationPlatformSize.z + 2f);
        SetColor(roof, new Color(0.35f, 0.35f, 0.4f));

        float halfX = stationPlatformSize.x * 0.45f;
        float halfZ = stationPlatformSize.z * 0.45f;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3[] pillarOffsets = new Vector3[]
        {
            right * halfX + forward * halfZ,
            right * halfX - forward * halfZ,
            -right * halfX + forward * halfZ,
            -right * halfX - forward * halfZ
        };

        for (int i = 0; i < pillarOffsets.Length; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"RoofPillar_{i}";
            pillar.transform.SetParent(root, true);
            pillar.transform.position = center + pillarOffsets[i] + up * (stationRoofHeight * 0.5f);
            pillar.transform.localScale = new Vector3(0.35f, stationRoofHeight * 0.5f, 0.35f);
            SetColor(pillar, new Color(0.45f, 0.45f, 0.5f));
        }
    }

    private void BuildStationWalls(Transform root, Vector3 center, Vector3 forward, Vector3 right, Vector3 up)
    {
        float halfX = stationPlatformSize.x * 0.5f;
        float wallHeight = stationRoofHeight * 0.6f;

        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "StationWall_Left";
        leftWall.transform.SetParent(root, true);
        leftWall.transform.position = center - right * halfX + up * (wallHeight * 0.5f);
        leftWall.transform.rotation = Quaternion.LookRotation(forward, up);
        leftWall.transform.localScale = new Vector3(0.15f, wallHeight, stationPlatformSize.z);
        SetColor(leftWall, new Color(0.5f, 0.5f, 0.52f));

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "StationWall_Right";
        rightWall.transform.SetParent(root, true);
        rightWall.transform.position = center + right * halfX + up * (wallHeight * 0.5f);
        rightWall.transform.rotation = Quaternion.LookRotation(forward, up);
        rightWall.transform.localScale = new Vector3(0.15f, wallHeight, stationPlatformSize.z);
        SetColor(rightWall, new Color(0.5f, 0.5f, 0.52f));
    }

    private void BuildQueueFloor(Transform queueRoot)
    {
        float queueWidth = (laneCount - 1) * laneSpacing + 2f;
        float floorThickness = 0.15f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "QueueFloor";
        floor.transform.SetParent(queueRoot, false);
        floor.transform.localPosition = new Vector3(0f, -floorThickness * 0.5f, laneLength * 0.5f);
        floor.transform.localScale = new Vector3(queueWidth + 1f, floorThickness, laneLength + 2f);
        SetColor(floor, new Color(0.6f, 0.6f, 0.58f));
    }

    private void BuildTurnstiles(Transform root, Vector3 queueOrigin, Vector3 forward, Vector3 right, Vector3 up)
    {
        float queueWidth = (laneCount - 1) * laneSpacing;
        int turnstileCount = 3;
        float startX = -queueWidth * 0.4f;
        float spacing = queueWidth * 0.8f / Mathf.Max(1, turnstileCount - 1);

        for (int i = 0; i < turnstileCount; i++)
        {
            float xOff = startX + i * spacing;
            Vector3 basePos = queueOrigin + right * xOff + forward * (laneLength + 3f);

            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPost.name = $"Turnstile_{i}_LeftPost";
            leftPost.transform.SetParent(root, true);
            leftPost.transform.position = basePos - right * 0.5f + up * 0.55f;
            leftPost.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
            SetColor(leftPost, new Color(0.7f, 0.7f, 0.7f));

            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPost.name = $"Turnstile_{i}_RightPost";
            rightPost.transform.SetParent(root, true);
            rightPost.transform.position = basePos + right * 0.5f + up * 0.55f;
            rightPost.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
            SetColor(rightPost, new Color(0.7f, 0.7f, 0.7f));

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = $"Turnstile_{i}_TopBar";
            topBar.transform.SetParent(root, true);
            topBar.transform.position = basePos + up * 1.15f;
            topBar.transform.rotation = Quaternion.LookRotation(forward, up);
            topBar.transform.localScale = new Vector3(1.2f, 0.08f, 0.08f);
            SetColor(topBar, new Color(0.7f, 0.7f, 0.7f));

            GameObject arm1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm1.name = $"Turnstile_{i}_Arm1";
            arm1.transform.SetParent(root, true);
            arm1.transform.position = basePos + up * 0.55f;
            arm1.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 0f, 45f);
            arm1.transform.localScale = new Vector3(0.8f, 0.05f, 0.05f);
            SetColor(arm1, new Color(0.8f, 0.2f, 0.2f));

            GameObject arm2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm2.name = $"Turnstile_{i}_Arm2";
            arm2.transform.SetParent(root, true);
            arm2.transform.position = basePos + up * 0.55f;
            arm2.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 0f, -45f);
            arm2.transform.localScale = new Vector3(0.8f, 0.05f, 0.05f);
            SetColor(arm2, new Color(0.8f, 0.2f, 0.2f));
        }
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
        SetColor(bollard, new Color(0.7f, 0.7f, 0.1f));
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
        SetColor(rope, new Color(0.15f, 0.15f, 0.6f));
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

    private static void SetColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat == null) mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.sharedMaterial = mat;
    }
}
