using UnityEngine;

public class AnimalRoamer : MonoBehaviour
{
    private Vector3 targetPos;
    private Animator animator;
    public float speed = 1.0f;
    public float roamRadius = 15f;
    private Vector3 startPos;
    private float waitTimer = 0f;

    private string activeSpeedParam = "";

    void Start()
    {
        startPos = transform.position;
        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();
        
        string[] possibleParams = { "Speed", "Walk", "Run", "isWalking", "Moving", "WalkForward" };
        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                foreach (string candidate in possibleParams)
                {
                    if (p.name == candidate)
                    {
                        activeSpeedParam = candidate;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(activeSpeedParam)) break;
            }
        }

        SetNewTarget();
        waitTimer = Random.Range(0f, 3f);
    }

    void Update()
    {
        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            if (animator != null && !string.IsNullOrEmpty(activeSpeedParam))
            {
                if (GetParamType(activeSpeedParam) == AnimatorControllerParameterType.Float) animator.SetFloat(activeSpeedParam, 0f);
                if (GetParamType(activeSpeedParam) == AnimatorControllerParameterType.Bool) animator.SetBool(activeSpeedParam, false);
            }
            return;
        }

        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;
        float dist = dir.magnitude;

        if (dist < 0.5f)
        {
            waitTimer = Random.Range(2f, 7f);
            SetNewTarget();
        }
        else
        {
            if (animator != null && !string.IsNullOrEmpty(activeSpeedParam))
            {
                if (GetParamType(activeSpeedParam) == AnimatorControllerParameterType.Float) animator.SetFloat(activeSpeedParam, 1f);
                if (GetParamType(activeSpeedParam) == AnimatorControllerParameterType.Bool) animator.SetBool(activeSpeedParam, true);
            }
            
            // Move
            transform.position += dir.normalized * speed * Time.deltaTime;
            
            // Adapt to terrain height
            if (Terrain.activeTerrain != null)
            {
                Vector3 tPos = transform.position;
                tPos.y = Terrain.activeTerrain.SampleHeight(tPos) + Terrain.activeTerrain.transform.position.y;
                transform.position = tPos;
            }

            // Rotate towards target smoothly
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    void SetNewTarget()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r = Random.Range(2f, roamRadius);
        targetPos = startPos + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
    }

    private AnimatorControllerParameterType GetParamType(string pName)
    {
        foreach (var p in animator.parameters)
            if (p.name == pName) return p.type;
        return AnimatorControllerParameterType.Float;
    }
}
