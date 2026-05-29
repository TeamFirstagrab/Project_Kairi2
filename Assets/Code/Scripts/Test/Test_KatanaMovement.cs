using UnityEngine;

/// <summary>
/// 🏃‍♂️ [플레이어의 든든한 하체 역할 - 초정밀 물리 및 이동 엔진!]
/// 수평 질주, 수직 점프, 중력 배율 실시간 제어, 3중 지면 레이더 스캔(CheckGround),
/// 언덕 아래로 끌려 흘러내리지 않게 꽁꽁 묶는 언덕 제로 미끄러짐 락(slopebase 기법),
/// 그리고 둥근 빗면 기울기에 완벽히 정렬하여 미끄러지는 '그라운드 스니핑(지면 각도 법선 투사) 구르기'를 총괄하는
/// 이 프로젝트에서 가장 크고 거대한 물리 공학의 심장부입니다.
/// </summary>
public class Test_KatanaMovement : MonoBehaviour
{
    [Header("이동 및 물리 설정")]
    [Tooltip("지상에서 걷고 달리는 기본 질주 속도")]
    [SerializeField] private float moveSpeed = 8f;
    [Tooltip("점프 버튼을 누르는 순간 위로 솟구치도록 밀어올리는 도약 임펄스 힘")]
    [SerializeField] private float jumpForce = 15f;
    [Tooltip("점프 최고점을 지나 공중에서 바닥으로 뚝 떨어질 때 묵직하고 찰지게 당겨주는 중력 가속도 배율")]
    [SerializeField] private float fallMultiplier = 3.5f;
    [Tooltip("스페이스바를 살짝만 톡 뗐을 때 작동하는 아주 가벼운 상승 제동 중력 배율 (가변 점프용)")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("레이캐스트 바닥 및 경사 검출")]
    [Tooltip("바닥 타일로 판정하여 충돌을 감지할 타겟 레이어 마스크 (Ground 레이어)")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("메인 콜라이더 발바닥 하단 모서리로부터 아래로 발사할 정밀 지면 감지선 길이")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [Tooltip("플레이어가 오르거나 정상 걸음으로 비빌 수 있는 한계 언덕 각도 (60도 초과는 벽으로 간주)")]
    [SerializeField] private float maxSlopeAngle = 60f;

    [Header("점프 디테일 버퍼 설정")]
    [Tooltip("💡 [점프 선입력 버퍼 시간] 공중에서 땅에 닿기 직전(0.15초 전)에 미리 누른 점프 입력을 씹지 않고 기억했다가 착지 즉시 도약시켜 주는 반응속도 방패 타이머")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter; // 선입력된 잔여 점프 유효 시간 카운터

    [Header("스마트 경사 마찰 설정")]
    [Tooltip("2D 게임 특유의 경사면 찌걱거림이나 타일 벽면 부딪힘 멈춤 현상을 방지하기 위한 마찰력 0 물리 재질")]
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;

    [Header("쪼그려 앉기 & 구르기 설정")]
    [Tooltip("S를 눌러 쪼그려 앉은 채 엉금엉금 기어서 기어갈 때의 느린 감쇄 속도")]
    [SerializeField] private float crouchSpeed = 3f;
    [Tooltip("구르기 회피 시 순간적으로 팍 튀어나가는 폭발적인 회피 돌진 속도")]
    [SerializeField] private float rollSpeed = 12f;
    [Tooltip("한 번 구를 때 뒹굴거리며 굴러가는 찰나의 회피 지속 시간 (초)")]
    [SerializeField] private float rollDuration = 0.4f;

    // 연결할 내 물리 컴포넌트 장치부
    private Rigidbody2D rigid;
    private Collider2D mainCollider;
    private Animator anim;
    private Test_KatanaPlayerController controller; // 뇌 조종 대장
    private Test_KatanaWallMovement wallMovement;   // 벽타기 기동 장치
    private PlayerHealth playerHealth;              // 무적 판정을 넣어줄 체력 부서

    // 실시간 다리 제어 물리 변수
    private Vector2 inputVector;       // 뇌 조종사로부터 넘겨받은 방향키 축값
    private bool isGrounded;           // 플레이어가 안정적으로 땅을 딛고 서 있는지 여부
    private bool isJumpPressed;        // 점프 버튼이 지금 눌려 있는 상태인가?

    // 경사로 방지 및 각도 보정용 코어 변수
    private bool isOnSlope;                     // 지금 서 있는 타일이 경사면 언덕인가?
    private float slopeAngle;                   // 현재 서 있는 빗면의 정확한 경사 각도 (도 단위)
    private Vector2 slopeNormal;                // 빗면 표면에서 직각(90도)으로 솟구치는 수직 법선 벡터
    private float defaultGravityScale = 3f;     // 인스펙터에 등록된 내 원래 물리 기본 중력 기준값
    private float slopeJumpProtectionTimer;     // 💡 언덕 도약 점프 시 미끄러짐 방지 락 코드가 내 도약 속도를 갉아먹지 않게 막아주는 방패 시간

    // 쪼그려 앉기 & 구르기 상태 관리 프라이빗 변수들
    private bool isCrouchPressed; // 현재 S 키를 꾹 누르고 쪼그려 앉아 있으려 하는지 여부
    private bool isRolling;        // 지금 데굴데굴 구르며 회피 중인가?
    private float rollTimer;       // 구르기 잔여 시간 타이머
    private float rollDirection;   // 구르는 X축 방향 (-1: 왼쪽, 1: 오른쪽)
    private bool rollRequested;    // 뇌 조종 대장으로부터 수신한 1회성 구르기 발동 트리거 예약 스위치
    private Vector2 currentRollVelocity; // 구르기 시작 프레임에 최종 확정되어 락이 걸린 사선 구르기 속도 벡터

    // C# 애니메이션 다이렉트 제어 변수
    private string currentAnimState;  // 중복 Play() 방지용 현재 재생 애니메이션 이름 백업 캐싱
    private float landingImpactTimer; // 높은 곳에서 떨어져 착지했을 때 'LandDown' 모션을 0.12초간 확실하게 유지시켜 주기 위한 세이프티 타이머

    // 외부에 내 물리 상태를 노출해 대화하는 창구(Property)
    public bool IsGrounded => isGrounded;
    public Vector2 InputVector => inputVector;
    public LayerMask GroundLayer => groundLayer;
    public bool IsCrouchPressed => isCrouchPressed;
    public bool IsOnSlope => isOnSlope;
    public Vector2 SlopeNormal => slopeNormal;

    /// <summary>
    /// 뇌 조종 대장(Controller)이 쪼그려 앉아있던 도중 AD를 탁 누르는 순간 호출해 구르기를 장전시킵니다.
    /// </summary>
    public void TriggerRollInput()
    {
        rollRequested = true;
    }

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        controller = GetComponent<Test_KatanaPlayerController>();
        wallMovement = GetComponent<Test_KatanaWallMovement>();
        playerHealth = GetComponent<PlayerHealth>();

        // 💡 [바닥 찌걱임 방지 스마트 장치] 마찰력 0 물리 재질이 없으면 런타임에 즉시 빚어서 장착합니다.
        if (noFrictionMaterial == null)
        {
            noFrictionMaterial = new PhysicsMaterial2D("Dynamic_NoFriction") { friction = 0f, bounciness = 0f };
        }
        
        // 마찰력을 0으로 주어 2D 플랫포머 타일 모서리에 몸이 쩍쩍 달라붙고 찌걱거리는 엔진 고유 버그를 제거합니다.
        mainCollider.sharedMaterial = noFrictionMaterial;
    }

    private void Update()
    {
        // 1. 매 프레임 정밀 지면 체크를 돌려 바닥 감지 상태를 갱신하기 전에, 직전 상태 백업
        bool wasGroundedBefore = isGrounded;
        CheckGround();
        
        // 💡 [착지 연출 감지] 공중에 있다가 바닥에 닿았고, 시속 -0.5m 이하의 하강 속도가 실재했다면 ➡️
        // 착지 시의 쿵 주저앉는 모션('LandDown')을 0.12초간 강제로 확실하게 각인시키기 위해 임팩트 타이머 장전!
        if (!wasGroundedBefore && isGrounded && rigid.linearVelocity.y < -0.5f)
        {
            landingImpactTimer = 0.12f;
        }

        if (landingImpactTimer > 0f)
        {
            landingImpactTimer -= Time.deltaTime;
        }

        // 공중에 몸이 붕 떴거나 S 키를 떼어 앉은 자세가 풀리면 구르기 예약을 안전 파괴합니다.
        if (!isGrounded || !isCrouchPressed)
        {
            rollRequested = false;
        }

        // 2. C# 물리 직권으로 실시간 최적의 연출 모션을 애니메이터에 덮어씌웁니다.
        UpdateAnimator();

        // 3. 점프 선입력 버퍼 예약 수명을 흘려보냅니다.
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 4. 경사 점프 방패 타이머 수명을 흘려보냅니다.
        if (slopeJumpProtectionTimer > 0f)
        {
            slopeJumpProtectionTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        // 🌟 [최우선 1순위 물리 연산 - 데굴데굴 구르기 상태!]
        // 구르는 동안에는 중력을 잠시 무효화(0)하고, 조작 키를 안 받아 조작 방해를 차단하며, 
        // 오직 구르기 시작 시점에 락을 걸었던 사선 속도 벡터를 리지드바디에 우직하게 밀어넣습니다.
        if (isRolling)
        {
            rollTimer -= Time.fixedDeltaTime;
            rigid.gravityScale = 0f;
            rigid.linearVelocity = currentRollVelocity; // 락이 적용된 구르기 사선 속도 강제 주입!

            if (rollTimer <= 0f)
            {
                isRolling = false;
                rigid.gravityScale = defaultGravityScale; // 구르기가 끝나면 기본 중력 3배 복원
            }
            return;
        }
        else if (rigid.gravityScale == 0f) 
        {
            // 구르기나 특수 동작이 끝난 직후 공중에 물리 속도가 0이 되어 허공에 고정되는 멈춤 현상을 막기 위해 강제 원복
            rigid.gravityScale = defaultGravityScale;
        }

        // 3순위: 일반 이동 연산 및 점프 예약 연산
        Move();

        // 지면이나 벽점프가 가능하고, 선입력 점프가 장전되어 유효하다면 즉시 공중 도약을 집행합니다.
        if (jumpBufferCounter > 0f)
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (wallMovement != null && wallMovement.CanWallJump())
            {
                wallMovement.ExecuteWallJump();
                jumpBufferCounter = 0f; // 점프 발동 즉시 버퍼 수명 0 소거
            }
        }

        // 플랫포머 타이트 조작감의 핵심인 가변 중력 감가 속도를 연산합니다.
        ApplyGravityModifiers();
    }

    /// <summary>
    /// 뇌 조종사가 수신한 방향키 키보드 입력을 복사해 옵니다.
    /// </summary>
    public void SetInputVector(Vector2 input)
    {
        inputVector = input;
    }

    /// <summary>
    /// 꾹 누름(가변 점프) 및 스페이스 누름 순간(선입력 예약)을 통제합니다.
    /// </summary>
    public void SetJumpInput(bool isPressed)
    {
        isJumpPressed = isPressed;

        if (isJumpPressed)
        {
            jumpBufferCounter = jumpBufferTime; // 스페이스를 누르는 찰나에 0.15초 예약 장전!
        }
        else
        {
            jumpBufferCounter = 0f; // 손가락을 떼면 예약 자동 폐기
        }
    }

    /// <summary>
    /// S 키가 입력되고 있는지 실시간 동기화합니다.
    /// </summary>
    public void SetCrouchInput(bool isPressed)
    {
        isCrouchPressed = isPressed;
    }

    /// <summary>
    /// 🏃‍♂️ [수평 조작 및 언덕 미끄러짐 방지 코어 물리 연산부]
    /// </summary>
    private void Move()
    {
        bool isCrouching = isGrounded && isCrouchPressed; // 땅을 딛고 S를 누르면 ➡️ 쪼그려 앉기 상태!
        bool hasHorizontalInput = Mathf.Abs(inputVector.x) > 0.1f;

        // 🌟 [A. 구르기 회피 기동 발동 게이트스캔!]
        if (rollRequested && isCrouching && hasHorizontalInput && !isRolling)
        {
            isRolling = true;
            rollTimer = rollDuration;
            rollDirection = inputVector.x > 0f ? 1f : -1f;
            rollRequested = false; // 예약 신호 사용 완료 즉시 파괴!

            // 구르는 시선 방향으로 트랜스폼 회전 설정
            transform.eulerAngles = new Vector3(0f, rollDirection > 0f ? 0f : 180f, 0f);

            // 💡 [지능형 사면 락 구르기 알고리즘 - Ground Sniffing (지면 각도 1점 스캔 법선 투사)]
            // 1. 구르려는 수평 방향 벡터를 만듭니다. (왼쪽은 -1, 오른쪽은 1)
            Vector2 dirVec = new Vector2(rollDirection, 0f);
            // 2. 발바닥 아래 0.8m 공간까지 가상의 단 1개의 레이저(Raycast)를 수직 발사합니다.
            Vector2 rayOrigin = (Vector2)mainCollider.bounds.center;
            float sniffDistance = mainCollider.bounds.extents.y + 0.8f;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, sniffDistance, groundLayer);
            
            // 땅을 찾으면 🟢하늘색(Cyan), 못 찾으면 🔴자홍색(Magenta) 디버그 라인을 씬에 그려줍니다.
            Color rayColor = (hit.collider != null) ? Color.cyan : Color.magenta;
            Debug.DrawRay(rayOrigin, Vector2.down * sniffDistance, rayColor, 1.5f);
            
            if (hit.collider != null)
            {
                // 3. [1개 레이저 각도 분석]
                // 닿은 1점 표면에서 뿜어 나오는 직각 솟구침 법선 벡터(hit.normal)를 획득하여 경사면의 빗면 각도를 구합니다.
                
                // 🔍 [수학 공식 상세 설명]
                //  - Vector2.Angle(A, B) 란?
                //    * 두 개의 직선 화살표(벡터)가 이루는 각도(도 단위, 0~180도)를 구해주는 고마운 공식입니다.
                //    * 하늘로 곧게 뻗은 기준선인 Vector2.up(0, 1) 화살표와 바닥 표면에서 수직으로 삐죽 솟아난 hit.normal 화살표 사이의 각도를 잽니다.
                //    * 만약 바닥이 완전 평지라면 hit.normal도 하늘을 보므로 각도는 0도가 되고, 바닥이 30도 꺾인 경사로라면 각도도 30도가 됩니다!
                float slopeAngle = Vector2.Angle(Vector2.up, hit.normal);
                if (slopeAngle > 2f && slopeAngle < maxSlopeAngle)
                {
                    // 4. [언덕 평행 속도 투사 연산]
                    // 구르려던 방향 벡터 `dirVec`을 빗면의 각도 기둥에 맞춰 부드럽게 눕힙니다. (ProjectOnPlane)
                    // 이 공식 하나만으로 수평 속도가 언덕 경사를 타고 흐르도록 예쁘게 각도가 비틀어 꺾입니다!
                    
                    // 🔍 [수학 및 물리학 상세 설명]
                    //  - 법선 벡터(Normal Vector) 란?
                    //    * 어떤 평면이나 빗면 표면에서 딱 '90도 직각' 방향으로 공중으로 곧게 솟구치는 가상의 수직 기준 화살표를 뜻합니다.
                    //  - Vector3.ProjectOnPlane(방향벡터, 법선벡터) 이란?
                    //    * 물리학에서 가장 신기한 '사영(Projection) 및 투사' 수학 공식입니다.
                    //    * 우리가 가려는 수평 방향 화살표(`dirVec`)를 경사면의 법선 벡터가 지키는 빗면 평면 위에 그대로 '빔 프로젝터 빛을 쏘듯' 착 눕혀 투사시킵니다.
                    //    * 이 수학 공식을 한 번 통과하면, 원래 좌우 일직선으로 달리던 속도가 언덕의 둥근 곡선이나 경사 빗면을 따라 부드럽게 미끄러지듯이 휘어지는 완벽한 빗면 이동 속도로 변신합니다!
                    Vector2 normal = hit.normal;
                    Vector3 projected = Vector3.ProjectOnPlane(dirVec, normal);
                    dirVec = projected.normalized; // 정규화하여 방향 비율만 추출
                }
            }
            
            // 최종적으로 각도가 사선으로 락이 걸린 속도에 구르기 스피드(12f)를 곱해 고정 물리 벡터를 만듭니다.
            currentRollVelocity = dirVec * rollSpeed;
            return;
        }

        // 🌟 [B. 쪼그려 앉은 상태의 느린 엉금엉금 포복 이동]
        if (isCrouching)
        {
            float targetSpeedCrouch = inputVector.x * crouchSpeed;
            float yVelCrouch = rigid.linearVelocity.y;

            // 💡 [경사로 쪼그려 앉기 미끄러짐 방지 적용]
            // 언덕에 서서 멈춰있으려 방향키를 다 뗐다면, 흘러내리는 수직 Y 속도마저 0으로 고정해서 접착제처럼 고정시킵니다.
            if (isOnSlope && Mathf.Abs(targetSpeedCrouch) < 0.01f && yVelCrouch <= 0.01f && slopeJumpProtectionTimer <= 0f)
            {
                targetSpeedCrouch = 0f;
                yVelCrouch = 0f;
            }

            rigid.linearVelocity = new Vector2(targetSpeedCrouch, yVelCrouch);
            return;
        }

        // 칼을 휘둘러 베고 있는 찰나 동안에는 발바닥을 땅에 접착제로 묶어 제자리 공격이 되도록 유도합니다.
        if (controller != null && controller.IsAttacking())
        {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
            return;
        }

        // 벽을 힘차게 차고 날아가는 반동 프레임 격리 시간 동안에는 조작을 일절 무시합니다.
        if (wallMovement != null && wallMovement.IsWallJumping)
        {
            return;
        }

        // 🌟 [C. 기본 질주 수평 속도 세팅]
        float targetSpeed = inputVector.x * moveSpeed;
        float yVel = rigid.linearVelocity.y;

        // 💡 [지능형 언덕 미끄러짐 방지 락 (slopebase 기법) - 핵심 중의 핵심!]
        // 땅에 있고, 경사 언덕 위에 서있는데, 좌우 키를 다 떼고 가만히 서 있으려 한다면 ➡️
        // 중력으로 질질 흘러내려 미끄러지는 것을 차단하기 위해 목표 X 및 Y 속도를 완전 0으로 꽉 묶어버립니다.
        if (isGrounded && isOnSlope && Mathf.Abs(inputVector.x) < 0.01f && rigid.linearVelocity.y <= 0.01f && slopeJumpProtectionTimer <= 0f)
        {
            targetSpeed = 0f;
            yVel = 0f;
        }
        // 💡 [오르막 급반전 붕 뜸 방지]
        // 오르막을 마구 질주하다가 갑자기 반대 방향으로 키를 틀었을 때 속도 역풍으로 공중에 붕 뜨는 오버런을 잡기 위해 속도를 꺾습니다.
        else if (isGrounded && isOnSlope && yVel > 0.05f)
        {
            bool isChangingDirection = (inputVector.x > 0.01f && rigid.linearVelocity.x < -0.01f) || 
                                       (inputVector.x < -0.01f && rigid.linearVelocity.x > 0.01f);
            if (isChangingDirection)
            {
                yVel = 0f;
            }
        }
        // 💡 [오르막 평지 탈출 관성 뜨기 방지]
        // 오르막 끝에 도달해 평지로 빠져나오는 순간 관성 추진력에 의해 붕 날아올라 착지 판정이 풀려 덜덜거리는 현상을 억제합니다.
        else if (isGrounded && !isOnSlope && yVel > 0.05f)
        {
            yVel = 0f;
        }

        // 최종 가공된 수평/수직 물리 속도를 가차 없이 주입합니다.
        rigid.linearVelocity = new Vector2(targetSpeed, yVel);

        // 달리는 방향(X)에 맞춰 플레이어 캐릭터 시선을 좌우 트랜스폼 회전으로 순간 반전(Flip) 시켜 줍니다.
        if (inputVector.x > 0)
        {
            transform.eulerAngles = Vector3.zero;
        }
        else if (inputVector.x < 0)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }

