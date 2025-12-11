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

    [Header("초기 지연 설정")]
    public float startDelay = 0f;   // 🌟 게임 시작 시, 첫 레이저가 나오기 전 대기 시간

    [Header("공격력 설정")]
    public int attackDamage = 1;    // 레이저 공격력

    // === 컴포넌트 참조 ===
    private LineRenderer lineRenderer;
    private BoxCollider2D boxCollider; 
    private TimeAffectedObject timeAffect; // 시간 지연 시스템 참조

    private Vector3 startPos;         // LineRenderer의 로컬 시작 위치 (Position 0)
    private Vector3 endPosTarget;     // LineRenderer의 로컬 최종 목표 위치 (Position 1)

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        timeAffect = GetComponent<TimeAffectedObject>(); 
        
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
        // 🌟 1. 초기 지연 시간 적용 (게임 시작 시 한 번만 실행)
        // 이 시간 동안 레이저는 꺼진 상태로 대기합니다.
        if (startDelay > 0f)
        {
            yield return StartCoroutine(WaitForAdjustedSeconds(startDelay));
        }

        // 🌟 2. 무한 반복 루프 시작
        while (true)
        {
            // TimeOff 대기: 조정된 시간 계수를 사용
            yield return StartCoroutine(WaitForAdjustedSeconds(timeOff)); 
            
            yield return StartCoroutine(FadeLaser(true)); // 나타남
            
            // TimeOn 대기: 조정된 시간 계수를 사용
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
            // Time.deltaTime 대신 조정된 DeltaTime 사용
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
            // Time.deltaTime 대신 조정된 DeltaTime 사용
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
        if (boxCollider.enabled)
        {
            // 🌟 1. 플레이어 탐지 및 데미지 (지속 데미지가 필요한 경우, 이 로직은 Enter 시에도 실행되어야 함)
            if (other.CompareTag("Player"))
            {
                DamagePlayer(other);
            }
            
            // 🌟 2. 적(Enemy) 탐지 및 데미지 (오직 진입 시 한 번의 데미지만 허용)
            EnemyStats enemy = other.GetComponent<EnemyStats>();
            if (enemy != null)
            {
                DamageEnemy(enemy);
            }
        }
    }

    private void DamageEnemy(EnemyStats enemy)
    {
        enemy.TakeDamage(attackDamage);
    }

    // ⭐ 2. 충돌 지속 시 호출 (Stay) - 🌟 적 처리 로직 제거 🌟
    private void OnTriggerStay2D(Collider2D other)
    {
        if (boxCollider.enabled)
        {
            // 🌟 오직 플레이어만 처리 (플레이어는 지속 데미지 허용)
            if (other.CompareTag("Player"))
            {
                DamagePlayer(other);
            }
            
            // 🚨 EnemyStats 체크 및 DamageEnemy(enemy); 로직을 여기서 삭제합니다.
            // 이로 인해 적은 레이저 위에 계속 있어도 추가 데미지를 입지 않습니다.
        }
    }
    
    // 피해 처리 로직을 위한 보조 함수
    private void DamagePlayer(Collider2D other)
    {
        PlayerHealthAndMovement playerScript = other.GetComponent<PlayerHealthAndMovement>();
            
        if (playerScript != null)
        {
            Vector2 laserDirection = (endPosTarget - startPos).normalized;
            playerScript.TakeDamage(attackDamage, laserDirection);
        }
    }
}