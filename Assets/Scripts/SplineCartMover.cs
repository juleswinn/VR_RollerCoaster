using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineCartMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform cartTransform;

    [Header("Motion")]
    [SerializeField, Range(0f, 1f)] private float t = 0f;
    [SerializeField, Min(0f)] private float speed = 6f;
    [SerializeField] private bool loop = true;

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float positionLerp = 0f;
    [SerializeField, Min(0f)] private float rotationLerp = 10f;

    private float splineLength = 1f;

    private void Reset()
    {
        cartTransform = transform;
    }

    private void Start()
    {
        if (cartTransform == null)
            cartTransform = transform;

        CacheSplineLength();
    }

    private void Update()
    {
        if (splineContainer == null) return;

        if (splineLength <= 0.001f)
        {
            CacheSplineLength();
            if (splineLength <= 0.001f) return;
        }

        float deltaT = (speed / splineLength) * Time.deltaTime;
        t += deltaT;
        t = loop ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);

        splineContainer.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);

        Vector3 targetPos = (Vector3)pos;
        Quaternion targetRot = Quaternion.LookRotation((Vector3)tangent, (Vector3)up);

        if (positionLerp > 0f)
        {
            float factor = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
            cartTransform.position = Vector3.Lerp(cartTransform.position, targetPos, factor);
        }
        else
        {
            cartTransform.position = targetPos;
        }

        if (rotationLerp > 0f)
        {
            float factor = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);
            cartTransform.rotation = Quaternion.Slerp(cartTransform.rotation, targetRot, factor);
        }
        else
        {
            cartTransform.rotation = targetRot;
        }
    }

    public void SetSpline(SplineContainer container)
    {
        splineContainer = container;
        CacheSplineLength();
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
}