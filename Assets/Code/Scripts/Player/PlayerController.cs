using EnumType;
using Globals;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

enum DashType { NONE = 0, NORMAL, READY, DOWN }

public class PlayerController : MonoBehaviour, IDamageable
{
	// 플레이어 정보
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private bool isGrounded;    // 땅 여부
	private bool isGroundedSpecial;     // 떨어질 수 있는 땅
	private bool isDash;        // 대쉬 사용 여부
	private bool isAttack;      // 공격 여부
	float originalGravity;

	// 애니메이션
	private Animator animator;

	// 이동
	private Vector2 inputVec;   // 입력된 플레이어 이동값 (-1, 0, 1)
	private float speed;        // 플레이어 이동 속도
	private float dashTime;     // 대쉬 지속 시간

	// 땅 체크
	[SerializeField] private Transform groundCheckObj;      // 땅 체크 오브젝트 (프리펩)
	public float checkRadius = 0.1f;    // 땅 체크 반지름
	private LayerMask groundMask;
	private LayerMask oneWayPlatformMask;

	// 마우스 입력
	Vector2 mousePos, transPos, targetPos;

	/// <summary>
	/// Init
	/// </summary>
	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		groundMask = LayerMask.GetMask(TagName.ground);
		oneWayPlatformMask = LayerMask.GetMask(TagName.oneWayPlatform);
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		isGrounded = true;
		isGroundedSpecial = false;
		isDash = false;
		isAttack = false;
		speed = GameManager.Instance.playerStatsRuntime.speed;
		originalGravity = rigid.gravityScale;
	}

	/// <summary>
	/// Update
	/// </summary>
	private void FixedUpdate()
	{
		if (dashTime <= 0)
		{
			speed = GameManager.Instance.playerStatsRuntime.speed;
			if (isDash)
				dashTime = GameManager.Instance.playerStatsRuntime.dashDuration;
		}
		else
		{
			dashTime -= Time.deltaTime;
			speed = GameManager.Instance.playerStatsRuntime.dashSpeed;
		}
		isDash = false;

		if (!isDash)
			rigid.linearVelocity = new Vector2(inputVec.x * speed, rigid.linearVelocityY);
		UpdateSprite();     // 좌우 플립
	}

	private void Update()
	{
		if (inputVec.x == 0)        // 좌우 이동 입력이 없을 경우
			rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
		else    // 좌우 이동이 있을 경우
			rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
		rigid.gravityScale = originalGravity;
	}

	// 플레이어 스프라이트 업데이트
	private void UpdateSprite()
	{
		// 좌우 플립
		if (inputVec.x > 0)
			transform.eulerAngles = new Vector2(0f, 0f);
		else if (inputVec.x < 0)
			transform.eulerAngles = new Vector2(0f, 180f);
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

	private void GroundCheck()
	{
		isGrounded = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius);
		isGroundedSpecial = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, oneWayPlatformMask);
	}

	/// <summary>
	/// Input System
	/// </summary>
	private void OnMove(InputValue val)     // 좌우 이동 (AD)
	{
		if (isDash) return;     // 대쉬 사용 중일 경우 리턴
		inputVec = val.Get<Vector2>();
	}

	private void OnJump(InputValue val)     // 점프 (W)
	{
		if (!isGrounded) return;    // 땅에 서있지 않을 경우 리턴

		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		isGrounded = false;
	}

	private void OnCrouch(InputValue val)
	{
		if (isDash) return;

		if (isGroundedSpecial)
		{
			transform.position += Vector3.down * 0.1f;
		}
		else if (isGrounded)
		{
			Vector2 dir = inputVec;

			// 입력 없으면 바라보는 방향으로 대쉬
			if (dir == Vector2.zero)
				dir = transform.eulerAngles.y == 0 ? Vector2.right : Vector2.left;

			StartCoroutine(PlayerDash(dir));
		}
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
	private IEnumerator PlayerDash(Vector2 dir)
	{
		isDash = true;

		float originalGravity = rigid.gravityScale;
		rigid.gravityScale = 0f;

		float dashSpeed = GameManager.Instance.playerStatsRuntime.dashSpeed;
		float dashDuration = GameManager.Instance.playerStatsRuntime.dashDuration;

		float time = 0f;

		while (time < dashDuration)
		{
			rigid.linearVelocity = dir * dashSpeed;
			time += Time.deltaTime;
			yield return null;
		}

		rigid.gravityScale = originalGravity;
		isDash = false;
	}

	IEnumerator PlayerAttack(Vector2 target)
	{
		animator.Play(PlayerAnimName.Attack);
		isDash = true;
		isAttack = true;

		float dashSpeed = GameManager.Instance.playerStatsRuntime.dashSpeed;
		float dashDuration = GameManager.Instance.playerStatsRuntime.dashDuration;

		Vector2 startPos = transform.position;
		Vector2 dir = (target - (Vector2)transform.position).normalized;

		rigid.gravityScale = 0f;    // 중력 0으로

		transform.localScale = new Vector3(((dir.x < 0) ? -1 : 1), 1, 1);

		float dashDistance = dashSpeed * dashDuration;      // 대쉬 거리
		Vector2 endPos = startPos + dir * dashDistance;     // 목표 위치 (기본값)

		CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
		LayerMask isLayer = ~LayerMask.GetMask("Player");
		RaycastHit2D hit = Physics2D.CapsuleCast(
			col.bounds.center,
			col.size,
			CapsuleDirection2D.Vertical,
			0f,
			dir,
			dashDistance,
			isLayer
		);

		float time = 0f;

		if (hit)
		{
			// 공격 범위가 벽을 넘었을 경우
			if (hit.transform.CompareTag(TagName.ground))
				endPos = startPos + dir * (hit.distance - 0.1f);    // 벽 바로 앞에서 멈춤
			// 부서지는 오브젝트인 경우
			if(hit.transform.CompareTag(TagName.crackObj) || hit.transform.CompareTag(TagName.enemy))
			{
				endPos = startPos + dir * GameManager.Instance.playerStatsRuntime.attackDist;
				hit.transform.GetComponent<IDamageable>().TakeDamage(GameManager.Instance.playerStatsRuntime.attack);	// 데미지 주기
			}

		}

		while (time < dashDuration)
		{
			transform.position = Vector2.Lerp(startPos, endPos, time / dashDuration);
			time += Time.deltaTime;
			yield return null;
		}

		transform.position = endPos; // 마지막 보정
		rigid.gravityScale = originalGravity;

		yield return new WaitForSeconds(GameManager.Instance.playerStatsRuntime.attackCoolTime);

		isDash = false;
		isAttack = false;
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
			return;
		}
	}
}
