using Globals;
using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
	public bool IsAttacking { get; private set; }

	private Rigidbody2D rigid;
	private Animator animator;
	private PlayerMovement movement;
	private PlayerDash dash;
	private float originalGravity;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
		dash = GetComponent<PlayerDash>();
	}

	public void TryAttack(Vector2 target)
	{
		if (IsAttacking) return;
		StartCoroutine(AttackRoutine(target));
	}

	public bool CanDeflect() => IsAttacking;

	private IEnumerator AttackRoutine(Vector2 target)
	{
		IsAttacking = true;
		dash.enabled = false;   // 공격 중 대쉬 잠금

		animator.Play(PlayerAnimName.attack);
		originalGravity = rigid.gravityScale;
		rigid.gravityScale = 0f;

		var stats = GameManager.Instance.playerStatsRuntime;
		Vector2 startPos = transform.position;
		Vector2 dir = (target - startPos).normalized;
		movement.UpdateSprite(dir);

		Vector2 endPos = startPos;

		// CapsuleCast로 충돌 검사
		CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
		LayerMask mask = ~LayerMask.GetMask(LayerName.player);
		RaycastHit2D hit = Physics2D.CapsuleCast(startPos, col.size,
		CapsuleDirection2D.Vertical, 0f, dir, stats.attackDist + 0.5f, mask);

		if (hit)
		{
			if (hit.transform.CompareTag(TagName.ground))
				endPos = startPos + dir * (hit.distance - 0.1f);
			else if (hit.transform.CompareTag(TagName.crackObj)
				  || hit.transform.CompareTag(TagName.expObj)
				  || hit.transform.CompareTag(TagName.enemy))
			{
				GameManager.Instance.cameraShake.ShakeForSeconds();
				hit.transform.GetComponent<IDamageable>()?.TakeDamage(stats.attack);
			}
			else if (hit.transform.CompareTag(TagName.bullet))
			{
				hit.transform.GetComponent<EnemyBullet>()?.DeflectBullet();
			}
			else if (hit.transform.CompareTag(TagName.door))
			{
				hit.transform.GetComponent<IInteractionObject>().OnInteract();
			}
		}
		else
			endPos = startPos + dir * stats.attackDist;

			float time = 0f;
		while (time < stats.dashDuration)
		{
			transform.position = Vector2.MoveTowards(
				transform.position,
				endPos,
				stats.attackSpeed * Time.deltaTime);
			transform.position = Vector2.Lerp(startPos, endPos, time / stats.dashDuration);
			time += Time.deltaTime;
			yield return null;
		}

		transform.position = endPos;
		rigid.gravityScale = originalGravity;
		yield return new WaitForSeconds(stats.attackCoolTime);

		IsAttacking = false;
		dash.enabled = true;
	}
}