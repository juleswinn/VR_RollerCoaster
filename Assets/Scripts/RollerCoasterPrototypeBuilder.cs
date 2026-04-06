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
        CleanupAudioListeners();
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
        // The lapbar pivot is at Y=1.15 in cart space. Raise camera significantly to Y=1.95f to clear everything.
        seatAnchor.localPosition = new Vector3(0f, 1.95f, -0.5f); 
        // Look down slightly to see the track better
        seatAnchor.localRotation = Quaternion.Euler(5f, 0f, 0f);

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
        
        Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyMat.color = new Color(0.8f, 0.1f, 0.1f); // Red sleek body
        bodyMat.SetFloat("_Smoothness", 0.9f);
        bodyMat.SetFloat("_Metallic", 0.6f);
        
        Material seatMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        seatMat.color = new Color(0.1f, 0.1f, 0.1f); // Dark leather
        bodyMat.SetFloat("_Smoothness", 0.3f);

        Material silverMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        silverMat.color = new Color(0.9f, 0.9f, 0.9f); // Silver trim
        silverMat.SetFloat("_Metallic", 0.9f);

        for (int i = 0; i < carCount; i++)
        {
            GameObject car = new GameObject($"Car_{i:00}");
            car.transform.SetParent(trainVisualRoot, false);
            car.transform.localPosition = new Vector3(0f, 0.35f, -i * carSpacing);

            // Create a proper hollow chassis
            // Bottom Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(floor);
            floor.name = "Floor";
            floor.transform.SetParent(car.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0f, 0f);
            floor.transform.localScale = new Vector3(1.6f, 0.15f, 2.6f);
            if (floor.GetComponent<Renderer>() != null) floor.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Side Walls (Lowered to waist height)
            GameObject wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(wallL);
            wallL.name = "WallL";
            wallL.transform.SetParent(car.transform, false);
            wallL.transform.localPosition = new Vector3(-0.75f, 0.25f, 0f);
            wallL.transform.localScale = new Vector3(0.1f, 0.5f, 2.6f);
            if (wallL.GetComponent<Renderer>() != null) wallL.GetComponent<Renderer>().sharedMaterial = bodyMat;

            GameObject wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(wallR);
            wallR.name = "WallR";
            wallR.transform.SetParent(car.transform, false);
            wallR.transform.localPosition = new Vector3(0.75f, 0.25f, 0f);
            wallR.transform.localScale = new Vector3(0.1f, 0.5f, 2.6f);
            if (wallR.GetComponent<Renderer>() != null) wallR.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Front/Back Walls (Lowered so passenger can see over)
            GameObject wallF = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(wallF);
            wallF.name = "WallF";
            wallF.transform.SetParent(car.transform, false);
            wallF.transform.localPosition = new Vector3(0f, 0.25f, 1.25f);
            wallF.transform.localScale = new Vector3(1.6f, 0.5f, 0.1f);
            if (wallF.GetComponent<Renderer>() != null) wallF.GetComponent<Renderer>().sharedMaterial = bodyMat;

            GameObject wallB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(wallB);
            wallB.name = "WallB";
            wallB.transform.SetParent(car.transform, false);
            wallB.transform.localPosition = new Vector3(0f, 0.25f, -1.25f);
            wallB.transform.localScale = new Vector3(1.6f, 0.5f, 0.1f);
            if (wallB.GetComponent<Renderer>() != null) wallB.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Proper Seats
            GameObject seatBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(seatBase);
            seatBase.name = "SeatBase";
            seatBase.transform.SetParent(car.transform, false);
            seatBase.transform.localPosition = new Vector3(0f, 0.15f, -0.6f);
            seatBase.transform.localScale = new Vector3(1.4f, 0.2f, 1.0f);
            if (seatBase.GetComponent<Renderer>() != null) seatBase.GetComponent<Renderer>().sharedMaterial = seatMat;

            GameObject seatBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(seatBack);
            seatBack.name = "SeatBack";
            seatBack.transform.SetParent(car.transform, false);
            seatBack.transform.localPosition = new Vector3(0f, 0.60f, -1.0f);
            seatBack.transform.localScale = new Vector3(1.4f, 0.8f, 0.2f);
            if (seatBack.GetComponent<Renderer>() != null) seatBack.GetComponent<Renderer>().sharedMaterial = seatMat;

            // Wheels / Base trims underneath
            GameObject trimL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoveCollider(trimL);
            trimL.name = "TrimL";
            trimL.transform.SetParent(car.transform, false);
            trimL.transform.localPosition = new Vector3(-0.95f, -0.1f, 0f);
            trimL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            trimL.transform.localScale = new Vector3(0.5f, 0.1f, 2.5f);
            if (trimL.GetComponent<Renderer>() != null) trimL.GetComponent<Renderer>().sharedMaterial = silverMat;

            GameObject trimR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoveCollider(trimR);
            trimR.name = "TrimR";
            trimR.transform.SetParent(car.transform, false);
            trimR.transform.localPosition = new Vector3(0.95f, -0.1f, 0f);
            trimR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            trimR.transform.localScale = new Vector3(0.5f, 0.1f, 2.5f);
            if (trimR.GetComponent<Renderer>() != null) trimR.GetComponent<Renderer>().sharedMaterial = silverMat;

            // Nose cone for the first car ONLY
            if (i == 0)
            {
                // Push the nose down to be a bumper, not a giant face blocker
                GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                RemoveCollider(nose);
                nose.name = "Nose";
                nose.transform.SetParent(car.transform, false);
                nose.transform.localPosition = new Vector3(0f, 0.25f, 1.3f);
                nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Point forward
                nose.transform.localScale = new Vector3(1.6f, 0.5f, 0.5f);
                if (nose.GetComponent<Renderer>() != null) nose.GetComponent<Renderer>().sharedMaterial = bodyMat;

                // Better Lap Bar structure inside the hollow chassis
                Transform lapBarPivot = FindOrCreateChildObject(car.transform, "LapBarPivot").transform;
                lapBarPivot.localPosition = new Vector3(0f, 0.8f, 0.6f);

                GameObject lapBarHingeL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RemoveCollider(lapBarHingeL);
                lapBarHingeL.name = "LapBarArmL";
                lapBarHingeL.transform.SetParent(lapBarPivot, false);
                lapBarHingeL.transform.localPosition = new Vector3(-0.65f, -0.15f, -0.5f);
                lapBarHingeL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lapBarHingeL.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                if (lapBarHingeL.GetComponent<Renderer>() != null) lapBarHingeL.GetComponent<Renderer>().sharedMaterial = silverMat;

                GameObject lapBarHingeR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RemoveCollider(lapBarHingeR);
                lapBarHingeR.name = "LapBarArmR";
                lapBarHingeR.transform.SetParent(lapBarPivot, false);
                lapBarHingeR.transform.localPosition = new Vector3(0.65f, -0.15f, -0.5f);
                lapBarHingeR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lapBarHingeR.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                if (lapBarHingeR.GetComponent<Renderer>() != null) lapBarHingeR.GetComponent<Renderer>().sharedMaterial = silverMat;

                GameObject lapBarChest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                RemoveCollider(lapBarChest);
                lapBarChest.name = "LapBarChest";
                lapBarChest.transform.SetParent(lapBarPivot, false);
                lapBarChest.transform.localPosition = new Vector3(0f, -0.15f, -1.2f);
                lapBarChest.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                lapBarChest.transform.localScale = new Vector3(0.12f, 0.75f, 0.12f);
                if (lapBarChest.GetComponent<Renderer>() != null) lapBarChest.GetComponent<Renderer>().sharedMaterial = seatMat;

                controller?.SetLapBarPivot(lapBarPivot);
            }
        }
    }

    private void RemoveCollider(GameObject go)
    {
        if (go == null) return;
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Object.Destroy(c);
            else Object.DestroyImmediate(c);
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

        Material railMat = trackRailMaterial != null ? new Material(trackRailMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        railMat.name = "Realistic_RailMaterial";
        railMat.color = new Color(0.2f, 0.22f, 0.25f);
        railMat.SetFloat("_Smoothness", 0.7f);
        railMat.SetFloat("_Metallic", 0.8f);

        Material sleepMat = sleeperMaterial != null ? new Material(sleeperMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sleepMat.name = "Realistic_SleeperMaterial";
        sleepMat.color = new Color(0.28f, 0.18f, 0.10f);
        
        Material suppMat = supportMaterial != null ? new Material(supportMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        suppMat.name = "Realistic_SupportMaterial";
        suppMat.color = new Color(0.85f, 0.85f, 0.85f);

        builder.SetMaterials(railMat, sleepMat, suppMat);
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
            Camera existingCam = existing.GetComponentInChildren<Camera>();
            if (existingCam != null)
                existingCam.farClipPlane = 25000f; // Daglari daha rahat gormek icin artirildi
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
        cam.farClipPlane = 25000f; // Daglari gormek icin artirildi

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

        // Ses dinleyici ekle - cleanup olacak
        fallbackCamObj.AddComponent<AudioListener>();

        return fallbackCamObj.transform;
    }

    private void CleanupAudioListeners()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length <= 1) return;

        bool keptOne = false;
        foreach (var listener in listeners)
        {
            // Eger aktif ve Main Camera ise öncelikli tut
            if (!keptOne && listener.gameObject.CompareTag("MainCamera") && listener.gameObject.activeInHierarchy)
            {
                keptOne = true;
                continue;
            }
            // Değilse destroy et
            if (keptOne)
            {
                if (Application.isPlaying) Destroy(listener);
                else DestroyImmediate(listener);
            }
            else
            {
                keptOne = true; // ilk buldugunu tut eger main camera yoksa
            }
        }
    }
}