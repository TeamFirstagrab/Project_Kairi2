using UnityEngine;

[System.Serializable]
public class PlayerStatsRuntime
{
	[Header("플레이어 기본 스텟")]
	[Header("이동속도")]
	public float speed;
	[Header("점프 높이")]
	public float jumpForce;
	[Header("공격 시 추가로 길어질 사거리")]
	public float attackDist;
	[Header("공격력")]
	public int attack;
	[Header("공격 쿨타임")]
	public float attackCoolTime;
	[Header("공격 속도")]
	public float attackSpeed;
	[Header("체력")]
	public float maxHP;
	public float currentHP;
	[Header("대쉬 사거리")]
	public float dashDist;
	[Header("대쉬 시간")]
	public float dashDuration;
	[Header("무적 시간")]
	public float invincibilityDuration;

	// 생성자
	public PlayerStatsRuntime(PlayerStats baseStats)
	{
		speed = baseStats.speed;
		jumpForce = baseStats.jumpForce;
		attack = baseStats.attack;
		attackDist = baseStats.attackDist;
		attackCoolTime = baseStats.attackCoolTime;
		attackSpeed = baseStats.attackSpeed;
		maxHP = baseStats.maxHP;
		currentHP = maxHP;
		dashDist = baseStats.dashDist;
		dashDuration = baseStats.dashDuration;
		invincibilityDuration = baseStats.invincibilityDuration;
	}
}
