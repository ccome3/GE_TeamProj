using UnityEngine;
using Unity.Cinemachine; // 시네머신 버전에 따라 'using Cinemachine;' 일 수 있습니다.
using System.Collections;

public class BossCameraTrigger : MonoBehaviour
{
    [Header("카메라 설정")]
    public CinemachineCamera bossVirtualCamera; // 보스를 비추는 가상 카메라
    public float displayDuration = 2f;         // 보스를 비출 시간
    public int activePriority = 20;            // 활성화 시 우선순위 (플레이어 카메라보다 높아야 함)

    private int defaultPriority = 5;           // 기본 우선순위
    private bool isTriggered = false;          // 한 번만 실행되도록 체크

    private void Start()
    {
        if (bossVirtualCamera != null)
        {
            defaultPriority = (int)bossVirtualCamera.Priority;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 존에 들어왔고, 아직 연출이 실행되지 않았다면
        if (!isTriggered && other.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(ShowBossRoutine());
        }
    }

    private IEnumerator ShowBossRoutine()
    {
        // 1. 보스 카메라 우선순위를 높여 화면 전환
        bossVirtualCamera.Priority = activePriority;

        // 2. 지정된 시간(2초) 동안 대기
        yield return new WaitForSeconds(displayDuration);

        // 3. 다시 우선순위를 낮춰서 플레이어 카메라로 복귀
        bossVirtualCamera.Priority = defaultPriority;
        
        // 필요하다면 연출이 끝난 후 트리거 오브젝트를 삭제하거나 비활성화
        // gameObject.SetActive(false);
    }
}