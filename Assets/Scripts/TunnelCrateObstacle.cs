using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tünel içindeki tahta kasa engellerini yönetir.
/// İlk çarpmada 1 kasa patlayıp coaster geri gider,
/// ikinci çarpmada kalan kasalar parçalanıp coaster geçer.
/// </summary>
public class TunnelCrateObstacle : MonoBehaviour
{
    // ── Inspector Referansları ──────────────────────────────────────
    [Header("Crate References")]
    [Tooltip("Tam (bütün) kasa objeleri – rayların üstüne yerleştirilmiş")]
    public List<GameObject> wholeCrates = new List<GameObject>();

    [Tooltip("Her kasa için karşılık gelen debris (parçalanmış) prefab'ları")]
    public List<GameObject> debrisPrefabs = new List<GameObject>();

    [Header("Coaster Reference")]
    [Tooltip("CoasterTrainController bileşeni")]
    public CoasterTrainController coasterController;

    [Header("Collision Settings")]
    [Tooltip("Çarpışma algılama mesafesi (metre)")]
    public float collisionDistance = 4f;

    [Tooltip("İlk çarpmada patlatılacak kasa indeksi (0 = ön kasa)")]
    public int firstBreakIndex = 0;

    [Header("Bounce Back")]
    [Tooltip("Geri gitme miktarı (spline t birimi cinsinden)")]
    public float bounceBackAmount = 0.008f;

    [Tooltip("Geri gitme süresi (saniye)")]
    public float bounceBackDuration = 1.2f;

    [Tooltip("Geri gittikten sonra bekleme süresi")]
    public float pauseAfterBounce = 1.0f;

    [Tooltip("İkinci saldırı hızlanma süresi")]
    public float chargeUpDuration = 1.5f;

    [Header("Explosion Settings")]
    [Tooltip("Patlama kuvveti")]
    public float explosionForce = 500f;

    [Tooltip("Patlama yarıçapı")]
    public float explosionRadius = 8f;

    [Tooltip("İkinci patlama kuvveti (daha büyük)")]
    public float secondExplosionForce = 800f;

    [Header("Audio")]
    [Tooltip("Kırılma sesleri")]
    public AudioClip[] breakSounds;

    // ── Dahili durum ────────────────────────────────────────────────
    private enum Phase { WaitingForFirstHit, BouncingBack, Pausing, ChargingUp, WaitingForSecondHit, SecondBreak, Done }
    private Phase currentPhase = Phase.WaitingForFirstHit;

    private Transform coasterTransform;
    private float originalSpeed;
    private float bounceTimer;
    private float pauseTimer;
    private float chargeTimer;
    private float bounceStartT;
    private float bounceTargetT;
    private bool initialized = false;

    void Start()
    {
        // CoasterTrainController'ı bul
        if (coasterController == null)
        {
            coasterController = FindFirstObjectByType<CoasterTrainController>();
        }

        if (coasterController != null)
        {
            coasterTransform = coasterController.transform;

            // trainRoot varsa onu kullan
            Transform trainRoot = coasterController.GetTrainRoot();
            if (trainRoot != null) coasterTransform = trainRoot;
        }

        initialized = (coasterController != null && wholeCrates.Count > 0);

        if (!initialized)
        {
            Debug.LogWarning("[TunnelCrateObstacle] Coaster veya kasalar bulunamadı, sistem devre dışı.");
        }
    }

    void Update()
    {
        if (!initialized) return;

        switch (currentPhase)
        {
            case Phase.WaitingForFirstHit:
                CheckFirstCollision();
                break;

            case Phase.BouncingBack:
                UpdateBounceBack();
                break;

            case Phase.Pausing:
                UpdatePause();
                break;

            case Phase.ChargingUp:
                UpdateChargeUp();
                break;

            case Phase.WaitingForSecondHit:
                CheckSecondCollision();
                break;

            case Phase.SecondBreak:
                // Tek frame'de işlenir, Done'a geçer
                break;

            case Phase.Done:
                // Artık hiçbir şey yapma
                break;
        }
    }

    // ── PHASE: İlk çarpma kontrolü ─────────────────────────────────
    private void CheckFirstCollision()
    {
        if (coasterTransform == null) return;

        // En yakın kasaya mesafe
        float minDist = float.MaxValue;
        foreach (var crate in wholeCrates)
        {
            if (crate == null) continue;
            float dist = Vector3.Distance(coasterTransform.position, crate.transform.position);
            if (dist < minDist) minDist = dist;
        }

        if (minDist <= collisionDistance)
        {
            PerformFirstBreak();
        }
    }

