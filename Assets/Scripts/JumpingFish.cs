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
    public float cinematicHeightMultiplier   = 3.5f;
    public float cinematicDistanceMultiplier = 2.5f;
    public float cinematicDuration           = 1.0f;
    public int   burstJumpCount  = 6;
    public float burstJumpDelay  = 0.45f;
    public float burstCooldown   = 12f;
    [Tooltip("Kameraya en yakın geçiş mesafesi (kameranın önünde) – daha yakın için küçült)")]
    public float cameraPassDistance = 6f;

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

        int count = Random.Range(burstJumpCount - 1, burstJumpCount + 1);

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

        // ── Gerçekçi parabolik ark ──────────────────────────────────
        // Zirve noktasını arc'ın ortasında tut, iniş öncesi hızlanma ile
        float t = _jProgress;
        float arc = 4f * _jHeight * t * (1f - t);

        // Hafif asimetri: zirveyi biraz öne çek (0.45 civarı)
        float asymT = Mathf.Pow(t, 0.9f);
        Vector3 pos = Vector3.Lerp(_jStart, _jEnd, asymT);
        pos.y = _basePos.y + arc;

        // ── Balık vücut yönelimi — hareket yönüne bak + kıvrılma ─────
        Vector3 velocity = pos - transform.position;
        if (velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            
            // Gerçekçi kıvrılma: yüzme hissi
            float wiggleFreq = 22f;
            float wiggleAmp = Mathf.Lerp(20f, 8f, t); // Başlangıçta güçlü, zirveye yakın azal
            float wiggle = Mathf.Sin(Time.time * wiggleFreq) * wiggleAmp;
            
            // Eğim: yükselirken yukarı, inerken aşağı bak
            float pitchFromArc = Mathf.Atan2(velocity.y, new Vector2(velocity.x, velocity.z).magnitude) * Mathf.Rad2Deg;
            targetRot *= Quaternion.Euler(0, wiggle, 0);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }

        // ── Su damlası trail — sıçrama boyunca arkada bırak ──────────
        _trailTimer += Time.deltaTime;
        if (_trailTimer > 0.04f) // Daha sık (daha yoğun iz)
        {
            _trailTimer = 0f;
            SpawnTrailDroplet(pos);
        }

        // ── Kamera splash — kameraya yakınsa su vur ──────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null && !_hasScreenSplashed)
        {
            float distToCam = Vector3.Distance(pos, mainCam.transform.position);
            if (distToCam < 8f)
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
        if (fishPos.y > _basePos.y + 0.5f) // Su yüzeyinin üzerindeyse
        {
            var airDrop = Instantiate(rippleTrailPrefab, dropPos, Quaternion.identity);
            airDrop.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);
            Destroy(airDrop, 1.2f);
        }
        
        // Su yüzeyinde iz halkası
        var surfaceDrop = Instantiate(rippleTrailPrefab, surfacePos, Quaternion.identity);
        surfaceDrop.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
        Destroy(surfaceDrop, 1.8f);
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
        
        // Main splash at camera centre
        Vector3 centerPos = camT.position + camT.forward * 0.45f + camT.up * 0.05f;
        var mainSplash = Instantiate(splashFXPrefab, centerPos, camT.rotation, splashParent.transform);
        mainSplash.transform.localScale = Vector3.one * 0.9f;
        StartCoroutine(DripDown(mainSplash, camT, 0.25f, 3.0f));
        
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
            float dripDuration = Random.Range(2.0f, 3.5f);
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
        
        // Kameranın önündeki geçiş noktası
        Vector3 passPoint = refTf.position + camForward * cameraPassDistance;
        passPoint.y = _basePos.y;
        
        // Sağdan sola mı soldan sağa mı? Rastgele veya balığın konumuna göre
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
        // Splash'ı kameraya doğru yönlendir
        if (Camera.main != null) fx.transform.LookAt(Camera.main.transform.position);
        fx.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
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
