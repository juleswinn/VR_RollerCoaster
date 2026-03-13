using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class RealisticTrackBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Rails")]
    [SerializeField, Min(0.4f)] private float railGauge = 1.2f;
    [SerializeField, Min(0.02f)] private float railRadius = 0.08f;
    [SerializeField, Min(0.5f)] private float railSegmentLength = 1.2f;

    [Header("Sleepers")]
    [SerializeField, Min(0.5f)] private float sleeperSpacing = 1.8f;
    [SerializeField] private Vector3 sleeperSize = new Vector3(2.1f, 0.12f, 0.26f);

    [Header("Supports")]
    [SerializeField, Min(1f)] private float supportSpacing = 7f;
    [SerializeField, Min(0.05f)] private float supportRadius = 0.18f;
    [SerializeField] private float supportBaseY = 0f;
    [SerializeField] private bool usePhysicsGroundProbe = true;

    [Header("Materials")]
    [SerializeField] private Material railMaterial;
    [SerializeField] private Material sleeperMaterial;
    [SerializeField] private Material supportMaterial;

    public void SetMaterials(Material rails, Material sleepers, Material supports)
    {
        railMaterial = rails;
        sleeperMaterial = sleepers;
        supportMaterial = supports;
    }

    private Transform railsRoot;
    private Transform sleepersRoot;
    private Transform supportsRoot;

    private void Reset()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    [ContextMenu("Build Realistic Track")]
    public void BuildRealisticTrack()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
            if (splineContainer == null)
            {
                Debug.LogWarning("RealisticTrackBuilder: SplineContainer bulunamadi.");
                return;
            }
        }

        EnsureRoots();
        ClearRoots();

        float length = SplineUtility.CalculateLength(
            splineContainer.Spline,
            splineContainer.transform.localToWorldMatrix
        );

        length = Mathf.Max(2f, length);

        BuildRails(length);
        BuildSleepers(length);
        BuildSupports(length);
    }

    private void EnsureRoots()
    {
        railsRoot = FindOrCreateChild("RailGeometry");
        sleepersRoot = FindOrCreateChild("SleeperGeometry");
        supportsRoot = FindOrCreateChild("SupportGeometry");
    }

    private Transform FindOrCreateChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child;

        GameObject obj = new GameObject(childName);
        obj.transform.SetParent(transform, false);
        return obj.transform;
    }

    private void ClearRoots()
    {
        DestroyChildren(railsRoot);
        DestroyChildren(sleepersRoot);
        DestroyChildren(supportsRoot);
    }

    private static void DestroyChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Object.Destroy(root.GetChild(i).gameObject);
            else Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private void BuildRails(float length)
    {
        int steps = Mathf.Max(4, Mathf.CeilToInt(length / railSegmentLength));

        Vector3? prevLeft = null;
        Vector3? prevRight = null;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;

            EvaluateTrackFrame(t, out Vector3 position, out _, out Vector3 up, out Vector3 right);

            Vector3 left = position - right * (railGauge * 0.5f);
            Vector3 rightRail = position + right * (railGauge * 0.5f);

            if (prevLeft.HasValue)
            {
                CreateTubeSegment(prevLeft.Value, left, railRadius, railsRoot, "RailLeft", railMaterial);
                CreateTubeSegment(prevRight.Value, rightRail, railRadius, railsRoot, "RailRight", railMaterial);
            }

            prevLeft = left;
            prevRight = rightRail;
        }
    }

    private void BuildSleepers(float length)
    {
        int sleeperCount = Mathf.Max(2, Mathf.CeilToInt(length / sleeperSpacing));

        for (int i = 0; i <= sleeperCount; i++)
        {
            float t = i / (float)sleeperCount;

            EvaluateTrackFrame(t, out Vector3 position, out Vector3 forward, out Vector3 up, out _);

            GameObject sleeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sleeper.name = $"Sleeper_{i:0000}";
            sleeper.transform.SetParent(sleepersRoot, true);

            sleeper.transform.position = position - up * (railRadius + sleeperSize.y * 0.5f);
            sleeper.transform.rotation = Quaternion.LookRotation(forward, up);
            sleeper.transform.localScale = sleeperSize;

            ApplyMaterial(sleeper, sleeperMaterial);
        }
    }

    private void BuildSupports(float length)
    {
        int supportCount = Mathf.Max(2, Mathf.CeilToInt(length / supportSpacing));

        for (int i = 0; i <= supportCount; i++)
        {
            float t = i / (float)supportCount;

            EvaluateTrackFrame(t, out Vector3 position, out _, out _, out _);

            float baseY = ResolveSupportBaseHeight(position);
            Vector3 basePoint = new Vector3(position.x, baseY, position.z);

            if (position.y - basePoint.y < 0.75f)
                continue;

            CreateTubeSegment(basePoint, position, supportRadius, supportsRoot, $"Support_{i:0000}", supportMaterial);

            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foot.name = $"SupportFoot_{i:0000}";
            foot.transform.SetParent(supportsRoot, true);
            foot.transform.position = basePoint + Vector3.up * 0.06f;
            foot.transform.localScale = new Vector3(supportRadius * 2.2f, 0.06f, supportRadius * 2.2f);

            ApplyMaterial(foot, supportMaterial);
        }
    }

    private float ResolveSupportBaseHeight(Vector3 trackPosition)
    {
        if (usePhysicsGroundProbe)
        {
            Vector3 origin = trackPosition + Vector3.up * 4f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 800f))
                return hit.point.y;
        }

        return supportBaseY;
    }

    private void EvaluateTrackFrame(float t, out Vector3 position, out Vector3 forward, out Vector3 up, out Vector3 right)
    {
        SplineUtility.Evaluate(
            splineContainer.Spline,
            t,
            out float3 localPosition,
            out float3 localTangent,
            out float3 localUp
        );

        position = splineContainer.transform.TransformPoint((Vector3)localPosition);
        forward = splineContainer.transform.TransformDirection((Vector3)localTangent).normalized;
        up = splineContainer.transform.TransformDirection((Vector3)localUp).normalized;
        right = Vector3.Cross(up, forward).normalized;
    }

    private static void CreateTubeSegment(
        Vector3 start,
        Vector3 end,
        float radius,
        Transform parent,
        string objectName,
        Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length <= 0.001f)
            return;

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        segment.name = objectName;
        segment.transform.SetParent(parent, true);
        segment.transform.position = (start + end) * 0.5f;
        segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        segment.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

        ApplyMaterial(segment, material);
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        if (material == null)
            return;

        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;
    }
}