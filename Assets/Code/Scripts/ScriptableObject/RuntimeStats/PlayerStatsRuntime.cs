using UnityEngine;

[System.Serializable]
public class PlayerStatsRuntime
{
	[Header("플레이어 기본 스텟")]
	[Header("이동속도")]
	public float speed;
	[Header("점프 높이")]
	public float jumpForce;
	[Header("공격 대쉬 사거리")]
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
	[Header("벽 체크 거리")]
	public float wallChkDist;
	[Header("벽에서 내려가는 속도")]
	public float climbSlidingSpeed;
	[Header("벽 점프 속도")]
	public float wallJumpPower;

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
		wallChkDist = baseStats.wallChkDist;
		climbSlidingSpeed = baseStats.climbSlidingSpeed;
		wallJumpPower = baseStats.wallJumpPower;
	}
}
