using UnityEngine;

public class CloudDrifter : MonoBehaviour
{
    [Header("Drift")]
    public float driftSpeed = 2f;
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0.3f);
    [SerializeField] private float resetDistance = 600f;

    private void Update()
    {
        Vector3 drift = windDirection.normalized * driftSpeed * Time.deltaTime;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cloud = transform.GetChild(i);
            cloud.localPosition += drift;

            if (cloud.localPosition.magnitude > resetDistance)
            {
                Vector3 pos = cloud.localPosition;
                pos = -pos.normalized * (resetDistance * 0.6f);
                pos.y = cloud.localPosition.y;
                cloud.localPosition = pos;
            }
        }
    }
}
