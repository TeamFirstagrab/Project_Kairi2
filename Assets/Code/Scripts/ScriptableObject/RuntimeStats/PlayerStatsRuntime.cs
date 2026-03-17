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
	public float attack;
	[Header("체력")]
	public float maxHP;
	public float currentHP;

	// 생성자
	public PlayerStatsRuntime(PlayerStats baseStats)
	{
		speed = baseStats.speed;
		jumpForce = baseStats.jumpForce;
		attack = baseStats.attack;
		maxHP = baseStats.maxHP;
		currentHP = maxHP;
	}
}
