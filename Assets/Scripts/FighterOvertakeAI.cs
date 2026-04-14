using UnityEngine;
using System.Collections;

/// <summary>
/// Prototip: Arkadan gelip coaster'ı geçen savaş uçağı yapay zekası.
/// Çok daha sinematik ve profesyonel bir flyby hissi verir.
/// </summary>
public class FighterOvertakeAI : MonoBehaviour
{
    public float startDelay = 0f;
    public float speed = 320f;
    public float passDelay = 0.5f; // Tam geçtiği an sarsıntı için ince ayar
    public float shakeIntensity = 0.85f;
    public float bankingIntensity = 45f;

    private Transform _target;
    private bool _started = false;
    private bool _shaken = false;
    private float _timer = 0f;
    private Vector3 _velocity;

    [Header("Audio")]
    public AudioClip jetSound;

    void Start()
    {
        SetVisibility(false);
        var cam = Camera.main;
        if (cam != null) _target = cam.transform;
        
        if (jetSound != null)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = jetSound;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Custom; // Özel mesafe kodu için
            src.dopplerLevel = 2.8f; // Gürültü hissi için artırıldı
            src.volume = 0f;
            src.playOnAwake = false; // We start it when the jet actuates
        }
    }

    void Update()
    {
        if (_target == null) return;

        if (!_started)
        {
            float approachDist = Vector3.Distance(transform.position, _target.position);
            // Sadece yaklaşınca çalışsın ki arkada boş yere geçip gitmesin
            if (approachDist < 800f)
            {
                _timer += Time.deltaTime;
                if (_timer >= startDelay)
                {
                    _started = true;
                    SetVisibility(true);
                    _velocity = transform.forward * speed;
                    
                    // Sesi tam hareket anında tetikle
                    AudioSource srcc = GetComponent<AudioSource>();
                    if (srcc != null) {
                        srcc.Play();
                    }
                }
            }
            else return;
        }

        // Mesafe bazlı manuel ses volümü yönetimi
        AudioSource src = GetComponent<AudioSource>();
        if (src != null && _started)
        {
            float d = Vector3.Distance(transform.position, _target.position);
            // 600 metrede ses 0.15, 100 metreye geldiğinde 1.0 (Dramatik peak)
            float vol = Mathf.Clamp01(1.0f - ((d - 100f) / 500f));
            src.volume = Mathf.Max(vol, 0.15f); // Minimum 0.15 — her zaman duyulur
        }

        // --- İLERLEME (DOSDOĞRU - Straight Flight) ---
        transform.position += _velocity * Time.deltaTime;

        // --- DRAMATİK MANEVRA İPTAL (Sadece düz gidiş istendi) ---
        // Kullanıcı uçağın 360 dönmesini veya kavis çizmesini istemediği için banking kaldırıldı.

        if (_target == null) return;

        // --- SARSINTI TETİKLEME (Tam kulak hizasından geçerken) ---
        float dot = Vector3.Dot(_target.forward, transform.position - _target.position);
        float dist = Vector3.Distance(transform.position, _target.position);

        // 7D Sinema için sarsıntı (SimpleEnvironmentBuilder'dan gelen ayarı kullan)
        if (!_shaken && _started && dot > 3f && dist < 300f) 
        {
            _shaken = true;
            if (CoasterShakeEffect.Instance != null && shakeIntensity > 0.05f)
                CoasterShakeEffect.Instance.Shake(1.2f, shakeIntensity);
        }

        // Çok uzaklaşırsa sil
        if (dist > 5500f) Destroy(gameObject);
    }

    private void SetVisibility(bool visible)
    {
        var rs = GetComponentsInChildren<Renderer>();
        foreach (var r in rs) r.enabled = visible;
    }
}
