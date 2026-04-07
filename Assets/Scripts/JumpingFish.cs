using System.Collections;
using UnityEngine;

/// <summary>
/// Sinematik Balık v4
/// – Coaster yaklaştığında kameranın ÖNÜNDEN (çarpmadan) geçecek şekilde zıplar.
/// – 5-6 kere art arda atlar, ardından cooldown.
/// – Atlarken su yüzeyinde iz (ripple) bırakır.
/// </summary>
public class JumpingFish : MonoBehaviour
{
    [Header("Normal Jump")]
    public float jumpIntervalMin = 6f;
    public float jumpIntervalMax = 14f;
    public float jumpHeight      = 3f;
    public float jumpDistance    = 5f;
    public float jumpDuration    = 1.4f;

    [Header("Cinematic Burst")]
    public float coasterTriggerDistance      = 75f;
    public float cinematicHeightMultiplier   = 3.5f;
    public float cinematicDistanceMultiplier = 2.5f;
    public float cinematicDuration           = 1.0f;
    public int   burstJumpCount  = 6;
    public float burstJumpDelay  = 0.45f;
    public float burstCooldown   = 12f;
    [Tooltip("Kamerayla arasındaki minimum güvenli mesafe (çarpmamak için)")]
    public float cameraSafeDistance = 15f;

    [Header("FX")]
    public GameObject splashFXPrefab;
    [Tooltip("Atlama sırasında su yüzeyinde çıkacak iz halkası")]
    public GameObject rippleTrailPrefab;

    // ── State ─────────────────────────────────────
    private Vector3  _basePos;
    private float    _timer;
    private float    _nextNormalJumpTime;
    private bool     _isJumping;
    private bool     _burstActive;
    private bool     _burstOnCooldown;
    private float    _trailTimer;

    // Jump arc
    private Vector3 _jStart, _jEnd;
    private float   _jProgress, _jDuration, _jHeight;

