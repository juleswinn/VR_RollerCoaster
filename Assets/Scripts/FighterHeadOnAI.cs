using UnityEngine;

/// <summary>
/// Savaş uçaklarının coaster'a karşıdan (head-on) gelip yanından geçmesini sağlayan AI.
/// Yere çakılmayı önlemek için vagonun yüksekliğini takip eder.
/// </summary>
public class FighterHeadOnAI : MonoBehaviour
{
    public float startDelay = 0f; // Başlangıç gecikmesi (saniye)
    public float speed = 250f;
    public float shakeDistance = 85f;
    public float shakeIntensity = 0.7f;
    public float maneuverDist = 350f;
    public Vector3 maneuverOffset = new Vector3(25f, 15f, 0f);

    private Transform _coaster;
    private bool _shaken = false;
    private Vector3 _targetOffset = Vector3.zero;
    private float _timer = 0f;
    private bool _started = false;

    void Start()
    {
        // Başlangıçta görünmez yap (gecikme süresince)
        SetVisibility(false);
        
        var cam = Camera.main;
        if (cam != null) _coaster = cam.transform;
    }

    void Update()
    {
        if (!_started)
        {
            _timer += Time.deltaTime;
            if (_timer >= startDelay)
            {
                _started = true;
                SetVisibility(true);
            }
            else return; // Beklemeye devam
        }

        if (_coaster == null) 
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
            return;
        }
        
        float dist = Vector3.Distance(transform.position, _coaster.position);
        
        // --- MANEVRA MANTIĞI ---
        if (dist < maneuverDist)
        {
            _targetOffset = Vector3.Lerp(_targetOffset, transform.right * maneuverOffset.x + transform.up * maneuverOffset.y, Time.deltaTime * 2.5f);
        }

        Vector3 moveDir = (transform.forward * 100f + _targetOffset).normalized;
        transform.position += moveDir * speed * Time.deltaTime;

        // --- SARSINTI ---
        if (!_shaken && dist < shakeDistance)
        {
            _shaken = true;
            if (CoasterShakeEffect.Instance != null)
                CoasterShakeEffect.Instance.Shake(1.2f, shakeIntensity);
        }

        if (dist > 4500f && Vector3.Dot(transform.forward, _coaster.position - transform.position) < 0)
        {
            Destroy(gameObject);
        }
    }

    private void SetVisibility(bool visible)
    {
        var rs = GetComponentsInChildren<Renderer>();
        foreach (var r in rs) r.enabled = visible;
    }
}
