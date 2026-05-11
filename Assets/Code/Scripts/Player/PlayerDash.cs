using Globals;
using System.Collections;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
	public bool isDashing = false;
	public bool isDashReady = false;

	private Rigidbody2D rigid;
	private Animator animator;
	private PlayerMovement movement;
	private float originalGravity;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
	}

	public void TryDash()
	{
		if (!isDashReady || isDashing || movement.inputVec == Vector2.zero)
			return;

		StartCoroutine(DashRoutine());
		isDashReady = false;
	}

	private IEnumerator DashRoutine()
	{
		isDashing = true;

		originalGravity = rigid.gravityScale;
		rigid.gravityScale = 0f;

		animator.Play(PlayerAnimName.roll);

		var stats = GameManager.Instance.playerStatsRuntime;
		Vector2 dashDir = movement.inputVec.normalized;		// 대쉬 방향
		float time = 0f;

		while (time < stats.dashDuration)
		{
			rigid.linearVelocity = dashDir * stats.dashDist;

			time += Time.deltaTime;
			yield return null;
		}

		rigid.gravityScale = originalGravity;
		rigid.linearVelocity = Vector2.zero;

		isDashing = false;

		// S키 유지 시 다시 준비 상태
		if (Input.GetKey(KeyCode.S))
		{
			isDashReady = true;
			animator.Play(PlayerAnimName.landDown);
		}
		else
		{
			isDashReady = false;
			animator.Play(PlayerAnimName.idle);
		}
	}
}