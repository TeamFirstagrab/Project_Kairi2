using UnityEngine;

public class EnemyDataManager : MonoBehaviour
{
	public EnemyStats _enemyStats;
    private void Start()
    {
		GetEnemyStats();
	}

	private void GetEnemyStats()
	{
		print($"Name: {_enemyStats.EnemyName}\n" +
			$"HP: {_enemyStats.HP}\n" +
			$"Attack: {_enemyStats.Attack}\n" +
			$"MoveSpeed: {_enemyStats.PatrolSpeed}\n" +
			$"Description: {_enemyStats.EnemyDescription}");
	}
}
