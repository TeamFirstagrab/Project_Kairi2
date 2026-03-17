using UnityEngine;

[System.Serializable]
public class EnemyStatsRuntime
{
	[Header("적 기본 스탯")]
	[Header("공격력")]
	public float attack;
	[Header("체력")]
	public float maxHP;
	public float currentHP;

	// 생성자
	public EnemyStatsRuntime(EnemyStats baseStats)
	{
		attack = baseStats.attack;
		maxHP = baseStats.HP;
		currentHP = maxHP;
	}
}
