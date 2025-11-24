using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 진입/퇴장에 따라 기믹의 시간 계수를 조절하는 영역입니다.
/// </summary>
public class TimeDilationZone : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("플레이어 오브젝트의 태그 (예: Player)")]
    public string playerTag = "Player";
    
    [Tooltip("시간 영향을 받는 기믹의 레이어 마스크")]
    public LayerMask timeAffectedLayer;

    // 현재 존 내부에 있는 TimeAffectedObject들을 추적합니다.
    private List<TimeAffectedObject> objectsInsideZone = new List<TimeAffectedObject>();
    
    // 플레이어가 존 내부에 있는지 여부
    private bool isPlayerInside = false;
    
    private Collider2D zoneCollider; // 🌟 Collider 참조 추가

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null || !zoneCollider.isTrigger)
        {
            // 치명적인 오류가 발생할 수 있으므로 강제 로깅하고 비활성화합니다.
            Debug.LogError($"[TimeDilationZone - {gameObject.name}] 스크립트는 'Is Trigger'가 켜진 Collider2D를 필요로 합니다. 현재 Collider 상태: {zoneCollider?.isTrigger}");
            enabled = false; // 스크립트 비활성화
        }
    }

    private void Start()
    {
        // 🌟🌟🌟 씬 시작 시 존 내부에 있는 기믹들 감지 및 초기 속도 적용 🌟🌟🌟
        
        if (zoneCollider == null) return;
        
        // --- 디버깅 코드 추가: LayerMask 값과 Zone Bounds 확인 ---
        Debug.Log($"[TimeDilationZone - Debug] Time Affected Layer Mask Value: {timeAffectedLayer.value}");
        Debug.Log($"[TimeDilationZone - Debug] Zone Bounds (Min): {zoneCollider.bounds.min}, (Max): {zoneCollider.bounds.max}");

        // OverlapAreaAll을 사용하여 존 내부에 있는 TimeAffectedObject 찾기
        Collider2D[] initialHits = Physics2D.OverlapAreaAll(
            zoneCollider.bounds.min, 
            zoneCollider.bounds.max, 
            timeAffectedLayer);

        int detectedCount = 0; // 🌟 감지된 기믹 수 카운트

        foreach (var hit in initialHits)
        {
            TimeAffectedObject affectedObj = hit.GetComponent<TimeAffectedObject>();
            if (affectedObj != null && !objectsInsideZone.Contains(affectedObj))
            {
                objectsInsideZone.Add(affectedObj);
                // 플레이어가 밖에 있으므로, 내부 기믹은 느리게 (0.333x) 설정
                TimeDilationManager.OverrideFactor(affectedObj, TimeDilationManager.ZONE_IN_FACTOR_SLOW);
                detectedCount++; // 🌟 카운트 증가
                
                // --- 디버깅 코드 추가: 어떤 오브젝트가 감지되었는지 출력 ---
                Debug.Log($"[TimeDilationZone - Detected] 감지된 기믹: {hit.gameObject.name}");
            }
        }
        
        // 외부 기믹은 정상 속도 유지 (TimeDilationManager의 기본값 1.0f)
        TimeDilationManager.SetGlobalFactor(TimeDilationManager.NORMAL_FACTOR);
        
        // 🌟🌟🌟 중요 디버그 로그 활성화 🌟🌟🌟
        Debug.Log($"[TimeDilationZone] 초기 설정 완료: 존 내부 기믹 {detectedCount}개 느리게 시작 ({TimeDilationManager.ZONE_IN_FACTOR_SLOW}x). 총 감지된 콜라이더 수: {initialHits.Length}개.");
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 플레이어 진입/퇴장 감지
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = true;
            ApplyTimeDilationEffect();
        }

        // 2. 시간 영향을 받는 오브젝트 진입 감지
        TimeAffectedObject affectedObj = other.GetComponent<TimeAffectedObject>();
        if (affectedObj != null && !objectsInsideZone.Contains(affectedObj))
        {
            objectsInsideZone.Add(affectedObj);
            
            // 이 오브젝트는 존에 "새로" 들어왔으므로 현재 플레이어 상태에 따라 속도 설정
            if (isPlayerInside)
            {
                // 플레이어가 이미 안에 있다면, 새로 들어온 오브젝트는 정상 속도로 오버라이드 설정
                TimeDilationManager.OverrideFactor(affectedObj, TimeDilationManager.NORMAL_FACTOR);
            }
            else
            {
                // 플레이어가 밖에 있다면, 새로 들어온 오브젝트는 느린 속도로 오버라이드 설정
                TimeDilationManager.OverrideFactor(affectedObj, TimeDilationManager.ZONE_IN_FACTOR_SLOW);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 1. 플레이어 진입/퇴장 감지
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;
            ApplyTimeDilationEffect();
        }

        // 2. 시간 영향을 받는 오브젝트 퇴장 감지
        TimeAffectedObject affectedObj = other.GetComponent<TimeAffectedObject>();
        if (affectedObj != null && objectsInsideZone.Contains(affectedObj))
        {
            objectsInsideZone.Remove(affectedObj);
            TimeDilationManager.ClearOverride(affectedObj); // 오버라이드 해제
        }
    }

    /// <summary>
    /// 플레이어의 위치에 따라 전체 시간 배율을 조정하고, 존 내부 오브젝트에 오버라이드를 적용합니다.
    /// </summary>
    private void ApplyTimeDilationEffect()
    {
        if (isPlayerInside)
        {
            // --- 1. 플레이어가 존 안에 있는 경우 ---
            
            // 글로벌 기믹 (존 밖에 있는 기믹)은 배속 (GLOBAL_FACTOR_FAST)
            TimeDilationManager.SetGlobalFactor(TimeDilationManager.GLOBAL_FACTOR_FAST);

            // 존 내부 기믹은 정상 속도 (NORMAL_FACTOR)로 오버라이드
            foreach (var obj in objectsInsideZone)
            {
                TimeDilationManager.OverrideFactor(obj, TimeDilationManager.NORMAL_FACTOR);
            }
        }
        else
        {
            // --- 2. 플레이어가 존 밖에 있는 경우 ---

            // 글로벌 기믹 (존 밖에 있는 기믹)은 정상 속도 (NORMAL_FACTOR)
            TimeDilationManager.SetGlobalFactor(TimeDilationManager.NORMAL_FACTOR);

            // 존 내부 기믹은 역배속 (ZONE_IN_FACTOR_SLOW)으로 오버라이드
            foreach (var obj in objectsInsideZone)
            {
                TimeDilationManager.OverrideFactor(obj, TimeDilationManager.ZONE_IN_FACTOR_SLOW);
            }
        }
    }
}