    // ── İlk kasa patlatma ──────────────────────────────────────────
    private void PerformFirstBreak()
    {
        // İlk kasayı patla
        int idx = Mathf.Clamp(firstBreakIndex, 0, wholeCrates.Count - 1);
        GameObject crateToBreak = wholeCrates[idx];

        if (crateToBreak != null)
        {
            // Debris spawn
            if (idx < debrisPrefabs.Count && debrisPrefabs[idx] != null)
            {
                SpawnDebris(crateToBreak, debrisPrefabs[idx], explosionForce);
            }

            // Ses çal
            PlayBreakSound(crateToBreak.transform.position);

            // Kasayı sil
            Destroy(crateToBreak);
            wholeCrates[idx] = null;
        }

        // Coaster geri gitme başlat
        originalSpeed = coasterController.GetCurrentSpeed();
        bounceStartT = coasterController.GetT();
        bounceTargetT = bounceStartT - bounceBackAmount;
        if (bounceTargetT < 0f) bounceTargetT += 1f; // Wrap around

        bounceTimer = 0f;
        coasterController.SetCurrentSpeed(0f);
        coasterController.SetExternalControl(true);

        currentPhase = Phase.BouncingBack;
        Debug.Log("[TunnelCrateObstacle] İlk çarpma! Kasa patladı, coaster geri gidiyor.");
    }

    // ── PHASE: Geri gitme animasyonu ───────────────────────────────
    private void UpdateBounceBack()
    {
        bounceTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(bounceTimer / bounceBackDuration);

        // Ease-out geri gitme
        float eased = 1f - (1f - progress) * (1f - progress);
        float newT = Mathf.Lerp(bounceStartT, bounceTargetT, eased);

        coasterController.SetT(newT);
        coasterController.SetCurrentSpeed(0f);

        if (progress >= 1f)
        {
            pauseTimer = 0f;
            currentPhase = Phase.Pausing;
            Debug.Log("[TunnelCrateObstacle] Geri gitme tamamlandı, bekleniyor...");
        }
    }

    // ── PHASE: Bekleme ─────────────────────────────────────────────
    private void UpdatePause()
    {
        pauseTimer += Time.deltaTime;
        coasterController.SetCurrentSpeed(0f);

        if (pauseTimer >= pauseAfterBounce)
        {
            chargeTimer = 0f;
            currentPhase = Phase.ChargingUp;
            Debug.Log("[TunnelCrateObstacle] Bekleme bitti, hızlanıyor...");
        }
    }

    // ── PHASE: Hızlanma ────────────────────────────────────────────
    private void UpdateChargeUp()
    {
        chargeTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(chargeTimer / chargeUpDuration);

        // Yavaştan hızlıya (ease-in)
        float easedSpeed = originalSpeed * 1.5f * progress * progress;
        coasterController.SetCurrentSpeed(easedSpeed);
        coasterController.SetExternalControl(false); // Hareket etsin

        if (progress >= 1f)
        {
            currentPhase = Phase.WaitingForSecondHit;
            Debug.Log("[TunnelCrateObstacle] Hızlanma tamamlandı, ikinci çarpma bekleniyor...");
        }
    }

    // ── PHASE: İkinci çarpma kontrolü ──────────────────────────────
    private void CheckSecondCollision()
    {
        if (coasterTransform == null) return;

        float minDist = float.MaxValue;
        foreach (var crate in wholeCrates)
        {
            if (crate == null) continue;
            float dist = Vector3.Distance(coasterTransform.position, crate.transform.position);
            if (dist < minDist) minDist = dist;
        }

        if (minDist <= collisionDistance)
        {
            PerformSecondBreak();
        }
    }

    // ── Kalan tüm kasaları patlatma ────────────────────────────────
    private void PerformSecondBreak()
    {
        for (int i = 0; i < wholeCrates.Count; i++)
        {
            if (wholeCrates[i] == null) continue;

            // Debris spawn
            if (i < debrisPrefabs.Count && debrisPrefabs[i] != null)
            {
                SpawnDebris(wholeCrates[i], debrisPrefabs[i], secondExplosionForce);
            }

            // Ses çal
            PlayBreakSound(wholeCrates[i].transform.position);

            Destroy(wholeCrates[i]);
            wholeCrates[i] = null;
        }

        // Coaster serbest bırak — boostered geçiş
        coasterController.SetCurrentSpeed(originalSpeed * 1.3f);
        coasterController.SetExternalControl(false);

        currentPhase = Phase.Done;
        Debug.Log("[TunnelCrateObstacle] İkinci çarpma! Tüm kasalar parçalandı, coaster geçiyor.");
    }

    // ── Debris spawn yardımcısı ────────────────────────────────────
    private void SpawnDebris(GameObject source, GameObject debrisPrefab, float force)
    {
        GameObject debris = Instantiate(debrisPrefab, source.transform.position, source.transform.rotation);
        debris.transform.localScale = source.transform.localScale;

        // URP materyal düzeltmesi (pembe görünüm engellenir)
        FixDebrisMaterials(debris);

        // Her parçaya patlama kuvveti uygula
        Rigidbody[] rbs = debris.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.AddExplosionForce(force, source.transform.position + Vector3.down * 0.5f, explosionRadius, 0.5f);
        }

