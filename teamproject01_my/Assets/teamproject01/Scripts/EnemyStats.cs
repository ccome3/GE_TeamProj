using UnityEngine;
using System.Collections;
using System.Linq; // Rigidbody2D 가져오기 등을 위해 추가

/// <summary>
/// 일반 적 유닛의 체력과 공격력을 관리하는 스크립트.
/// Dive Dash 공격을 받아 체력이 감소하고, 사망 시 회전하며 투명해지며 사라지는 기능을 포함합니다.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("능력치 설정")]

    public HealthBarController healthBar;
    private int maxHealth;
    public int health = 3;
    public int attackDamage = 1;

    [Header("피격 설정")]
    public float invulnerabilityDuration = 0.1f; 
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;

    [Header("사망 효과 설정")]
    public float fadeOutDuration = 0.5f;
    // 🌟 [추가] 초기 회전 속도 (초당 각도)
    public float initialRotationSpeed = 720f; 

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    // 🌟 [추가] 회전 효과를 위한 변수
    private bool isDying = false; 

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"EnemyStats: {gameObject.name}에 SpriteRenderer가 없습니다.");
            enabled = false;
            return;
        }
        originalColor = spriteRenderer.color;

        maxHealth = health;
    
    // 🌟 [추가] 시작 시 체력바 초기화
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health, maxHealth);
        }
    }

    private void Update()
    {
        if (isInvulnerable && !isDying) // 사망 중에는 무적 타이머 무시
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
                if (spriteRenderer != null && gameObject.activeInHierarchy)
                {
                    spriteRenderer.color = originalColor; 
                }
            }
        }
    }

    /// <summary>
    /// 적이 피해를 입었을 때 호출.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable || isDying) return; // 사망 중 다시 데미지 입는 것 방지

        health -= damageAmount;

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health, maxHealth);
        }

        if (health <= 0)
        {
            isDying = true; // 사망 플래그 설정
            StartCoroutine(DieCoroutine());
        }
        else
        {
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityDuration;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red; 
            }
        }
    }

    /// <summary>
    /// 적의 체력이 0이 되었을 때 호출. 회전 및 투명도 감소 후 비활성화.
    /// </summary>
    private IEnumerator DieCoroutine()
    {
        // 1. 충돌 및 물리 비활성화
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false; // 물리 시뮬레이션 중단
        }

        float timer = 0f;
        
        // 2. 피격 후 페이드 아웃 시작
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red; 
            yield return new WaitForSeconds(0.05f); 
        }

        // 3. 투명도(Alpha)와 회전(Rotation)을 동시에 처리
        float currentRotation = transform.eulerAngles.z; // 현재 회전 각도 가져오기
        
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeOutDuration;
            
            // 🌟 [핵심] 감속 회전 계산 🌟
            // Lerp(시작속도, 끝속도, t)를 통해 시간 t가 증가할수록 회전 속도가 0에 가까워지게 합니다.
            // t=0일 때 initialRotationSpeed, t=1일 때 0이 됩니다.
            float rotationSpeed = Mathf.Lerp(initialRotationSpeed, 0f, t);
            
            // 회전 적용: deltaTime과 Lerp된 속도를 곱하여 현재 프레임에서 회전할 각도를 구합니다.
            // 회전 방향은 임의로 -1을 곱하여 시계 반대 방향으로 설정했습니다.
            currentRotation += rotationSpeed * Time.deltaTime * -1f; 
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            // 투명도(Alpha) 감소
            if (spriteRenderer != null)
            {
                Color tempColor = originalColor;
                // t를 사용하여 1f에서 0f로 선형 보간 (Fade Out)
                tempColor.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = tempColor;
            }
            yield return null;
        }
        
        // 4. 오브젝트 비활성화 (투명도 및 회전이 끝난 후)
        gameObject.SetActive(false); 
    }
}