    /// <summary>
    /// 🚀 [수직 공중 도약 점프 연산기]
    /// </summary>
    private void Jump()
    {
        // Y축 위로 폭발적인 상승 탄력 힘(jumpForce: 15)을 리프팅합니다.
        rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f; // 점프 1회 발동으로 선입력 예약을 소모 완료!
        
        // 💡 언덕 미끄러짐 방지 코드가 내 상승 점프 속도까지 가로막아 갉아먹는 대참사를 방지하는 방패 보호막을 0.2초간 가동시킵니다!
        slopeJumpProtectionTimer = 0.2f; 
    }

    /// <summary>
    /// 🍃 [가변 낙하 중력 및 특수 물리 감가 가속기 - 프로의 찰진 점프감 연출]
    /// </summary>
    private void ApplyGravityModifiers()
    {
        // 2D 스프라이트가 언덕에서 구르다가 혼자 발라당 회전하며 뒤집어지지 않도록 물리 Z축 회전 오차 고정을 고수합니다.
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (isGrounded)
        {
            // 💡 땅에 서서 정지해 있다면 미끄러지지 않도록 순간 중력 스케일을 0으로 잠금하여 마찰 저항을 흉내 냅니다.
            float targetHorizontalSpeed = isCrouchPressed ? (inputVector.x * crouchSpeed) : (inputVector.x * moveSpeed);
            if (isOnSlope && Mathf.Abs(targetHorizontalSpeed) < 0.01f && rigid.linearVelocity.y <= 0.01f && slopeJumpProtectionTimer <= 0f)
            {
                rigid.gravityScale = 0f;
                rigid.linearVelocity = Vector2.zero; // 물리 고정
            }
            else
            {
                rigid.gravityScale = defaultGravityScale; // 움직일 때는 원래 중력 복원
            }
        }
        else if (wallMovement != null && wallMovement.IsWallSliding)
        {
            rigid.gravityScale = defaultGravityScale; // 벽에 매달려 비벼 떨어질 때는 기본 중력 적용
        }
        else
        {
            // 💡 [프로의 중력 연산 법칙 - 가변 중력]
            // 최고점을 돌파해 낙하하고 있을 때(y < 0) ➡️ 더 묵직하게 빠르게 땅으로 처박히도록 무려 3.5배 중력을 주입! (타이트한 점프 손맛 완성)
            // 최고점에 도달하기도 전에 유저가 스페이스에서 손가락을 일찍 뗐다면 ➡️ 상승 감속 브레이크를 더 무겁게 당겨 조금만 도약하게 2배 중력을 적용!
            
            // 🔍 [물리학 상세 설명]
            //  - rigid.linearVelocity.y 란?
            //    * 리지드바디2D 물리 엔진에서 실시간으로 작동하는 플레이어의 수직(Y축) 진짜 속도입니다.
            //    * 이 값이 양수(+)이면 하늘로 치솟아 올라가는 중이고, 음수(-)이면 중력에 끌려 바닥으로 떨어지고 있는 상태입니다.
            //  - rigid.gravityScale 이란?
            //    * 지구 중력(9.81 m/s²)을 내 캐릭터에게 몇 배로 곱해서 적용할지 결정하는 리지드바디 전용 '중력 배율 슬라이더'입니다.
            //    * 낙하할 때 중력 배율을 fallMultiplier(3.5배)로 높이면 깃털처럼 흐느적 떨어지지 않고 닌자처럼 묵직하고 신속하게 쿵 안착하는 타이트한 손맛이 연출됩니다.
            //    * 상승 도중 점프키를 떼었을 때 중력을 lowJumpMultiplier(2배)로 높이면 하늘 높이 붕 떠버리지 않고 키를 살짝 뗀 만큼 바로 하강 브레이크가 걸리는 '가변 점프'가 완성됩니다!
            if (rigid.linearVelocity.y < 0)
            {
                rigid.gravityScale = fallMultiplier;
            }
            else if (rigid.linearVelocity.y > 0 && !isJumpPressed)
            {
                rigid.gravityScale = lowJumpMultiplier;
            }
        }
    }

