using Globals;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	private Rigidbody2D rigid;
	private Collider2D coll;
	private PlayerGroundChecker groundChecker;
	private PlayerWallDetector wallDetector;
	private PlayerAttack attack;
	private bool isJump;    // 점프 중
	private PlayerStatsRuntime stats;
	[HideInInspector] public bool canMove = true;

	// 점프
	[Header("플레이어 점프 설정")]
	[Tooltip("선행 입력으로 유효한 입력 시간")]
	[SerializeField] private float jumpBufferTime = 0.15f;
	[Tooltip("낙하 시 추가 중력")]
	[SerializeField] private float fallMultiplier = 3f;
	[Tooltip("점프 키를 살짝 뗐을 때 추가 중력값")]
	[SerializeField] private float lowJumpMultiplier = 8f;
	private float jumpBufferCounter;	// 입력된 잔여 선행 입력 시간 카운터
	private float landingImpactTimer;   // 착지 타이머

	// 벽 타기 (Wall Jump / Wall Slide)
	[Header("플레이어 벽 타기 설정")]
	[Tooltip("벽을 밀면서 점프 시 적용되는 점프 배수")]
	[SerializeField] private float wallPushJumpMultiplier = 2f;
	[Tooltip("벽 미끄러짐 최대 속도")]
	[SerializeField] private float wallSlideSpeed = 2f;
	[Tooltip("벽 점프 시 적용되는 힘 (X: 반대 방향, Y: 상향)")]
	[SerializeField] private Vector2 wallJumpForce = new Vector2(50f, 12f);
	[Tooltip("벽 점프 시 수평 이동 제어 지속 시간")]
	[SerializeField] private float wallJumpDuration = 1f;

	public bool IsWallSliding { get; private set; }
	public bool IsWallJumping { get; private set; }
	private float wallJumpTimer;

	// 크라우치
	private bool dashRequested;
	public bool isCrouchPressed;

	// 경사로 및 기타 제어
	private float slopeJumpProtectionTimer;
	private float defaultGravityScale = 3f;

	// 대시
	[Header("플레이어 대시 설정")]
	[SerializeField] GameObject dashEffectPref;
	[SerializeField] Vector3 dashEffectOffset = new Vector3(0f, -1f, 0f);
	[SerializeField] private float dashCooldown = 1f;   // 대시 쿨타임
	public bool isDash;						// 대시 중
	private float dashTimer;				// 대시 타이머
	private Vector2 currDashVelocity;		// 대시 당시 수평 방향 대시 속도 벡터
	private float dashDir;					// 대시 X방향 (-1: 좌, 1: 우)
	private float dashCooldownTimer;        // 쿨타임 타이머
	private Transform collPlatform;

	public Vector2 inputVec;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		coll = GetComponent<Collider2D>();
		groundChecker = GetComponent<PlayerGroundChecker>();
		wallDetector = GetComponent<PlayerWallDetector>();
		attack = GetComponent<PlayerAttack>();
	}

	private void Update()
	{
		stats = GameManager.Instance.playerStatsRuntime;
		bool wasGrounded = groundChecker.isGrounded;
		groundChecker.CheckGround();

		// 착지 (0.12초간)
		if(!wasGrounded && groundChecker.isGrounded && rigid.linearVelocityY < -0.5f)
		{
			landingImpactTimer = 0.12f;
		}
		if(landingImpactTimer > 0f)
		{
			landingImpactTimer -= Time.deltaTime;
		}

		// 벽 점프 타이머 관리
		if (IsWallJumping)
		{
			wallJumpTimer -= Time.deltaTime;
			if (wallJumpTimer <= 0f)
			{
				IsWallJumping = false;
			}
		}

		// 벽 슬라이딩 상태 체크
		if (wallDetector != null && !groundChecker.isGrounded && wallDetector.IsTouchingWall)
		{
			IsWallSliding = true;
		}
		else
		{
			IsWallSliding = false;
		}

		// 공중에 없을 때나 크라우치가 아닐 때 대시 취소
		if (!groundChecker.isGrounded || !isCrouchPressed)
		{
			dashRequested = false;
		}

		if (jumpBufferCounter > 0f)
		{
			jumpBufferCounter -= Time.deltaTime;
		}

		if (dashCooldownTimer > 0f)		// 대시 쿨타임
		{
			dashCooldownTimer -= Time.deltaTime;
		}

		if (slopeJumpProtectionTimer > 0f)
		{
			slopeJumpProtectionTimer -= Time.deltaTime;
		}
	}

	private void FixedUpdate()
	{
		if (!canMove) return;

		// 대시
		if(isDash)
		{
			dashTimer -= Time.fixedDeltaTime;
			rigid.gravityScale = 0f;
			rigid.linearVelocity = currDashVelocity;

			if(dashTimer <= 0f)
			{
				EndDash();
			}
			return;
		}
		else if(rigid.gravityScale == 0f)
		{
			rigid.gravityScale = defaultGravityScale;
		}

		// 원웨이 플랫폼에서 크라우치 시작 시 살짝 아래로 이동
		if (isCrouchPressed && groundChecker.isGroundedOneway)
		{
			if (collPlatform.TryGetComponent<OneWayPlatformController>(out var oneWayP))
			{
				oneWayP.SetTriggerOn();
				dashRequested = false;      // 대쉬 해제
			}
		}

		Move();     // 이동

		// 벽 슬라이딩 속도 제어
		if (IsWallSliding)
		{
			if (rigid.linearVelocityY < -wallSlideSpeed)
			{
				rigid.linearVelocity = new Vector2(rigid.linearVelocityX, -wallSlideSpeed);
			}
		}

		// 점프 또는 벽 점프 실행
		if (jumpBufferCounter > 0f)
		{
			// 벽 점프
			if (wallDetector != null && wallDetector.IsTouchingWall && inputVec.x != 0f)
			{
				print($"push wall");
				ExecuteWallPushJump();
				jumpBufferCounter = 0f;
			}
			// 벽 슬라이딩
			else if (IsWallSliding)
			{
				print($"sliding");
				ExecuteWallJump();
				jumpBufferCounter = 0f;
			}
			// 일반 점프
			else if (groundChecker.isGrounded)
			{
				print($"jump");
				Jump();
				jumpBufferCounter = 0f;
			}
		}

		// 중력 추가 및 속도 제어
		ApplyGravityModifiers();
	}

	private void Move()	// 플레이어 이동
	{
		bool isCrouching = groundChecker.isGrounded && isCrouchPressed;
		bool hasHorizontalInput = Mathf.Abs(inputVec.x) > 0.1f;

		// 대시 이동
		if(dashRequested && isCrouching && hasHorizontalInput && !isDash)
		{
			isDash = true;
			dashTimer = stats.dashDuration;
			dashDir = inputVec.x > 0f ? 1f : -1f;
			dashRequested = false;  // 대시 예약 해제

			// 대시하는 방향 이펙트 회전
			transform.eulerAngles = new Vector3(0f, dashDir > 0f ? 0f : 180f, 0f);

			// 대시 이펙트
			GameObject dashObj = Instantiate(dashEffectPref, (transform.position + dashEffectOffset), transform.rotation);

			Vector2 dirVec = new Vector2(dashDir, 0f);  // 대시 방향 기본값
			Vector2 rayOrigin = (Vector2)coll.bounds.center;    // 발 아래 Raycast 위치
			float sniffDist = coll.bounds.extents.y + 0.8f;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, sniffDist, LayerMask.GetMask(LayerName.ground));

			// DEBUG: 땅 찾으면 하늘색, 못 찾으면 분홍색
			Color rayColor = hit ? Color.cyan : Color.magenta;
			Debug.DrawRay(rayOrigin, Vector2.down * sniffDist, rayColor, 1.5f);

			if(hit)
			{
				float slopeAngle = Vector2.Angle(Vector2.up, hit.normal);
				if(slopeAngle > 2f && groundChecker.CheckMaxSlope(slopeAngle))
				{
					// 경사에 맞춰 이동
					Vector2 normal = hit.normal;
					dirVec = Vector3.ProjectOnPlane(dirVec, normal).normalized;
				}
			}

			currDashVelocity = dirVec * stats.dashSpeed;
			return;
		}

		// 벽 점프 수행 중 수평 이동 제한
		if (IsWallJumping)
		{
			return;
		}

		// 기본 이동
		float targetSpeed = inputVec.x * stats.moveSpeed;
		float velY = rigid.linearVelocityY;

		// 경사로 미끄러짐 방지 (이동속도 0일 때)
		if(groundChecker.isGrounded && groundChecker.isSlope && Mathf.Abs(inputVec.x) < 0.01f 
			&& rigid.linearVelocityY <= 0.01f && slopeJumpProtectionTimer <= 0f)
		{
			targetSpeed = 0f;
			velY = 0f;
		}

		// 경사로 이동 처리
		else if(groundChecker.isGrounded && groundChecker.isSlope && velY > 0.05f)
		{
			bool isChangingDir = (inputVec.x > 0.01f && rigid.linearVelocityX < -0.01f) ||
								 (inputVec.x < -0.01f && rigid.linearVelocityX > 0.01f);

			if (isChangingDir)
			{
				velY = 0f;
			}
		}

		// 평지 올라갈 때 Y속도 튀는 현상 방지
		else if(groundChecker.isGrounded && !groundChecker.isSlope && velY > 0.05f)
		{
			velY = 0f;
		}

		// 이동 적용
		rigid.linearVelocity = new Vector2(targetSpeed, velY);

		if(inputVec.x > 0f)
		{
			transform.eulerAngles = Vector2.zero;
		}
		else if(inputVec.x < 0)
		{
			transform.eulerAngles = new Vector3(0f, 180f, 0f);
		}
	}

	private void Jump()	// 플레이어 점프
	{
		rigid.linearVelocity = new Vector2(rigid.linearVelocityX, stats.jumpForce);
		slopeJumpProtectionTimer = 0.2f;
		if (groundChecker != null) groundChecker.ForceUnground();
	}

	private void ExecuteWallJump() // 벽 밀어내기 점프
	{
		IsWallJumping = true;
		wallJumpTimer = wallJumpDuration;

		float jumpDir = wallDetector != null ? -wallDetector.WallDirection : (transform.eulerAngles.y == 0f ? -1f : 1f);
		rigid.linearVelocity = new Vector2(jumpDir * wallJumpForce.x, wallJumpForce.y);

		if (jumpDir > 0f)
		{
			transform.eulerAngles = Vector3.zero;
		}
		else if (jumpDir < 0f)
		{
			transform.eulerAngles = new Vector3(0f, 180f, 0f);
		}

		IsWallSliding = false;
		if (groundChecker != null) groundChecker.ForceUnground();
	}

	private void ExecuteWallPushJump() // 벽 밀기 수직 상승 점프
	{
		rigid.linearVelocity = new Vector2(rigid.linearVelocityX, stats.jumpForce * wallPushJumpMultiplier);
		IsWallJumping = true;
		wallJumpTimer = wallJumpDuration;
		slopeJumpProtectionTimer = 0.2f;
		if (groundChecker != null) groundChecker.ForceUnground();
	}

	// 중력 추가 및 속도 제어
	private void ApplyGravityModifiers()
	{
		if(groundChecker.isGrounded)
		{
			// 경사로 + 수평 이동 안할 때 중력 잠금
			float targetSpeed = isCrouchPressed ? 0 : (inputVec.x * stats.moveSpeed);
			if(groundChecker.isSlope && Mathf.Abs(targetSpeed) < 0.01f 
				&& rigid.linearVelocityY <= 0.01f
				&& slopeJumpProtectionTimer <= 0f
				&& (attack == null || !attack.IsAttacking))
			{
				// x축, z축 고정
				rigid.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
			}
			else
			{
				// z축 고정 (기본값)
				rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
			}
		}
		else if (IsWallSliding)
		{
			rigid.gravityScale = defaultGravityScale;
		}
		else
		{
			// 공중 중력
			if(rigid.linearVelocityY < 0f)
			{
				rigid.gravityScale = fallMultiplier;
			}
			else if(rigid.linearVelocityY > 0f && !isJump)
			{
				rigid.gravityScale = lowJumpMultiplier;
			}
		}
	}

	public void SetJumpInput(bool isPressed)
	{
		isJump = isPressed;

		if(isJump)
		{
			jumpBufferCounter = jumpBufferTime;
		}
		else
		{
			jumpBufferCounter = 0;		// 손뗴면 바로 0으로
		}
	}

	public void TriggerRollInput()
	{
		dashRequested = true;
	}

	public void EndDash()
	{
		print("End Dash");
		isDash = false;
		rigid.gravityScale = defaultGravityScale;
		print($"excludeLayer: {coll.excludeLayers.value}");
	}

	internal void SetCrouchInput(bool isPressed)
	{
		isCrouchPressed = isPressed;
	}

	public void UpdateSprite()
	{
		// 방향 전환
		if (inputVec.x > 0) transform.eulerAngles = Vector3.zero;
		else if (inputVec.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
	}

	public void UpdateSprite(Vector2 dir)
	{
		if (dir.x > 0) transform.eulerAngles = Vector3.zero;
		else if (dir.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if(collision.transform.CompareTag(TagName.oneWayPlatform))
		{
			collPlatform = collision.transform;
		}
	}
}