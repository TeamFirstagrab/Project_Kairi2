using EnumType;
using Globals;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
	// 플레이어 정보
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private bool isGrounded;	// 땅 여부
	private bool isDash;        // 대쉬 사용 여부
	private PlayerState state;  // 플레이어 상태

	// 이동
	private Vector2 inputVec;   // 입력된 플레이어 이동값 (-1, 0, 1)
	private float speed;        // 플레이어 이동 속도
	private float dashTime;		// 대쉬 지속 시간

	// 땅 체크
	[SerializeField] private Transform groundCheckObj;      // 땅 체크 오브젝트 (프리펩)
	public float checkRadius = 0.1f;    // 땅 체크 반지름
	LayerMask groundMask;

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
	}

	private void Start()
	{
		isGrounded = true;
		isDash = false;
		state = PlayerState.Idle;
		speed = GameManager.Instance.playerStatsRuntime.speed;
	}

	/// <summary>
	/// Update
	/// </summary>
	private void FixedUpdate()
	{
		if(dashTime <= 0)
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
		isGrounded = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, groundMask);
	}

	/// <summary>
	/// Input System
	/// </summary>
	void OnMove(InputValue val)     // 좌우 이동 (AD)
	{
		if (isDash) return;     // 대쉬 사용 중일 경우 리턴
		inputVec = val.Get<Vector2>();
	}

	void OnJump(InputValue val)     // 점프 (W)
	{
		if (!isGrounded) return;    // 땅에 서있지 않을 경우 리턴

		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		isGrounded = false;
	}

	void OnCrouch(InputValue val)   // 구르기/대쉬/내려가기 (S)
	{
		if (isDash) return;     // 대쉬 사용 중일 경우 리턴
		isDash = true;
	}

	/// <summary>
	/// Coroutine
	/// </summary>
	//IEnumerator PlayerDash()    // 플레이어 대쉬
	//{
	//	float originalGravity = rigid.gravityScale;     // 원래 이동 속도 저장
	//	isDash = true;
	//	rigid.linearVelocity = new Vector2(inputVec.x * GameManager.Instance.playerStatsRuntime.dashSpeed, 0f);
	//	// 정해진 무적 시간만큼 기다리기
	//	yield return new WaitForSeconds(GameManager.Instance.playerStatsRuntime.invincibilityDuration);
	//	isDash = false;
	//}

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