        // 8 saniye sonra debris'i temizle
        Destroy(debris, 8f);

        // Patlama partikül efekti (runtime oluşturma)
        SpawnExplosionParticle(source.transform.position);
    }

    /// <summary>
    /// Runtime'da spawn edilen debris objelerinin pembe görünmesini engeller.
    /// Standard/Legacy shader'ları URP Lit'e çevirir.
    /// </summary>
    private void FixDebrisMaterials(GameObject obj)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            Material[] mats = r.materials; // Runtime'da .materials kullan (.sharedMaterials değil)
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string sn = (mats[i].shader != null) ? mats[i].shader.name : "";
                if (sn.StartsWith("Universal Render Pipeline")) continue;

                // Renk ve texture bilgisini shader değişmeden önce al
                Color col = Color.white;
                if (mats[i].HasProperty("_Color")) col = mats[i].GetColor("_Color");
                if (mats[i].HasProperty("_BaseColor")) col = mats[i].GetColor("_BaseColor");

                Texture tex = null;
                if (mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");
                if (tex == null && mats[i].HasProperty("_BaseMap")) tex = mats[i].GetTexture("_BaseMap");
                if (tex == null && mats[i].HasProperty("_BaseColorMap")) tex = mats[i].GetTexture("_BaseColorMap");

                // Normal map
                Texture normalTex = null;
                if (mats[i].HasProperty("_BumpMap")) normalTex = mats[i].GetTexture("_BumpMap");
                if (normalTex == null && mats[i].HasProperty("_NormalMap")) normalTex = mats[i].GetTexture("_NormalMap");

                // Shader değiştir
                mats[i].shader = urpLit;

                // URP Lit property'lerini ayarla
                if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", col);
                if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", col);
                if (tex != null)
                {
                    if (mats[i].HasProperty("_BaseMap")) mats[i].SetTexture("_BaseMap", tex);
                    if (mats[i].HasProperty("_MainTex")) mats[i].SetTexture("_MainTex", tex);
                }
                if (normalTex != null && mats[i].HasProperty("_BumpMap"))
                {
                    mats[i].SetTexture("_BumpMap", normalTex);
                    mats[i].EnableKeyword("_NORMALMAP");
                }

                changed = true;
            }
            if (changed) r.materials = mats;
        }
    }

    // ── Patlama partikül efekti ────────────────────────────────────
    private void SpawnExplosionParticle(Vector3 position)
    {
        GameObject fxObj = new GameObject("CrateExplosionFX");
        fxObj.transform.position = position;

        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 1.5f;
        main.startSpeed = 6f;
        main.startSize = 0.3f;
        main.startColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Ahşap rengi
        main.maxParticles = 40;
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        // Toz bulutu efekti
        GameObject dustObj = new GameObject("DustCloudFX");
        dustObj.transform.position = position;

        ParticleSystem dustPs = dustObj.AddComponent<ParticleSystem>();
        var dustMain = dustPs.main;
        dustMain.duration = 0.3f;
        dustMain.loop = false;
        dustMain.startLifetime = 2.5f;
        dustMain.startSpeed = 3f;
        dustMain.startSize = 2f;
        dustMain.startColor = new Color(0.5f, 0.4f, 0.3f, 0.4f); // Toz rengi
        dustMain.maxParticles = 15;
        dustMain.gravityModifier = -0.1f; // Hafif yukarı kalk
        dustMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var dustEmission = dustPs.emission;
        dustEmission.rateOverTime = 0;
        dustEmission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 12)
        });

        var dustShape = dustPs.shape;
        dustShape.shapeType = ParticleSystemShapeType.Sphere;
        dustShape.radius = 1f;

        var dustSize = dustPs.sizeOverLifetime;
        dustSize.enabled = true;
        dustSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 2f));

        // Oto temizle
        Destroy(fxObj, 3f);
        Destroy(dustObj, 4f);
    }

    // ── Ses çalma ──────────────────────────────────────────────────
    private void PlayBreakSound(Vector3 position)
    {
        if (breakSounds == null || breakSounds.Length == 0) return;

        AudioClip clip = breakSounds[Random.Range(0, breakSounds.Length)];
        if (clip == null) return;

        GameObject audioObj = new GameObject("CrateBreakAudio");
        audioObj.transform.position = position;
        AudioSource src = audioObj.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f; // 3D ses
        src.volume = 0.8f;
        src.pitch = Random.Range(0.85f, 1.15f);
        src.Play();
        Destroy(audioObj, clip.length + 0.5f);
    }
}
