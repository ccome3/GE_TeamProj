using UnityEngine;

public class JumpTutorialTrigger : MonoBehaviour
{
    [Header("매니저 참조")]
    public TutorialManager manager;

    private void Start()
    {
        if (manager == null)
        {
            Debug.LogError("TutorialManager를 할당해야 합니다!");
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 오브젝트의 태그가 "Player"인지 확인
        if (other.CompareTag("Player"))
        {
            // 1. 투명도 변경 시작 요청
            manager.StartFadeIn();

            // 2. 현재 튜토리얼 트리거 오브젝트 파괴
            Destroy(gameObject); 
            
            Debug.Log("Jump 튜토리얼 트리거 파괴 완료 및 투명도 변경 시작.");
        }
    }
}