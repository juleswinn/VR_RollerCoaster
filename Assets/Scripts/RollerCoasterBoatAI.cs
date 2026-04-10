using System.Collections;
using UnityEngine;

/// <summary>
/// Geliştirilmiş Sinematik Tekne AI v2
/// – Coaster wagonunun pozisyonunu ve yönünü gerçek zamanlı takip eder.
/// – Normal: gölde sakin patrol. Burst: coaster'ın tam önüne keserek sprint atar.
/// – Arkadan sürekli ParticleSystem (su izi/wake) çıkarır.
/// – Kameraya yaklaştığında ekstra splash patlatır.
/// – ApplyBobRoll, hareket yönünü bozmadan yalnızca lokal eksen üzerinde çalışır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RollerCoasterBoatAI : MonoBehaviour
{
    [Header("Lake Settings")]
    public Vector3 lakeCenter   = Vector3.zero;
    public float   patrolRadius = 100f;
    public float   waterY       = 0.15f;   // su yüzeyi Y yüksekliği

    [Header("Speed")]
    public float normalSpeed = 5f;
    public float burstSpeed  = 20f;
    public float turnSpeed   = 90f;        // derece/saniye

    [Header("Coaster Reaction")]
    public float coasterReactDistance = 110f;  // Daha uzaktan fark etmesi için artırıldı
    public float interceptLeadTime    = 3.5f;  // Daha ileriyi hedeflesin (altından geçmek için)

    [Header("Water FX")]
    [Tooltip("NamuFX Water_Splash prefab – burst sırasında patlar")]
    public GameObject splashPrefab;
    [Tooltip("Teknenin arkasındaki wake/iz için ParticleSystem prefab")]
    public GameObject wakePrefab;
    public float splashInterval = 0.35f;

    [Header("Bob")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 1.1f;
    public float rollAmplitude = 2f;

    // ──────────────────────────────────────────────────────────────────
    private Rigidbody    _rb;
    private Transform    _coaster;
    private Vector3      _waypoint;
    private bool         _isBursting;
    private float        _splashTimer;
    private float        _currentYaw;   // sadece yaw – bob'u bozmaz
    private ParticleSystem _wakePS;
    private float        _interceptTimeVar;

    private static System.Collections.Generic.List<RollerCoasterBoatAI> _allBoats = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _allBoats = new();

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ;
        _rb.linearDamping  = 2f;
        _rb.angularDamping = 5f;
        _rb.useGravity = false;   // su yüzeyini biz yönetiyoruz

        _currentYaw = transform.eulerAngles.y;
        SnapToWater();
        PickNewWaypoint();
        _interceptTimeVar = Random.Range(1.2f, 3.5f);
        _allBoats.Add(this);

        // Wake (iz) particle sistemi oluştur
        if (wakePrefab != null)
        {
            var wakeGO = Instantiate(wakePrefab,
                transform.position - transform.forward * 1.5f,
                Quaternion.identity, transform);
            _wakePS = wakeGO.GetComponent<ParticleSystem>();
            if (_wakePS != null)
            {
                var em = _wakePS.emission;
                em.enabled = true;
                
                // --- WIDER WAKE: İz genişliğini ve boyutunu artır ---
                var main = _wakePS.main;
                main.startSizeMultiplier = 2.2f;
                var shape = _wakePS.shape;
                shape.radius = 1.6f;
            }
        }

        StartCoroutine(FindCoasterDelayed());
    }

    void Update()
    {
        SnapToWater();   // y sabit tut

        bool coasterNear = IsCoasterNear();

        // ── Burst on/off ─────────────────────────────────────────
        if (coasterNear && !_isBursting)  StartBurst();
        if (!coasterNear && _isBursting)  StopBurst();

        // ── Hedef waypoint ────────────────────────────────────────
        if (coasterNear && _coaster != null)
        {
            // Coaster'ın önüne intercept noktası hesapla – daha ileriyi hedefleyerek yolu kesmesini sağlıyoruz
            Vector3 coasterForward = _coaster.forward;
            coasterForward.y = 0; // Yatay intercept
            
            // Waypoint'i coaster'ın ilerisindeki bir nokta olarak belirle
            Vector3 futurePos = _coaster.position + coasterForward * (burstSpeed * interceptLeadTime);
            futurePos.y = waterY;
            
            // Eğer waypoint göl sınırları dışındaysa sınırda tut
            Vector3 offset = futurePos - lakeCenter;
            offset.y = 0;
            if (offset.magnitude > patrolRadius) futurePos = lakeCenter + offset.normalized * patrolRadius;
            
            _waypoint = futurePos;
        }
        else
        {
            float dist2D = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(_waypoint.x, _waypoint.z));
            if (dist2D < 8f) PickNewWaypoint();
        }

        // ── Hareket ─────────────────────────────────────────────
        MoveStep(coasterNear);

        // ── Splash timer ─────────────────────────────────────────
        if (_isBursting)
        {
            _splashTimer -= Time.deltaTime;
            if (_splashTimer <= 0f) { SpawnSplash(); _splashTimer = splashInterval; }
        }

        // ── Wake emit hızı: Cok daha fazla su izi ───────────────────
        if (_wakePS != null)
        {
            float speed = _isBursting ? burstSpeed : normalSpeed;
            var em = _wakePS.emission;
            // 5f -> 20f, 60f -> 180f (3 kat artırıldı)
            em.rateOverTime = Mathf.Lerp(20f, 180f, speed / burstSpeed);
        }

        // ── Bob ───────────────────────────────────────────────────
        ApplyBob();
    }

    // ──────────────────────────────────────────────────────────────────
    void MoveStep(bool bursting)
    {
        Vector3 toWaypoint = _waypoint - transform.position;
        toWaypoint.y = 0f;
        float dist = toWaypoint.magnitude;
        if (dist < 0.1f) return;

        // Yaw döndür
        float targetYaw = Mathf.Atan2(toWaypoint.x, toWaypoint.z) * Mathf.Rad2Deg;
        float maxTurn = turnSpeed * Time.deltaTime;
        _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, targetYaw, maxTurn);

        float speed = bursting ? burstSpeed : normalSpeed;
        // Hız lerp ile geçiş
        float smoothSpeed = Mathf.Lerp(
            _rb.linearVelocity.magnitude > 0.1f ? _rb.linearVelocity.magnitude : normalSpeed,
            speed, Time.deltaTime * 3f);

        Vector3 avoidVec = AvoidanceForce();
        Vector3 dir = (Quaternion.Euler(0, _currentYaw, 0) * Vector3.forward + avoidVec * 0.6f).normalized;
        _rb.linearVelocity = dir * smoothSpeed;

        // Göl sınırı
        Vector3 offset = transform.position - lakeCenter;
        offset.y = 0f;
        if (offset.magnitude > patrolRadius)
        {
            transform.position = lakeCenter + offset.normalized * (patrolRadius * 0.85f)
                                 + Vector3.up * waterY;
            PickNewWaypoint();
        }
    }

    void ApplyBob()
    {
        float t = Time.time;
        float bobY  = waterY + Mathf.Sin(t * bobFrequency) * bobAmplitude;
        float roll  = Mathf.Sin(t * bobFrequency * 0.7f)  * rollAmplitude;
        float pitch = Mathf.Cos(t * bobFrequency * 0.5f)  * (rollAmplitude * 0.4f);

        // Yalnızca Y pozisyon
        Vector3 pos = transform.position;
        pos.y = bobY;
        transform.position = pos;

        // Rotasyon: yaw (hareket yönü) + bob
        transform.rotation = Quaternion.Euler(pitch, _currentYaw, roll);
    }

    void SnapToWater()
    {
        Vector3 p = transform.position;
        p.y = waterY;
        transform.position = p;
    }

    void PickNewWaypoint()
    {
        float a = Random.Range(0f, Mathf.PI * 2f);
        float d = Random.Range(patrolRadius * 0.15f, patrolRadius * 0.85f);
        _waypoint = lakeCenter + new Vector3(Mathf.Cos(a) * d, waterY, Mathf.Sin(a) * d);
    }

    bool IsCoasterNear() =>
        _coaster != null &&
        Vector3.Distance(transform.position, _coaster.position) < coasterReactDistance;

    void StartBurst()
    {
        _isBursting  = true;
        _splashTimer = 0f;
    }

    void StopBurst() => _isBursting = false;

    void SpawnSplash()
    {
        if (splashPrefab == null) return;
        // Teknenin arkasından + kameraya doğru eğ
        Vector3 spawnPos = transform.position - transform.forward * 2f + Vector3.up * 0.2f;
        var fx = Instantiate(splashPrefab, spawnPos, Quaternion.identity);
        if (Camera.main != null)
            fx.transform.LookAt(Camera.main.transform.position);
        Destroy(fx, 2.5f);
    }

    void OnDestroy() => _allBoats.Remove(this);

    // Birbirinin içinden geçmelerini önleme (Separation)
    Vector3 AvoidanceForce()
    {
        Vector3 force = Vector3.zero;
        float neighborDist = 18f; // Mesafe artırıldı
        int count = 0;

        foreach (var other in _allBoats)
        {
            if (other == this) continue;
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < neighborDist && d > 0.001f)
            {
                Vector3 diff = (transform.position - other.transform.position).normalized;
                force += (diff / d) * 3f; // İtme gücü artırıldı
                count++;
            }
        }
        return count > 0 ? force.normalized : Vector3.zero;
    }

    IEnumerator FindCoasterDelayed()
    {
        yield return new WaitForSeconds(1f);
        // Coaster aracını bul
        string[] names = {
            "RollerCoasterCar","CoasterCar","VRCar","CartRoot",
            "CoasterCart","RailCart","Cart","Wagon"
        };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) { _coaster = go.transform; yield break; }
        }
        // Player tag dene
        var tagged = GameObject.FindWithTag("Player");
        if (tagged != null) _coaster = tagged.transform;
    }
}
