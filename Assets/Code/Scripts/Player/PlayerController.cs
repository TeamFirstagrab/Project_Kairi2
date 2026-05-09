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
	private float originalGravity;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
		dash = GetComponent<PlayerDash>();
		attack = GetComponent<PlayerAttack>();
		slowMode = GetComponent<PlayerSlowMode>();
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
		//rigid.constraints = movement.InputVec.x == 0
		//	? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
		//	: RigidbodyConstraints2D.FreezeRotation;
		rigid.gravityScale = originalGravity;
	}

	// Input System 콜백
	private void OnMove(InputValue val)
	{
		if (dash.isDashing)
		{
			movement.inputVec = Vector2.zero;
			return;
		}
		movement.inputVec = val.Get<Vector2>();
		animator.Play(PlayerAnimName.run);
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
		rigid.AddForce(Vector2.up * GameManager.Instance.playerStatsRuntime.jumpForce, ForceMode2D.Impulse);
		groundChecker.SetGrounded(false);
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
		if(val.isPressed)
			slowMode.EnterSlow();
		else
			slowMode.ExitSlow();
	}

	private void OnCollisionEnter2D(Collision2D col) => groundChecker.Check();
	private void OnCollisionStay2D(Collision2D col) => groundChecker.Check();
	private void OnCollisionExit2D(Collision2D col)
	{
		if (col.transform.CompareTag(TagName.ground))
			groundChecker.SetGrounded(false);
	}

	private void OnTriggerEnter2D(Collider2D col)
	{
		if (attack.CanDeflect() && col.CompareTag(TagName.bullet))
			col.GetComponent<EnemyBullet>().DeflectBullet();
	}
}