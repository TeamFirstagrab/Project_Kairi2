using EnumType;
using Globals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
	// 플레이어 정보
	private Rigidbody2D rigid;
	private bool isGrounded;    // 땅 여부
	private bool isGroundedSpecial;     // 떨어질 수 있는 땅
	private bool isAttack;      // 공격 여부
	float originalGravity;

	// 애니메이션
	private Animator animator;

	// 이동
	[SerializeField] float acceleration = 15f;
	[SerializeField] float deceleration = 10f;
	private Vector2 inputVec;   // 입력된 플레이어 이동값 (-1, 0, 1)
	private float maxSpeed;        // 플레이어 이동 속도

	// 대쉬
	private float dashTime;     // 대쉬 지속 시간
	private bool isDash;        // 대쉬 사용 여부
	private bool isDashReady;	// 대쉬 준비
	private bool canDash;       // 대쉬 사용 가능 여부 (쿨타임 지났을 때)
	private Vector2 dashDir;    // 대쉬 방향

	// 공격
	private Vector2 attackStartPos;
	private Vector2 attackEndPos;
	private Vector2 attackDir;

	// 땅 체크
	[SerializeField] private Transform groundCheckObj;      // 땅 체크 오브젝트 (프리펩)
	public float checkRadius = 0.1f;    // 땅 체크 반지름
	private LayerMask oneWayPlatformMask;

	// 마우스 입력
	Vector2 mousePos, transPos, targetPos;

	/// <summary>
	/// Init
	/// </summary>
	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		oneWayPlatformMask = LayerMask.GetMask(TagName.oneWayPlatform);
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		isGrounded = true;
		isGroundedSpecial = false;
		isDash = false;
		isDashReady = false;
		canDash = true;
		isAttack = false;
		maxSpeed = GameManager.Instance.playerStatsRuntime.speed;
		originalGravity = rigid.gravityScale;
	}

	/// <summary>
	/// Update
	/// </summary>
	private void FixedUpdate()
	{
		UpdateDash();	// 대쉬
	}

	private void Update()
	{
		Debug.DrawLine(attackStartPos, attackEndPos);
		//if (inputVec.x == 0)        // 좌우 이동 입력이 없을 경우
		//	rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
		//else    // 좌우 이동이 있을 경우
		//	rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

		rigid.gravityScale = originalGravity;
	}

	// 플레이어 대쉬
	private void UpdateDash()
	{
		if (!isDash)
		{
			HandleMovement();
		}

		UpdateSprite(inputVec);     // 좌우 플립
	}

	// 플레이어 스프라이트 업데이트
	private void UpdateSprite(Vector2 target)
	{
		// 좌우 플립
		if (target.x > 0)
			transform.eulerAngles = new Vector2(0f, 0f);
		else if (target.x < 0)
			transform.eulerAngles = new Vector2(0f, 180f);
	}

	private void HandleMovement()
	{
		float targetSpeed = inputVec.x * maxSpeed;
		float currentSpeed = rigid.linearVelocity.x;

		float speedDiff = targetSpeed - currentSpeed;

		// 입력 있으면 가속 / 없으면 감속
		float accelRate = (Mathf.Abs(inputVec.x) > 0.01f) ? acceleration : deceleration;

		// 힘 적용
		float movement = speedDiff * accelRate;
		rigid.AddForce(Vector2.right * movement);

		// 최대 속도 제한
		float clampedSpeed = Mathf.Clamp(rigid.linearVelocity.x, -maxSpeed, maxSpeed);
		rigid.linearVelocity = new Vector2(clampedSpeed, rigid.linearVelocity.y);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (isAttack && collision.CompareTag(TagName.bullet))
		{
			collision.GetComponent<EnemyBullet>().DeflectBullet();
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		GroundCheck();
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		GroundCheck();
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.transform.CompareTag(TagName.ground))
			isGrounded = false;
	}

	// 땅 체크
	private void GroundCheck()
	{
		isGrounded = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius);
		isGroundedSpecial = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, oneWayPlatformMask);
	}

	// 대쉬 시도
	private void TryDash()
	{
		if (isDashReady && !isDash && inputVec != Vector2.zero)
		{
			dashDir = inputVec.normalized;   // 방향 설정
			StartCoroutine(PlayerDash());
			isDash = true;
			isDashReady = false;
		}
	}

	/// <summary>
	/// Input System
	/// </summary>
	private void OnMove(InputValue val)     // 좌우 이동 (AD)
	{
		inputVec = val.Get<Vector2>();
		animator.Play(PlayerAnimName.run);
		TryDash();
	}

	private void OnReleaseMove(InputValue val)
	{
		animator.Play(PlayerAnimName.idle);
	}

	private void OnJump(InputValue val)     // 점프 (W)
	{
		if (!isGrounded) return;    // 땅에 서있지 않을 경우 리턴

		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		isGrounded = false;
	}

	private void OnCrouch()			// 대쉬 준비
	{
		isDashReady = true;
		animator.Play(PlayerAnimName.landDown);

		if (isGroundedSpecial)
			transform.position += Vector3.down * 0.1f;

		TryDash();
	}

	private void OnReleaseCrouch()  // 대쉬 해제
	{
		isDashReady = false;
		animator.Play(PlayerAnimName.landUp);
	}

	private void OnAttack(InputValue val)
	{
		if (isAttack) return;

		mousePos = Input.mousePosition;
		transPos = Camera.main.ScreenToWorldPoint(mousePos);
		targetPos = new Vector2(transPos.x, transPos.y);

		// 마우스 방향으로 공격
		StartCoroutine(PlayerAttack(targetPos));
	}



	/// <summary>
	/// Debug
	/// </summary>
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(groundCheckObj.position, checkRadius);
	}

	/// <summary>
	/// Coroutine
	/// </summary>
	private IEnumerator PlayerDash()
	{
		isDash = true;

		float originalGravity = rigid.gravityScale;
		rigid.gravityScale = 0f;

		float dashSpeed = GameManager.Instance.playerStatsRuntime.dashDist;
		float dashDuration = GameManager.Instance.playerStatsRuntime.dashDuration;

		float time = 0f;

		while (time < dashDuration)
		{
			dashDir = inputVec.normalized;
			rigid.linearVelocity = dashDir * dashSpeed;
			time += Time.deltaTime;
			yield return null;
		}

		rigid.gravityScale = originalGravity;
		isDash = false;

		// 기본 상태로 돌아오기
		rigid.linearVelocity = Vector2.zero;
		animator.Play(PlayerAnimName.idle);
		inputVec.x = 0;		// x 입력값 0으로
	}

	IEnumerator PlayerAttack(Vector2 target)
	{
		animator.Play(PlayerAnimName.attack);
		isDash = true;
		isAttack = true;

		float dashSpeed = GameManager.Instance.playerStatsRuntime.dashDist;
		float dashDuration = GameManager.Instance.playerStatsRuntime.dashDuration;

		attackStartPos = transform.position;
		attackDir = (target - (Vector2)transform.position).normalized;
		UpdateSprite(attackDir);     // 좌우 플립

		rigid.gravityScale = 0f;    // 중력 0으로

		float dashDistance = dashSpeed * dashDuration;      // 대쉬 거리
		attackEndPos = attackStartPos + attackDir * dashDistance;     // 목표 위치 (기본값)

		CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
		LayerMask isLayer = ~LayerMask.GetMask(LayerName.player);
		RaycastHit2D hit = Physics2D.Raycast(attackStartPos, attackDir, dashDistance + 0.5f);	// 0.5f: 보정값

		float time = 0f;

		if (hit)
		{
			// 공격 범위가 벽을 넘었을 경우
			if (hit.transform.CompareTag(TagName.ground))
				attackEndPos = attackStartPos + attackDir * (hit.distance - 0.5f);    // 벽 바로 앞에서 멈춤

			// 부서지는 오브젝트 또는 적일 경우
			else if (hit.transform.CompareTag(TagName.crackObj) || hit.transform.CompareTag(TagName.enemy))
			{
				IDamageable damage = hit.transform.GetComponent<IDamageable>();
				if (damage != null)
				{
					damage.TakeDamage(GameManager.Instance.playerStatsRuntime.attack);
					attackEndPos = attackStartPos + attackDir * (dashDistance + GameManager.Instance.playerStatsRuntime.attackDist);
				}
			}
		}

		while (time < dashDuration)
		{
			transform.position = Vector2.Lerp(attackStartPos, attackEndPos, time / dashDuration);
			time += Time.deltaTime;
			yield return null;
		}

		transform.position = attackEndPos; // 마지막 보정
		rigid.gravityScale = originalGravity;

		yield return new WaitForSeconds(GameManager.Instance.playerStatsRuntime.attackCoolTime);

		isAttack = false;
		isDash = false;
	}

	/// <summary>
	/// Interface
	/// </summary>
	public void TakeDamage(int attack)  // 데미지
	{
		if (isDash) return;   // 무적일 경우 리턴

		GameManager.Instance.playerStatsRuntime.currentHP -= attack;    // 체력 감소

		if (GameManager.Instance.playerStatsRuntime.currentHP <= 0)     // 체력이 0 이하일 때
		{
			Debug.Log("플레이어 사망");
			return;
		}
	}
}
