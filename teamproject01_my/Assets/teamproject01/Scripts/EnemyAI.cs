using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Chase, Indicator, Dash, Cooldown }
    
    [Header("상태 확인")]
    public State currentState = State.Idle;

    [Header("공격 예고 설정")]
    public GameObject indicatorObject;   
    public SpriteRenderer indicatorSprite; 
    public float indicatorDuration = 0.5f; 
    // 🌟 수동 거리 설정 변수 (인스펙터에서 몬스터 돌진 거리와 맞게 조절하세요)
    public float manualIndicatorLength = 5f; 

    [Header("거리 설정")]
    public float detectionRange = 10f;   
    public float patrolRange = 3f;      

    [Header("속도 설정")]
    public float patrolSpeed = 1.5f;    
    public float moveSpeed = 3f;        
    public float dashSpeed = 18f;       

    [Header("시간 설정")]
    public float chaseDuration = 3f;    
    public float cooldownDuration = 3f;  
    public float dashTime = 0.4f;       

    private Transform playerTransform;
    private PlayerHealthAndMovement playerScript;
    private Rigidbody2D rb;
    private EnemyStats stats;
    private SpriteRenderer spriteRenderer;
    
    private bool isPatternRunning = false;
    private Vector2 spawnPosition; 
    private int patrolDirection = 1;

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
        
        spawnPosition = transform.position;

        if (indicatorObject != null) indicatorObject.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null || stats.health <= 0) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (!isPatternRunning)
        {
            if (distance <= detectionRange)
                StartCoroutine(AttackPatternRoutine());
            else
                HandlePatrol();
        }

        if (currentState == State.Dash)
            CheckDashOverlap();

        if (currentState != State.Dash && currentState != State.Indicator)
            FlipSprite();
    }

    // 🌟 수동으로 설정한 길이를 적용하는 함수
    private void UpdateIndicatorSize()
    {
        if (indicatorObject == null) return;

        Vector3 newScale = indicatorObject.transform.localScale;
        newScale.x = manualIndicatorLength; 
        indicatorObject.transform.localScale = newScale;
    }

    private void HandlePatrol()
    {
        currentState = State.Idle;
        float currentRelativeX = transform.position.x - spawnPosition.x;
        if (Mathf.Abs(currentRelativeX) >= patrolRange)
            patrolDirection = currentRelativeX > 0 ? -1 : 1;

        rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
    }

    private IEnumerator AttackPatternRoutine()
    {
        isPatternRunning = true;

        // 1. 추격 단계
        currentState = State.Chase;
        float timer = 0f;
        while (timer < chaseDuration)
        {
            if (Vector2.Distance(transform.position, playerTransform.position) > detectionRange + 2f)
            {
                isPatternRunning = false;
                yield break;
            }
            float moveDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 공격 예고 단계
        currentState = State.Indicator;
        rb.linearVelocity = Vector2.zero; 
        UpdateIndicatorSize(); // 수동 설정값 적용

        if (indicatorObject != null)
        {
            indicatorObject.SetActive(true);
            float iTimer = 0f;
            while (iTimer < indicatorDuration)
            {
                if (indicatorSprite != null)
                {
                    Color c = indicatorSprite.color;
                    c.a = (c.a < 0.1f) ? 0.5f : 0.0f;
                    indicatorSprite.color = c;
                }
                yield return new WaitForSeconds(0.1f);
                iTimer += 0.1f;
            }
        }

        // 3. 돌진 단계
        if (indicatorObject != null) indicatorObject.SetActive(false);
        currentState = State.Dash;
        float dashDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashTime);

        // 4. 대기 단계
        currentState = State.Cooldown;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(cooldownDuration);

        isPatternRunning = false;
    }

    private void CheckDashOverlap()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, 1f); 
        foreach (var col in hitPlayers)
        {
            if (col.CompareTag("Player"))
                ApplyDamageToPlayer(col.gameObject);
        }
    }

    private void ApplyDamageToPlayer(GameObject player)
    {
        if (playerScript != null)
        {
            Vector2 attackDirection = (player.transform.position - transform.position).normalized;
            playerScript.TakeDamage(stats.attackDamage, attackDirection);
        }
    }

    private void FlipSprite()
    {
        float moveX = rb.linearVelocity.x;
        if (moveX > 0.1f) 
        {
            spriteRenderer.flipX = true; 
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (moveX < -0.1f) 
        {
            spriteRenderer.flipX = false;
            if (indicatorObject != null) indicatorObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == State.Dash && other.CompareTag("Player"))
            ApplyDamageToPlayer(other.gameObject);
    }
}