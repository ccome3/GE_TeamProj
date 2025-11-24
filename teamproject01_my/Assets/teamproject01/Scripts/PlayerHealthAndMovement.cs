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

    [Header("대쉬 잔상 설정")]
    public float trailClearDelay = 0.5f; // 트레일이 완전히 사라질 때까지의 지연 시간
    public TrailRenderer trailRenderer; // 🌟 새로 추가

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        if (trailRenderer == null)
        {
            trailRenderer.enabled = false;
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
        // 무중력 공간 진입 시 로프 강제 해제 및 점프 방지 로직 (대쉬 중이 아닐 때만)
        if (rb.gravityScale == 0f && !isDashing)
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

        // 점프 입력 (대쉬, 스윙 중 점프 방지)
        if (Input.GetButtonDown("Jump") && IsGrounded() && !isSwinging && !isDashing)
        {
            jumpCommand = true;
        }

        // 덩쿨 판정 범위 내의 Pivot 확인 
        if (!isRopeExtending && !isSwinging) 
        {
            CheckForVine();
        }

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
        
        // 잡기 (Shift 키 누름)
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isSwinging && !isRopeExtending && currentVinePivot != null)
        {
            StartCoroutine(ExtendRopeAndGrab(currentVinePivot));
        }

        // 놓기 (Shift 키 뗌)
        if (Input.GetKeyUp(KeyCode.LeftShift) && (isSwinging || isRopeExtending))
        {
            ReleaseVine();
        }

        // 대쉬 입력 감지 (마우스 우클릭)
        if (Input.GetMouseButtonDown(1) && !isDashing && dashCooldownTimer <= 0)
        {
            if (isSwinging || isRopeExtending)
            {
                ReleaseVine();
            }
            StartCoroutine(DashCoroutine());
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
    }

    private void FixedUpdate()
    {
        // 대쉬 중에는 다른 물리 로직을 막습니다.
        if (isDashing)
        {
            return; 
        }
        
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

    // === 대쉬 기믹 전용 함수 ===
    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        
        if (trailRenderer != null)
        {
            trailRenderer.Clear(); // 이전 잔상 초기화
            trailRenderer.enabled = true; // 🌟 잔상 활성화
        }

        // 1. 무적 시간 시작
        isInvulnerable = true;
        invulnerabilityTimer = dashInvulnerabilityDuration;
        
        // 2. 쿨타임 시작
        dashCooldownTimer = dashCooldown;
        
        // 3. 이동 방향 및 속도 계산
        float direction = spriteRenderer.flipX ? -1f : 1f; 
        float requiredSpeed = dashDistance / dashDuration; 
        Vector2 dashVelocity = new Vector2(direction * requiredSpeed, 0); 
        
        // 4. 대쉬 물리 적용
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // 중력 일시 비활성화
        rb.linearVelocity = dashVelocity; 

        // 5. 대쉬 이동 시간 대기
        yield return new WaitForSeconds(dashDuration);
        
        // 6. 대쉬 종료 및 물리 복구
        rb.gravityScale = originalGravity; // 중력 복원
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y); 
        
        isDashing = false;

        // 🌟 잔상 비활성화 (지연 시간 후)
        if (trailRenderer != null)
        {
            yield return new WaitForSeconds(trailClearDelay); 
            trailRenderer.enabled = false;
        }
    }

    // === 로프/스윙 기믹 전용 함수 ===
    private void CheckForVine()
    {
        // Y 좌표가 플레이어보다 높은 피벗만 선택 (자신의 위쪽에 있는 피벗만 감지)
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
            // 코루틴 동안 로프 끝 위치 보간
            Vector3 currentRopeEnd = Vector3.Lerp(startPos, endPos, t); 
            
            if (ropeRenderer != null)
            {
                ropeRenderer.SetPosition(0, startPos); 
                ropeRenderer.SetPosition(1, currentRopeEnd); 
            }
            
            yield return null;
        }

        // 코루틴 완료 후 위치를 한 번 더 고정
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
        // 1. Pivot Collider 비활성화 (충돌/굳음 문제 방지)
        vinePivotCollider = pivot.GetComponent<Collider2D>();
        if (vinePivotCollider != null)
        {
            vinePivotCollider.enabled = false; 
            Debug.Log("Pivot Collider 비활성화 - 충돌 문제 방지");
        }

        // 2. DistanceJoint2D 컴포넌트 추가 및 설정
        vineJoint = gameObject.AddComponent<DistanceJoint2D>();
        vineJoint.connectedBody = pivot.GetComponent<Rigidbody2D>();
        
        // 로프 길이 계산 및 설정
        ropeLength = Vector2.Distance(transform.position, pivot.transform.position); 
        vineJoint.distance = ropeLength;
        
        vineJoint.anchor = Vector2.zero; 
        vineJoint.connectedAnchor = Vector2.zero; 
        
        vineJoint.autoConfigureDistance = false; 
        vineJoint.enableCollision = false; 
        
        // 3. 상태 업데이트 및 초기 힘 적용
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
            // Momentum Launch 적용
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

        // Pivot Collider 복구
        if (vinePivotCollider != null)
        {
            vinePivotCollider.enabled = true; 
            vinePivotCollider = null; 
        }
        
        // 로프 해제 시 currentVinePivot을 명시적으로 초기화합니다.
        currentVinePivot = null; 

        // 로프 숨김
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

        // 조절될 양을 계산 (FixedUpdate이므로 Time.fixedDeltaTime 사용)
        float adjustment = verticalRopeInput * ropeAdjustSpeed * Time.fixedDeltaTime;

        // 현재 로프 길이에 적용. W(1)을 누르면 (-)되어 길이가 짧아집니다.
        vineJoint.distance -= adjustment; 

        // 최소/최대 길이 제한
        float minRopeLength = 1.0f; 
        // 최초 연결 길이(ropeLength)를 최대 길이의 기준으로 삼습니다.
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

    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        // 착지 후에는 로프의 높은 속도를 movementSpeed로 덮어씁니다.
        rb.linearVelocity = new Vector2(moveInput * movementSpeed, rb.linearVelocity.y);

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
        
        // 덩쿨 감지 반경 시각화
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

    // 땅에 닿으면 로프 자동 해제 기능
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSwinging && (groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            ReleaseVine(); 
            // 착지 시 불필요한 속도 감속
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y * 0.1f); 
        }
    }
}