using UnityEngine;

/// <summary>
/// 🧗‍♂️ [플레이어의 날렵한 발 역할 - 벽타기 및 벽점프 통제 모듈!]
/// 벽에 돌진해 닿는 순간, 그냥 주르륵 떨어지지 않고 찰나의 시간(0.12초) 동안 찰싹 매달리는 연출을 시전합니다.
/// 그리고 벽 충돌 시의 수평 돌진 속도를 수직 상승 에너지로 뿜어 올리는(Inertia Conversion) 
/// 매우 짜릿하고 다이내믹한 고급 벽타기 시스템입니다.
/// </summary>
public class Test_KatanaWallMovement : MonoBehaviour
{
    [Header("벽점프 & 벽슬라이딩 설정")]
    [Tooltip("벽 슬라이딩 시 천천히 아래로 비벼 떨어지도록 브레이크를 잡는 감속 한계치 속도")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Tooltip("벽을 발로 차고 점프할 때 밀어내는 폭발적인 탄력 힘 (X축: 벽 반대로 튕겨나감, Y축: 솟구쳐 오름)")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);

    [Tooltip("벽점프 발동 직후, 아주 미세한 순간 동안 유저의 좌우 조작(A/D)을 임시 격리하여 반동 튕김이 가로막히지 않게 잠그는 프레임 시간")]
    [SerializeField] private float wallJumpDuration = 0.15f;

    [Tooltip("벽과의 거리를 정밀하게 낚아챌 둥근 레이저 사거리 길이")]
    [SerializeField] private float wallCheckDistance = 0.5f;

    [Tooltip("벽 타일을 판정할 타겟 타일맵 레이어 마스크")]
    [SerializeField] private LayerMask wallLayer;


    [Header("실시간 수평 진입속도 ➡️ 수직 상승관성 변환 설정")]
    [Tooltip("벽에 충돌하기 1프레임 직전 수평 속도의 몇 %를 위로 솟구치는 상승 관성으로 변환할지 비율 (0.7f = 70%)")]
    [Range(0f, 1.5f)]
    [SerializeField] private float inertiaConvertRatio = 0.7f;

    [Tooltip("상승 관성의 최소 보증 탄력 (아무리 살살 기어가서 비벼도 매달리는 손맛은 최소한 보장되도록 설정)")]
    [SerializeField] private float minClimbInertia = 2.0f;

    [Tooltip("상승 관성의 최대 한계선 (초고속 대쉬로 박았을 때 하늘 끝 우주 밖으로 날아가 버리는 오버런을 물리 차단)")]
    [SerializeField] private float maxClimbInertia = 8.0f;


    [Header("벽 매달리기 자세 시간 타이머")]
    [Tooltip("벽에 처음 부딪혔을 때 찰싹 달라붙어 매달린 비주얼을 먼저 보여주는 딜레이 대기 시간 (초)")]
    [SerializeField] private float wallClimbDelay = 0.12f; // 0.12초 동안 매달림 자세 노출!


    // 💡 [실전 프로의 손맛 조절을 위한 초정밀 제어 변수들]
    private float climbDelayTimer;        // 매달리기 시간 카운트다운 타이머
    private bool pendingClimbBoost;       // 찰싹 달라붙은 딜레이 후 솟구치는 타이밍 예약 플래그
    private bool hasClimbedInThisAir;     // 공중에 떠있는 동안 벽 오르기 관성 혜택을 단 1회만 제공하도록 잠그는 스위치
    private float lastHorizontalVelocity; // 벽에 부딪히기 딱 1프레임 전의 '진짜 수평 돌격 속도' 백업 메모리
    private bool wasTouchingWall;         // 이전 프레임에 벽에 닿고 있었는지 감지하기 위한 역사 기록기

    // 내 물리 하드웨어 컴포넌트 연결부
    private Rigidbody2D rigid;
    private Collider2D mainCollider;
    private Test_KatanaMovement movement;
    private Animator anim;

    // 양옆 벽에 닿아있는 상태 정보
    private bool isTouchingLeftWall;
    private bool isTouchingRightWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpTimeCounter;
    private float wallJumpDirection;

