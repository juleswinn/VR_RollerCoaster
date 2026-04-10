using System.Collections;
using UnityEngine;

/// <summary>
/// Sinematik Balık v5 — Kamera Paralel Sıçrama
/// – Coaster yaklaştığında kameranın ÖNÜNDE, sağdan sola veya soldan sağa
///   gerçekçi bir ark çizerek sıçrar.
/// – Sıçrama sırasında arkasında su parçacıkları (trail) bırakır.
/// – Kameraya yakın geçtiğinde su damlaları kameraya vurur ve yukarıdan
///   aşağıya süzülür.
/// – Normal zamanlarda gölette rasgele küçük zıplamalar yapar.
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
    public float cinematicHeightMultiplier = 6.2f; // Daha devasa zıplamalar (7.5m civarı)
    public float cinematicDistanceMultiplier = 2.5f;
    public float cinematicDuration           = 1.15f;
    public int   burstJumpCount  = 6;
    public float burstJumpDelay  = 0.45f;
    public float burstCooldown   = 12f;
    [Tooltip("Kameraya en yakın geçiş mesafesi")]
    public float cameraPassDistance = 10.5f;

    [Header("FX")]
    [Tooltip("Splash particle prefab – realistic default")]
    public GameObject splashFXPrefab;
    [Tooltip("Atlama sırasında arkada bırakılacak su damlası iz prefabı")]
    public GameObject rippleTrailPrefab;
    [Tooltip("Path to realistic splash prefab used if splashFXPrefab is not assigned")]
    public string splashPrefabPath = "Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_Multiple.prefab";

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
    private Vector3 _lStart, _lEnd; // Yerel koordinatlar (Coaster'a göre)
    private float   _jProgress, _jDuration, _jHeight;
    private bool    _isCinematic;

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
        
        // Load realistic splash prefab if not assigned in inspector
        if (splashFXPrefab == null && !string.IsNullOrEmpty(splashPrefabPath))
        {
            splashFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(splashPrefabPath);
        }
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

        // --- JET SENKRONİZASYONU ---
        // 3. savaş uçağı tam geçerken zıplaması için ayarlandı (~5.0s)
        yield return new WaitForSeconds(5.0f);

        int count = Random.Range(burstJumpCount - 1, burstJumpCount + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 end = CinematicEnd();
            BeginJump(end,
                jumpHeight * cinematicHeightMultiplier,
                cinematicDuration, true);

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
        float t = Mathf.Clamp01(_jProgress);
        float arc = 4f * _jHeight * t * (1f - t);
        
        Vector3 pos;
        float fishJumpScale = 1.0f;
        if (_isCinematic && _coasterTf != null)
        {
            // --- HAREKETLİ REFERANS (Coaster'a göre kilitli) ---
            pos = _coasterTf.TransformPoint(Vector3.Lerp(_lStart, _lEnd, t));
            pos.y = _basePos.y + arc;
            fishJumpScale = 6.0f; // Balığı görünür kılmak için 6 kat büyüttük
        }
        else
        {
            pos = Vector3.Lerp(_jStart, _jEnd, t);
            pos.y = _basePos.y + arc;
        }
        transform.localScale = Vector3.one * fishJumpScale;

        // ── Yönelme (Hareket yönüne bakış) ─────────────────────────
        Vector3 velocity = pos - transform.position;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            
            // Gerçekçi kıvrılma (balık kuyruk hareketi)
            float wiggle = Mathf.Sin(Time.time * 20f) * 12f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot * Quaternion.Euler(0, wiggle, 0), Time.deltaTime * 18f);
        }

        // ── Su damlası trail ───────────────────────────────────────
        _trailTimer += Time.deltaTime;
        if (_trailTimer > 0.04f)
        {
            _trailTimer = 0f;
            SpawnTrailDroplet(pos);
        }

        // ── Kamera splash (Sadece sinematik zıplamalarda ve yakınken) ────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null && !_hasScreenSplashed && _isCinematic)
        {
            float distToCam = Vector3.Distance(pos, mainCam.transform.position);
            // 11 metreden zıpladığı için menzili biraz geniş tuttuk
            if (distToCam < 18.0f)
            {
                SpawnCameraSplash(mainCam);
                _hasScreenSplashed = true;
            }
        }

        transform.position = pos;
    }

    /// <summary>
    /// Sıçrama arkasında su damlası/parçacık iz bırak
    /// </summary>
    void SpawnTrailDroplet(Vector3 fishPos)
    {
        if (rippleTrailPrefab == null) return;
        
        // Balığın arkasından (kuyruk yönü) birkaç damla serpintisi
        Vector3 behindDir = -transform.forward;
        Vector3 dropPos = fishPos + behindDir * 0.5f;
        
        // Su yüzeyine yakın damlalar da bırak
        Vector3 surfacePos = new Vector3(fishPos.x, _basePos.y + 0.08f, fishPos.z);
        
        // Havada iz
        if (fishPos.y > _basePos.y + 0.5f) 
        {
            var airDrop = Instantiate(rippleTrailPrefab, dropPos, Quaternion.identity);
            // Sadece sinematik zıplamalarda devasa efekt kullan
            float s = _isCinematic ? Random.Range(10.5f, 15.5f) : Random.Range(0.4f, 0.9f);
            airDrop.transform.localScale = Vector3.one * s; 
            Destroy(airDrop, _isCinematic ? 1.4f : 1.0f);
        }
        
        // Su yüzeyinde iz halkası
        var surfaceDrop = Instantiate(rippleTrailPrefab, surfacePos, Quaternion.identity);
        float ss = _isCinematic ? Random.Range(12.0f, 18.2f) : Random.Range(1.2f, 2.2f);
        surfaceDrop.transform.localScale = Vector3.one * ss; 
        Destroy(surfaceDrop, _isCinematic ? 2.2f : 1.5f);
    }

    /// <summary>
    /// Kameraya su damlaları  vurur ve yukarıdan aşağıya süzülür
    /// </summary>
    void SpawnCameraSplash(Camera cam)
    {
        if (splashFXPrefab == null) return;
        
        Transform camT = cam.transform;
        
        // Create a parent object to keep splash particles moving with the camera
        GameObject splashParent = new GameObject("CameraSplashParent");
        splashParent.transform.SetParent(camT);
        splashParent.transform.localPosition = Vector3.zero;
        splashParent.transform.localRotation = Quaternion.identity;
        
        // --- KRİTİK TEMİZLİK: Su damlacıklarının peşini bırakması için parent silinmeli ---
        Destroy(splashParent, 1.6f);
        
        Vector3 centerPos = camT.position + camT.forward * 0.45f + camT.up * 0.05f;
        var mainSplash = Instantiate(splashFXPrefab, centerPos, camT.rotation, splashParent.transform);
        mainSplash.transform.localScale = Vector3.one * 0.9f;
        // Süre 1.1 saniyeye indirildi (Görüşü kapatmaması için)
        StartCoroutine(DripDown(mainSplash, camT, 0.25f, 1.1f));
        
        // Additional droplets around centre for richer effect
        int dropCount = Random.Range(4, 7);
        for (int i = 0; i < dropCount; i++)
        {
            float offsetX = Random.Range(-0.25f, 0.25f);
            float offsetY = Random.Range(0.12f, 0.38f);
            Vector3 pos = camT.position + camT.forward * 0.5f + camT.right * offsetX + camT.up * offsetY;
            var dropFX = Instantiate(splashFXPrefab, pos, camT.rotation, splashParent.transform);
            dropFX.transform.localScale = Vector3.one * Random.Range(0.35f, 0.75f);
            float dripSpeed = Random.Range(0.18f, 0.5f);
            float dripDuration = Random.Range(0.9f, 1.3f); // Kısa süreli damlalar
            StartCoroutine(DripDown(dropFX, camT, dripSpeed, dripDuration));
        }
    }

    /// <summary>
    /// Su damlası kameranın önünde yukarıdan aşağıya doğru süzülür
    /// </summary>
    IEnumerator DripDown(GameObject fx, Transform cam, float dripSpeed, float duration)
    {
        if (fx == null) yield break;
        
        float elapsed = 0f;
        Vector3 localOffset = Vector3.zero;
        
        while (elapsed < duration && fx != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Stronger gravity curve for a smooth slide
            float gravity = dripSpeed * (1f + t * 3f);
            localOffset += Vector3.down * gravity * Time.deltaTime;
            
            // Random lateral sway
            float sway = Mathf.Sin(elapsed * 4f) * 0.025f * Time.deltaTime;
            localOffset += Vector3.right * sway;
            
            // Update position relative to camera each frame
            if (cam != null)
            {
                fx.transform.position = cam.position + cam.forward * 0.45f + cam.TransformDirection(localOffset);
                fx.transform.rotation = cam.rotation;
            }
            
            // Fade out scale gradually
            fx.transform.localScale = Vector3.Lerp(fx.transform.localScale, Vector3.one * 0.2f, t * 0.5f);
            
            yield return null;
        }
        
        if (fx != null) Destroy(fx);
    }

    public void BeginJump(Vector3 endPos, float h, float dur, bool cinematic = false)
    {
        _jStart = transform.position;
        _jEnd = endPos;
        _jHeight = h;
        _jDuration = dur;
        _jProgress = 0f;
        _isJumping = true;
        _isCinematic = cinematic;

        if (cinematic && _coasterTf != null)
        {
            // Coaster'a göre yerel koordinatları kaydet
            _lStart = _coasterTf.InverseTransformPoint(_jStart);
            _lEnd = _coasterTf.InverseTransformPoint(_jEnd);
        }

        SpawnSplash(_jStart);
        _hasScreenSplashed = false;
    }

    void EndJump()
    {
        _isJumping = false;
        _timer     = 0f;
        _basePos   = new Vector3(_jEnd.x, _basePos.y, _jEnd.z);
        SpawnSplash(_basePos);
        ScheduleNormalJump();
    }

    /// <summary>
    /// Sinematik atlama — Kameranın ÖNÜNDEN sağdan sola veya soldan sağa geçiş
    /// Balık kameranın bakış yönüne dik (perpendicular) olarak ark çizer
    /// </summary>
    Vector3 CinematicEnd()
    {
        Camera cam = Camera.main;
        if (cam == null && _coasterTf == null) return NormalEnd();
        
        // Kullanılacak referans: Kamera varsa kamera, yoksa coaster
        Transform refTf = cam != null ? cam.transform : _coasterTf;
        
        // Kameranın ileri yönü (yatay düzlem)
        Vector3 camForward = refTf.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.forward;
        camForward.Normalize();
        
        // Kameranın sağ yönü (bu, sağ-sol geçiş ekseni)
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward).normalized;
        
        // Kameranın önündeki geçiş noktası (10-12m arası mesafe istendi - Net görüş için 10.5m seçildi)
        Vector3 passPoint = refTf.position + camForward * 10.5f;
        passPoint.y = _basePos.y;
        
        // Sağa veya sola rastgele meyil (Sıkıcılığı önlemek için)
        float side = Vector3.Dot(transform.position - passPoint, camRight);
        float dir = side >= 0 ? 1f : -1f;
        
        // Geçiş en ve boyu
        float crossDist = jumpDistance * cinematicDistanceMultiplier;
        
        // Başlangıç: kameranın bir yanından
        Vector3 startPos = passPoint + camRight * (dir * crossDist * 0.5f);
        startPos.y = _basePos.y;
        
        // Balığı başlangıç noktasına taşı
        transform.position = startPos;
        _basePos = new Vector3(startPos.x, _basePos.y, startPos.z);
        
        // Bitiş: kameranın diğer yanına
        Vector3 endPos = passPoint + camRight * (-dir * crossDist * 0.5f);
        endPos.y = _basePos.y;
        
        return endPos;
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
        
        // Sadece sinematik zıplamalarda büyük splash kullan
        float s = _isCinematic ? Random.Range(10.5f, 15.5f) : Random.Range(1.0f, 2.5f);
        fx.transform.localScale = Vector3.one * s; 
        Destroy(fx, _isCinematic ? 3.5f : 2.0f);
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
