using UnityEngine;

/// <summary>
/// Gökyüzünde süzülen uçakları yöneten basit AI.
/// – İleri doğru sabit hızla hareket eder.
/// – Belli bir loop mesafesini geçince başlangıca döner.
/// </summary>
public class AmbientAircraftAI : MonoBehaviour
{
    public float speed = 40f;
    public float loopDistance = 1200f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // İleri git (Local forward)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Mesafe kontrolü
        float dist = Vector3.Distance(transform.position, _startPos);
        if (dist > loopDistance)
        {
            // Başa sar (Sürekli akış hissi)
            transform.position = _startPos;
        }
    }
}
