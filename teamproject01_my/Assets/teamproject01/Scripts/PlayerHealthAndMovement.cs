using UnityEngine;
using System.Collections;
using System.Linq; 

public class PlayerHealthAndMovement : MonoBehaviour
{
    // === 플레이어 스탯 및 상태 ===
    [Header("플레이어 스탯")]
    public int health = 1000;
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
    
    // 🌟 [추가] 낙하 대쉬 설정 (Dive Dash) 🌟
    [Header("낙하 대쉬 (Dive Dash) 설정")]
    public float diveDashSpeed = 20.0f; // 내리꽂는 속도
    public float diveDashDistance = 100.0f; // 최대 이동 거리 (땅바닥까지 이동하는 것의 안전 장치)
    public float diveDashInvulnerabilityDuration = 0.3f; // 무적 시간
    public float diveDashCooldown = 0.5f; // 쿨다운
    private bool isDiveDashing = false;
    private float diveDashCooldownTimer = 0f;
    private bool hasLandedAfterDive = false; // 낙하 대쉬 후 착지 여부

    [Header("점프 최적화 설정")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);
    private bool jumpCommand = false;
    
    // 🌟 튜토리얼 관련 변수 🌟
    [Header("튜토리얼 시스템")]
    public TutorialManager tutorialManager; // Dash 튜토리얼 종료를 위해 할당 필요
    public bool isDashTutorialActive = false; // Dash 튜토리얼 모드 활성화 여부
    public bool isRopeTutorialActive = false;

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
        if (rb.gravityScale == 0f && !isDashing && !isClimbingLadder && !isDiveDashing) // 🌟 [수정] Dive Dash 상태 추가
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

        // 🌟 [추가] 낙하 대쉬 쿨타임 갱신
        if (diveDashCooldownTimer > 0)
        {
            diveDashCooldownTimer -= Time.deltaTime;
        }

        // ==============================================================================
        // 🌟 [핵심 로직] Left Click (Dive Dash Key) 입력 처리 🌟
        // ==============================================================================
        if (Input.GetMouseButtonDown(0))
        {
            // 🌟 공중 상태여야 하고, 기존 대쉬/스윙/낙하대쉬 중이 아니어야 하며, 쿨타임이 지나야 합니다.
            if (!IsGrounded() && !isDashing && !isSwinging && !isRopeExtending && !isDiveDashing && diveDashCooldownTimer <= 0)
            {
                // 로프를 잡고 있다면 해제
                if (isSwinging || isRopeExtending)
                {
                    ReleaseVine();
                }
                
                StartCoroutine(DiveDashCoroutine());
                // 낙하 대쉬는 튜토리얼 멈춤 해제 로직이 없으므로 추가적인 튜토리얼 플래그 검사는 생략합니다.
            }
        }

        // ==============================================================================
        // 🌟 [핵심 로직] RClick (Dash Key) 입력 처리: 기존 로직
        // ==============================================================================
        if (Input.GetMouseButtonDown(1))
        {
            bool wasTimeStoppedByTutorial = false;
            
            // 1. 튜토리얼 모드이고 시간이 멈춰 있다면, 시간을 풀고 플래그 해제
            if (Time.timeScale == 0f && isDashTutorialActive)
            {
                Time.timeScale = 1f; 
                rb.gravityScale = originalGravityScale; 
                if (tutorialManager != null)
                {
                    tutorialManager.EndDashTutorial();
                }
                isDashTutorialActive = false;
                wasTimeStoppedByTutorial = true;
            }
            
            // 2. 시간이 멈춰있지 않고, 대쉬 조건이 맞을 때만 실행 (일반적인 경우)
            // OR 시간이 방금 풀렸다면 (wasTimeStoppedByTutorial), 무조건 대쉬 실행
            if (!isDiveDashing && (wasTimeStoppedByTutorial || (!isDashing && dashCooldownTimer <= 0))) // 🌟 [수정] Dive Dash 상태 중 Dash 방지
            {
                // 일반 로직 수행 (튜토리얼 해제 시에도 대쉬가 바로 나가도록)
                if (isSwinging || isRopeExtending)
                {
                    ReleaseVine();
                }
                if (isClimbingLadder)
                {
                    StopClimbing(false); 
                }
                
                StartCoroutine(DashCoroutine());
            }
            
            if (wasTimeStoppedByTutorial) return;
        }