    /// <summary>
    /// 🟢🔴 [3중 지면 스캔 정밀 레이더 - 씬 뷰 시각화 가동]
    /// </summary>
    private void CheckGround()
    {
        if (mainCollider == null) return;

        float offset = 0.05f; // 발바닥 틈새 오프셋
        float totalDistance = offset + groundCheckDistance;

        // 발바닥 모서리 안쪽으로 약 0.02미터 마진을 주어 절벽 벽면에 걸려 허공 답보하는 현상을 제거
        float sideMargin = 0.02f; 
        Vector2 centerOrigin = new Vector2(mainCollider.bounds.center.x, mainCollider.bounds.min.y + offset);
        Vector2 leftOrigin = new Vector2(mainCollider.bounds.min.x + sideMargin, mainCollider.bounds.min.y + offset);
        Vector2 rightOrigin = new Vector2(mainCollider.bounds.max.x - sideMargin, mainCollider.bounds.min.y + offset);

        // 좌/중/우 3방향 둥근 발밑 레이저 슛!
        
        // 🔍 [물리 및 공간수학 상세 설명]
        //  - Physics2D.Raycast(출발점, 방향, 거리, 감지대상) 란?
        //    * 게임 개발에서 가장 중요하게 쓰이는 '빛(Ray) 던지기' 물리 공식입니다.
        //    * 칠흑 같은 어둠 속에서 레이저 포인터를 쭉 쏘듯이, 특정 시작점(`origin`)에서 정해진 방향(`Vector2.down = 아래`)으로 지정한 최대 길이(`totalDistance`)만큼 실시간 레이저 광선을 날려 장애물을 스캔합니다.
        //    * 이때 레이저가 지정된 레이어(`groundLayer = 바닥`) 타일에 충돌하면 충돌 지점 좌표, 표면의 법선 각도, 부딪힌 벽 이름 등 온갖 물리 데이터를 가득 채운 `RaycastHit2D` 보따리 상자를 선물로 가져옵니다!
        RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, Vector2.down, totalDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, totalDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, totalDistance, groundLayer);

