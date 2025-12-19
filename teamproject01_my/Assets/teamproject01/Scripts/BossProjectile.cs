using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 50;
    public float maxDistance = 20f;

    private Vector2 direction;
    private Vector3 startPos;
    private bool isInitialized = false;

    public void Setup(Vector2 dir)
    {
        direction = dir.normalized;
        startPos = transform.position;

        // 🌟 총알이 날아가는 방향을 바라보게 회전 (2D)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // 🌟 Translate는 로컬 좌표 기준이므로, Setup에서 회전시켰다면 Vector2.right가 앞방향이 됩니다.
        transform.Translate(Vector2.right * speed * Time.deltaTime);

        // 최대 비거리 체크 (Z축 차이로 인한 오차 방지를 위해 Vector2.Distance 사용 권장)
        if (Vector2.Distance(startPos, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 🌟 보스 본인이나 다른 적, 혹은 보스의 자식 트리거는 무시합니다.
        if (other.CompareTag("Enemy") || other.CompareTag("BossTrigger")) 
        {
            return;
        }

        // 2. 플레이어에게 데미지 전달
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerHealthAndMovement>();
            if (player != null)
            {
                player.TakeDamage(damage, direction);
            }
            
            // 플레이어에게 닿으면 즉시 파괴
            Destroy(gameObject);
        }
        
        // 3. 벽에 부딪혔을 때 파괴 (벽 태그가 "Obstacle"이나 "Ground"라면 추가)
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}