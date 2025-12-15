using UnityEngine;
using System.Collections;
using System.Linq; 

public class PlayerHealthAndMovement : MonoBehaviour
{
    [Header("플레이어 스탯")]
    public int health = 1000;
    public float movementSpeed = 5.0f;
    public float jumpForce = 10.0f;

    [Header("UI 연결")]
    public HealthBarController healthBar; 
    private int maxHealth;

    [Header("피격 설정")]
    public float invulnerabilityDuration = 0.5f;
    public float knockbackForce = 10.0f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    public float hitFlashDuration = 0.2f;
    private Color originalColor;

    [Header("대쉬 설정")]
    public float dashDistance = 5.0f; 
    public float dashDuration = 0.2f; 
    public float dashInvulnerabilityDuration = 0.5f; 
    public float dashCooldown = 1.0f; 
    private bool isDashing = false; 
    private float dashCooldownTimer = 0f;
    
    [Header("낙하 대쉬 (Dive Dash) 설정")]
    public float diveDashSpeed = 20.0f;
    public float diveDashDistance = 100.0f;
    public float diveDashInvulnerabilityDuration = 0.3f;
    public float diveDashCooldown = 0.5f;
    private bool isDiveDashing = false;
    private float diveDashCooldownTimer = 0f;
    private bool hasLandedAfterDive = false;

    [Header("낙하 대쉬 공격력 설정")]
    public int diveDashBaseDamage = 1;
    public float damagePerUnitDistance = 0.5f;
    private Vector3 diveDashStartPosition;
    private float traveledDistance;
    [Header("점프 최적화 설정")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);
    private bool jumpCommand = false;
    
    [Header("튜토리얼 시스템")]
    public TutorialManager tutorialManager;
    public bool isDashTutorialActive = false;
    public bool isRopeTutorialActive = false;

