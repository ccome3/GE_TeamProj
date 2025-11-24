using UnityEngine;
using System.Collections;

/// <summary>
/// 시간 지연 시스템과 연동되어 작동 주기가 플레이어의 위치에 따라 변하는 레이저 기믹입니다.
/// </summary>
[RequireComponent(typeof(TimeAffectedObject))] // TimeAffectedObject 컴포넌트 필수 요구
public class LaserGimmick : MonoBehaviour
{
    // === 레이저 주기 관련 변수 (Inspector에서 조정 가능) ===
    [Header("레이저 작동 주기 설정")]
    public float timeOn = 3.0f;     // 레이저가 나타나는 시간 (정상 시간 기준 n초)
    public float timeOff = 2.0f;    // 레이저가 사라지는 시간 (정상 시간 기준 m초)
    public float fadeDuration = 0.3f; // 레이저가 그려지듯 나타나고 사라지는 시간

    // === 컴포넌트 참조 ===
    private LineRenderer lineRenderer;
    private BoxCollider2D boxCollider; 
    private TimeAffectedObject timeAffect; // 🌟 시간 지연 시스템 참조

    private Vector3 startPos;         // LineRenderer의 로컬 시작 위치 (Position 0)
    private Vector3 endPosTarget;     // LineRenderer의 로컬 최종 목표 위치 (Position 1)

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        timeAffect = GetComponent<TimeAffectedObject>(); // 🌟 컴포넌트 가져오기
        
        startPos = lineRenderer.GetPosition(0); 
        endPosTarget = lineRenderer.GetPosition(1);

        if (boxCollider == null)
        {
            Debug.LogError("LaserGimmick 스크립트는 BoxCollider2D 컴포넌트가 필요합니다. 추가해주세요.");
        }
        if (timeAffect == null)
        {
            Debug.LogError("TimeAffectedObject 컴포넌트를 찾을 수 없습니다. LaserGimmick 오브젝트에 TimeAffectedObject를 추가했는지 확인해주세요.");
        }
    }

    private void Start()
    {
        lineRenderer.SetPosition(1, startPos);
        lineRenderer.enabled = false;
        boxCollider.enabled = false;
        
        UpdateCollider(0f); 
        
        StartCoroutine(LaserCycleCoroutine());
    }

    // 레이저 나타남/사라짐 반복 코루틴
    IEnumerator LaserCycleCoroutine()
    {
        while (true)
        {
            // 🌟 TimeOff 대기: 조정된 시간 계수를 사용
            yield return StartCoroutine(WaitForAdjustedSeconds(timeOff)); 
            
            yield return StartCoroutine(FadeLaser(true)); // 나타남
            
            // 🌟 TimeOn 대기: 조정된 시간 계수를 사용
            yield return StartCoroutine(WaitForAdjustedSeconds(timeOn)); 
            
            yield return StartCoroutine(FadeLaser(false)); // 사라짐
        }
    }
    
    /// <summary>
    /// 시간 계수에 따라 조정된 시간만큼 대기합니다.
    /// </summary>
    IEnumerator WaitForAdjustedSeconds(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            // 🌟 Time.deltaTime 대신 조정된 DeltaTime 사용
            timer += timeAffect.DeltaTime(); 
            yield return null;
        }
    }

    // 레이저 페이드 인/아웃 코루틴
    IEnumerator FadeLaser(bool isAppearing)
    {
        float startRatio = isAppearing ? 0f : 1f;
        float endRatio = isAppearing ? 1f : 0f;
        float timer = 0f;

        lineRenderer.enabled = true; 
        
        if (isAppearing)
        {
            boxCollider.enabled = true;
        }

        while (timer < fadeDuration)
        {
            // 🌟 Time.deltaTime 대신 조정된 DeltaTime 사용
            timer += timeAffect.DeltaTime(); 
            float t = timer / fadeDuration; 
            float currentRatio = Mathf.Lerp(startRatio, endRatio, t);

            Vector3 currentEndPos = Vector3.Lerp(startPos, endPosTarget, currentRatio);
            lineRenderer.SetPosition(1, currentEndPos);

            UpdateCollider(currentRatio);

            yield return null;
        }

        // --- 최종 상태 보장 및 컴포넌트 활성화/비활성화 ---
        if (isAppearing)
        {
            lineRenderer.SetPosition(1, endPosTarget);
            UpdateCollider(1f);
        }
        else // 사라짐 (Final Ratio = 0)
        {
            lineRenderer.SetPosition(1, startPos); 
            UpdateCollider(0f);
            lineRenderer.enabled = false; 
            boxCollider.enabled = false; 
        }
    }

    // 레이저 길이에 맞춰 BoxCollider2D의 크기와 위치를 동적으로 조정하는 함수
    void UpdateCollider(float lengthRatio)
    {
        Vector3 currentEndPos = Vector3.Lerp(startPos, endPosTarget, lengthRatio);
        float targetLength = Vector3.Distance(startPos, endPosTarget);
        float currentLength = targetLength * lengthRatio;
        
        float safeLength = Mathf.Max(0.01f, currentLength); 
        boxCollider.size = new Vector2(safeLength, lineRenderer.startWidth);

        Vector3 center = (startPos + currentEndPos) / 2f;
        boxCollider.offset = center;
    }

    // ⭐ 1. 충돌 시작 시 호출 (Enter)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // "Player" 태그 확인 및 콜라이더 활성화 상태 확인
        if (other.CompareTag("Player") && boxCollider.enabled)
        {
            DamagePlayer(other);
        }
    }

    // ⭐ 2. 충돌 지속 시 호출 (Stay) - 이것이 연속 피해의 핵심
    private void OnTriggerStay2D(Collider2D other)
    {
        // "Player" 태그 확인 및 콜라이더 활성화 상태 확인
        if (other.CompareTag("Player") && boxCollider.enabled)
        {
            DamagePlayer(other);
        }
    }
    
    // 피해 처리 로직을 위한 보조 함수
    private void DamagePlayer(Collider2D other)
    {
        PlayerHealthAndMovement playerScript = other.GetComponent<PlayerHealthAndMovement>();
            
        if (playerScript != null)
        {
            // TakeDamage 함수가 호출되면, 플레이어 스크립트 내부의 무적 시간(0.5초)에 따라
            // 피해를 입거나 무시됩니다.
            Vector2 laserDirection = (endPosTarget - startPos).normalized;
            playerScript.TakeDamage(laserDirection);
        }
    }
}