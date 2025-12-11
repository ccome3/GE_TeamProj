using UnityEngine;

/// <summary>
/// 플레이어의 낙하 대쉬 공격을 감지하기 위한 몬스터 하위 Trigger 스크립트.
/// 이 Trigger는 몬스터의 '발바닥' 위치에 배치되어야 합니다.
/// </summary>
public class EnemyDamageTrigger : MonoBehaviour
{
    private EnemyStats parentEnemyStats;

    private void Start()
    {
        parentEnemyStats = GetComponentInParent<EnemyStats>();

        if (parentEnemyStats == null)
        {
            Debug.LogError("Error: EnemyDamageTrigger는 EnemyStats 스크립트를 가진 상위 오브젝트 아래에 있어야 합니다.");
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealthAndMovement player = other.GetComponent<PlayerHealthAndMovement>();
        
        if (player != null)
        {
            // 플레이어에게 현재 Dive Dash 상태인지 확인하고 데미지를 계산하도록 요청
            player.TryDiveDashAttack(parentEnemyStats);
        }
    }
}