using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineTrackGenerator : MonoBehaviour
{
    public enum TrackPreset
    {
        BasicLoop,
        MegaCoaster
    }

    [Header("Track")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private TrackPreset preset = TrackPreset.MegaCoaster;
    [SerializeField, Min(4)] private int controlPointCount = 10;
    [SerializeField, Min(5f)] private float radius = 35f;
    [SerializeField] private float heightAmplitude = 4f;
    [SerializeField, Range(0.05f, 1f)] private float handleScale = 0.35f;
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

        List<Vector3> points = preset == TrackPreset.MegaCoaster
            ? BuildMegaCoasterPointsFixed()
            : BuildBasicPoints();

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

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = Mathf.Sin(angle * 2f) * heightAmplitude;

            points.Add(new Vector3(x, y, z));
        }

        return points;
    }

    private List<Vector3> BuildMegaCoasterPointsFixed()
    {
        List<Vector3> points = new List<Vector3>();

        // Bölüm 1: Ýstasyon çýkýþý
        points.Add(new Vector3(-10f, 8f, -30f));
        points.Add(new Vector3(-12f, 10f, -10f));
        points.Add(new Vector3(-10f, 14f, 15f));

        // Bölüm 2: Ana týrmanýþ
        points.Add(new Vector3(-6f, 22f, 45f));
        points.Add(new Vector3(-2f, 35f, 80f));
        points.Add(new Vector3(4f, 45f, 110f));
        points.Add(new Vector3(10f, 42f, 125f));

        // Bölüm 3: Drop ve banklý dönüþ
        points.Add(new Vector3(22f, 28f, 130f));
        points.Add(new Vector3(35f, 15f, 120f));
        points.Add(new Vector3(48f, 10f, 95f));

        // Bölüm 4: Helix
        AddHelix(points, new Vector3(58f, 18f, 65f), 22f, 4f, 12);

        // Bölüm 5: Orta hýz bölümü
        points.Add(new Vector3(50f, 22f, 30f));
        points.Add(new Vector3(35f, 18f, 5f));
        points.Add(new Vector3(20f, 15f, -15f));
        points.Add(new Vector3(5f, 12f, -5f));

        // Bölüm 6: Slalom
        AddGentleSlalom(points, new Vector3(-5f, 10f, 20f), 50f, 10f, 5);

        // Bölüm 7: Son dönüþ
        points.Add(new Vector3(-20f, 16f, 55f));
        points.Add(new Vector3(-35f, 12f, 35f));
        points.Add(new Vector3(-40f, 10f, 10f));
        points.Add(new Vector3(-35f, 9f, -15f));

        // Bölüm 8: Ýstasyona dönüþ
        points.Add(new Vector3(-25f, 8.5f, -28f));
        points.Add(new Vector3(-15f, 8f, -32f));
        points.Add(new Vector3(-10f, 8f, -30f));

        return points;
    }

    private static void AddHelix(List<Vector3> points, Vector3 center, float radius, float heightGain, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            float angle = t * Mathf.PI * 1.5f;

            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            float y = center.y + t * heightGain;

            points.Add(new Vector3(x, y, z));
        }
    }

    private static void AddGentleSlalom(List<Vector3> points, Vector3 start, float travel, float amplitude, int turns)
    {
        int segments = turns * 3;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;

            float z = start.z + t * travel;

            float x = start.x + Mathf.Sin(t * turns * Mathf.PI) * amplitude * 0.6f;

            float y = start.y + Mathf.Sin(t * Mathf.PI * 2f) * 1.5f;

            points.Add(new Vector3(x, y, z));
        }
    }
}