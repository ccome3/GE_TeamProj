using UnityEngine;

public class MonsterTutorialTrigger : MonoBehaviour
{
    [Header("매니저 참조")]
    public TutorialManager manager;

    private void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<TutorialManager>();
            if (manager == null)
            {
                Debug.LogError("TutorialManager를 씬에서 찾을 수 없습니다!");
                enabled = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 오브젝트의 태그 확인
        if (other.CompareTag("Player"))
        {
            // 1. 매니저에게 몬스터 소환 및 페이드 인 요청
            manager.StartMonsterTutorial();

            // 2. 트리거 오브젝트 파괴 (한 번만 실행되도록)
            Destroy(gameObject); 
            
            Debug.Log("Monster 튜토리얼 트리거 작동 및 파괴 완료.");
        }
    }
}