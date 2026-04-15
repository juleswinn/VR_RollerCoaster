using UnityEngine;
using System.Collections;

/// <summary>
/// Pistin son bölgesinde havai fişek patlatır.
/// VR kamera yaklaşınca tetiklenir, sol ve sağdan ardışık fişekler fırlatılır.
/// VFX Graph asset bulunamazsa, ParticleSystem ile oluşturulur.
/// </summary>
public class FireworksFinale : MonoBehaviour
{
    [Header("Trigger")]
    public float triggerDistance = 200f;
    
    [Header("Fireworks Setup")]
    public GameObject fireworkPrefab;     // VFX Graph prefab (isteğe bağlı)
    public int totalBursts = 8;           // Toplam patlama sayısı
    public float burstInterval = 1.5f;    // Patlamalar arası süre
    public float launchHeight = 60f;      // Patlama yüksekliği
    public float sideOffset = 45f;        // Sağ/sol mesafe
    
    [Header("Audio")]
    public AudioClip fireworkSound;
    
    private Transform _target;

    // YENİ SİSTEM: Spline üzerinde fiziksel değerlendirilen tam noktalar
    public Vector3 endPos;
    public Vector3 halfwayPos;

    private bool _halfwayReached = false;
    private bool _triggered = false;
    private float _elapsedTime = 0f;

    void Start()
    {
        if (_target == null)
        {
            Camera cam = Camera.main;
            if (cam != null) _target = cam.transform;
        }
    }

    void Update()
    {
        if (_triggered) return;
        if (_target == null) return;

        _elapsedTime += Time.deltaTime;

        // 1. AŞAMA: Önce pisti yarılamış olması ŞART (Oyuna başlar başlamaz Bitiş tetiklenmesin diye)
        if (!_halfwayReached)
        {
            if (Vector3.Distance(_target.position, halfwayPos) < 150f)
            {
                _halfwayReached = true;
                Debug.Log("[FireworksFinale] Halfway point reached!");
            }
        }
        else
        {
            // 2. AŞAMA: Yarıladıktan sonra Bitiş'e (İstasyona) 250 metreden daha yakın olduğu an PATLAT!
            // Ekstra güvenlik: En az 30 saniye geçmiş olsun.
            if (_elapsedTime > 30f && Vector3.Distance(_target.position, endPos) < 250f)
            {
                _triggered = true;
                StartCoroutine(FireworksSequence());
            }
        }

        // Failsafe: 60 saniyede mutlaka havai fişek patlar
        if (_elapsedTime > 60f && !_triggered)
        {
            _triggered = true;
            StartCoroutine(FireworksSequence());
        }
    }

    IEnumerator FireworksSequence()
    {
        Debug.Log($"[FireworksFinale] TRIGGERED! Spawning {totalBursts} bursts. Distance to target: {Vector3.Distance(transform.position, _target.position)}");

        for (int i = 0; i < totalBursts; i++)
        {
            // KAMERA ODAKLI GARANTİ ÇÖZÜM: Havai fişekler istasyonun değil, DOĞRUDAN KULLANICININ BAKIŞ AÇISININ
            // içine (tam önüne) spawn olacak. Gözükmeme ihtimali sıfır.
            Vector3 refPos = _target != null ? _target.position : transform.position;
            Vector3 camRight = _target != null ? _target.right : Vector3.right;
            Vector3 camForward = _target != null ? _target.forward : Vector3.forward;
            
            camForward.y = 0; 
            camForward.Normalize();

            float side = (i % 2 == 0) ? -sideOffset : sideOffset;
            
            // Kameranın 60-90m ilerisinde (tam önünde), 30-60m hafif yukarısında. SADECE GÖRÜŞ AÇISI İÇİ.
            Vector3 launchPos = refPos
                + camForward * Random.Range(60f, 95f)
                + camRight * side
                + Vector3.up * Random.Range(30f, 60f)
                + Random.insideUnitSphere * 15f;


            if (fireworkPrefab != null)
            {
                // VFX Graph prefab kullan
                GameObject fw = Instantiate(fireworkPrefab, launchPos, Quaternion.identity);
                Destroy(fw, 6f);
            }
            else
            {
                // Fallback: ParticleSystem ile havai fişek oluştur
                SpawnParticleFirework(launchPos, i);
            }

            // Patlama sesi
            if (fireworkSound != null)
            {
                AudioSource.PlayClipAtPoint(fireworkSound, launchPos, 1f);
            }
            else
            {
                // Ses yoksa basit bir boom
                GameObject sfx = new GameObject("FW_SFX");
                sfx.transform.position = launchPos;
                AudioSource src = sfx.AddComponent<AudioSource>();
                src.spatialBlend = 1f;
                src.maxDistance = 500f;
                src.volume = 0.8f;
                // Ses dosyası olmadan sadece pozisyon marker, gerçek ses asset'ten gelecek
                Destroy(sfx, 3f);
            }

            yield return new WaitForSeconds(burstInterval + Random.Range(-0.3f, 0.3f));
        }
    }

