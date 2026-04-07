using UnityEngine;

/// <summary>
/// Roller coaster kamerasina veya vagonuna sarsinti (shake) etkisi ekler.
/// Savas ucaklari gecerken tetiklenir.
/// FIX: Delta-offset yaklaşımı — kameranın mevcut konumunu bozmaz, sadece offset ekler.
/// </summary>
public class CoasterShakeEffect : MonoBehaviour
{
    private static CoasterShakeEffect _instance;
    public static CoasterShakeEffect Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _instance = null;

    private void Awake()
    {
        _instance = this;
    }

    private float _shakeTime = 0f;
    private float _shakeIntensity = 0f;
    private Vector3 _currentOffset = Vector3.zero;

    /// <summary>
    /// Sarsintiyi baslatir.
    /// </summary>
    public void Shake(float duration, float intensity)
    {
        _shakeTime = duration;
        _shakeIntensity = intensity;
    }

    void LateUpdate()
    {
        // Önce önceki frame'in offset'ini geri al
        transform.localPosition -= _currentOffset;

        if (_shakeTime > 0)
        {
            // Perlin noise ile sarsıntı offset'i hesapla
            _currentOffset = Random.insideUnitSphere * _shakeIntensity;
            _shakeTime -= Time.deltaTime;
        }
        else
        {
            // Sarsıntı bitince smooth sıfırla
            _currentOffset = Vector3.Lerp(_currentOffset, Vector3.zero, Time.deltaTime * 8f);
            if (_currentOffset.magnitude < 0.001f) _currentOffset = Vector3.zero;
        }

        // Yeni offset'i uygula
        transform.localPosition += _currentOffset;
    }

    void OnDisable()
    {
        // Devre dışı kalırken offset'i temizle
        transform.localPosition -= _currentOffset;
        _currentOffset = Vector3.zero;
    }
}
