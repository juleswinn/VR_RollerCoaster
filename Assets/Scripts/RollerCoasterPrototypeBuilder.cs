using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem.XR;
#if UNITY_EDITOR || UNITY_STANDALONE
using Unity.XR.CoreUtils;
#endif

public class RollerCoasterPrototypeBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Material skyboxMaterial;

    [Header("Build")]
    [SerializeField] private bool generateEnvironment = true;
    [SerializeField] private bool bindXROriginToSeat = true;
    [SerializeField] private bool buildRealisticTrack = true;
    [SerializeField] private bool buildStationAndQueue = true;

    [Header("Materials")]
    [SerializeField] private Material trackRailMaterial;
    [SerializeField] private Material sleeperMaterial;
    [SerializeField] private Material supportMaterial;
    [SerializeField] private Material queueMaterial;
    [SerializeField] private Material cloudMaterial;

    [ContextMenu("Build VR Roller Coaster Prototype")]
    public void BuildPrototype()
    {
        // XR Origin yoksa olustur
        EnsureXROrigin();

        SplineContainer spline = BuildTrack();

        if (spline == null)
        {
            Debug.LogError("Spline oluşturulamadı.");
            return;
        }

        Transform seat = BuildCart(spline);

        if (buildRealisticTrack)
            BuildRealisticTrackGeometry(spline);

        if (buildStationAndQueue)
            BuildStationAndQueue(spline);

        if (bindXROriginToSeat && seat != null)
            BindXROrigin(seat);

        if (generateEnvironment)
            BuildEnvironment();

        EnsureDirectionalLight();
    }

    private SplineContainer BuildTrack()
    {
        GameObject track = FindOrCreateRootObject("Track");

        SplineTrackGenerator trackGenerator = track.GetComponent<SplineTrackGenerator>();
        if (trackGenerator == null)
            trackGenerator = track.AddComponent<SplineTrackGenerator>();

        SplineContainer spline = track.GetComponent<SplineContainer>();
        if (spline == null)
            spline = track.AddComponent<SplineContainer>();

        trackGenerator.GenerateTrack();

        return spline;
    }

    private Transform BuildCart(SplineContainer spline)
    {
        GameObject cartRoot = FindOrCreateRootObject("CartRoot");

        CoasterTrainController mover = cartRoot.GetComponent<CoasterTrainController>();
        if (mover == null)
            mover = cartRoot.AddComponent<CoasterTrainController>();

        BuildTrainVisuals(cartRoot.transform, mover);

        Transform seatAnchor = FindOrCreateChildObject(cartRoot.transform, "SeatAnchor").transform;
        seatAnchor.localPosition = new Vector3(0f, 1.5f, -0.2f);
        seatAnchor.localRotation = Quaternion.identity;

        mover.SetSpline(spline);
        mover.CacheSplineLength();

        // Legacy mover varsa devre d��� b�rak
        Component legacy = cartRoot.GetComponent("SplineCartMover");
        if (legacy is Behaviour b)
            b.enabled = false;

        return seatAnchor;
    }

    private void BuildTrainVisuals(Transform cartRoot, CoasterTrainController controller)
    {
        Transform trainVisualRoot = FindOrCreateChildObject(cartRoot, "TrainCars").transform;

        for (int i = trainVisualRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(trainVisualRoot.GetChild(i).gameObject);

        const int carCount = 5;
        const float carSpacing = 2.8f;

        for (int i = 0; i < carCount; i++)
        {
            GameObject car = new GameObject($"Car_{i:00}");
            car.transform.SetParent(trainVisualRoot, false);
            car.transform.localPosition = new Vector3(0f, 0.52f, -i * carSpacing);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(car.transform, false);
            body.transform.localScale = new Vector3(1.7f, 1.0f, 2.4f);

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(car.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.35f, -0.25f);
            seat.transform.localScale = new Vector3(1.3f, 0.5f, 1.1f);

            if (i == 0)
            {
                Transform lapBarPivot = FindOrCreateChildObject(car.transform, "LapBarPivot").transform;
                lapBarPivot.localPosition = new Vector3(0f, 0.95f, 0.68f);

                GameObject lapBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lapBar.name = "LapBar";
                lapBar.transform.SetParent(lapBarPivot, false);
                lapBar.transform.localPosition = new Vector3(0f, -0.18f, -0.5f);
                lapBar.transform.localScale = new Vector3(1.4f, 0.14f, 1.05f);

                controller?.SetLapBarPivot(lapBarPivot);
            }
        }
    }

    private void BindXROrigin(Transform seatAnchor)
    {
        if (xrOrigin == null)
            xrOrigin = FindCandidateXROrigin();

        if (xrOrigin == null)
        {
            Debug.LogWarning("XR Origin bulunamad.");
            return;
        }

        // Eger XR Origin degil de bizim Fallback Kameramızsa, dogrudan koltuga parent yap
        if (xrOrigin.name == "FallbackMainCamera")
        {
            xrOrigin.SetParent(seatAnchor, false);
            xrOrigin.localPosition = new Vector3(0f, 0.6f, 0f); // Goz hizasi
            xrOrigin.localRotation = Quaternion.identity;
            return;
        }

        XROriginCartSeatBinder binder = seatAnchor.GetComponent<XROriginCartSeatBinder>();
        if (binder == null)
            binder = seatAnchor.gameObject.AddComponent<XROriginCartSeatBinder>();

        binder.SetSeatAnchor(seatAnchor);
        binder.SetXROrigin(xrOrigin);
        binder.Bind();
    }

    private void BuildEnvironment()
    {
        SimpleEnvironmentBuilder builder = GetComponent<SimpleEnvironmentBuilder>();
        if (builder == null)
            builder = gameObject.AddComponent<SimpleEnvironmentBuilder>();

        if (skyboxMaterial != null)
            builder.SetSkyboxMaterial(skyboxMaterial);

        if (cloudMaterial != null)
            builder.SetCloudMaterial(cloudMaterial);

        builder.BuildEnvironment();
    }

    private void BuildRealisticTrackGeometry(SplineContainer spline)
    {
        if (spline == null) return;

        RealisticTrackBuilder builder = spline.GetComponent<RealisticTrackBuilder>();
        if (builder == null)
            builder = spline.gameObject.AddComponent<RealisticTrackBuilder>();

        builder.SetMaterials(trackRailMaterial, sleeperMaterial, supportMaterial);
        builder.BuildRealisticTrack();
    }

    private void BuildStationAndQueue(SplineContainer spline)
    {
        if (spline == null) return;

        CoasterStationQueueBuilder stationBuilder = GetComponent<CoasterStationQueueBuilder>();
        if (stationBuilder == null)
            stationBuilder = gameObject.AddComponent<CoasterStationQueueBuilder>();

        stationBuilder.SetSpline(spline);

        if (queueMaterial != null)
            stationBuilder.SetMaterials(queueMaterial, queueMaterial, queueMaterial);

        stationBuilder.BuildStationAndQueue();
    }

    private void EnsureDirectionalLight()
    {
        Light existingLight = Object.FindFirstObjectByType<Light>();
        if (existingLight != null) return;

        GameObject lightObject = new GameObject("Directional Light");
        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        lightComponent.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
    }

    private static GameObject FindOrCreateRootObject(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
            obj = new GameObject(name);

        return obj;
    }

    private static GameObject FindOrCreateChildObject(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    /// <summary>
    /// Sahnede XR Origin yoksa sifirdan olusturur:
    /// XR Origin (VR)
    ///   └─ Camera Offset
    ///        └─ Main Camera (Camera + TrackedPoseDriver + AudioListener)
    /// </summary>
    private void EnsureXROrigin()
    {
        // Zaten varsa dokunma
        GameObject existing = GameObject.Find("XR Origin (VR)");
        if (existing == null)
            existing = GameObject.Find("XR Origin");

        if (existing != null)
        {
            xrOrigin = existing.transform;
            return;
        }

        // --- Yeni XR Origin olustur ---
        GameObject xrOriginObj = new GameObject("XR Origin (VR)");

#if UNITY_EDITOR || UNITY_STANDALONE
        XROrigin xrComp = xrOriginObj.AddComponent<XROrigin>();
#endif

        // Camera Offset
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOriginObj.transform, false);
        cameraOffset.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        xrComp.CameraFloorOffsetObject = cameraOffset;
#endif

        // Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(cameraOffset.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f); // Goz yuksekligi

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        camObj.AddComponent<AudioListener>();

        // TrackedPoseDriver — basi VR cihaz pozisyonuna baglar
        TrackedPoseDriver tpd = camObj.AddComponent<TrackedPoseDriver>();

#if UNITY_EDITOR || UNITY_STANDALONE
        xrComp.Camera = cam;
#endif

        xrOrigin = xrOriginObj.transform;

        Debug.Log("XR Origin (VR) sahnede bulunamadigi icin yeniden olusturuldu.");
    }

    private static Transform FindCandidateXROrigin()
    {
        GameObject xr = GameObject.Find("XR Origin (VR)");
        if (xr != null) return xr.transform;

        xr = GameObject.Find("XR Origin");
        if (xr != null) return xr.transform;

        if (Camera.main != null)
        {
            Transform root = Camera.main.transform;
            while (root.parent != null)
                root = root.parent;

            return root;
        }

        // Failsafe camera
        GameObject fallbackCamObj = new GameObject("FallbackMainCamera");
        Camera fallbackCam = fallbackCamObj.AddComponent<Camera>();
        fallbackCamObj.tag = "MainCamera";

        // Ses dinleyici ekle
        fallbackCamObj.AddComponent<AudioListener>();

        return fallbackCamObj.transform;
    }
}