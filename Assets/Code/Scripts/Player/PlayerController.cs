using EnumType;
using Globals;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using EnumType;

public class PlayerController : MonoBehaviour, IDamageable
{
	// 플레이어 정보
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private bool isGrounded;    // 땅 여부
	private bool isGroundedSpecial;		// 떨어질 수 있는 땅
	private bool isDash;        // 대쉬 사용 여부
	private bool isAttack;      // 공격 여부

	// 공격
	public Transform attackPos;
	public Vector2 attackBoxSize;

	// 애니메이션
	private Animator animator;

	// 이동
	private Vector2 inputVec;   // 입력된 플레이어 이동값 (-1, 0, 1)
	private float speed;        // 플레이어 이동 속도
	private float dashTime;		// 대쉬 지속 시간

	// 땅 체크
	[SerializeField] private Transform groundCheckObj;      // 땅 체크 오브젝트 (프리펩)
	public float checkRadius = 0.1f;    // 땅 체크 반지름
	LayerMask groundMask;
	LayerMask groundSpecialMask;

	// 코루틴
	private Coroutine playerDashCoroutine;  // 플레이어 대쉬

	/// <summary>
	/// Init
	/// </summary>
	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		groundMask = LayerMask.GetMask(TagName.ground);
		groundSpecialMask = LayerMask.GetMask(TagName.groundSpecial);
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		isGrounded = true;
		isGroundedSpecial = false;
		isDash = false;
		isAttack = false;
		speed = GameManager.Instance.playerStatsRuntime.speed;
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

		rigid.linearVelocity = new Vector2(inputVec.x * speed, rigid.linearVelocityY);
		UpdateSprite();		// 좌우 플립
	}

	private void Update()
	{
		if(inputVec.x == 0)		// 좌우 이동 입력이 없을 경우
			rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
		else	// 좌우 이동이 있을 경우
			rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
	}

	// 플레이어 스프라이트 업데이트
	private void UpdateSprite()
	{
		// 좌우 플립
		if (inputVec.x > 0)
			sprite.flipX = false;
		else if (inputVec.x < 0)
			sprite.flipX = true;
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
		isGroundedSpecial = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, groundSpecialMask);
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

	private void OnCrouch(InputValue val)   // 구르기/대쉬/내려가기 (S)
	{
		Debug.Log("isGrounded: " + isGrounded + ", isGroundedS: " + isGroundedSpecial);
		if (isDash) return;     // 대쉬 사용 중일 경우 리턴
		if (isGroundedSpecial)	// 아래로 내려갈 수 있는 플랫폼에 있을 경우
		{
			// 플레이어 위치가 살짝 내려가게
			transform.position += Vector3.down * 0.1f;
		}
		else if(isGrounded)		// 땅에 있을 경우 대쉬
		{
			StartCoroutine(PlayerDash());
		}
	}

	private void OnAttack(InputValue val)	// 공격 (LClick)
	{
		if (isAttack) return;
		
		// 플레이어 공격에 닿은 적 및 부서지는 오브젝트 처리
		Collider2D[] colls = Physics2D.OverlapBoxAll(attackPos.position, attackBoxSize, 0);
		foreach(var item in colls)
		{
			if (item.CompareTag(TagName.enemy)) // 적
				item.GetComponent<Enemy>().TakeDamage(GameManager.Instance.playerStatsRuntime.attack);
			else if (item.CompareTag(TagName.crackObj))     // 부서지는 오브젝트
				item.GetComponent<ObjectController>().count -= GameManager.Instance.playerStatsRuntime.attack;
			Debug.Log(item.tag);				
		}
		StartCoroutine(PlayerAttack());
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(attackPos.position, attackBoxSize);
	}

	/// <summary>
	/// Coroutine
	/// </summary>
	IEnumerator PlayerDash()    // 플레이어 대쉬
	{
		speed = GameManager.Instance.playerStatsRuntime.speed;
		isDash = true;

		yield return new WaitForSeconds(GameManager.Instance.playerStatsRuntime.dashDuration);

		dashTime -= Time.deltaTime;
		speed = GameManager.Instance.playerStatsRuntime.dashSpeed;

		isDash = false;
	}

	IEnumerator PlayerAttack()	// 플레이어 공격
	{
		// 공격
		animator.SetTrigger("Attack");
		isAttack = true;

		// 쿨타임 대기
		yield return new WaitForSeconds(GameManager.Instance.playerStatsRuntime.attackCoolTime);
		
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

	public void Die()   // 죽음 처리
	{

	}
}
