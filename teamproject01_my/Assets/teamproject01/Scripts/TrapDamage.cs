using UnityEngine;

/// <summary>
/// 플레이어에게 피해만 입히고 체력이 없는 기믹(함정) 스크립트.
/// </summary>
public class TrapDamage : MonoBehaviour
{
    [Header("공격력 설정")]
    public int attackDamage = 1; // 기믹이 플레이어에게 입힐 피해량
    
    // (선택 사항) 함정이 활성화되는 쿨타임 또는 무적 시간 설정도 가능합니다.
    
    // 🌟 이 스크립트의 주요 역할은 플레이어와의 충돌을 감지하는 것입니다.
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 대상이 플레이어인지 확인
        PlayerHealthAndMovement player = other.GetComponent<PlayerHealthAndMovement>();
        
        if (player != null)
        {
            // 플레이어에게 가할 넉백 방향 계산 (플레이어 위치 - 함정 위치 = 함정 반대 방향)
            Vector2 pushDirection = (player.transform.position - transform.position).normalized;

            // 플레이어에게 데미지와 넉백 방향 전달
            player.TakeDamage(attackDamage, pushDirection); 
            
            Debug.Log($"함정 '{gameObject.name}'이(가) 플레이어에게 {attackDamage} 피해를 입혔습니다.");
            
            // (선택 사항) 한 번만 피해를 입히는 함정이라면 여기서 비활성화
            // gameObject.SetActive(false);
        }
    }
}