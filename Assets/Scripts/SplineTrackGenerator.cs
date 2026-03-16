using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineTrackGenerator : MonoBehaviour
{
    public enum TrackPreset
    {
        BasicLoop,
        MegaCoaster,
        EpicScenic
    }

    [Header("Track")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private TrackPreset preset = TrackPreset.EpicScenic;
    [SerializeField, Min(4)] private int controlPointCount = 10;
    [SerializeField, Min(5f)] private float radius = 35f;
    [SerializeField] private float heightAmplitude = 4f;
    [SerializeField, Range(0.05f, 1f)] private float handleScale = 0.45f;
    [SerializeField] private bool closedLoop = true;

    public SplineContainer SplineContainer => splineContainer;

    private void Reset()
    {
        splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null)
            splineContainer = gameObject.AddComponent<SplineContainer>();
    }

    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
            if (splineContainer == null)
                splineContainer = gameObject.AddComponent<SplineContainer>();
        }

        var spline = splineContainer.Spline;
        spline.Clear();

        List<Vector3> points;
        if (preset == TrackPreset.EpicScenic)
            points = BuildEpicScenicPoints();
        else if (preset == TrackPreset.MegaCoaster)
            points = BuildMegaCoasterPointsFixed();
        else
            points = BuildBasicPoints();

        int pointCount = points.Count;

        for (int i = 0; i < pointCount; i++)
        {
            int prevIndex = i - 1;
            int nextIndex = i + 1;

            if (closedLoop)
            {
                prevIndex = (prevIndex + pointCount) % pointCount;
                nextIndex %= pointCount;
            }
            else
            {
                prevIndex = Mathf.Clamp(prevIndex, 0, pointCount - 1);
                nextIndex = Mathf.Clamp(nextIndex, 0, pointCount - 1);
            }

            Vector3 prev = points[prevIndex];
            Vector3 current = points[i];
            Vector3 next = points[nextIndex];

            Vector3 direction = (next - prev).normalized;
            float handleLength = (next - current).magnitude * handleScale;

            Vector3 tangentOut = direction * handleLength;
            Vector3 tangentIn = -tangentOut;

            spline.Add(new BezierKnot(
                (float3)current,
                (float3)tangentIn,
                (float3)tangentOut,
                quaternion.identity
            ));
        }

        spline.Closed = closedLoop;
    }

    private List<Vector3> BuildBasicPoints()
    {
        List<Vector3> points = new List<Vector3>(controlPointCount);
        for (int i = 0; i < controlPointCount; i++)
        {
            float t = i / (float)controlPointCount;
            float angle = t * Mathf.PI * 2f;
            points.Add(new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle * 2f) * heightAmplitude,
                Mathf.Sin(angle) * radius
            ));
        }
        return points;
    }

    private List<Vector3> BuildEpicScenicPoints()
    {
        List<Vector3> points = new List<Vector3>();

        points.Add(new Vector3(0f, 2f, 0f));
        points.Add(new Vector3(0f, 2f, 30f));

        points.Add(new Vector3(5f, 5f, 80f));
        points.Add(new Vector3(10f, 12f, 140f));
        points.Add(new Vector3(12f, 22f, 200f));
        points.Add(new Vector3(10f, 38f, 265f));

        points.Add(new Vector3(20f, 30f, 300f));
        points.Add(new Vector3(45f, 8f, 325f));
        
        points.Add(new Vector3(75f,  5f, 330f));
        points.Add(new Vector3(95f,  5f, 330f));

        points.Add(new Vector3(105f,  5f, 330f)); 
        points.Add(new Vector3(115f, 15f, 330f)); 
        points.Add(new Vector3(122f, 32f, 328f)); 
        points.Add(new Vector3(120f, 48f, 323f)); 
        points.Add(new Vector3(112f, 52f, 320f)); 
        points.Add(new Vector3(104f, 48f, 317f)); 
        points.Add(new Vector3(102f, 32f, 312f)); 
        points.Add(new Vector3(109f, 15f, 310f)); 
        points.Add(new Vector3(115f,  5f, 310f)); 
        
        points.Add(new Vector3(125f,  5f, 310f));
        points.Add(new Vector3(145f,  5f, 290f));

        points.Add(new Vector3(160f,  5f, 240f));
        points.Add(new Vector3(175f, 25f, 200f));
        points.Add(new Vector3(160f,  5f, 160f));

        points.Add(new Vector3(140f,  5f, 130f));
        points.Add(new Vector3(120f, 15f, 100f));
        points.Add(new Vector3(140f,  5f,  70f));
        points.Add(new Vector3(160f, 15f,  40f));
        points.Add(new Vector3(120f,  5f,  10f));
        
        points.Add(new Vector3(80f, 6f, -30f));
        points.Add(new Vector3(40f, 8f, -70f));
        points.Add(new Vector3(10f, 5f, -110f));
        points.Add(new Vector3(-5f, 5f, -130f));

        points.Add(new Vector3(-25f,  5f, -130f)); 
        points.Add(new Vector3(-35f, 15f, -130f)); 
        points.Add(new Vector3(-42f, 32f, -128f)); 
        points.Add(new Vector3(-40f, 48f, -125f)); 
        points.Add(new Vector3(-32f, 52f, -122f)); 
        points.Add(new Vector3(-24f, 48f, -119f)); 
        points.Add(new Vector3(-22f, 32f, -117f)); 
        points.Add(new Vector3(-29f, 15f, -115f)); 
        points.Add(new Vector3(-35f,  5f, -115f)); 
        
        points.Add(new Vector3(-45f,  5f, -115f));

        points.Add(new Vector3(-65f,  5f, -100f));
        points.Add(new Vector3(-80f, 18f, -80f));
        points.Add(new Vector3(-65f,  5f, -60f));

        points.Add(new Vector3(-40f,  3f, -40f));
        points.Add(new Vector3(-20f,  2f, -20f));
        points.Add(new Vector3(  0f,  2f, -20f));
        points.Add(new Vector3(  0f,  2f, -10f));

        return points;
    }

    private static void AddWideHelix(List<Vector3> points, Vector3 center, float helixRadius, float heightGain, int segments)
    {
        segments = Mathf.Max(4, segments);
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            float angle = t * Mathf.PI * 1.5f;
            points.Add(new Vector3(
                center.x + Mathf.Cos(angle) * helixRadius,
                center.y + t * heightGain,
                center.z + Mathf.Sin(angle) * helixRadius
            ));
        }
    }

    private List<Vector3> BuildMegaCoasterPointsFixed()
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(new Vector3(-10f, 8f, -30f));
        points.Add(new Vector3(-12f, 10f, -10f));
        points.Add(new Vector3(-10f, 14f, 15f));
        points.Add(new Vector3(-6f, 22f, 45f));
        points.Add(new Vector3(-2f, 35f, 80f));
        points.Add(new Vector3(4f, 45f, 110f));
        points.Add(new Vector3(10f, 42f, 125f));
        points.Add(new Vector3(22f, 28f, 130f));
        points.Add(new Vector3(35f, 15f, 120f));
        points.Add(new Vector3(48f, 10f, 95f));
        AddWideHelix(points, new Vector3(58f, 18f, 65f), 22f, 4f, 12);
        points.Add(new Vector3(50f, 22f, 30f));
        points.Add(new Vector3(35f, 18f, 5f));
        points.Add(new Vector3(20f, 15f, -15f));
        points.Add(new Vector3(5f, 12f, -5f));
        points.Add(new Vector3(-20f, 16f, 55f));
        points.Add(new Vector3(-35f, 12f, 35f));
        points.Add(new Vector3(-40f, 10f, 10f));
        points.Add(new Vector3(-35f, 9f, -15f));
        points.Add(new Vector3(-25f, 8.5f, -28f));
        points.Add(new Vector3(-15f, 8f, -32f));
        points.Add(new Vector3(-10f, 8f, -30f));
        return points;
    }

    public List<Vector3> GetTrackPoints()
    {
        List<Vector3> points;
        if (preset == TrackPreset.EpicScenic)
            points = BuildEpicScenicPoints();
        else if (preset == TrackPreset.MegaCoaster)
            points = BuildMegaCoasterPointsFixed();
        else
            points = BuildBasicPoints();
        return points;
    }
}