    // Coaster reference (shared)
    private static Transform _coasterTf;
    private static bool      _searched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _coasterTf = null;
        _searched = false;
    }

    void Start()
    {
        _basePos = transform.position;
        ScheduleNormalJump();
        if (!_searched) { _searched = true; StartCoroutine(FindCoaster()); }
        
        if (rippleTrailPrefab == null) rippleTrailPrefab = splashFXPrefab;
    }

    void Update()
    {
        if (_isJumping) { TickArc(); return; }

        bool coasterNear = _coasterTf != null &&
            Vector3.Distance(transform.position, _coasterTf.position) < coasterTriggerDistance;

        if (coasterNear && !_burstActive && !_burstOnCooldown)
        {
            StartCoroutine(BurstSequence());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _nextNormalJumpTime)
            BeginJump(NormalEnd(), jumpHeight, jumpDuration);
    }

    IEnumerator BurstSequence()
    {
        _burstActive     = true;
        _burstOnCooldown = true;

        int count = Random.Range(5, 7); // Kullanıcı 5-6 kez zıplamasını istedi

        for (int i = 0; i < count; i++)
        {
            Vector3 end = CinematicEnd();
            BeginJump(end,
                jumpHeight * cinematicHeightMultiplier,
                cinematicDuration);

            while (_isJumping) yield return null;
            yield return new WaitForSeconds(burstJumpDelay);
        }

        _burstActive = false;
        yield return new WaitForSeconds(burstCooldown);
        _burstOnCooldown = false;
    }

    private bool _hasScreenSplashed;

    void TickArc()
    {
        _jProgress += Time.deltaTime / _jDuration;
        if (_jProgress >= 1f) { _jProgress = 1f; EndJump(); return; }

        float arc = 4f * _jHeight * _jProgress * (1f - _jProgress);
        Vector3 pos = Vector3.Lerp(_jStart, _jEnd, _jProgress);
        pos.y += arc;

        // Su yüzeyinde iz (trail) bırak - Daha sık ve görünür
        _trailTimer += Time.deltaTime;
        if (_trailTimer > 0.08f)
        {
            _trailTimer = 0f;
            Vector3 surfacePos = new Vector3(pos.x, _basePos.y + 0.05f, pos.z);
            if (rippleTrailPrefab != null)
            {
                var ripple = Instantiate(rippleTrailPrefab, surfacePos, Quaternion.identity);
                Destroy(ripple, 1.5f);
            }
        }

        // --- CANLILIK: Balık kıvrılma/wiggle mantığı ---
        Vector3 look = pos - transform.position;
        if (look.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(look.normalized);
            // Sağa sola kıvrılma (Sinüs dalgası)
            float wiggle = Mathf.Sin(Time.time * 25f) * 15f; 
            targetRot *= Quaternion.Euler(0, wiggle, 0);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
        }

        // --- KAMERA SIÇRAMA (VR Gözlük Etkisi) ---
        if (Camera.main != null && !_hasScreenSplashed)
        {
            float distToCam = Vector3.Distance(pos, Camera.main.transform.position);
            if (distToCam < 6f) // 6 metreden yakınsa tetikle
            {
                SpawnScreenSplash();
                _hasScreenSplashed = true;
            }
        }

        transform.position = pos;
    }

    void SpawnScreenSplash()
    {
        if (splashFXPrefab == null || Camera.main == null) return;
        
        // Kameranın tam önüne 0.6 metre mesafede BÜYÜK bir splash oluştur
        Vector3 screenPos = Camera.main.transform.position + Camera.main.transform.forward * 0.6f + Camera.main.transform.up * -0.1f;
        var screenSplash = Instantiate(splashFXPrefab, screenPos, Camera.main.transform.rotation);
        screenSplash.transform.localScale = Vector3.one * 0.85f; // Boyut büyütüldü
        
        // Suların süzülmesi hissi (daha hızlı hareket)
        StartCoroutine(SlideSplashDown(screenSplash));
        Destroy(screenSplash, 2.0f);
    }

    IEnumerator SlideSplashDown(GameObject fx)
    {
        float t = 0;
        while (t < 1.0f && fx != null)
        {
            // Süzülme hızı artırıldı
            fx.transform.position += Vector3.down * Time.deltaTime * 0.35f;
            t += Time.deltaTime;
            yield return null;
        }
    }

    void BeginJump(Vector3 end, float height, float duration)
    {
        _isJumping = true;
        _hasScreenSplashed = false;
        _jStart = transform.position;
        _jStart.y = _basePos.y;
        _jEnd = end;
        _jEnd.y = _basePos.y;
        _jHeight = height;
        _jDuration = Mathf.Max(0.2f, duration);
        _jProgress = 0f;
        _trailTimer = 0f;
        transform.position = _jStart;
        SpawnSplash(_jStart);
    }

    void EndJump()
    {
        _isJumping = false;
        _timer     = 0f;
        _basePos   = new Vector3(_jEnd.x, _basePos.y, _jEnd.z);
        SpawnSplash(_basePos);
        ScheduleNormalJump();
    }

    Vector3 CinematicEnd()
    {
        if (_coasterTf == null) return NormalEnd();

        Vector3 coasterFwd = _coasterTf.forward;
        coasterFwd.y = 0f;
        if (coasterFwd.sqrMagnitude < 0.01f) coasterFwd = Vector3.forward;
        coasterFwd.Normalize();

        // Kameraya çarpmayacak şekilde (cameraSafeDistance kadar ötede) geçiş noktası
        Vector3 passPoint = _coasterTf.position + coasterFwd * cameraSafeDistance;
        passPoint.y = _basePos.y;

        Vector3 coasterRight = Vector3.Cross(Vector3.up, coasterFwd).normalized;
        float side = Vector3.Dot(transform.position - passPoint, coasterRight);
        float dir = side >= 0 ? 1f : -1f;

        float crossDist = jumpDistance * cinematicDistanceMultiplier;

        // Tam karşıdan karşıya (dik) zıpla
        transform.position = passPoint + coasterRight * (dir * crossDist * 0.4f);
        Vector3 end = passPoint + coasterRight * (-dir * crossDist * 0.4f);
        
        return end;
    }

    Vector3 NormalEnd()
    {
        float a = Random.Range(0f, Mathf.PI * 2f);
        return _basePos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * jumpDistance;
    }

    void SpawnSplash(Vector3 pos)
    {
        if (splashFXPrefab == null) return;
        var fx = Instantiate(splashFXPrefab, pos + Vector3.up * 0.15f, Quaternion.identity);
        if (Camera.main != null) fx.transform.LookAt(Camera.main.transform.position);
        Destroy(fx, 2.5f);
    }

    void ScheduleNormalJump() => _nextNormalJumpTime = Random.Range(jumpIntervalMin, jumpIntervalMax);

    IEnumerator FindCoaster()
    {
        yield return new WaitForSeconds(2f);
        string[] names = { "RollerCoasterCar","CoasterCar","VRCar","CartRoot","CoasterCart","RailCart","Cart","Wagon" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) { _coasterTf = go.transform; yield break; }
        }
    }
}
