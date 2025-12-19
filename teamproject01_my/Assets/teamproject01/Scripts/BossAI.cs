using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Bullets, Dash, Rest, Dead }
    [Header("현재 상태")]
    public BossState currentState;

    [Header("추격 및 속도 설정")]
    public float moveSpeed = 4f;         
    public float detectionRange = 15f;   

    [Header("총알 패턴 설정")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.2f; 
    public int projectileDamage = 50;

    [Header("돌진 설정")]
    public GameObject indicatorObject;
    public SpriteRenderer indicatorSprite;
    public float dashSpeed = 22f;
    public float dashTime = 0.4f;
    public float manualIndicatorLength = 7f;

    [Header("사망 연출 설정")]
    public List<GameObject> wreckagesToDestroy; 
    public float slowMotionFactor = 0.2f; 
    public float slowMotionDuration = 2f;

    [Header("카메라 진동 설정")]
    public float shakeDuration = 0.5f; 
    public float shakeMagnitude = 0.2f; 

    private Transform playerTransform;
    private PlayerHealthAndMovement playerScript;
    private Rigidbody2D rb;
    private EnemyStats stats;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;
    private bool isActivated = false; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<EnemyStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerHealthAndMovement>();
        }
        
        if (indicatorObject != null) indicatorObject.SetActive(false);
        currentState = BossState.Idle;
    }

    private void Update()
    {
        if (isDead || stats.health <= 0) 
        {
            if (!isDead && stats.health <= 0) StartCoroutine(DieRoutine());
            return;
        }

        if (!isActivated && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= detectionRange)
            {
                isActivated = true;
                StartCoroutine(BossPatternLoop());
            }
            return; 
        }

        FlipSprite();
    }

    private IEnumerator BossPatternLoop()
    {
        while (!isDead)
        {
            currentState = BossState.Bullets;
            float bulletTimer = 0f;
            float shootTimer = 0f;

            while (bulletTimer < 5f)
            {
                MoveTowardsPlayer();
                shootTimer += Time.deltaTime;
                if (shootTimer >= fireRate)
                {
                    ShootProjectile();
                    shootTimer = 0f;
                }
                bulletTimer += Time.deltaTime;
                yield return null;
            }

            currentState = BossState.Dash;
            for (int i = 0; i < 3; i++)
            {
                yield return StartCoroutine(SingleDashSequence());
            }

            currentState = BossState.Rest;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            yield return new WaitForSeconds(3f);
        }
    }

    private void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;
        float moveDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null || playerTransform == null) return;
        Vector3 spawnPos = firePoint.position;
        spawnPos.z = 0f;
        GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        BossProjectile projScript = bullet.GetComponent<BossProjectile>();
        if (projScript != null)
        {
            Vector2 targetDir = (playerTransform.position - firePoint.position).normalized;
            projScript.Setup(targetDir);
        }
    }

    private IEnumerator SingleDashSequence()
    {
        rb.linearVelocity = Vector2.zero;

        // 🌟 돌진 준비 시점에 플레이어를 바라보도록 강제 업데이트
        ForceLookAtPlayer();

        if (indicatorObject != null)
        {
            UpdateIndicatorSize();
            indicatorObject.SetActive(true);
            for(int i=0; i<3; i++) {
                indicatorSprite.color = new Color(1, 0, 0, 0.5f);
                yield return new WaitForSeconds(0.1f);
                indicatorSprite.color = new Color(1, 0, 0, 0f);
                yield return new WaitForSeconds(0.1f);
            }
            indicatorObject.SetActive(false);
        }

        // 🌟 실제 돌진 직전에 다시 한번 플레이어 방향 확인 (문워크 방지)
        float dashDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        ForceLookAtPlayer(); // 돌진 방향에 맞춰 스프라이트 최종 고정

        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
        yield return new WaitForSeconds(dashTime);
        
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.2f); 
    }

    // 🌟 돌진 중 문워크를 방지하기 위해 강제로 방향을 돌리는 함수
    private void ForceLookAtPlayer()
    {
        if (playerTransform == null) return;
        
        float dirX = playerTransform.position.x - transform.position.x;
        if (dirX > 0f) {
            spriteRenderer.flipX = true;
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        } else {
            spriteRenderer.flipX = false;
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == BossState.Dash && other.CompareTag("Player"))
        {
            if (playerScript != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                playerScript.TakeDamage(stats.attackDamage, attackDirection);
            }
        }
    }

    private void UpdateIndicatorSize()
    {
        if (indicatorObject == null) return;
        Vector3 newScale = indicatorObject.transform.localScale;
        newScale.x = manualIndicatorLength; 
        indicatorObject.transform.localScale = newScale;
    }

    private void FlipSprite()
    {
        // 돌진 상태일 때는 Update에서 자동으로 Flip하지 않음 (코루틴에서 제어)
        if (currentState == BossState.Dash || playerTransform == null) return; 
        
        float dirX = playerTransform.position.x - transform.position.x;
        if (dirX > 0.1f) {
            spriteRenderer.flipX = true;
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        } else if (dirX < -0.1f) {
            spriteRenderer.flipX = false;
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        isActivated = false;
        currentState = BossState.Dead;
        StopAllCoroutines(); 
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(ShakeCamera(slowMotionDuration, shakeMagnitude));
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        foreach (GameObject obj in wreckagesToDestroy) { if (obj != null) Destroy(obj); }
        yield return new WaitForSecondsRealtime(slowMotionDuration);
        ResetTimeScale();
        Destroy(gameObject);
    }

    private void ResetTimeScale()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void OnDestroy() { ResetTimeScale(); }
    private void OnDisable() { ResetTimeScale(); }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Transform camTransform = Camera.main.transform;
        if (camTransform == null) yield break;
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        camTransform.localPosition = originalPos;
    }
}