using UnityEngine;
using System.Collections;
using System.Linq; 

public class PlayerHealthAndMovement : MonoBehaviour
{
    // === 플레이어 스탯 및 상태 ===
    [Header("플레이어 스탯")]
    public int health = 3;
    public float movementSpeed = 5.0f;
    public float jumpForce = 10.0f;

    [Header("피격 설정")]
    public float invulnerabilityDuration = 0.5f;
    public float knockbackForce = 10.0f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    public float hitFlashDuration = 0.2f;
    private Color originalColor;

    // 대쉬 설정
    [Header("대쉬 설정")]
    public float dashDistance = 5.0f; 
    public float dashDuration = 0.2f; 
    public float dashInvulnerabilityDuration = 0.5f; 
    public float dashCooldown = 1.0f; 
    private bool isDashing = false; 
    private float dashCooldownTimer = 0f;
    
    [Header("점프 최적화 설정")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);
    private bool jumpCommand = false;
    
    // 🌟 튜토리얼 관련 변수 🌟
    [Header("튜토리얼 시스템")]
    public TutorialManager tutorialManager; // Dash 튜토리얼 종료를 위해 할당 필요
    public bool isDashTutorialActive = false; // Dash 튜토리얼 모드 활성화 여부

    // === 사다리 기믹 변수 ===
    [Header("사다리 설정")]
    public float ladderClimbSpeed = 3.5f; // 사다리 타는 속도
    public float gravityScaleOnLadder = 0.0f; // 사다리 탈 때 중력 (0으로 설정)
    private bool isClimbingLadder = false; // 현재 사다리를 타고 있는지 여부
    private GameObject currentLadder = null; // 현재 닿아있는 사다리 오브젝트
    private float originalGravityScale; // 원래 중력 스케일 저장
    private int originalLayer; // 원래 레이어 저장 변수 (충돌 무시용)
    
    // === 덩쿨 로프 스윙 기믹 변수 ===
    [Header("로프 스윙 설정")]
    public float vineDetectionRadius = 2.0f;
    public float swingForce = 50f;
    public float ropeExtendDuration = 0.15f; 
    public float ropeWidth = 0.05f; 
    
    public float launchBoost = 1.3f; 
    public float ropeAdjustSpeed = 5.0f; 
    
    public LayerMask vineLayer; 
    
    private DistanceJoint2D vineJoint; 
    private float ropeLength; 
    private float verticalRopeInput = 0f; 

    private LineRenderer ropeRenderer;
    private bool isSwinging = false;
    private bool isRopeExtending = false;
    private GameObject currentVinePivot; 
    private const string VINE_TAG = "VinePivot";
    public Material ropeMaterial;

    private Collider2D vinePivotCollider; 

    // === 컴포넌트 ===
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // Animator 컴포넌트

