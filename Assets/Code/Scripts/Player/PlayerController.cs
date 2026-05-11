// PlayerController.cs
using Globals;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	private Rigidbody2D rigid;
	private Animator animator;
	private PlayerMovement movement;
	private PlayerDash dash;
	private PlayerAttack attack;
	private PlayerGroundChecker groundChecker;
	private PlayerSlowMode slowMode;
	private PlayerClimb climb;
	private float originalGravity;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
		dash = GetComponent<PlayerDash>();
		attack = GetComponent<PlayerAttack>();
		slowMode = GetComponent<PlayerSlowMode>();
		climb = GetComponent<PlayerClimb>();
		groundChecker = GetComponent<PlayerGroundChecker>();
	}

	private void Start()
	{
		originalGravity = rigid.gravityScale;
		movement.Init();
	}

	private void FixedUpdate()
	{
		if (!dash.isDashing)
			movement.HandleMovement();

		movement.UpdateSprite();
	}

	private void Update()
	{
		rigid.gravityScale = originalGravity;
	}

	private void OnMove(InputValue val)
	{
		if (climb.isWallJump) return;	// 벽에서 점프 중일 경우 이동 X
		if (dash.isDashing)
		{
			movement.inputVec = Vector2.zero;
			return;
		}

		animator.Play(PlayerAnimName.run);
		movement.inputVec = val.Get<Vector2>();
		dash.TryDash();
	}

	private void OnReleaseMove(InputValue val)
	{
		if (dash.isDashing) return;
		animator.Play(PlayerAnimName.idle);
	}

	private void OnJump(InputValue val)
	{
		if (!groundChecker.isGrounded) return;
		if (climb.isWall)
		{
			climb.WallJump();
			return;
		}
		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		groundChecker.isGrounded = false;
	}

	private void OnCrouch()
	{
		if (!groundChecker.isGrounded) return;
		dash.isDashReady = true;
		animator.Play(PlayerAnimName.landDown);
		if (groundChecker.isGroundedSpecial)
			transform.position += Vector3.down * 0.1f;
		else
			dash.TryDash();
	}

	private void OnReleaseCrouch()
	{
		if (dash.isDashing) return;
		dash.isDashReady = false;
		animator.Play(PlayerAnimName.landUp);
	}

	private void OnAttack(InputValue val)
	{
		Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		attack.TryAttack(mouseWorld);
	}

	private void OnSlow(InputValue val)
	{
		// 플레이어 사망 시 슬로우 X
		if (GameManager.Instance.playerStatsRuntime.currentHP <= 0)
			return;

		if (val.isPressed)
			slowMode.EnterSlow();
		else
			slowMode.ExitSlow();
	}

	private void OnCollisionEnter2D(Collision2D col)
	{
		groundChecker.Check();      // 땅 체크

		// 문 열기
		if (col.transform.CompareTag(TagName.door))
		{
			if (col.transform.TryGetComponent(out IInteractionObject door))
			{
				// 플레이어가 문에 붙어서 움직일 때 문 열기
				if (movement.inputVec.x != 0)
				{
					door.OnInteract();
				}
			}
		}
	}
	private void OnCollisionStay2D(Collision2D col)
	{
		groundChecker.Check();      // 땅 체크
	}

	private void OnCollisionExit2D(Collision2D col)
	{
		if (col.transform.CompareTag(TagName.ground))
			groundChecker.isGrounded = false;
	}

	private void OnTriggerEnter2D(Collider2D col)
	{
		if (attack.CanDeflect() && col.CompareTag(TagName.bullet))
			col.GetComponent<EnemyBullet>().DeflectBullet();
	}
}