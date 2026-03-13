using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class CoasterTrainController : MonoBehaviour
{
    private enum RideState
    {
        Boarding,
        RestraintClosing,
        DispatchDelay,
        Launching,
        Running
    }

    [Serializable]
    public struct SpeedZone
    {
        [Range(0f, 1f)] public float startT;
        [Range(0f, 1f)] public float endT;
        [Min(0f)] public float targetSpeed;
        [Min(0f)] public float response;
        [Min(0f)] public float minSpeedInZone; // Yeni: bölge minimum hýzý
    }

    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform trainRoot;
    [SerializeField] private Transform lapBarPivot;

    [Header("Station")]
    [SerializeField, Range(0f, 1f)] private float stationStopT = 0.01f;
    [SerializeField, Min(0f)] private float boardingDuration = 2f;
    [SerializeField, Min(0f)] private float restraintCloseDuration = 1.2f;
    [SerializeField, Min(0f)] private float dispatchDelay = 3f;

    [Header("Launch")]
    [SerializeField, Min(0f)] private float launchDuration = 2.2f;
    [SerializeField, Min(0f)] private float launchSpeed = 30f;

    [Header("Motion")]
    [SerializeField] private bool loop = true;
    [SerializeField, Min(0f)] private float cruiseSpeed = 25f;
    [SerializeField, Min(0f)] private float acceleration = 16f;
    [SerializeField, Min(0f)] private float brakeDeceleration = 8f;
    [SerializeField] private float gravityInfluence = 1.5f; // düþürüldü
    [SerializeField, Min(0f)] private float stationCatchDistance = 0.006f;
    [SerializeField, Min(0f)] private float globalMinRunningSpeed = 8f; // asla düþmeyecek

    [Header("Uphill Assist")]
    [SerializeField, Min(0f)] private float uphillBoost = 15f; // yokuþ desteði
    [SerializeField] private float uphillThreshold = 0.3f;

    [Header("Lap Bar")]
    [SerializeField] private float lapBarOpenAngle = -6f;
    [SerializeField] private float lapBarClosedAngle = 48f;

    [Header("Speed Profile")]
    [SerializeField] private List<SpeedZone> speedZones = new List<SpeedZone>();

    [Header("Runtime")]
    [SerializeField, Range(0f, 1f)] private float t;
    [SerializeField, Min(0f)] private float currentSpeed;

    private RideState state = RideState.Boarding;
    private float stateTimer;
    private float splineLength = 1f;
    private int lapCounter;

    public void SetSpline(SplineContainer container)
    {
        splineContainer = container;
        CacheSplineLength();
    }

    public void SetLapBarPivot(Transform pivot)
    {
        lapBarPivot = pivot;
    }

    public void CacheSplineLength()
    {
        if (splineContainer == null)
        {
            splineLength = 0f;
            return;
        }

        splineLength = SplineUtility.CalculateLength(
            splineContainer.Spline,
            splineContainer.transform.localToWorldMatrix
        );

        splineLength = Mathf.Max(0.001f, splineLength);
    }

    private void Reset()
    {
        trainRoot = transform;
    }

    private void Start()
    {
        if (trainRoot == null) trainRoot = transform;

        CacheSplineLength();
        t = stationStopT;
        currentSpeed = 0f;
        state = RideState.Boarding;
        stateTimer = 0f;

        if (speedZones.Count == 0)

        SetLapBar(0f);
        ApplyPose();
    }

    private void Update()
    {
        if (splineContainer == null || trainRoot == null) return;

        stateTimer += Time.deltaTime;

        switch (state)
        {
            case RideState.Boarding:
                t = stationStopT;
                currentSpeed = 0f;
                SetLapBar(0f);
                if (stateTimer >= boardingDuration) EnterState(RideState.RestraintClosing);
                break;

            case RideState.RestraintClosing:
                t = stationStopT;
                currentSpeed = 0f;
                SetLapBar(SafeRatio(stateTimer, restraintCloseDuration));
                if (stateTimer >= restraintCloseDuration) EnterState(RideState.DispatchDelay);
                break;

            case RideState.DispatchDelay:
                t = stationStopT;
                currentSpeed = 0f;
                SetLapBar(1f);
                if (stateTimer >= dispatchDelay) EnterState(RideState.Launching);
                break;

            case RideState.Launching:
                {
                    SetLapBar(1f);

                    float ratio = SafeRatio(stateTimer, launchDuration);
                    float target = Mathf.Lerp(0f, launchSpeed, ratio);

                    currentSpeed = Mathf.MoveTowards(currentSpeed, target, acceleration * Time.deltaTime);
                    AdvanceAlongSpline(currentSpeed);

                    if (stateTimer >= launchDuration)
                        EnterState(RideState.Running);

                    break;
                }

            case RideState.Running:
                SetLapBar(1f);
                UpdateRunningSpeed();

                // Anti-stall
                currentSpeed = Mathf.Max(currentSpeed, globalMinRunningSpeed);

                AdvanceAlongSpline(currentSpeed);
                TryEnterStation();
                break;
        }

        ApplyPose();
    }

    private void UpdateRunningSpeed()
    {
        float zoneSpeed = ResolveZoneSpeed(t, cruiseSpeed, 6f);

        // eðim hesapla
        SplineUtility.Evaluate(splineContainer.Spline, t, out _, out float3 tangent, out _);

        Vector3 worldForward = splineContainer.transform.TransformDirection((Vector3)tangent).normalized;

        float slope = -worldForward.y;

        float targetSpeed = zoneSpeed;

        if (slope > uphillThreshold)
        {
            float uphillFactor = (slope - uphillThreshold) * 2f;
            targetSpeed += uphillBoost * uphillFactor;
        }
        else
        {
            targetSpeed += slope * gravityInfluence * 10f;
        }

        float zoneMin = GetZoneMinSpeed(t);
        targetSpeed = Mathf.Max(targetSpeed, zoneMin);

        float maxDelta = targetSpeed < currentSpeed ? brakeDeceleration : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, maxDelta * Time.deltaTime);
    }

    private float GetZoneMinSpeed(float normalizedT)
    {
        for (int i = 0; i < speedZones.Count; i++)
        {
            if (IsInRange(normalizedT, speedZones[i].startT, speedZones[i].endT))
                return Mathf.Max(globalMinRunningSpeed, speedZones[i].minSpeedInZone);
        }

        return globalMinRunningSpeed;
    }

    private void TryEnterStation()
    {
        if (lapCounter <= 0) return;

        float delta = Mathf.Abs(Mathf.DeltaAngle(t * 360f, stationStopT * 360f)) / 360f;

        if (delta <= stationCatchDistance && currentSpeed < 3f)
        {
            EnterState(RideState.Boarding);
            t = stationStopT;
            currentSpeed = 0f;
        }
    }

    private void AdvanceAlongSpline(float speed)
    {
        if (splineLength <= 0.001f)
        {
            CacheSplineLength();
            if (splineLength <= 0.001f) return;
        }

        float previousT = t;

        t += (speed / splineLength) * Time.deltaTime;

        if (loop)
        {
            if (t >= 1f)
            {
                t = Mathf.Repeat(t, 1f);
                lapCounter++;
            }
        }
        else
        {
            t = Mathf.Clamp01(t);

            if (Mathf.Approximately(t, 1f) && t > previousT)
            {
                currentSpeed = 0f;
                EnterState(RideState.Boarding);
                t = stationStopT;
            }
        }
    }

    private float ResolveZoneSpeed(float normalizedT, float fallback, float fallbackResponse)
    {
        for (int i = 0; i < speedZones.Count; i++)
        {
            SpeedZone zone = speedZones[i];

            if (IsInRange(normalizedT, zone.startT, zone.endT))
            {
                float response = Mathf.Max(0.1f, zone.response);

                return Mathf.Lerp(
                    currentSpeed,
                    zone.targetSpeed,
                    1f - Mathf.Exp(-response * Time.deltaTime)
                );
            }
        }

        return Mathf.Lerp(
            currentSpeed,
            fallback,
            1f - Mathf.Exp(-fallbackResponse * Time.deltaTime)
        );
    }

    private static bool IsInRange(float value, float start, float end)
    {
        if (start <= end) return value >= start && value <= end;
        return value >= start || value <= end;
    }

    private void ApplyPose()
    {
        SplineUtility.Evaluate(
            splineContainer.Spline,
            t,
            out float3 localPosition,
            out float3 localTangent,
            out float3 localUp
        );

        Vector3 worldPosition = splineContainer.transform.TransformPoint((Vector3)localPosition);
        Vector3 worldForward = splineContainer.transform.TransformDirection((Vector3)localTangent).normalized;
        Vector3 worldUp = splineContainer.transform.TransformDirection((Vector3)localUp).normalized;

        trainRoot.position = worldPosition;
        trainRoot.rotation = Quaternion.LookRotation(worldForward, worldUp);
    }

    private void SetLapBar(float normalizedClose)
    {
        if (lapBarPivot == null) return;

        float angle = Mathf.Lerp(lapBarOpenAngle, lapBarClosedAngle, Mathf.Clamp01(normalizedClose));

        Vector3 euler = lapBarPivot.localEulerAngles;

        lapBarPivot.localRotation = Quaternion.Euler(angle, euler.y, euler.z);
    }

    private void EnterState(RideState next)
    {
        state = next;
        stateTimer = 0f;
    }

    private static float SafeRatio(float elapsed, float duration)
    {
        if (duration <= 0.0001f) return 1f;

        return Mathf.Clamp01(elapsed / duration);
    }
}