    [Header("대쉬 잔상 설정")]
    public float trailClearDelay = 0f; // 트레일이 완전히 사라질 때까지의 지연 시간
    public TrailRenderer trailRenderer; // 새로 추가

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // Animator 컴포넌트 가져오기
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Player 오브젝트에 Animator 컴포넌트가 없습니다! 애니메이션 기능을 사용할 수 없습니다.");
        }

        // 원본 물리 및 레이어 스케일 저장
        originalGravityScale = rb.gravityScale; 
        originalLayer = gameObject.layer; // 현재 플레이어의 원래 레이어 저장

        if (trailRenderer == null)
        {
            if (GetComponent<TrailRenderer>() != null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
                trailRenderer.enabled = false;
            }
        }
        else
        {
            trailRenderer.enabled = false; // 시작 시 비활성화
        }
        
        // Line Renderer를 미리 가져오고 초기화
        ropeRenderer = GetComponent<LineRenderer>();
        if (ropeRenderer == null)
        {
            Debug.LogError("Player 오브젝트에 LineRenderer 컴포넌트를 추가해야 합니다! 로프 기능 비활성화.");
            enabled = false; 
            return;
        }
        
        // LineRenderer 안정화 및 시각화 초기화
        ropeRenderer.positionCount = 2; 
        ropeRenderer.material = ropeMaterial; 
        ropeRenderer.startWidth = ropeWidth;
        ropeRenderer.endWidth = ropeWidth;
        
        Color ropeColor = ropeRenderer.material.color;
        ropeColor.a = 0.0f;
        ropeRenderer.material.color = ropeColor;
    }

    private void Update()
    {
        // 무중력 공간 진입 시 로프 강제 해제 및 점프 방지 로직 (대쉬/사다리 중이 아닐 때만)
        if (rb.gravityScale == 0f && !isDashing && !isClimbingLadder)
        {
            if (isSwinging || isRopeExtending)
            {
                ReleaseVine(); 
            }
            jumpCommand = false; 
        }

        // 무적 시간 쿨다운
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }
        
        // 대쉬 쿨타임 갱신
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // 🌟 Dash 튜토리얼 활성화 시 대쉬 입력 외 모든 입력 무시
        if (!isDashTutorialActive)
        {
            // ************** 사다리 타기 로직 **************
            // 1. 사다리 타기 시작 입력 감지 (W 또는 S 키를 누르고 사다리 범위 내에 있을 때)
            if (currentLadder != null) 
            {
                bool isClimbInputPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S);
                
                if (isClimbInputPressed && !isClimbingLadder && !isDashing && !isSwinging)
                {
                    StartClimbing();
                }
            }
            
            // 2. 사다리에서 내려오기/뛰어내리기 감지 (사다리 타는 중일 때)
            if (isClimbingLadder)
            {
                if (Input.GetButtonDown("Jump")) 
                {
                    StopClimbing(true); 
                }
            }
            
            // ************** 사다리 타기 로직 종료 **************


            // 점프 입력 (대쉬, 스윙, 사다리 중 점프 방지)
            if (Input.GetButtonDown("Jump") && IsGrounded() && !isSwinging && !isDashing && !isClimbingLadder)
            {
                jumpCommand = true;
            }

            // 덩쿨 판정 범위 내의 Pivot 확인 
            if (!isRopeExtending && !isSwinging) 
            {
                CheckForVine();
            }

            // 잡기 (Shift 키 누름)
            if (Input.GetKeyDown(KeyCode.LeftShift) && !isSwinging && !isRopeExtending && currentVinePivot != null && !isClimbingLadder)
            {
                StartCoroutine(ExtendRopeAndGrab(currentVinePivot));
            }

            // 놓기 (Shift 키 뗌)
            if (Input.GetKeyUp(KeyCode.LeftShift) && (isSwinging || isRopeExtending))
            {
                ReleaseVine();
            }

            // 로프 길이 조절 입력 감지 로직 (W/S)
            if (isSwinging)
            {
                verticalRopeInput = Input.GetAxisRaw("Vertical");
            }
            else
            {
                verticalRopeInput = 0f;
            }
        } // 🌟 Dash 튜토리얼 모드 제어 영역 끝

        // LineRenderer 시각화 (Dash 튜토리얼 중에도 로프 시각화는 필요할 수 있으므로 밖에 둠)
        if (ropeRenderer == null) return; 
        if (ropeRenderer.positionCount != 2) ropeRenderer.positionCount = 2;

        Color ropeColor = ropeRenderer.material.color;
        
        // 스윙 중이거나 뻗는 중일 때 (활성화 상태)
        if (isSwinging || isRopeExtending)
        {
            ropeColor.a = 1.0f; 
            ropeRenderer.SetPosition(0, transform.position); 
            
            if (currentVinePivot != null)
            {
                ropeRenderer.SetPosition(1, currentVinePivot.transform.position); 
            }
            else
            {
                ropeRenderer.SetPosition(1, transform.position); 
            }
        }
        else if (currentVinePivot != null) // 감지 범위 내에 있을 때 (반투명)
        {
            ropeColor.a = 0.5f; 
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, currentVinePivot.transform.position); 
        }
        else // 아무것도 없을 때 (투명)
        {
            ropeColor.a = 0.0f;
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, transform.position);
        }
        
        ropeRenderer.material.color = ropeColor;
        
        // 🌟 [핵심 수정] 대쉬 입력 감지 및 튜토리얼 해제 로직 🌟
        if (Input.GetMouseButtonDown(1) && !isDashing && dashCooldownTimer <= 0)
        {
            // 튜토리얼 중이라면 대쉬 시작 '직전'에 시간을 먼저 풀고 상태를 해제합니다.
            if (isDashTutorialActive)
            {
                // 1. 시간을 먼저 정상화 (그래야 물리 엔진이 돌아서 대쉬가 나감)
                Time.timeScale = 1f; 
                
                // 2. 매니저에게 튜토리얼 종료(UI 숨김 등) 요청
                if (tutorialManager != null)
                {
                    tutorialManager.EndDashTutorial();
                }
                
                // 3. 플레이어 상태 변수 해제 (이제 일반 상태로 돌아감)
                isDashTutorialActive = false; 
            }

            // 기존 로직 수행
            if (isSwinging || isRopeExtending)
            {
                ReleaseVine();
            }
            if (isClimbingLadder)
            {
                StopClimbing(false); 
            }
            
            // 4. 시간이 흐르는 상태에서 대쉬 코루틴 시작!
            StartCoroutine(DashCoroutine());
        }


        // 🌟 애니메이션 상태 업데이트 (입력 즉시 반응을 위해 Update에서 호출)
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        // 🌟 튜토리얼 모드 중에는 물리 이동을 막습니다.
        if (isDashTutorialActive && !isDashing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 대쉬 중에는 다른 물리 로직을 막습니다.
        if (isDashing)
        {
            return; 
        }

        // ************** 사다리 이동 로직 (최우선) **************
        if (isClimbingLadder)
        {
            HandleLadderClimbing();
            return; 
        }
        // ************** 사다리 이동 로직 종료 **************
        
        if (isSwinging)
        {
            AdjustRopeLength();
            ApplySwingForce();
        }
        else
        {
            HandleMovement(); 
        }

        if (jumpCommand)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCommand = false;
        }
    }

    // === 애니메이션 제어 전용 함수 ===
    void UpdateAnimationState()
    {
        if (animator == null) return;
        
        float moveInput = Input.GetAxisRaw("Horizontal");
        float targetSpeed = 0f;

        // 🌟 튜토리얼 중에는 이동 애니메이션을 막습니다.
        if (!isClimbingLadder && !isDashing && !isSwinging && !isDashTutorialActive)
        {
            if (moveInput != 0 && IsGrounded()) 
            {
                targetSpeed = movementSpeed;
            }
            else
            {
                targetSpeed = 0f;
            }
        }
        
        animator.SetFloat("Speed", targetSpeed); 
    }
    
    // === 사다리 기믹 전용 함수 ===

    private void StartClimbing()
    {
        isClimbingLadder = true;
        
        gameObject.layer = LayerMask.NameToLayer("LadderClimber"); 
        
        rb.gravityScale = gravityScaleOnLadder;
        rb.linearVelocity = Vector2.zero;

        if (isSwinging || isRopeExtending)
        {
            ReleaseVine();
        }
        
        if (currentLadder != null)
        {
            float targetX = currentLadder.transform.position.x;
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        }
        
        Debug.Log("사다리 타기 시작.");
    }

    private void StopClimbing(bool triggerJump)
    {
        isClimbingLadder = false;
        
        gameObject.layer = originalLayer; 
        
        rb.gravityScale = originalGravityScale;
        currentLadder = null; 

        if (triggerJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
            Debug.Log("사다리에서 점프하여 이탈.");
        }
        else
        {
            Debug.Log("사다리 타기 종료.");
        }
    }
    
    private void HandleLadderClimbing()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        
        rb.linearVelocity = new Vector2(0f, verticalInput * ladderClimbSpeed);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            currentLadder = other.gameObject;
            Debug.Log("사다리 범위 진입.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            if (other.gameObject == currentLadder)
            {
                if (isClimbingLadder)
                {
                    StopClimbing(false); 
                }
                currentLadder = null;
                Debug.Log("사다리 범위 이탈.");
            }
        }
    }
    
    // === 대쉬 기믹 전용 함수 ===
    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        
        if (trailRenderer != null)
        {
            trailRenderer.Clear(); 
            trailRenderer.enabled = true; 
        }

        isInvulnerable = true;
        invulnerabilityTimer = dashInvulnerabilityDuration;
        
        dashCooldownTimer = dashCooldown;
        
        float direction = spriteRenderer.flipX ? -1f : 1f; 
        float requiredSpeed = dashDistance / dashDuration; 
        Vector2 dashVelocity = new Vector2(direction * requiredSpeed, 0); 
        
        float originalGravity = rb.gravityScale;
        
        if (!isClimbingLadder)
        {
            gameObject.layer = originalLayer;
        }

        rb.gravityScale = 0f; 
        rb.linearVelocity = dashVelocity; 

        // 🌟 수정: 이미 Update에서 시간을 풀고 들어왔으므로 일반 WaitForSeconds 사용
        yield return new WaitForSeconds(dashDuration);
        
        rb.gravityScale = originalGravity; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y); 
        
        isDashing = false;
        
        // 잔상 비활성화 (지연 시간 후)
        if (trailRenderer != null)
        {
            yield return new WaitForSeconds(trailClearDelay); 
            trailRenderer.enabled = false;
        }
    }

    // 🌟 Dash 튜토리얼 모드 설정 함수 🌟
    public void SetDashTutorialMode(bool isActive)
    {
        isDashTutorialActive = isActive;
        if (isActive)
        {
            // 튜토리얼 시작 시 현재 속도 초기화
            rb.linearVelocity = Vector2.zero;
        }
    }

    // === 로프/스윙 기믹 전용 함수 (생략... 기존 내용 유지) ===
    private void CheckForVine() { /* ... */ }
    private IEnumerator ExtendRopeAndGrab(GameObject pivot) { yield return null; }
    private void GrabVine(GameObject pivot) { /* ... */ }
    private void ReleaseVine() { /* ... */ }
    private void AdjustRopeLength() { /* ... */ }
    private void ApplySwingForce() { /* ... */ }
    
    // HandleMovement 함수
    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        
        if (moveInput != 0)
        {
            rb.linearVelocity = new Vector2(moveInput * movementSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (moveInput > 0) { spriteRenderer.flipX = false; }
        else if (moveInput < 0) { spriteRenderer.flipX = true; }
    }

    bool IsGrounded()
    {
        if (groundCheck == null) { Debug.LogError("GroundCheck Transform이 설정되지 않았습니다!"); return false; }
        Collider2D hit = Physics2D.OverlapBox(
            groundCheck.position, 
            groundCheckSize, 
            0f, 
            groundLayer);
        return hit != null;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        
        if (!isSwinging && !isRopeExtending)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, vineDetectionRadius);
        }
    }

    public void TakeDamage(Vector2 laserDirection)
    {
        if (isInvulnerable) return; 
        
        health -= 1;
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
        
        StartCoroutine(HitFlashCoroutine());
        rb.linearVelocity = Vector2.zero;
        
        Vector2 finalKnockback = new Vector2(-laserDirection.x * knockbackForce, knockbackForce * 0.5f);
        rb.AddForce(finalKnockback, ForceMode2D.Impulse);

        if (health <= 0) Debug.Log("Game Over!");
    }

    IEnumerator HitFlashCoroutine()
    {
        spriteRenderer.color = Color.red; 
        yield return new WaitForSeconds(hitFlashDuration); 
        spriteRenderer.color = originalColor; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSwinging && (groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            ReleaseVine(); 
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y * 0.1f); 
        }
    }
}