    [Header("사다리 설정")]
    public float ladderClimbSpeed = 3.5f;
    public float gravityScaleOnLadder = 0.0f;
    private bool isClimbingLadder = false;
    private GameObject currentLadder = null;
    private float originalGravityScale;
    private int originalLayer;
    
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

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("대쉬 잔상 설정")]
    public float trailClearDelay = 0f;
    public TrailRenderer trailRenderer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Player 오브젝트에 Animator 컴포넌트가 없습니다! 애니메이션 기능을 사용할 수 없습니다.");
        }

        originalGravityScale = rb.gravityScale; 
        originalLayer = gameObject.layer;

        maxHealth = health;
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health, maxHealth);
        }
        
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
            trailRenderer.enabled = false;
        }
        
        ropeRenderer = GetComponent<LineRenderer>();
        if (ropeRenderer == null)
        {
            Debug.LogError("Player 오브젝트에 LineRenderer 컴포넌트를 추가해야 합니다! 로프 기능 비활성화.");
            enabled = false; 
            return;
        }
        
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
        if (rb.gravityScale == 0f && !isDashing && !isClimbingLadder && !isDiveDashing)
        {
            if (isSwinging || isRopeExtending)
            {
                ReleaseVine(); 
            }
            jumpCommand = false; 
        }

        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }
        
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (diveDashCooldownTimer > 0)
        {
            diveDashCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsGrounded() && !isDashing && !isSwinging && !isRopeExtending && !isDiveDashing && diveDashCooldownTimer <= 0)
            {
                if (isSwinging || isRopeExtending)
                {
                    ReleaseVine();
                }
                
                StartCoroutine(DiveDashCoroutine());
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            bool wasTimeStoppedByTutorial = false;
            
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
            
            if (!isDiveDashing && (wasTimeStoppedByTutorial || (!isDashing && dashCooldownTimer <= 0)))
            {
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


        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            bool wasTimeStoppedByTutorial = false;

            if (Time.timeScale == 0f && isRopeTutorialActive)
            {
                Time.timeScale = 1f; 
                rb.gravityScale = originalGravityScale; 

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

            if (!isDiveDashing && (wasTimeStoppedByTutorial || (!isSwinging && !isRopeExtending && currentVinePivot != null && !isClimbingLadder)))
            {
                if (currentVinePivot != null)
                {
                    StartCoroutine(ExtendRopeAndGrab(currentVinePivot));
                }
            }
            
            if (wasTimeStoppedByTutorial) return;
        }

        if (!isDashTutorialActive && !isRopeTutorialActive)
        {
            if (Input.GetKeyUp(KeyCode.LeftShift) && (isSwinging || isRopeExtending))
            {
                ReleaseVine();
            }
            
            if (currentLadder != null) 
            {
                bool isClimbInputPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S);
                
                if (isClimbInputPressed && !isClimbingLadder && !isDashing && !isSwinging && !isDiveDashing) 
                {
                    StartClimbing();
                }
            }
            
            if (isClimbingLadder)
            {
                if (Input.GetButtonDown("Jump")) 
                {
                    StopClimbing(true); 
                }
            }


            if (Input.GetButtonDown("Jump") && IsGrounded() && !isSwinging && !isDashing && !isClimbingLadder && !isDiveDashing)
            {
                jumpCommand = true;
            }

            if (!isRopeExtending && !isSwinging) 
            {
                CheckForVine();
            }
            
            if (isSwinging)
            {
                verticalRopeInput = Input.GetAxisRaw("Vertical");
            }
            else
            {
                verticalRopeInput = 0f;
            }
        }

        if (ropeRenderer == null) return; 
        if (ropeRenderer.positionCount != 2) ropeRenderer.positionCount = 2;

        Color ropeColor = ropeRenderer.material.color;
        
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
        else if (currentVinePivot != null)
        {
            if (!isRopeTutorialActive)
            {
                ropeColor.a = 0.5f; 
                ropeRenderer.SetPosition(0, transform.position);
                ropeRenderer.SetPosition(1, currentVinePivot.transform.position); 
            }
            else
            {
                ropeColor.a = 0.0f;
                ropeRenderer.SetPosition(0, transform.position);
                ropeRenderer.SetPosition(1, transform.position);
            }
        }
        else
        {
            ropeColor.a = 0.0f;
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, transform.position);
        }
        
        ropeRenderer.material.color = ropeColor;
        
        UpdateAnimationState();
    }
    
    private void FixedUpdate()
    {
        if ((isDashTutorialActive || isRopeTutorialActive) && !isDashing && !isDiveDashing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing || isDiveDashing)
        {
            return; 
        }

        if (isClimbingLadder)
        {
            HandleLadderClimbing();
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

    void UpdateAnimationState()
    {
        if (animator == null) return;
        
        float moveInput = Input.GetAxisRaw("Horizontal");
        float targetSpeed = 0f;

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
    }
    

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
        
        if (trailRenderer != null)
        {
            yield return new WaitForSeconds(trailClearDelay); 
            trailRenderer.enabled = false;
        }
    }

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
        
        diveDashStartPosition = transform.position; 
        
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(0f, -diveDashSpeed);
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            diveDashDistance, 
            groundLayer);

        float travelDistance = diveDashDistance;
        if (hit.collider != null)
        {
            travelDistance = hit.distance; 
        }
        
        float travelDuration = travelDistance / diveDashSpeed;
        float startTime = Time.time;
        
        while (Time.time < startTime + travelDuration && !IsGrounded() && isDiveDashing)
        {
            if (IsGrounded()) break;
            yield return null; 
        }

        rb.gravityScale = originalGravity; 
        rb.linearVelocity = Vector2.zero;
        
        isDiveDashing = false;
        hasLandedAfterDive = true;

        if (trailRenderer != null)
        {
            yield return new WaitForSeconds(trailClearDelay); 
            trailRenderer.enabled = false;
        }
        
        Debug.Log("낙하 대쉬 종료.");
    }

    public void SetDashTutorialMode(bool isActive)
    {
        isDashTutorialActive = isActive;
        if (isActive)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetRopeTutorialMode(bool isActive)
    {
        isRopeTutorialActive = isActive;
        if (isActive)
        {
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

    public void TakeDamage(int damage, Vector2 damageSourceDirection)
    {
        if (isInvulnerable) return; 

        health -= damage; 

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health, maxHealth);
        }

        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
        
        StartCoroutine(HitFlashCoroutine());
        rb.linearVelocity = Vector2.zero;
        
        Vector2 finalKnockback = new Vector2(-damageSourceDirection.x * knockbackForce, knockbackForce * 0.5f);
        rb.AddForce(finalKnockback, ForceMode2D.Impulse);

        if (health <= 0) Debug.Log("Game Over!");
    }
    public void TryDiveDashAttack(EnemyStats targetEnemy)
    {
        if (!isDiveDashing) return;

        traveledDistance = Vector3.Distance(diveDashStartPosition, transform.position);

        int bonusDamage = Mathf.RoundToInt(traveledDistance * damagePerUnitDistance);
        int finalDamage = diveDashBaseDamage + bonusDamage;
        
        Debug.Log($"Dive Dash 거리: {traveledDistance:F2}m, 최종 피해량: {finalDamage}");

        targetEnemy.TakeDamage(finalDamage);
        
        StopCoroutine("DiveDashCoroutine");
        isDiveDashing = false;

        rb.gravityScale = originalGravityScale;

        float bounceForce = jumpForce * 0.8f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce); 

        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }

        Debug.Log("Dive Dash 공격 성공! 몬스터를 밟고 튕겨 오름.");

        isInvulnerable = true;
        invulnerabilityTimer = 0.1f;
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

        if (isDiveDashing && (groundLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void StopAllActionsForTutorial()
    {
        isDashing = false;
        StopCoroutine("DashCoroutine"); 
        
        isDiveDashing = false;
        StopCoroutine("DiveDashCoroutine"); 

        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        if (isSwinging || isRopeExtending)
        {
            ReleaseVine(); 
        }
        
        if (isClimbingLadder)
        {
            StopClimbing(false); 
        }

        rb.gravityScale = originalGravityScale;
        rb.linearVelocity = Vector2.zero;
    }
}