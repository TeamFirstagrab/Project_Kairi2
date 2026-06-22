// PlayerController.cs
using Globals;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	private Rigidbody2D rigid;
	private PlayerMovement movement;
	private PlayerDash dash;
	private PlayerAttack attack;
	private PlayerGroundChecker groundChecker;
	private PlayerSlowMode slowMode;
	private PlayerClimb climb;
	private PlayerSkillAttack skillAttack;
	private float originalGravity;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		movement = GetComponent<PlayerMovement>();
		dash = GetComponent<PlayerDash>();
		attack = GetComponent<PlayerAttack>();
		slowMode = GetComponent<PlayerSlowMode>();
		climb = GetComponent<PlayerClimb>();
		groundChecker = GetComponent<PlayerGroundChecker>();
		skillAttack = GetComponent<PlayerSkillAttack>();
    }

	private void Start()
	{
		originalGravity = rigid.gravityScale;
		//movement.Init();
	}

	private void OnMove(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;
		//if (climb.isWallJump) return;	// 벽에서 점프 중일 경우 이동 X
		//if (dash.isDashing)
		//{
		//	movement.inputVec = Vector2.zero;
		//	return;
		//}

		//animator.Play(PlayerAnimName.run);
		//movement.inputVec = val.Get<Vector2>();
		//dash.TryDash();

		Vector2 inputVec = val.Get<Vector2>();

		bool hadNoHorizontal = Mathf.Abs(movement.inputVec.x) < 0.01f;
		bool hasHorizontal = Mathf.Abs(inputVec.x) > 0.01f;

		if (hadNoHorizontal && hasHorizontal && movement.isCrouchPressed)
		{
			movement.TriggerRollInput(); // 구르기 준비
		}

		// Movement에 전달해서 플레이어가 수평 이동하도록 전달
		movement.inputVec = inputVec;
	}

	//private void OnReleaseMove(InputValue val)
	//{
	//	if (IsNullScript(movement)) return;

	//	if (dash.isDashing) return;
	//	animator.Play(PlayerAnimName.idle);
	//}

	private void OnJump(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;

		movement.SetJumpInput(val.isPressed);
	}

	private void OnCrouch(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;

		if(val.isPressed)
		{
			//if (!groundChecker.IsGrounded) return;
			//dash.isDashReady = true;
			//animator.Play(PlayerAnimName.landDown);
			//if (groundChecker.IsGroundedOneway)
			//	transform.position += Vector3.down * 0.1f;
			//else
			//	dash.TryDash();
			movement.SetCrouchInput(val.isPressed);
		}
		else
		{
			//if (dash.isDashing) return;
			//dash.isDashReady = false;
			//animator.Play(PlayerAnimName.landUp);

			movement.SetCrouchInput(false);
		}
	}

	private void OnAttack(InputValue val)
	{
		if (GlobalUtil.IsNullScript(attack)) return;
		if(val.isPressed)
		{
			rigid.gravityScale = 1f;	// 중력값 조절
			attack.TryAttack();
			movement.SetJumpInput(val.isPressed);
			rigid.gravityScale = originalGravity;	// 복구
		}
	}

	private void OnSkillAttack(InputValue val)
	{
		if(val.isPressed)
		{
			skillAttack.EnterSkill();
		}
		else
		{
			skillAttack.ExitSkill();
		}
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
		groundChecker.CheckGround();      // 땅 체크

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
		groundChecker.CheckGround();      // 땅 체크
	}

	private void OnCollisionExit2D(Collision2D col)
	{
		if (col.transform.CompareTag(TagName.ground))
			groundChecker.isGrounded = false;
	}

	//private void OnTriggerEnter2D(Collider2D col)
	//{
	//	if (col.CompareTag(TagName.bullet))
	//		col.GetComponent<EnemyBullet>().DeflectBullet();
	//}
}