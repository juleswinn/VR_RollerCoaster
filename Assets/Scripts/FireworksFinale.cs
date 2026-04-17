using UnityEngine;
using System.Collections;

/// <summary>
/// Pistin son bölgesinde havai fişek patlatır.
/// CoasterTrainController'ın t değerine göre tetiklenir (bitiş 3 sn öncesi).
/// </summary>
public class FireworksFinale : MonoBehaviour
{
    [Header("Fireworks Setup")]
    public int totalBursts = 8;
    public float burstInterval = 1.2f;
    public float launchHeight = 60f;
    public float sideOffset = 45f;
    
    [Header("Trigger")]
    [Tooltip("Havai fişeklerin başlayacağı t değeri (0-1 arası, 1=bitiş)")]
    public float triggerT = 0.96f;
    
    [Header("Audio")]
    public AudioClip fireworkSound;
    
#if UNITY_EDITOR
    void OnValidate()
    {
        if (fireworkSound == null)
            fireworkSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/569846__danlucaz__fireworks-1.wav");
    }
#endif

    private Transform _target;
    private CoasterTrainController _coasterController;
    private bool _triggered = false;

    void Start()
    {
        var train = FindFirstObjectByType<CoasterTrainController>();
        if (train != null) _target = train.transform;
        else 
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) _target = cams[0].transform;
        }
        
        _coasterController = FindFirstObjectByType<CoasterTrainController>();
    }

    void Update()
    {
        if (_triggered) return;
        if (_coasterController == null)
        {
            _coasterController = FindFirstObjectByType<CoasterTrainController>();
            return;
        }
        if (_target == null)
        {
            var train = FindFirstObjectByType<CoasterTrainController>();
            if (train != null) _target = train.transform;
            else 
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams.Length > 0) _target = cams[0].transform;
            }
            return;
        }

        // Coaster'ın spline üzerindeki ilerleme oranını kontrol et
        float currentT = _coasterController.GetT();
        
        // t >= triggerT olduğunda tetikle (pistin sonuna yaklaşırken)
        if (currentT >= triggerT && currentT < 1f)
        {
            _triggered = true;
            StartCoroutine(FireworksSequence());
            Debug.Log($"[FireworksFinale] TRIGGERED at t={currentT}!");
        }
    }

    IEnumerator FireworksSequence()
    {
        for (int i = 0; i < totalBursts; i++)
        {
            if (_target == null) yield break;

            // Kameranın bakış açısı içinde spawn
            Vector3 refPos = _target.position;
            Vector3 camRight = _target.right;
            Vector3 camForward = _target.forward;
            
            camForward.y = 0; 
            camForward.Normalize();

            float side = (i % 2 == 0) ? -sideOffset : sideOffset;
            
            // Kameranın 40-70m ilerisinde, yukarıda
            Vector3 launchPos = refPos
                + camForward * Random.Range(40f, 70f)
                + camRight * side * Random.Range(0.6f, 1.4f)
                + Vector3.up * Random.Range(25f, launchHeight)
                + Random.insideUnitSphere * 10f;

            SpawnParticleFirework(launchPos, i);

            // Patlama sesi (Geniş Erimli Özel AudioSource)
            if (fireworkSound != null)
            {
                GameObject sfxObj = new GameObject("FireworkSFX");
                sfxObj.transform.position = launchPos;
                AudioSource src = sfxObj.AddComponent<AudioSource>();
                src.clip = fireworkSound;
                src.spatialBlend = 0.8f; // %80 3D
                src.minDistance = 30f;
                src.maxDistance = 500f; // Uzaktan duyulması için devasa
                src.rolloffMode = AudioRolloffMode.Linear;
                src.Play();
                Destroy(sfxObj, fireworkSound.length + 1f);
            }

            yield return new WaitForSeconds(burstInterval + Random.Range(-0.2f, 0.2f));
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

        ParticleSystem ps = fwObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 35f);
        main.startSize = new ParticleSystem.MinMaxCurve(4.0f, 8.0f);
        main.startColor = new ParticleSystem.MinMaxGradient(mainColor, secondColor);
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 300, 500) 
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

        var trails = ps.trails;
        trails.enabled = false;

        ParticleSystemRenderer rend = fwObj.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        
        // URP Particle shader
        Shader safeShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (safeShader == null) safeShader = Shader.Find("Universal Render Pipeline/Lit");
        if (safeShader == null) safeShader = Shader.Find("Particles/Standard Unlit");

        if (safeShader != null)
        {
            Material safeMat = new Material(safeShader);
            if (safeMat.HasProperty("_BaseColor")) safeMat.SetColor("_BaseColor", mainColor);
            if (safeMat.HasProperty("_Color")) safeMat.SetColor("_Color", mainColor);
            safeMat.SetFloat("_Surface", 1);
            safeMat.SetFloat("_Blend", 0);
            safeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            safeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            safeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            safeMat.renderQueue = 3000;
            rend.material = safeMat;
        }

        // Parlama ışığı
        GameObject lightObj = new GameObject("FW_Light");
        lightObj.transform.SetParent(fwObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light fwLight = lightObj.AddComponent<Light>();
        fwLight.type = LightType.Point;
        fwLight.color = mainColor;
        fwLight.intensity = 25f;
        fwLight.range = 250f;

        Debug.Log($"[Firework_{index}] Spawned at {pos} color={mainColor}");
        ps.Play();
        Destroy(fwObj, 6f);
    }
}
