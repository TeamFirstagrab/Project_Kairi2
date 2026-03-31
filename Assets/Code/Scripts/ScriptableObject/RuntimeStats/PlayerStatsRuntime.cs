using UnityEngine;

[System.Serializable]
public class PlayerStatsRuntime
{
	[Header("플레이어 기본 스텟")]
	[Header("이동속도")]
	public float speed;
	[Header("점프 높이")]
	public float jumpForce;
	[Header("공격력")]
	public int attack;
	[Header("공격 쿨타임")]
	public float attackCoolTime;
	[Header("체력")]
	public float maxHP;
	public float currentHP;
	[Header("대쉬 속도")]
	public float dashSpeed;
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
		attackCoolTime = baseStats.attackCoolTime;
		maxHP = baseStats.maxHP;
		currentHP = maxHP;
		dashSpeed = baseStats.dashSpeed;
		dashDuration = baseStats.dashDuration;
		invincibilityDuration = baseStats.invincibilityDuration;
	}
}
