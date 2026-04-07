using UnityEngine;

/// <summary>
/// Savas ucağı için hılzı gecis (flyby) ve sarsinti tetikleyici.
/// – Çok yüksek hızda hareket eder.
/// – Roller coaster'ın yanından gecerken (35m mesafe) kamerayı titreştirir.
/// FIX: Camera.main her karede okunuyor (VR/XR kamera değişikliklerine uyumlu)
/// </summary>
public class FighterFlybyAI : MonoBehaviour
{
    public float speed = 180f;
    public float shakeDistance = 45f;
    public float shakeDuration = 1.0f;
    public float shakeIntensity = 0.5f;

    private bool _hasShaken = false;
    private Vector3 _startPos;
    public float loopDistance = 5000f;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // İleri hızla git (Sonic boom hissi için)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Kameraya yakınlık kontrolü — her karede Camera.main al (VR uyumlu)
        Camera mainCam = Camera.main;
        if (mainCam != null && !_hasShaken)
        {
            float dist = Vector3.Distance(transform.position, mainCam.transform.position);
            if (dist < shakeDistance)
            {
                TriggerShake();
                _hasShaken = true;
            }
        }

        // Mesafe dolunca başa sar
        if (Vector3.Distance(transform.position, _startPos) > loopDistance)
        {
            transform.position = _startPos;
            _hasShaken = false; // Yeniden titretebilir
        }
    }

    private void TriggerShake()
    {
        if (CoasterShakeEffect.Instance != null)
        {
            CoasterShakeEffect.Instance.Shake(shakeDuration, shakeIntensity);
        }
    }
}