        // 씬 뷰에 초록색(접촉 대성공)과 빨간색(허공)으로 레이저를 매 프레임 선명하게 그려줍니다.
        Debug.DrawRay(centerOrigin, Vector2.down * totalDistance, hitCenter.collider != null ? Color.green : Color.red);
        Debug.DrawRay(leftOrigin, Vector2.down * totalDistance, hitLeft.collider != null ? Color.green : Color.red);
        Debug.DrawRay(rightOrigin, Vector2.down * totalDistance, hitRight.collider != null ? Color.green : Color.red);

        // 3개 레이저 중 접촉에 성공한 하나를 구조체로 승계합니다.
        RaycastHit2D hit = hitCenter ? hitCenter : (hitLeft ? hitLeft : hitRight);
        
        bool currentGrounded = false;

        if (hit.collider != null)
        {
            // 💡 [1개 레이저 물리 법선 각도 추출 공식]
            // 부딪힌 1점에서 솟구쳐 오르는 수직 기둥(Normal)의 각도를 재서 빗면 각도를 도출합니다.
            slopeNormal = hit.normal;
            slopeAngle = Vector2.Angle(Vector2.up, slopeNormal);

            // 💡 [지능형 언덕 감지 사선 마진 튜닝 기법]
            // 사선 빗면 틈새 공간(slopeAngle > 5)에서는 순간적으로 붕 떠서 접지 판정이 풀려 덜덜거리지 않게 
            // 감지선 거리를 0.35m로 넓혀주고, 평지에서는 0.15m로 엄밀하게 좁혀 물리 오차를 이중 스케일링합니다!
            float margin = (slopeAngle > 5f) ? 0.35f : 0.15f;
            float strictLandingDistance = offset + margin; 
            
            if (hit.distance <= strictLandingDistance)
            {
                currentGrounded = true;
            }

            // 등반 허용 최대 한계선 내의 기울기라면 언덕 접지 플래그를 참으로 켭니다.
            if (slopeAngle > 0.05f && slopeAngle < maxSlopeAngle)
            {
                isOnSlope = true;
            }
            else
            {
                isOnSlope = false;
                slopeAngle = 0;
            }
        }
        else
        {
            isOnSlope = false;
            slopeAngle = 0;
        }

