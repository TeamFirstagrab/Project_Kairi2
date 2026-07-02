using Globals;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	private Rigidbody2D rigid;
	private Collider2D coll;
	private PlayerGroundChecker groundChecker;
	private PlayerAttack attack;
	private bool isJump;    // 점프 여부
	private PlayerStatsRuntime stats;

	// 점프
	[Header("플레이어 점프 관련")]
	[Tooltip("점프하고 땅에 닿기 전 점프 입력을 저장할 시간")]
	[SerializeField] private float jumpBufferTime = 0.15f;
	[Tooltip("점프하고 떨어질 때 가속도")]
	[SerializeField] private float fallMultiplier = 3f;
	[Tooltip("점프 키를 살짝만 눌렀을 때 점프할 중력값")]
	[SerializeField] private float lowJumpMultiplier = 8f;
	private float jumpBufferCounter;	// 선입력된 잔여 점프 유효 시간 카운터
	private float landingImpactTimer;   // 착지 모션 타이머

	// 웅크리기
	private bool dashRequested;
	public bool isCrouchPressed;

	// 경사로 방지 및 각도 보정용 코어 변수
	private float slopeJumpProtectionTimer;
	private float defaultGravityScale = 3f;

	// 대쉬
	[Header("플레이어 대쉬 관련")]
	[SerializeField] GameObject dashEffectPref;
	[SerializeField] Vector3 dashEffectOffset = new Vector3(0f, -1f, 0f);
	public bool isDash;		// 대쉬 여부
	private float dashTimer;    // 대쉬 타이머
	private Vector2 currDashVelocity;   // 대쉬 시작 시 경사면 대쉬 속도 벡터
	private float dashDir;      // 대쉬 X축 방향 (-1: 왼쪽, 1: 오른쪽)
	[SerializeField] private float dashCooldown = 1f;   // 대쉬 쿨타임
	private float dashCooldownTimer;                    // 남은 쿨타임

	public Vector2 inputVec;

    private void Awake()
    {
		rigid = GetComponent<Rigidbody2D>();
		coll = GetComponent<Collider2D>();
		groundChecker = GetComponent<PlayerGroundChecker>();
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

		// 공중에 몸이 떴거나 웅크리기가 해제될 경우 준비 해제
		if (!groundChecker.isGrounded || !isCrouchPressed)
		{
			dashRequested = false;
		}

		if (jumpBufferCounter > 0f)
		{
			jumpBufferCounter -= Time.deltaTime;
		}

		if (dashCooldownTimer > 0f)		// 대쉬 쿨타임
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
		// 대쉬
		if(isDash)
		{
			dashTimer -= Time.fixedDeltaTime;
			rigid.gravityScale = 0f;
			rigid.linearVelocity = currDashVelocity;

			if(dashTimer <= 0f)
			{
				isDash = false;
				rigid.gravityScale = defaultGravityScale;	// 대쉬가 끝나면 기본 중력으로 변경
			}
			return;
		}
		else if(rigid.gravityScale == 0f)
		{
			rigid.gravityScale = defaultGravityScale;
		}

		Move();     // 이동

		// 벽점프 또는 점프가 가능할 때 점프하기
		if (jumpBufferCounter > 0f)
		{
			if (groundChecker.isGrounded)
			{
				Jump();
			}
			// TODO: 벽 점프
			//else if (wallMovement != null && wallMovement.CanWallJump())
			//{
			//	wallMovement.ExecuteWallJump();
			//	jumpBufferCounter = 0f; // 점프 발동 즉시 버퍼 수명 0 소거
			//}
		}

		// 가변 중력 감가 속도 연산
		ApplyGravityModifiers();
	}

	private void Move()	// 플레이어 이동
	{
		bool isCrouching = groundChecker.isGrounded && isCrouchPressed;
		bool hasHorizontalInput = Mathf.Abs(inputVec.x) > 0.1f;

		// 대쉬 이동
		if(dashRequested && isCrouching && hasHorizontalInput && !isDash)
		{
			isDash = true;
			dashTimer = stats.dashDuration;
			dashDir = inputVec.x > 0f ? 1f : -1f;
			dashRequested = false;  // 대쉬 예약 파괴

			// 대쉬하는 방향으로 오브젝트 회전
			transform.eulerAngles = new Vector3(0f, dashDir > 0f ? 0f : 180f, 0f);

			// 대쉬 이펙트
			GameObject dashObj = Instantiate(dashEffectPref, (transform.position + dashEffectOffset), transform.rotation);

			Vector2 dirVec = new Vector2(dashDir, 0f);  // 대쉬 방향 벡터 생성
			Vector2 rayOrigin = (Vector2)coll.bounds.center;    // 발 아래 Raycast 방향
			float sniffDist = coll.bounds.extents.y + 0.8f;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, sniffDist, LayerMask.GetMask(LayerName.ground));

			// DEBUG: 땅을 찾으면 하늘색, 못 찾으면 자홍색
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

		// 벽 점프 동안에는 조작 무시
		//if (wallMovement != null && wallMovement.IsWallJumping)
		//{
		//	return;
		//}

		// 기본 이동
		float targetSpeed = inputVec.x * stats.moveSpeed;
		float velY = rigid.linearVelocityY;

		// 경사에 가만히 서있을 때 미끄러짐 방지 (이동속도 제거)
		if(groundChecker.isGrounded && groundChecker.isSlope && Mathf.Abs(inputVec.x) < 0.01f 
			&& rigid.linearVelocityY <= 0.01f && slopeJumpProtectionTimer <= 0f)
		{
			targetSpeed = 0f;
			velY = 0f;
		}

		// 오르막에서 붕 뜸 방지
		else if(groundChecker.isGrounded && groundChecker.isSlope && velY > 0.05f)
		{
			bool isChangingDir = (inputVec.x > 0.01f && rigid.linearVelocityX < -0.01f) ||
								 (inputVec.x < -0.01f && rigid.linearVelocityX > 0.01f);

			if (isChangingDir)
			{
				velY = 0f;
			}
		}

		// 오르막에서 나와 평지로 올라왔을 때 덜덜 떨리는 현상 방지
		else if(groundChecker.isGrounded && !groundChecker.isSlope && velY > 0.05f)
		{
			velY = 0f;
		}

		// 물리 적용
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
	}

	// 중력 추가 적용, 가변 낙하 중력
	private void ApplyGravityModifiers()
	{
		if(groundChecker.isGrounded)
		{
			// 땅에 서있을 경우 + 경사면일 경우 미끄러지지 않게 중력 제거
			float targetSpeed = isCrouchPressed ? 0 : (inputVec.x * stats.moveSpeed);
			if(groundChecker.isSlope && Mathf.Abs(targetSpeed) < 0.01f 
				&& rigid.linearVelocityY <= 0.01f
				&& slopeJumpProtectionTimer <= 0f
				&& (attack == null || !attack.IsAttacking))
			{
				// x축, z축 값 고정
				rigid.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
			}
			else
			{
				// z축 값 (기본값) 고정
				rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
			}
		}
		// TODO: 벽 이동. 기본중력 X 벽에 붙어있을 경우 정해진 중력에 맞춰 떨어지기 (+ 애니메이션)
		//else if (wallMovement != null && wallMovement.IsWallSliding)
		//{
		//	rigid.gravityScale = defaultGravityScale; // 벽에 매달려 비벼 떨어질 때는 기본 중력 적용
		//}
		else
		{
			// 가변 중력
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
			jumpBufferCounter = 0;		// 손가락 떼면 0으로
		}

		//if (climb.isWall)
		//{
		//	climb.WallJump();
		//	return;
		//}
		//if (!groundChecker.IsGrounded) return;

		//rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		//groundChecker.IsGrounded = false;
	}

	public void TriggerRollInput()
	{
		dashRequested = true;
	}

	internal void SetCrouchInput(bool isPressed)
	{
		isCrouchPressed = isPressed;
	}

	//public void HandleMovement()
 //   {
 //       float targetSpeed = inputVec.x * moveSpeed;
 //       float speedDiff = targetSpeed - rigid.linearVelocity.x;
 //       float accelRate = (Mathf.Abs(inputVec.x) > 0.01f) ? acceleration : deceleration;

 //       rigid.AddForce(Vector2.right * speedDiff * accelRate);
 //       rigid.linearVelocity = new Vector2(
 //           Mathf.Clamp(rigid.linearVelocity.x, -moveSpeed, moveSpeed),
 //           rigid.linearVelocity.y);
 //   }

    public void UpdateSprite()
    {
        // 방향 전환
        if (inputVec.x > 0) transform.eulerAngles = Vector3.zero;
        else if (inputVec.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);

        // x축 고정
        //if (inputVec.x == 0)
        //    rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        //else
        //    rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void UpdateSprite(Vector2 dir)
    {
        if (dir.x > 0) transform.eulerAngles = Vector3.zero;
        else if (dir.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
    }
}