        // ==============================================================================
        // 🌟 [핵심 로직] LeftShift (Rope Key) 입력 처리: 기존 로직
        // ==============================================================================
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            bool wasTimeStoppedByTutorial = false;

            // 1. 튜토리얼 모드이고 시간이 멈춰 있다면, 시간을 풀고 플래그 해제
            if (Time.timeScale == 0f && isRopeTutorialActive)
            {
                Time.timeScale = 1f; 
                rb.gravityScale = originalGravityScale; 

                // 멈춤 해제 시 모든 로프 상태를 강제 해제 (Stuck 방지)
                if (isSwinging || isRopeExtending)
                {
                    ReleaseVine();
                }
                
                if (tutorialManager != null)
                {
                    tutorialManager.EndRopeTutorial();
                }
                isRopeTutorialActive = false;
                wasTimeStoppedByTutorial = true;
            }

            // 2. 시간이 멈춰있지 않고, 로프 잡기 조건이 맞을 때만 실행 (일반적인 경우)
            // OR 시간이 방금 풀렸다면 (wasTimeStoppedByTutorial), 로프 잡기 조건 체크 후 실행
            if (!isDiveDashing && (wasTimeStoppedByTutorial || (!isSwinging && !isRopeExtending && currentVinePivot != null && !isClimbingLadder))) // 🌟 [수정] Dive Dash 상태 중 Rope Grab 방지
            {
                // 로프 잡기는 currentVinePivot != null 조건이 필요하므로 조건을 여기서 다시 체크합니다.
                if (currentVinePivot != null)
                {
                    StartCoroutine(ExtendRopeAndGrab(currentVinePivot));
                }
            }
            
