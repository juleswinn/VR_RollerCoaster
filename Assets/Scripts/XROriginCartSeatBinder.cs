using UnityEngine;

public class XROriginCartSeatBinder : MonoBehaviour
{
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private bool bindOnStart = true;
    [SerializeField] private bool keepWorldOffset = false;

    private void Reset()
    {
        seatAnchor = transform;
    }

    private void Start()
    {
        if (bindOnStart) Bind();
    }

    [ContextMenu("Bind XR Origin To Seat")]
    public void Bind()
    {
        if (seatAnchor == null) seatAnchor = transform;

        if (xrOrigin == null) xrOrigin = FindCandidateXROrigin();

        if (xrOrigin == null)
        {
            Debug.LogWarning("XROriginCartSeatBinder: XR Origin bulunamadi. xrOrigin alanini manuel atayin.");
            return;
        }

        xrOrigin.SetParent(seatAnchor, keepWorldOffset);

        if (!keepWorldOffset)
        {
            xrOrigin.localPosition = Vector3.zero;
            xrOrigin.localRotation = Quaternion.identity;
        }
    }

    public void SetXROrigin(Transform target) => xrOrigin = target;
    public void SetSeatAnchor(Transform target) => seatAnchor = target;

    private Transform FindCandidateXROrigin()
    {
        GameObject named = GameObject.Find("XR Origin (VR)");
        if (named != null) return named.transform;

        named = GameObject.Find("XR Origin");
        if (named != null) return named.transform;

        if (Camera.main != null)
        {
            Transform root = Camera.main.transform;
            while (root.parent != null) root = root.parent;
            return root;
        }

        return null;
    }
}
