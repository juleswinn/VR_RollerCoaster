using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JumpingFish : MonoBehaviour
{
    [Header("Cinematic Limits")]
    public static int globalJumpCount = 0;
    private static float lastGlobalJumpTime = 0f;
    private const int MAX_JUMPS = 12;
    private const float JUMP_COOLDOWN = 6f;

    [Header("Visual & FX")]
    public GameObject splashFXPrefab;
    public AudioClip jumpSound;
    private static Transform _coasterTf;
    
    // Arkadan su damlaciklari UI (Sprite/Graphic yerine direkt olarak Camera Child GameObjects ile halledecegiz)
    private static GameObject screenDropPrefab;

    void Start()
    {
        if (_coasterTf == null) StartCoroutine(FindCoaster());
    }

    IEnumerator FindCoaster()
    {
        while (_coasterTf == null)
        {
            Camera c = Camera.main;
            if (c != null) _coasterTf = c.transform;
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (_coasterTf == null) return;
        
        // Bu baligi baz aliyor muyuz (mesafe sarti)
        float dist = Vector3.Distance(transform.position, _coasterTf.position);
        
        // Eger hedefe (roller coaster'a) 120 birimden yakinsa ve jump limitine ulasilmamissa
        if (dist < 120f && globalJumpCount < MAX_JUMPS && (Time.time - lastGlobalJumpTime) > JUMP_COOLDOWN)
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

        // Dev form
        fishVisual.transform.localScale = Vector3.one * 5.0f;
        
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

        // Baslangic ve Bitis Koordinatlari (Kameranin tam karsisina diklemesine kesik bir parabol)
        Vector3 camFwd = _coasterTf.forward;
        Vector3 camRt = _coasterTf.right;
        Vector3 camUp = _coasterTf.up;
        Vector3 centerFocus = _coasterTf.position + camFwd * 18f; // 18 metre onunde

        // Tam ekrana paralel, goz onunden gecmeyen - hafif asagidan
        float dirSign = Random.value > 0.5f ? 1f : -1f;
        Vector3 startP = centerFocus + camRt * 12f * dirSign - camUp * 4f;
        Vector3 endP = centerFocus - camRt * 12f * dirSign - camUp * 4f;

        float duration = 1.6f;
        float elapsed = 0f;

        fishVisual.SetActive(true);
        StartCoroutine(SpawnSimpleDroplets(duration * 0.45f));

        // Koca yuvarlak su parcaciklarini (ön kameraya batanları) sildik! 
        // Onun yerine direkt sudan firlayan baligin arkasindan su izi birakmasi (Splash/Water trail)
        TrailRenderer tr = fishVisual.AddComponent<TrailRenderer>();
        tr.time = 0.5f;
        tr.startWidth = 0.4f;
        tr.endWidth = 0f;
        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Sprites/Default");
        if (trailShader == null) trailShader = Shader.Find("Universal Render Pipeline/Lit");
        tr.material = new Material(trailShader);
        tr.material.color = new Color(0.6f, 0.85f, 0.95f, 0.55f); // Şeffaf su mavisinde iz

        Vector3 previousPos = startP;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // X ve Z de dogrusal gecis
            Vector3 currentPos = Vector3.Lerp(startP, endP, t);
            
            // Y ekseninde Parabolik sicrama (Max yukseklik 15 birim)
            float arc = 4f * 15f * t * (1f - t); 
            currentPos.y += arc;

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

        // isimiz bitti, parcalanmayi yok et
        Destroy(fishVisual);
    }

    IEnumerator SpawnSimpleDroplets(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Transform targetCam = Camera.main != null ? Camera.main.transform : _coasterTf;
        
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
