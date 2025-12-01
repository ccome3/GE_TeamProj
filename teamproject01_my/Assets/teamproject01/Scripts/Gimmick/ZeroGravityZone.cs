using UnityEngine;

public class ZeroGravityZone : MonoBehaviour
{
    // 🌟 이 값들은 Unity Editor에서 설정된 플레이어 및 오브젝트의 Rigidbody 값과 동일해야 합니다.
    private const float ZERO_GRAVITY = 0f;
    private const float NORMAL_GRAVITY = 4.5f; // 👈 플레이어/오브젝트의 원래 Gravity Scale
    
    private const float ZERO_GRAVITY_DRAG = 0f;
    private const float NORMAL_DRAG = 0f; // 👈 플레이어/오브젝트의 원래 Linear Drag (혹은 0f)

    // 플레이어 또는 다른 물체가 영역에 진입했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 중력과 저항을 0으로 설정
            rb.gravityScale = ZERO_GRAVITY;
            rb.linearDamping = ZERO_GRAVITY_DRAG; 
        }
    }
    
    // 🌟🌟 물체가 영역 안에 머무는 동안 매 프레임 강제 적용하여 안정화
    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        // 중력 스케일이 0이 아니거나, 드래그가 0이 아니면 다시 강제 설정
        if (rb != null && (rb.gravityScale != ZERO_GRAVITY || rb.linearDamping != ZERO_GRAVITY_DRAG))
        {
            rb.gravityScale = ZERO_GRAVITY;
            rb.linearDamping = ZERO_GRAVITY_DRAG; 
        }
    }

    // 플레이어 또는 다른 물체가 영역을 벗어났을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 중력과 저항을 원래 값으로 복원
            rb.gravityScale = NORMAL_GRAVITY;
            rb.linearDamping = NORMAL_DRAG;
        }
    }
}