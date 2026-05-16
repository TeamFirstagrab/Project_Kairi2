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
    [Header("공격 시간")]
    public float attackDuration;
    [Header("공격 대쉬 사거리")]
    public float attackDist;
    [Header("공격 쿨타임")]
	public float attackCoolTime;
	[Header("공격 속도")]
	public float attackSpeed;
	[Header("체력")]
	public float maxHP;
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
	[Header("벽 점프 높이")]
	public float wallJumpPower;
}
