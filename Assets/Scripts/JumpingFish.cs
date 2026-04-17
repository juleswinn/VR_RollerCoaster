using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JumpingFish : MonoBehaviour
{
    public static int globalJumpCount = 0;
    private static float lastGlobalJumpTime = 0f;
    private const int MAX_JUMPS = 6;
    private const float TRIGGER_DISTANCE = 35f; // Sadece tam göletin yanından geçerken tetiklenir
    private const float JUMP_COOLDOWN = 15f; // Aynı gölette spamlanmayı önler, farklı göletlere dağıtır

    [Header("Visual & FX")]
    public GameObject splashFXPrefab;
    public AudioClip jumpSound;
    private static Transform _coasterTf;
    
    private static GameObject screenDropPrefab;
    private static CoasterTrainController _ctc;

    void Start()
    {
        if (_coasterTf == null) StartCoroutine(FindCoaster());
    }

    IEnumerator FindCoaster()
    {
        while (_coasterTf == null)
        {
            _ctc = FindFirstObjectByType<CoasterTrainController>();
            if (_ctc != null) _coasterTf = _ctc.transform;
            else 
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams.Length > 0) _coasterTf = cams[0].transform;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (_coasterTf == null) return;
        
        // Bu baligi baz aliyor muyuz (mesafe sarti)
        float dist = Vector3.Distance(transform.position, _coasterTf.position);
        
        bool inTunnel = _ctc != null && _ctc.IsInTunnel();

        // Pist etrafındaki 4-5 farklı gölette (yakınından geçerken) toplam 6 kez tetiklenecek, ancak tünelde asla tetiklenmeyecek!
        if (!inTunnel && dist < TRIGGER_DISTANCE && globalJumpCount < MAX_JUMPS && (Time.time - lastGlobalJumpTime) > JUMP_COOLDOWN)
        {
            globalJumpCount++;
            lastGlobalJumpTime = Time.time;
            StartCoroutine(ExecuteCinematicJump());
        }
    }

    IEnumerator ExecuteCinematicJump()
    {
        // Gorsel olarak bizzat bu prefabin ikizini uret ki GetChild hatasi (Null error) uretmesin
        GameObject fishVisual = Instantiate(gameObject, transform.position, transform.rotation);
        
        // Klonladigimiz bu yeni objenin uzerindeki hareket scriptini sil ki kendi kendine tekrar calismasin
        JumpingFish cloneScript = fishVisual.GetComponent<JumpingFish>();
        if (cloneScript != null) Destroy(cloneScript);

        // İstenen normal / hafif büyük boyutta (Devasa değil)
        fishVisual.transform.localScale = Vector3.one * 2.0f;
        
        // Ses calmak
        if (jumpSound != null)
        {
            GameObject sfxObj = new GameObject("JumpSFX");
            sfxObj.transform.position = _coasterTf.position; // Tam kulaginda calsin
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.clip = jumpSound;
            src.volume = 1f;
            src.Play();
            Destroy(sfxObj, 3f);
        }

        float dirSign = Random.value > 0.5f ? 1f : -1f;

        // Kameranın aşağısından fırlayacak
        if (splashFXPrefab != null) {
            Vector3 initP = _coasterTf.position + _coasterTf.forward * 4.5f - _coasterTf.up * 4f;
            Destroy(Instantiate(splashFXPrefab, initP, Quaternion.identity), 3f);
        }

        float duration = 1.6f; // 1.6 saniye sürsün, net görülsün
        float elapsed = 0f;

        fishVisual.SetActive(true);
        StartCoroutine(SpawnSimpleDroplets(duration * 0.45f));

        TrailRenderer tr = fishVisual.AddComponent<TrailRenderer>();
        tr.time = 0.6f;
        tr.startWidth = 0.5f;
        tr.endWidth = 0f;
        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Universal Render Pipeline/Lit");
        if (trailShader != null) {
            tr.material = new Material(trailShader);
            tr.material.color = new Color(0.6f, 0.85f, 0.95f, 0.65f);
        }

        Vector3 previousPos = _coasterTf.position; // Sadece tracking için
        while(elapsed < duration)
        {
            if (_coasterTf == null) break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Hızla ilerleyen vagona yapışık hareket koordinatları! 
            Vector3 camPos = _coasterTf.position;
            Vector3 cfwd = _coasterTf.forward;
            Vector3 crt = _coasterTf.right;
            Vector3 cup = _coasterTf.up;

            // X offset: Sağa/sola doğru hafif bir kavis
            float xOffset = Mathf.Lerp(2.5f * dirSign, -2.5f * dirSign, t);
            
            // Y offset: VR Kameranın Altından -> Tam Gözüne -> Geri Altına uçuş (mükemmel yunus atlayışı)
            float arc = 4f * 8f * t * (1f - t); // Max peak is 8 at t=0.5
            float yOffset = -5f + arc; // Starts slightly below viewport, peaks right in middle

            // Z offset: Burnun 4.5-5 metre önü
            float zOffset = Mathf.Lerp(4.5f, 5.5f, t);

            Vector3 currentPos = camPos + cfwd * zOffset + crt * xOffset + cup * yOffset;
            fishVisual.transform.position = currentPos;
            
            // Hareket yönüne gerçekçi bakış atma
            Vector3 moveDir = (currentPos - previousPos);
            if (moveDir.sqrMagnitude > 0.001f)
            {
                fishVisual.transform.rotation = Quaternion.LookRotation(moveDir.normalized);
            }
            
            previousPos = currentPos;
            yield return null;
        }

        // Bitiş su sıçraması (Son ulaşılan konuma koy)
        if (splashFXPrefab != null) {
            Destroy(Instantiate(splashFXPrefab, fishVisual.transform.position, Quaternion.identity), 3f);
        }

        // isimiz bitti, parcalanmayi yok et
        Destroy(fishVisual);
    }

    IEnumerator SpawnSimpleDroplets(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Transform targetCam = _coasterTf;
        if (targetCam == null)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) targetCam = cams[0].transform;
        }
        
        int dropAmount = Random.Range(3, 6);
        for (int i = 0; i < dropAmount; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(drop.GetComponent<Collider>());
            drop.name = "WaterDrop";
            drop.transform.SetParent(targetCam, false);
            
            // Tam lensin onunde
            drop.transform.localPosition = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.1f, 0.3f), 0.5f);
            
            float size = Random.Range(0.015f, 0.035f);
            drop.transform.localScale = new Vector3(size, size * 1.5f, size * 0.2f);
            
            Renderer r = drop.GetComponent<Renderer>();
            Shader dropShader = Shader.Find("Universal Render Pipeline/Lit");
            if (dropShader == null) dropShader = Shader.Find("Standard");
            Material m = new Material(dropShader);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1); // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            // Seffaf camsi mavi
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.6f, 0.8f, 0.95f, 0.55f));
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.6f, 0.8f, 0.95f, 0.55f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.95f);
            r.material = m;

            StartCoroutine(SlideCustomDrop(drop));
        }
    }

    IEnumerator SlideCustomDrop(GameObject drop)
    {
        float dur = Random.Range(1.2f, 2.0f);
        float elapsed = 0f;
        Vector3 startLoc = drop.transform.localPosition;
        Vector3 endLoc = startLoc - new Vector3(0, Random.Range(0.4f, 0.7f), 0);
        Vector3 initialScale = drop.transform.localScale;
        
        while (elapsed < dur && drop != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float curve = t * t; 
            drop.transform.localPosition = Vector3.Lerp(startLoc, endLoc, curve);
            
            drop.transform.localScale = new Vector3(
                Mathf.Lerp(initialScale.x, 0f, curve),
                Mathf.Lerp(initialScale.y, initialScale.y * 1.2f, curve), 
                initialScale.z
            );
            
            yield return null;
        }
        if (drop != null) Destroy(drop);
    }
}
