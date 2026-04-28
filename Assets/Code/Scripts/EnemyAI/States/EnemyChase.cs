using EnumType;
using UnityEngine;

public class EnemyChase : IEnemyState
{
	public void EnterState(Enemy p_enemy)
	{
		Debug.Log("Enter Chase");
	}

	public void UpdateState(Enemy p_enemy)
	{
		Debug.Log("Chasing...");

		p_enemy.ChangStateDebug(EnemyState.ATTACK);
	}
	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Chase");
	}

}
