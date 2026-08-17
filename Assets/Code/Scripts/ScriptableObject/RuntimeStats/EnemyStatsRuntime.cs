using UnityEngine;

[System.Serializable]
public class EnemyStatsRuntime
{
	[Header("적 기본 스탯")]
	[Header("이동 속도")]
	public float MoveSpeed;
	[Header("정찰 속도")]
	public float PatrolSpeed;
	[Header("플레이어 추격 속도")]
	public float ChaseSpeed;
	[Header("근접 시야 범위")]
	public float SightRoundRange;
	[Header("시야 범위")]
	public float SIghtRange;
    [Header("발견 시 추격/공격 쿨타임")]
    public float FindCoolTime;
    [Header("공격력")]
	public int Attack;
	[Header("공격 범위")]
	public float AttackRange;
	[Header("체력")]
	public float MaxHP;
	public float CurrentHP;

	// 생성자
	public EnemyStatsRuntime(EnemyStats baseStats)
	{
		MoveSpeed = baseStats.MoveSpeed;
		PatrolSpeed = baseStats.PatrolSpeed;
		ChaseSpeed = baseStats.ChaseSpeed;
		SightRoundRange = baseStats.SightRoundRange;
		SIghtRange = baseStats.SightRange;
        FindCoolTime = baseStats.FindCoolTime;
        Attack = baseStats.Attack;
		AttackRange = baseStats.AttackRange;
		MaxHP = baseStats.HP;
		CurrentHP = MaxHP;
	}
}
