using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    [System.Serializable]
    public struct TimeSettings
    {
        public Color skyColor;
        public Color equatorColor;
        public Color groundColor;
        public Color sunColor;
        public float sunIntensity;
        [Range(0f, 360f)] public float sunRotationX;
    }

    [Header("References")]
    [SerializeField] private Light directionalLight;

    [Header("Cycle Settings")]
    [SerializeField] private float dayCycleSpeed = 0.05f; // Hiz
    [SerializeField, Range(0f, 1f)] private float timeOfDay = 0.45f;

    [Header("Config")]
    [SerializeField] private TimeSettings dayTime = new TimeSettings
    {
        skyColor = new Color(0.48f, 0.70f, 0.90f),
        equatorColor = new Color(0.75f, 0.85f, 0.92f),
        groundColor = new Color(0.40f, 0.45f, 0.50f),
        sunColor = new Color(1f, 0.98f, 0.93f),
        sunIntensity = 1.2f,
        sunRotationX = 50f
    };

    [SerializeField] private TimeSettings sunsetTime = new TimeSettings
    {
        skyColor = new Color(0.85f, 0.45f, 0.25f),
        equatorColor = new Color(0.95f, 0.35f, 0.15f),
        groundColor = new Color(0.25f, 0.20f, 0.20f),
        sunColor = new Color(1f, 0.45f, 0.15f),
        sunIntensity = 0.8f,
        sunRotationX = 10f
    };

    [SerializeField] private TimeSettings nightTime = new TimeSettings
    {
        skyColor = new Color(0.05f, 0.05f, 0.15f),
        equatorColor = new Color(0.10f, 0.10f, 0.20f),
        groundColor = new Color(0.02f, 0.02f, 0.05f),
        sunColor = new Color(0.2f, 0.3f, 0.5f),
        sunIntensity = 0.2f,
        sunRotationX = 270f
    };

    private void Start()
    {
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { directionalLight = l; break; }
            }
        }
        
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // Baslangicta direkt gunduz degerini uygula ki ilk frame'de siyah kalmasin
        ApplyAtmosphereParams();
    }

    private void Update()
    {
        timeOfDay += Time.deltaTime * dayCycleSpeed;
        if (timeOfDay >= 1f) timeOfDay -= 1f;

        ApplyAtmosphereParams();
    }

    private void ApplyAtmosphereParams()
    {
        TimeSettings current, next;
        float t;

        if (timeOfDay < 0.3f) 
        {
            // Gece -> Sabaha karsi (Sunset config)
            current = nightTime; next = sunsetTime; t = timeOfDay / 0.3f;
        }
        else if (timeOfDay < 0.6f)
        {
            // Sabah -> Gunduz
            current = sunsetTime; next = dayTime; t = (timeOfDay - 0.3f) / 0.3f;
        }
        else if (timeOfDay < 0.8f)
        {
            // Gunduz -> Gun Batimi
            current = dayTime; next = sunsetTime; t = (timeOfDay - 0.6f) / 0.2f;
        }
        else
        {
            // Gun Batimi -> Gece
            current = sunsetTime; next = nightTime; t = (timeOfDay - 0.8f) / 0.2f;
        }

        // Renkler
        RenderSettings.ambientSkyColor = Color.Lerp(current.skyColor, next.skyColor, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(current.equatorColor, next.equatorColor, t);
        RenderSettings.ambientGroundColor = Color.Lerp(current.groundColor, next.groundColor, t);

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(current.sunColor, next.sunColor, t);
            directionalLight.intensity = Mathf.Lerp(current.sunIntensity, next.sunIntensity, t);
            
            float rotX = Mathf.LerpAngle(current.sunRotationX, next.sunRotationX, t);
            directionalLight.transform.rotation = Quaternion.Euler(rotX, 45f, 0f);
        }
    }
}
