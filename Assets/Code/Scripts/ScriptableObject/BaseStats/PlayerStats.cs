using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
	[Header("플레이어 기본 스탯")]
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
	[Header("대쉬 속도")]
	public float dashSpeed;
	[Header("대쉬 시간")]
	public float dashDuration;
	[Header("무적 시간")]
	public float invincibilityDuration;
}