    // 다른 동작 전담 부서(이동, 컨트롤러)에 내 벽 상태 정보를 전달해 주는 인터페이스 프로퍼티
    public bool IsWallSliding => isWallSliding;
    public bool IsWallJumping => isWallJumping;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        movement = GetComponent<Test_KatanaMovement>();
        anim = GetComponent<Animator>();

        // [예외 안전망] 만약 벽 레이어가 지정되지 않았다면 다리(Movement)의 바닥 레이어 값을 이어받습니다.
        if (wallLayer == 0 && movement != null)
        {
            wallLayer = movement.GroundLayer;
        }
    }

    private void Update()
    {
        // 💡 1. [양옆 정밀 벽 탐색] 매 프레임 양옆 모서리에서 레이저를 쏴 벽과의 충돌을 파악합니다.
        CheckWall();

        bool isCurrentlyTouching = isTouchingLeftWall || isTouchingRightWall;
        bool isGrounded = movement != null ? movement.IsGrounded : false;

        // 💡 2. [오르기 제한 리셋] 땅에 발이 착지하는 순간, 공중 관성 오르기 잠금을 원상태로 해제합니다.
        if (isGrounded)
        {
            hasClimbedInThisAir = false;
        }

        // 💡 3. [진짜 진입 수평 속도 실시간 버퍼링 백업]
        // 벽에 아예 닿지 않고 자유 비행(혹은 대쉬/달리기) 중일 때의 실제 X축 속도를 매 프레임 백업해 둡니다.
        // 왜냐하면 벽에 부딪히는 순간 수평 속도는 유니티 물리 엔진에 의해 강제로 0이 되어 버려, 충돌 후에는 이전 속도를 구할 수 없기 때문입니다!
        if (!isCurrentlyTouching)
        {
            lastHorizontalVelocity = rigid.linearVelocity.x;
        }

        // 💡 4. [동적 관성 매달리기 딜레이 시스템 발동 조건 스캔!]
        // 땅에 닿아있지 않고(공중) + 직전 프레임엔 벽에 안 닿았는데 이번에 딱 벽에 도킹했으며 + 이번 공중 체공 중엔 관성 변환을 아직 안 썼고 + 수직 속도가 추락 중이 아닐 때만!
        if (!isGrounded && !wasTouchingWall && isCurrentlyTouching && !hasClimbedInThisAir && rigid.linearVelocity.y >= -0.1f)
        {
            float inputX = movement != null ? movement.InputVector.x : 0f;
            // 유저가 벽을 향해 방향키를 꾹 밀어붙이고 있는지 체크
            bool isPushingWall = (isTouchingLeftWall && inputX < -0.01f) || (isTouchingRightWall && inputX > 0.01f);
            
            if (isPushingWall)
            {
                climbDelayTimer = wallClimbDelay; // 0.12초 동안 찰싹 매달릴 타이머 시작
                pendingClimbBoost = true;        // 매달림 대기 후 상승 뿜기 예약 장전
                hasClimbedInThisAir = true;      // 이번 공중에선 관성 혜택 끝! (중복 발동 버그 차단)

                // 🌟 [비주얼 즉각 반응] 벽타기 모션을 지연 없이 0프레임만에 재생하기 위해 애니메이터 직접 소통!
                if (anim != null)
                {
                    anim.SetBool("isWallSliding", true);
                }

                // Y 속도를 즉시 강제로 0으로 잡아서 벽에 찰싹 붙어 공중에 달라붙게 만듭니다.
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
            }
        }

        // 💡 5. [매달리기 대기 카운트다운 타이머 연산부]
        if (pendingClimbBoost && climbDelayTimer > 0f)
        {
            climbDelayTimer -= Time.deltaTime;
            
            // 0.12초 동안은 Y축 속도를 무조건 0으로 꽉 묶어서 아래로 흘러내리는 것을 미세 정지시킵니다.
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);

            // 드디어 0.12초의 매달리기 시각 연출 대기가 정확히 완료되는 시점 프레임!
            if (climbDelayTimer <= 0f)
            {
                pendingClimbBoost = false;

                // 💡 6. [수평 돌격 에너지 ➡️ 수직 상승 관성 변환 공식 실행]
                // 충돌 직전 백업해 두었던 진짜 수평 진입속도에 변환율(70%)을 곱해 위로 솟구칠 관성 힘을 유동적으로 구합니다.
                
                // 🔍 [수학 공식 상세 설명]
                //  - Mathf.Abs(lastHorizontalVelocity) 란?
                //    * 절대값(Absolute Value) 함수입니다. 플레이어가 왼쪽(-속도)으로 달리든 오른쪽(+속도)으로 달리든 상관없이 마이너스 부호를 떼어 버리고 순수한 속력의 크기(절대값)만 계산하기 위해 사용합니다.
                //  - Mathf.Clamp(대상, 최소값, 최대값) 이란?
                //    * 숫자의 크기를 감옥 속에 집어넣어 가두어 버리는 '가두기(Clamp)' 수학 공식입니다.
                //    * 계산된 상승 힘(`dynamicInertia`)이 아무리 작아도 최소한 `minClimbInertia`(2.0미터)만큼의 높이는 무조건 보장하고, 
                //    * 대쉬 속도가 너무 빨라서 계산된 값이 하늘 높이 치솟아 우주 밖으로 탈출하려 해도 최댓값인 `maxClimbInertia`(8.0미터)의 천장에 강제로 부딪히도록 막아 줍니다.
                //    * 즉, 결과값이 [최솟값 ~ 최댓값] 범위 사이에 항상 갇히도록 안전지대를 규정하는 아주 고마운 함수입니다!
                float dynamicInertia = Mathf.Abs(lastHorizontalVelocity) * inertiaConvertRatio;
                // 천장을 뚫고 가출해 버리지 않도록 최소 2미터, 최대 8미터 범위로 클램프 고정시킵니다.
                dynamicInertia = Mathf.Clamp(dynamicInertia, minClimbInertia, maxClimbInertia);

                // 🌟 [관성 솟구침 시전] 수평 방향 튕겨남 속도는 0으로 락하고, Y축 상승 속도에 관성 힘 주입!
                rigid.linearVelocity = new Vector2(0f, dynamicInertia);
            }
        }

        wasTouchingWall = isCurrentlyTouching;

        // 💡 7. [벽점프 제어 락 타이머 관리]
        if (wallJumpTimeCounter > 0f)
        {
            wallJumpTimeCounter -= Time.deltaTime;
            if (wallJumpTimeCounter <= 0f)
            {
                isWallJumping = false; // 격리 시간이 끝나면 다시 유저 조작이 복원됩니다.
            }
        }
    }

    private void FixedUpdate()
    {
        // 💡 8. [벽에 매달려 비벼 떨어질 때의 슬라이딩 감속 연산]
        HandleWallSliding();
    }

    /// <summary>
    /// 🧗‍♂️ [뇌 컨트롤러의 쿼리 인터페이스] "스페이스바 눌렸는데 지금 벽점프 뛸 수 있는 상태니?"
    /// </summary>
    public bool CanWallJump()
    {
        bool isGrounded = movement != null ? movement.IsGrounded : false;
        // 땅에 딛고 서 있지 않으며, 현재 벽에 매달려 비비는 상태이거나 좌우 벽 중 한 곳에라도 물리적으로 접촉해 있다면 가능!
        return !isGrounded && (isWallSliding || isTouchingLeftWall || isTouchingRightWall);
    }

    /// <summary>
    /// 🧗‍♂️ [벽점프 폭발적 실행기 - 벽을 차고 날아올라라!]
    /// </summary>
    public void ExecuteWallJump()
    {
        // 내가 붙어있던 벽이 왼쪽벽이면 ➡️ 오른쪽(1)으로 튕겨나가고, 오른쪽벽이면 ➡️ 왼쪽(-1)으로 튕겨나갑니다.
        wallJumpDirection = isTouchingLeftWall ? 1f : -1f;
        isWallJumping = true;
        wallJumpTimeCounter = wallJumpDuration; // 조작 격리 잠금 타이머 세팅

        // 벽점프 물리 힘 다이렉트 주입! (X축: 벽 반대로 밀림, Y축: 높이 도약)
        rigid.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
        
        // 캐릭터 몸 시선 방향도 날아가는 벽 반대 방향으로 180도 순간 Flip 정렬해 줍니다.
        transform.eulerAngles = new Vector3(0f, wallJumpDirection > 0f ? 0f : 180f, 0f);

        isWallSliding = false; // 점프 뛰어 날아가므로 슬라이딩 상태 해제
    }

    /// <summary>
    /// 🧗‍♂️ [벽 슬라이딩 마찰 제어기]
    /// </summary>
    private void HandleWallSliding()
    {   
        bool isGrounded = movement != null ? movement.IsGrounded : false;
        float inputX = movement != null ? movement.InputVector.x : 0f;

        // 공중에 떠있으며, 벽 방향으로 유저가 방향키를 우직하게 계속 밀어붙이고 있는가?
        if (!isGrounded && (isTouchingLeftWall || isTouchingRightWall))
        {
            bool isPushingWall = (isTouchingLeftWall && inputX < -0.01f) || (isTouchingRightWall && inputX > 0.01f);
            
            if (isPushingWall)
            {
                isWallSliding = true;
                
                // 💡 [지능형 브레이크 감속]
                // Y축 수직 속도가 실제로 중력에 끌려 '하강(linearVelocity.y < 0)'하고 있을 때만 제동 브레이크 속도(`-wallSlideSpeed`)로 묶습니다.
                // 이 덕분에 위에서 매달림 끝에 상승 관성으로 솟구칠 때는 벽 슬라이딩 제동이 전혀 걸리지 않아 비상을 가로막지 않습니다!
                
                // 🔍 [수학 공식 상세 설명]
                //  - Mathf.Max(A, B) 란?
                //    * 두 수 중에서 더 큰 수(Maximum)를 선택해서 데려오는 함수입니다.
                //    * 음수(마이너스)의 세계에서는 숫자가 0에 가까워질수록(즉, 절댓값이 작아질수록) 더 큰 수가 됩니다!
                //    * 예시: -1.0(천천히 떨어짐)과 -5.0(빠르게 낙하) 중에서 더 큰 수는 -1.0입니다!
                //    * 따라서 떨어지는 실시간 하강 속도(`y`)와 설정해 둔 슬라이딩 브레이크 제동 한계속도(`-wallSlideSpeed = -2.0`) 중에서 
                //    * 더 큰 수를 고르는 방식으로 연산하면 ➡️ 아래로 무섭게 빠르게 떨어지던 속도가 브레이크 장벽인 -2.0m/s 아래로 절대 뚫고 내려가지 못하게 꽉 잡아 가둘 수 있게 됩니다!
                if (rigid.linearVelocity.y < 0f)
                {
                    rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, Mathf.Max(rigid.linearVelocity.y, -wallSlideSpeed));
                }
                return;
            }
        }

        isWallSliding = false;
    }

    /// <summary>
    /// 🧗‍♂️ [양옆 듀얼 벽 스캔 레이더 센서]
    /// </summary>
    private void CheckWall()
    {
        if (mainCollider == null) return;

        float yOffset = 0.2f;   // 발가락 타일 걸림 예외 오프셋
        float xOffset = 0.05f;  // 몸체 콜라이더 모서리 오프셋
        float totalDistance = xOffset + wallCheckDistance;

        // 좌측 하단, 우측 하단에서 각각 사방으로 길게 레이저를 수평 발사합니다.
        Vector2 leftOrigin = new Vector2(mainCollider.bounds.min.x + xOffset, mainCollider.bounds.min.y + yOffset);
        Vector2 rightOrigin = new Vector2(mainCollider.bounds.max.x - xOffset, mainCollider.bounds.min.y + yOffset);

        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.left, totalDistance, wallLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.right, totalDistance, wallLayer);

        // 씬 뷰 에디터 상에 레이저 상태를 파란색(벽 접촉 성공)과 노란색(공중)으로 실시간 드로잉합니다.
        Debug.DrawRay(leftOrigin, Vector2.left * totalDistance, hitLeft.collider != null ? Color.blue : Color.yellow);
        Debug.DrawRay(rightOrigin, Vector2.right * totalDistance, hitRight.collider != null ? Color.blue : Color.yellow);

        // 부딪힌 대상이 내 몸통 자체가 아닌 다른 벽 타일인 경우에만 닿음 상태로 동기화합니다.
        isTouchingLeftWall = hitLeft.collider != null && hitLeft.collider.gameObject != gameObject;
        isTouchingRightWall = hitRight.collider != null && hitRight.collider.gameObject != gameObject;
    }
}