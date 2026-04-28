using EnumType;
using UnityEngine;

public class EnemyAttack : IEnemyState
{
	public void EnterState(Enemy p_enemy)
	{
		Debug.Log("Enter Attack");
	}

	public void UpdateState(Enemy p_enemy)
	{
		Debug.Log("Attaking...");

		p_enemy.ChangStateDebug(EnemyState.PATROL);
	}

	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Attack");
	}
}
