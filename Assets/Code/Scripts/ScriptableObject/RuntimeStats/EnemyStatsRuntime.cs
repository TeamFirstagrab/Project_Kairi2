using UnityEngine;

[System.Serializable]
public class EnemyStatsRuntime
{
	[Header("적 기본 스탯")]
	[Header("정찰 속도")]
	public float PatrolSpeed;
	[Header("플레이어 추격 속도")]
	public float ChaseSpeed;
	[Header("공격력")]
	public float Attack;
	[Header("체력")]
	public float MaxHP;
	public float CurrentHP;

	// 생성자
	public EnemyStatsRuntime(EnemyStats baseStats)
	{
		PatrolSpeed = baseStats.PatrolSpeed;
		PatrolSpeed = baseStats.ChaseSpeed;
		Attack = baseStats.Attack;
		MaxHP = baseStats.HP;
		CurrentHP = MaxHP;
	}
}
