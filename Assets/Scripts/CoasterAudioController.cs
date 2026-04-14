using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CoasterAudioController : MonoBehaviour
{
    private AudioSource _src;
    private Vector3 _lastPos;

    void Start()
    {
        _src = GetComponent<AudioSource>();
        _lastPos = transform.position;
    }

    void Update()
    {
        if (_src == null || _src.clip == null) return;

        // Kinematik arabalar için direkt pozisyon üzerinden hız hesaplanması
        float frameDist = (transform.position - _lastPos).magnitude;
        float speed = Time.deltaTime > 0f ? (frameDist / Time.deltaTime) : 0f;
        _lastPos = transform.position;
        
        if (speed > 1.5f)
        {
            if (!_src.isPlaying) _src.Play();
            _src.volume = Mathf.Lerp(_src.volume, 0.75f, Time.deltaTime * 3f);
            
            // Dinamik pitch kayması (hız arttıkça ses inceden cıyaklar, rüzgar/ray hissiyatı)
            _src.pitch = Mathf.Lerp(_src.pitch, 0.85f + (speed / 50f), Time.deltaTime * 2f);

            // Bypass silent tail in audio safely
            if (_src.time >= _src.clip.length - 0.2f)
            {
                _src.time = 0.05f; // loop back seamlessly
            }
        }
        else
        {
            _src.volume = Mathf.Lerp(_src.volume, 0f, Time.deltaTime * 6f);
            _src.pitch = Mathf.Lerp(_src.pitch, 0.8f, Time.deltaTime * 4f);
        }
    }
}
