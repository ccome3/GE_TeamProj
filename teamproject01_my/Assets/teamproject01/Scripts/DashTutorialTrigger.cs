using UnityEngine;

public class DashTutorialTrigger : MonoBehaviour
{
    [Header("매니저 참조")]
    public TutorialManager manager;

    [Header("플레이어 참조")]
    // PlayerHealthAndMovement에 TutorialManager 참조를 할당하기 위해 필요
    public PlayerHealthAndMovement playerController; 

    private void Start()
    {
        if (manager == null || playerController == null)
        {
            Debug.LogError("TutorialManager 또는 Player Controller를 할당해야 합니다!");
            enabled = false;
        }
        
        // 플레이어 스크립트에게 이 매니저 인스턴스를 알려줍니다. (DashCoroutine에서 사용)
        if (playerController != null && playerController.tutorialManager == null)
        {
            playerController.tutorialManager = manager;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 플레이어 스크립트를 튜토리얼 모드로 전환 (다른 입력 비활성화)
            if (playerController != null)
            {
                playerController.SetDashTutorialMode(true);
            }
            
            // 2. 튜토리얼 매니저에게 시간 정지 및 페이드 인 요청
            manager.StartDashTutorial();

            // 3. 현재 튜토리얼 트리거 오브젝트 파괴
            Destroy(gameObject); 
            
            Debug.Log("Dash 튜토리얼 시작, 시간 정지 및 트리거 파괴 완료.");
        }
    }
}