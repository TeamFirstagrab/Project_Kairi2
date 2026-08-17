using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 실제 이동, 일반 점프, 벽 슬라이딩 및 벽 점프(Wall Jump)의 물리 처리를 담당하는 핵심 클래스입니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(KatanaPlayerInputReader))]
[RequireComponent(typeof(KatanaPlayerGroundDetector))]
[RequireComponent(typeof(KatanaPlayerWallDetector))]
public class KatanaPlayerMovement : MonoBehaviour
{
    [Header("기본 이동 및 점프")]
    [Tooltip("지상 가로 이동 속도입니다.")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("일반 수직 점프의 힘(속도)입니다.")]
    [SerializeField] private float jumpForce = 12f;
	[Tooltip("벽을 밀며 점프할 때의 점프 힘 배율입니다.")]
	[SerializeField] private float wallPushJumpMultiplier = 1.5f;

	[Header("벽 슬라이드 설정")]
    [Tooltip("벽에 달라붙어 미끄러져 내려갈 때의 최대 낙하 속도입니다.")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("벽 점프(Wall Jump) 설정")]
    [Tooltip("벽 점프 시 플레이어에게 가해지는 힘의 크기입니다. X축은 튕겨 나가는 힘, Y축은 위로 솟구치는 힘을 나타냅니다.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 12f);
    [Tooltip("벽 점프 시 사용자 입력을 무시하고 물리력을 유지할 시간(초)입니다. 이 시간 동안 튕겨나가는 X축 운동량이 유지됩니다.")]
    [SerializeField] private float wallJumpDuration = 0.15f;

    // 컴포넌트 레퍼런스
    private Rigidbody2D rb;
    private KatanaPlayerInputReader inputReader;
    private KatanaPlayerGroundDetector groundDetector;
    private KatanaPlayerWallDetector wallDetector;

    // 플레이어의 현재 응시 방향 및 움직임 관련 변수
    private bool isFacingRight = true;
    
    /// <summary>
    /// 플레이어가 벽에 닿아 있고, 해당 방향으로 입력기를 미는 등의 조건을 만족하여 벽 슬라이딩 중인지 여부를 나타내는 프로퍼티입니다.
    /// </summary>
    public bool IsWallSliding { get; private set; }
    
    // 벽 점프 작동 여부 및 시간 관리용 변수
    private bool isWallJumping;
    private float wallJumpTimer;

    /// <summary>
    /// 플레이어의 물리 엔진(Rigidbody2D)상 현재 속도를 외부(예: 애니메이터)에 제공하는 읽기 전용 프로퍼티입니다.
    /// </summary>
    public Vector2 CurrentVelocity => rb.linearVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputReader = GetComponent<KatanaPlayerInputReader>();
        groundDetector = GetComponent<KatanaPlayerGroundDetector>();
        wallDetector = GetComponent<KatanaPlayerWallDetector>();
    }

    private void Update()
    {
        // 1. 벽 점프 작동 시간 타이머 처리
        // 벽 점프 후 일정 시간 동안(wallJumpDuration)은 플레이어가 방향키 입력을 반대로 조작해도 튕겨 나가는 힘이 유지되도록 제어합니다.
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0f)
            {
                isWallJumping = false;
            }
        }

        // 공중에 떠 있고, 벽에 밀착해 있으며, 벽 쪽으로 입력을 누르고 있는 경우 벽 슬라이딩 상태가 됩니다.
        if (!groundDetector.IsGrounded && wallDetector.IsTouchingWall)
        {
            IsWallSliding = true;
        }
        else
        {
            IsWallSliding = false;
        }

        // 3. 방향 전환(Flip) 제어
        // 벽 점프 도중이나 벽 슬라이드 도중이 아닐 때에만 캐릭터가 입력값의 방향에 맞게 좌우를 보게 합니다.
        if (!isWallJumping && !IsWallSliding)
        {
            FlipCharacter(inputReader.HorizontalInput);
        }
		// 4. 점프 입력 처리
		if (inputReader.JumpPressedThisFrame)
        {
			if (IsWallSliding)
			{
				isWallJumping = true;
				wallJumpTimer = wallJumpDuration;

				// 벽의 반대 방향으로 점프 방향을 설정합니다.
				float jumpDir = -wallDetector.WallDirection;
				rb.linearVelocity = new Vector2(jumpDir * wallJumpForce.x, wallJumpForce.y);

				// 캐릭터가 벽 반대편을 바라보도록 변경합니다.
				FlipCharacter(jumpDir);

				// 벽 점프를 수행했으므로 슬라이드 상태는 강제 해제합니다.
				IsWallSliding = false;
			}
            // 공중에 떠서 벽 슬라이딩 중인 경우 벽 점프(Wall Jump) 수행
            else
			{
				// 벽을 향해 방향키를 밀고 있는지 체크
				bool isPushingTowardsWall = wallDetector.IsTouchingWall &&
					((inputReader.HorizontalInput > 0 && wallDetector.WallDirection > 0) ||
					 (inputReader.HorizontalInput < 0 && wallDetector.WallDirection < 0));

				if (isPushingTowardsWall)
				{
					// 벽 방향으로 밀면서 점프 시 높게 점프
					rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce * wallPushJumpMultiplier);

					// 입력 방지 작동 (동일하게 0.15초 적용)
					isWallJumping = true;
					wallJumpTimer = wallJumpDuration;
				}
				else
				{
					rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
				}
			}
        }
    }

    private void FixedUpdate()
    {
        // 1. 가로축 이동 처리 (벽 점프 관성 상태가 아닐 때만 플레이어가 완전히 제어 가능)
        if (!isWallJumping)
        {
            rb.linearVelocity = new Vector2(inputReader.HorizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        // 2. 벽 슬라이딩 마찰(속도 제한) 처리
        if (IsWallSliding)
        {
            // 벽을 탈 때 중력 때문에 너무 빨리 떨어지지 않도록 y축 하강 속도를 최대 wallSlideSpeed로 고정합니다.
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
    }

    /// <summary>
    /// 입력값(가로 축) 또는 설정된 방향에 맞춰 캐릭터의 좌우 스프라이트 및 콜라이더 스케일을 반전시키는 메서드입니다.
    /// </summary>
    /// <param name="moveX">바라볼 방향값 (양수: 우측, 음수: 좌측)</param>
    private void FlipCharacter(float moveX)
    {
        if ((moveX > 0 && !isFacingRight) || (moveX < 0 && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            // X축 로컬 스케일을 반전시켜 캐릭터의 방향을 전환합니다.
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
}