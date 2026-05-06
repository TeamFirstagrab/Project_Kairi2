using EnumType;
using Globals;
using System.Collections;
using UnityEngine;

public class EnemyLongRangeAttack : IEnemyState
{
	private float shootTime = 1.0f;
	private float shootTimer = 0f;
	public void EnterState(Enemy p_enemy)
	{
		Debug.Log("Enter Attack");

		p_enemy.GetComponent<Animator>().Play(EnemyAnimName.attack);	// 애니메이션 플레이
	}

	public void UpdateState(Enemy p_enemy)
	{
		// 공격
		shootTimer += Time.deltaTime;

		if (shootTimer >= shootTime)
		{
			GameManager.Instance.poolManager.SpawnFromPool(
				PrefabName.bullet,
				p_enemy.transform.position,
				p_enemy.transform.rotation
			);
			shootTimer = 0f;
		}

		AnimatorStateInfo stateInfo = p_enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);  // 애니메이션

		// 공격 끝났을 경우 (애니메이션 끝)
		if (!stateInfo.IsName(EnemyAnimName.attack))
		{
			p_enemy.GetComponent<Animator>().Play(EnemyAnimName.recharge);	// 재장전 애니메이션

			// 플레이어가 시야 내에 있을 경우 추적
			if (p_enemy.GetComponent<EnemySight>().IsPlayerInRange())
			{
				p_enemy.ChangeState(EnemyState.CHASE);
				Debug.Log($"[EnemyAttack] ChangeState: CHASE");
			}
			else    // 아닐 경우 기본 상태로 변경
			{
				p_enemy.ChangeState(EnemyState.IDLE);
				Debug.Log($"[EnemyAttack] ChangeState: IDLE");
			}

			return;
		}
	}

	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Attack");
	}
}