        if (isGrounded != currentGrounded)
        {
            isGrounded = currentGrounded;
        }
    }

    /// <summary>
    /// 🎬 [C# 물리 전권 모션 스위칭 장치 - 유니티 화살표 트랜지션을 완전히 대체!]
    /// </summary>
    private void UpdateAnimator()
    {
        if (anim == null) return;

        // 1순위: 칼을 베고 있는 동안 ➡️ 공격 애니메이션 강제 재생
        if (controller != null && controller.IsAttacking())
        {
            ChangeAnimationState("Attack", 0f); // 쿨타임 없이 즉각 0초 컷 프레임 스위칭
            return;
        }

        // 2순위: 적의 포화를 구르고 있는 회피 상태 ➡️ 구르기 모션 강제 고정
        if (isRolling)
        {
            ChangeAnimationState("Roll", 0f);
            return;
        }

        // 3순위: 벽에 매달려 문지르는 마찰 하강 상태 ➡️ 벽 슬라이딩 모션 강제 고정
        if (wallMovement != null && wallMovement.IsWallSliding)
        {
            ChangeAnimationState("WallSliding", 0.05f);
            return;
        }

        // 4순위: 발이 지면에 없는 공중 상태 ➡️ 수직 속도값에 따라 상승(LandUp)과 하강(Landing) 교차 재생
        if (!isGrounded)
        {
            if (rigid.linearVelocity.y > 0.1f)
            {
                ChangeAnimationState("LandUp", 0.05f);
            }
            else
            {
                ChangeAnimationState("Landing", 0.05f);
            }
            return;
        }

        // 5순위: 땅에 쿵 주저앉은 착지 임팩트 딜레이 찰나 ➡️ 착지 흡수 모션 재생
        if (landingImpactTimer > 0f)
        {
            ChangeAnimationState("LandDown", 0f);
            return;
        }

        // 6순위: 유저가 방향키를 떼고 엎드려 있는 쪼그려 앉은 상태 
        // 💡 씬 기획 상태(테스트씬 vs 메인게임씬)에 맞춰 애니메이터 노드를 지능적으로 선별해 재생하는 가상 쉴드 탑재!
        if (isCrouchPressed)
        {
            if (anim.HasState(0, Animator.StringToHash("Crouch ")))
            {
                ChangeAnimationState("Crouch ", 0.05f); // 테스트 씬용 (공백 1칸 포함)
            }
            else if (anim.HasState(0, Animator.StringToHash("Crouch")))
            {
                ChangeAnimationState("Crouch", 0.05f);  // 표준형
            }
            else
            {
                // 💡 [메인 씬용 원래 디자인 폴백] 
                // 전용 쪼그려 앉기 애니메이션 노드가 메인 애니메이터에 존재하지 않는 경우, 
                // 기존 기획 디자인대로 '착지 대기 모션(LandDown)'을 활용해 쪼그려 앉는 외형을 연출합니다.
                ChangeAnimationState("LandDown", 0.05f);
            }
            return;
        }

        // 7순위: 발을 땅에 딛고 좌우 키를 입력해 움직이는 질주 상태 ➡️ 달리기 모션 재생
        if (Mathf.Abs(inputVector.x) > 0.01f)
        {
            ChangeAnimationState("Run", 0.05f);
        }
        // 8순위: 아무것도 안 하고 얌전히 숨 쉬는 평화 상태 ➡️ Idle 모션 재생
        else
        {
            ChangeAnimationState("Idle", 0.05f);
        }
    }

    /// <summary>
    /// 💡 [프로의 기술 - 뷰 데싱크 완벽 방지 헬퍼]
    /// </summary>
    public void ChangeAnimationState(string newState, float crossFadeTime = 0.05f)
    {
        // 💡 [초정밀 1프레임 찌걱거림 차단 기법]
        // 이미 해당 애니메이션 상태가 재생 중(currentAnimState == newState)이라면 즉시 리턴합니다.
        // 만약 이 필터를 빼버리면 매 프레임마다 CrossFade나 Play가 연속 호출되어 첫 프레임에서 애니메이션이 영원히 굳어버리게 됩니다!
        if (currentAnimState == newState) return;

        if (anim != null)
        {
            if (crossFadeTime > 0f)
            {
                anim.CrossFade(newState, crossFadeTime); // 미세하고 부드러운 블렌딩
            }
            else
            {
                anim.Play(newState); // 딜레이 없는 순간 변환
            }
        }
        currentAnimState = newState;
    }

    public virtual void UpdateSprite(Vector2 dir)
    {
        if (dir.x > 0) transform.eulerAngles = Vector3.zero;
        else if (dir.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
    }
}