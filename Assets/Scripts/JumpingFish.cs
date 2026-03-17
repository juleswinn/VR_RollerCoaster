using UnityEngine;

public class JumpingFish : MonoBehaviour
{
    private Vector3 startPos;
    private float timer = 0f;
    
    [Header("Jump Config")]
    public float jumpIntervalMin = 2f;
    public float jumpIntervalMax = 7f;
    public float jumpHeight = 3f;
    public float jumpDistance = 4f;
    public float jumpDuration = 1.5f;

    private float nextJumpTime;
    private bool isJumping = false;
    private Vector3 jumpStartPos;
    private Vector3 jumpEndPos;
    private float jumpProgress = 0f;

    private void Start()
    {
        startPos = transform.position;
        ScheduleNextJump();
    }

    private void Update()
    {
        if (!isJumping)
        {
            timer += Time.deltaTime;
            if (timer >= nextJumpTime)
            {
                StartJump();
            }
        }
        else
        {
            jumpProgress += Time.deltaTime / jumpDuration;
            
            if (jumpProgress >= 1f)
            {
                jumpProgress = 1f;
                EndJump();
            }

            // Parabolik sekil: y = 4 * h * x * (1 - x)
            float arc = 4f * jumpHeight * jumpProgress * (1f - jumpProgress);
            Vector3 currentPos = Vector3.Lerp(jumpStartPos, jumpEndPos, jumpProgress);
            currentPos.y += arc;
            
            // Havaya dogru burnunu kaldir - inisine gore eg
            Vector3 lookDir = (currentPos - transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            transform.position = currentPos;
        }
    }

    private void StartJump()
    {
        isJumping = true;
        jumpProgress = 0f;
        
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        
        jumpStartPos = startPos;
        jumpEndPos = startPos + dir * jumpDistance;
        
        // Zemin hizasini kontrol et
        if (Terrain.activeTerrain != null)
        {
            float limitY = Terrain.activeTerrain.SampleHeight(jumpEndPos) + Terrain.activeTerrain.transform.position.y;
            if (jumpEndPos.y < limitY) jumpEndPos.y = limitY;
        }

        transform.position = jumpStartPos;
    }

    private void EndJump()
    {
        isJumping = false;
        timer = 0f;
        startPos = jumpEndPos; // Yeni pozisyon
        ScheduleNextJump();
    }

    private void ScheduleNextJump()
    {
        nextJumpTime = Random.Range(jumpIntervalMin, jumpIntervalMax);
    }
}