            if (wasTimeStoppedByTutorial) return;
        }

        // 🌟 [일반 입력 영역] Dash 또는 Rope 튜토리얼이 활성화되어 있으면 이 안의 모든 입력 무시
        if (!isDashTutorialActive && !isRopeTutorialActive)
        {
            // 3. 놓기 (Shift 키 뗌)
            if (Input.GetKeyUp(KeyCode.LeftShift) && (isSwinging || isRopeExtending))
            {
                ReleaseVine();
            }
            
            // ************** 사다리 타기 로직 **************
            // 1. 사다리 타기 시작 입력 감지 (W 또는 S 키를 누르고 사다리 범위 내에 있을 때)
            if (currentLadder != null) 
            {
                bool isClimbInputPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S);
                
                // 🌟 [수정] Dive Dash 상태 중 사다리 진입 방지
                if (isClimbInputPressed && !isClimbingLadder && !isDashing && !isSwinging && !isDiveDashing) 
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


            // 점프 입력 (대쉬, 스윙, 사다리, 낙하대쉬 중 점프 방지)
            if (Input.GetButtonDown("Jump") && IsGrounded() && !isSwinging && !isDashing && !isClimbingLadder && !isDiveDashing) // 🌟 [수정] Dive Dash 상태 중 점프 방지
            {
                jumpCommand = true;
            }

            // 덩쿨 판정 범위 내의 Pivot 확인 
            if (!isRopeExtending && !isSwinging) 
            {
                CheckForVine();
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
        } // 🌟 일반 입력 방지 영역 끝

        // LineRenderer 시각화
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
            // 튜토리얼 중이 아닐 때만 반투명 로프 표시
            if (!isRopeTutorialActive)
            {
                ropeColor.a = 0.5f; 
                ropeRenderer.SetPosition(0, transform.position);
                ropeRenderer.SetPosition(1, currentVinePivot.transform.position); 
            }
            else // 로프 튜토리얼 중에는 로프를 아예 숨길 수도 있습니다. (선택 사항)
            {
                ropeColor.a = 0.0f;
                ropeRenderer.SetPosition(0, transform.position);
                ropeRenderer.SetPosition(1, transform.position);
            }
        }
        else // 아무것도 없을 때 (투명)
        {
            ropeColor.a = 0.0f;
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, transform.position);
        }
        
        ropeRenderer.material.color = ropeColor;
        
        // 애니메이션 상태 업데이트 (입력 즉시 반응을 위해 Update에서 호출)
        UpdateAnimationState();
    }
    
    private void FixedUpdate()
    {
        // 🌟 튜토리얼 모드 중에는 물리 이동을 막습니다.
        if ((isDashTutorialActive || isRopeTutorialActive) && !isDashing && !isDiveDashing) // 🌟 [수정] Dive Dash 상태 제외
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 대쉬 또는 낙하 대쉬 중에는 다른 물리 로직을 막습니다.
        if (isDashing || isDiveDashing) // 🌟 [수정] Dive Dash 상태 추가
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

        // 🌟 [추가] 낙하 대쉬 종료 후 착지 감지 및 속도 정리
        if (hasLandedAfterDive && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            hasLandedAfterDive = false;
            Debug.Log("낙하 대쉬 후 착지 완료.");
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

        // 🌟 튜토리얼, 대쉬, 스윙, 낙하 대쉬 중에는 이동 애니메이션을 막습니다.
        if (!isClimbingLadder && !isDashing && !isSwinging && !isDashTutorialActive && !isDiveDashing)
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
        animator.SetBool("IsDashing", isDashing || isDiveDashing); // 🌟 [수정] 낙하 대쉬도 대쉬 애니메이션 사용
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

    /// <summary>
    /// 🌟 [추가] 공중에서 땅으로 내리꽂는 낙하 대쉬 코루틴
    /// </summary>
    private IEnumerator DiveDashCoroutine()
    {
        isDiveDashing = true;
        
        if (trailRenderer != null)
        {
            trailRenderer.Clear(); 
            trailRenderer.enabled = true; 
        }

        isInvulnerable = true;
        invulnerabilityTimer = diveDashInvulnerabilityDuration;
        
        diveDashCooldownTimer = diveDashCooldown;

        float originalGravity = rb.gravityScale;
        Vector3 startPosition = transform.position;
        
        rb.gravityScale = 0f; // 중력 비활성화
        rb.linearVelocity = new Vector2(0f, -diveDashSpeed); // 수직 하강 속도 적용

        // 🌟 레이캐스트를 사용하여 바닥까지의 거리를 확인합니다.
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            diveDashDistance, 
            groundLayer);

        float travelDistance = diveDashDistance;
        if (hit.collider != null)
        {
            // 착지 지점까지의 거리
            travelDistance = hit.distance; 
        }
        
        // 이동에 걸릴 예상 시간
        float travelDuration = travelDistance / diveDashSpeed;
        float startTime = Time.time;
        
        // 이동 루프 (시간 또는 거리에 의해 제한)
        while (Time.time < startTime + travelDuration && !IsGrounded())
        {
            // 땅에 닿을 때까지 다음 프레임을 기다립니다.
            // 물리 업데이트는 FixedUpdate에서 진행되므로, 여기서는 시간만 체크합니다.
            if (IsGrounded()) break;
            yield return null; 
        }

        // 땅에 닿았거나 최대 거리에 도달했습니다.
        rb.gravityScale = originalGravity; 
        rb.linearVelocity = Vector2.zero; // 속도 초기화 (충격 효과)
        
        isDiveDashing = false;
        hasLandedAfterDive = true; // FixedUpdate에서 착지 처리용 플래그

        // 잔상 비활성화 (지연 시간 후)
        if (trailRenderer != null)
        {
            yield return new WaitForSeconds(trailClearDelay); 
            trailRenderer.enabled = false;
        }
        
        Debug.Log("낙하 대쉬 종료.");
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

    public void SetRopeTutorialMode(bool isActive)
    {
        isRopeTutorialActive = isActive;
        if (isActive)
        {
            // 튜토리얼 시작 시 현재 속도 초기화
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void CheckForVine(){ 
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position,
            vineDetectionRadius,
            vineLayer);

        currentVinePivot = hitColliders
            .Select(col => col.gameObject)
            .FirstOrDefault(go => go.CompareTag(VINE_TAG) && go.transform.position.y > transform.position.y);
    }
    private IEnumerator ExtendRopeAndGrab(GameObject pivot)
    {
        isRopeExtending = true;
        Vector3 endPos = pivot.transform.position;
        float timer = 0f;
        while (timer < ropeExtendDuration)
        {
            timer += Time.deltaTime;
            float t = timer / ropeExtendDuration;
            Vector3 startPos = transform.position;
            Vector3 currentRopeEnd = Vector3.Lerp(startPos, endPos, t);
            if (ropeRenderer != null)
            {
                ropeRenderer.SetPosition(0, startPos);
                ropeRenderer.SetPosition(1, currentRopeEnd);
            }
            yield return null;
        }
        if (ropeRenderer != null)
        {
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, endPos);
        }
        GrabVine(pivot);
        isRopeExtending = false;

    }
    private void GrabVine(GameObject pivot)
    {
        vinePivotCollider = pivot.GetComponent<Collider2D>();
        if (vinePivotCollider != null)
        {
            vinePivotCollider.enabled = false;
            Debug.Log("Pivot Collider 비활성화 - 충돌 문제 방지");
        }

        vineJoint = gameObject.AddComponent<DistanceJoint2D>();
        vineJoint.connectedBody = pivot.GetComponent<Rigidbody2D>();
        ropeLength = Vector2.Distance(transform.position, pivot.transform.position);
        vineJoint.distance = ropeLength;
        vineJoint.anchor = Vector2.zero;
        vineJoint.connectedAnchor = Vector2.zero;
        vineJoint.autoConfigureDistance = false;
        vineJoint.enableCollision = false;
        rb.linearVelocity = Vector2.zero;
        isSwinging = true;
        rb.AddForce(Vector2.down * 0.1f, ForceMode2D.Impulse);
        Debug.Log("로프 연결 완료! Distance Joint 사용.");
    }

    private void ReleaseVine()
    {
        StopAllCoroutines();
        isRopeExtending = false;
        if (vineJoint != null)
        {
            Vector2 launchVelocity = rb.linearVelocity;
            isSwinging = false;
            Destroy(vineJoint);
            rb.linearVelocity = launchVelocity * launchBoost;
            Debug.Log("로프 해제 및 반동 시작. 발사 속도: " + rb.linearVelocity.magnitude);
        }
        else if (isRopeExtending)
        {
            Debug.Log("로프 발사 취소.");
        }
        if (vinePivotCollider != null)
        {
            vinePivotCollider.enabled = true;
            vinePivotCollider = null;
        }
        currentVinePivot = null;
        if (ropeRenderer != null)
        {
            Color ropeColor = ropeRenderer.material.color;
            ropeColor.a = 0.0f;
            ropeRenderer.material.color = ropeColor;
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, transform.position);
        }
    }

    private void AdjustRopeLength()
    {
        if (vineJoint == null || verticalRopeInput == 0f) return;
        float adjustment = verticalRopeInput * ropeAdjustSpeed * Time.fixedDeltaTime;
        vineJoint.distance -= adjustment;
        float minRopeLength = 1.0f;
        float maxRopeLength = ropeLength + 5.0f;
        vineJoint.distance = Mathf.Clamp(vineJoint.distance, minRopeLength, maxRopeLength);
    }

    private void ApplySwingForce()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
        {
            rb.AddForce(new Vector2(moveInput * swingForce, 0), ForceMode2D.Force);
        }
    }
    
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

        // 🌟 [추가] 낙하 대쉬 중 착지 시 속도 정리 (이미 FixedUpdate에서 처리되지만, 안전 장치)
        if (isDiveDashing && (groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            // 코루틴은 다음 프레임에 종료되겠지만, 즉시 물리 상태를 정리
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void StopAllActionsForTutorial()
    {
        // 1. 대쉬 상태 강제 종료
        isDashing = false;
        StopCoroutine("DashCoroutine"); 
        
        // 🌟 [추가] 낙하 대쉬 상태 강제 종료
        isDiveDashing = false;
        StopCoroutine("DiveDashCoroutine"); 

        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        // 2. 로프 상태 강제 종료
        if (isSwinging || isRopeExtending)
        {
            ReleaseVine(); 
        }
        
        // 3. 사다리 상태 강제 종료 (필요하다면)
        if (isClimbingLadder)
        {
            StopClimbing(false); 
        }

        // 4. 강제로 중력 및 속도 복구 (안전 장치)
        rb.gravityScale = originalGravityScale;
        rb.linearVelocity = Vector2.zero; // 속도를 0으로 만들어 멈춘 상태 유지
    }
}