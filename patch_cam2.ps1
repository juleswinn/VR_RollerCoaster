$file = 'Assets\Scripts\RollerCoasterPrototypeBuilder.cs'
$content = Get-Content $file -Raw -Encoding UTF8

$oldCode = "    private void BindXROrigin(Transform seatAnchor)
    {
        if (xrOrigin == null)
            xrOrigin = FindCandidateXROrigin();

        if (xrOrigin == null)
        {
            Debug.LogWarning(\"XR Origin bulunamad.\");
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

        builder.BuildEnvironment();
    }

    private void BuildRealisticTrackGeometry(SplineContainer spline)
    {
        if (spline == null) return;

        RealisticTrackBuilder builder = GetComponent<RealisticTrackBuilder>();
        if (builder == null)
            builder = gameObject.AddComponent<RealisticTrackBuilder>();

        builder.SetSpline(spline);

        if (trackMaterials != null && trackMaterials.Length >= 2)
            builder.SetMaterials(trackMaterials[0], trackMaterials[1]);

        builder.BuildGeometry();
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

        GameObject lightObject = new GameObject(\"Directional Light\");
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

    private static Transform FindCandidateXROrigin()
    {
        GameObject xr = GameObject.Find(\"XR Origin (VR)\");
        if (xr != null) return xr.transform;

        xr = GameObject.Find(\"XR Origin\");
        if (xr != null) return xr.transform;

        if (Camera.main != null)
        {
            Transform root = Camera.main.transform;
            while (root.parent != null)
                root = root.parent;

            return root;
        }

        return null;
    }
}
"

$newCode = "    private void BindXROrigin(Transform seatAnchor)
    {
        if (xrOrigin == null)
            xrOrigin = FindCandidateXROrigin();

        if (xrOrigin != null && xrOrigin.name == \"XR Origin (VR)\" && xrOrigin.GetComponentInChildren<Camera>() != null)
        {
            // Eger bizim urettigimiz failsafe ise direkt parentla, cunku binder karisikliga yol acabilir
            if (xrOrigin.tag == \"MainCamera\" || xrOrigin.GetComponent<Camera>() != null)
            {
                xrOrigin.SetParent(seatAnchor, false);
                xrOrigin.localPosition = new Vector3(0f, 0.8f, 0f);
                xrOrigin.localRotation = Quaternion.identity;
                return;
            }
        }

        if (xrOrigin == null) return;

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

        builder.BuildEnvironment();
    }

    private void BuildRealisticTrackGeometry(SplineContainer spline)
    {
        if (spline == null) return;

        RealisticTrackBuilder builder = GetComponent<RealisticTrackBuilder>();
        if (builder == null)
            builder = gameObject.AddComponent<RealisticTrackBuilder>();

        builder.SetSpline(spline);

        if (trackMaterials != null && trackMaterials.Length >= 2)
            builder.SetMaterials(trackMaterials[0], trackMaterials[1]);

        builder.BuildGeometry();
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

        GameObject lightObject = new GameObject(\"Directional Light\");
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

    private static Transform FindCandidateXROrigin()
    {
        GameObject xr = GameObject.Find(\"XR Origin (VR)\");
        if (xr != null) return xr.transform;

        xr = GameObject.Find(\"XR Origin\");
        if (xr != null) return xr.transform;

        if (Camera.main != null)
        {
            Transform root = Camera.main.transform;
            while (root.parent != null)
                root = root.parent;

            return root;
        }

        // Failsafe camera adini XR Origin (VR) yapiyoruz ki kullanici mutlu olsun
        GameObject fallbackCamObj = new GameObject(\"XR Origin (VR)\");
        Camera cam = fallbackCamObj.AddComponent<Camera>();
        fallbackCamObj.tag = \"MainCamera\";
        fallbackCamObj.AddComponent<AudioListener>();

        return fallbackCamObj.transform;
    }
}
"

$content = $content.Replace($oldCode, $newCode)

Set-Content $file -Value $content -Encoding UTF8 -NoNewline
