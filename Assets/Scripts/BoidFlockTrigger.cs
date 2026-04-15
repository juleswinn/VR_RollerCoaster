using UnityEngine;

/// <summary>
/// Vagon yaklaşınca kuş sürüsünü panik moduna sokan tetikleyici.
/// NVBoids'un danger modu yerine, doğrudan hız ve dağılma parametrelerini değiştirir.
/// </summary>
public class BoidFlockTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public float triggerRadius = 40f;     // Tetikleme mesafesi ÇOK kısaltıldı, tam üstünden geçerken!
    public float panicSpeed = 8f;         // Panik hızı
    public float normalSpeed = 2f;        // Normal hız
    public float panicSoaring = 2.5f;     // Panik dönüş hızı
    public float normalSoaring = 0.5f;    // Normal dönüş
    public AudioClip scatterSound;        // Kuş sesi dosyası
    
    private NVBoids _boids;
    private Transform _target;
    private bool _panicked;
    private float _calmTimer;
    private AudioSource _audioSource;

    void Start()
    {
        _boids = GetComponent<NVBoids>();
        
        // VR kamerayı bul
        Camera cam = Camera.main;
        if (cam != null) _target = cam.transform;

        if (scatterSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = scatterSound;
            _audioSource.spatialBlend = 1f; // Tamamen 3D ses
            _audioSource.maxDistance = 300f; // Geniş alan
            _audioSource.volume = 1f;
        }
    }

    void Update()
    {
        if (_boids == null || _target == null)
        {
            if (_target == null)
            {
                Camera cam = Camera.main;
                if (cam != null) _target = cam.transform;
            }
            return;
        }

        float dist = Vector3.Distance(transform.position, _target.position);

        if (dist < triggerRadius && !_panicked)
        {
            // PANIK! Kuşlar dağılıyor
            _panicked = true;
            _calmTimer = 0f;
            _boids.birdSpeed = panicSpeed;
            _boids.soaring = panicSoaring;
            _boids.fragmentedFlock = 120;    // Sürü çok dağılsın
            _boids.fragmentedBirds = 40;     // Bireysel kuşlar uzaklaşsın

            // Ses çal (ilk 1 saniyeyi atla — direkt kanat sesi)
            if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.time = 1f;
                _audioSource.Play();
            }
        }
        else if (_panicked)
        {
            _calmTimer += Time.deltaTime;
            
            // 8 saniye sonra sakinleşsinler
            if (_calmTimer > 8f)
            {
                _boids.birdSpeed = Mathf.Lerp(_boids.birdSpeed, normalSpeed, Time.deltaTime * 0.5f);
                _boids.soaring = Mathf.Lerp(_boids.soaring, normalSoaring, Time.deltaTime * 0.5f);
                _boids.fragmentedFlock = (int)Mathf.Lerp(_boids.fragmentedFlock, 30, Time.deltaTime * 0.5f);
                _boids.fragmentedBirds = (int)Mathf.Lerp(_boids.fragmentedBirds, 10, Time.deltaTime * 0.5f);

                if (dist > triggerRadius * 2f)
                {
                    _panicked = false;
                }
            }
        }
    }
}