    void SpawnParticleFirework(Vector3 pos, int index)
    {
        GameObject fwObj = new GameObject("Firework_" + index);
        fwObj.transform.position = pos;

        Color[] palette = {
            new Color(1f, 0.2f, 0.1f),
            new Color(0.1f, 0.8f, 1f),
            new Color(1f, 0.85f, 0.1f),
            new Color(0.6f, 0.1f, 1f),
            new Color(0.1f, 1f, 0.3f),
            new Color(1f, 0.5f, 0f),
            new Color(1f, 0.1f, 0.6f),
        };
        Color mainColor = palette[index % palette.Length];
        Color secondColor = palette[(index + 3) % palette.Length];

        // ParticleSystem — DEFAULT MATERYALİ KULLAN (Shader.Find runtime'da çöker!)
        ParticleSystem ps = fwObj.AddComponent<ParticleSystem>();
        
        // Önce STOP et — aktifken main değiştirmek hata verir
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 45f);
        main.startSize = new ParticleSystem.MinMaxCurve(5.0f, 10.0f); // Çok büyük, uzaktan görülebilir
        main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondColor);
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 350, 600) 
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;

        // Renk solma
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(mainColor, 0f), 
                new GradientColorKey(secondColor, 0.5f), 
                new GradientColorKey(Color.white * 0.2f, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.7f, 0.5f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        col.color = grad;

        // Boyut küçülme
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        // Trail DEVRE DIŞI — "duration while playing" hatasını önler
        var trails = ps.trails;
        trails.enabled = false;

        ParticleSystemRenderer rend = fwObj.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        
        // PEMBE GÖRÜNME VE GÖRÜNMEZLİK SORUNU İÇİN URP LIT ÇÖZÜMÜ:
        // Sprites shader'ı tünelde sorun yaratmış olabilir, bu yüzden mağaradakilerle (kristallerle vb) aynı,
        // %100 her yerde çalışan "Universal Render Pipeline/Lit" shader'ı kullanıyoruz.
        Shader safeShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (safeShader == null) safeShader = Shader.Find("Universal Render Pipeline/Lit");
        if (safeShader == null) safeShader = Shader.Find("Particles/Standard Unlit");

        if (safeShader != null)
        {
            Material safeMat = new Material(safeShader);
            if (safeMat.HasProperty("_BaseColor")) safeMat.SetColor("_BaseColor", mainColor);
            if (safeMat.HasProperty("_Color")) safeMat.SetColor("_Color", mainColor);
            // Particle Alpha'nın çalışabilmesi için surface type vb ayarları URP'de Transparent yapılabilir
            safeMat.SetFloat("_Surface", 1); // 1 = Transparent
            safeMat.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply vs.
            rend.material = safeMat;
        }

        // Parlama ışığı
        GameObject lightObj = new GameObject("FW_Light");
        lightObj.transform.SetParent(fwObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light fwLight = lightObj.AddComponent<Light>();
        fwLight.type = LightType.Point;
        fwLight.color = mainColor;
        fwLight.intensity = 20f;
        fwLight.range = 200f;

        Debug.Log($"[Firework_{index}] Spawned at {pos} color={mainColor}");
        ps.Play();
        Destroy(fwObj, 6f);
    }
}
