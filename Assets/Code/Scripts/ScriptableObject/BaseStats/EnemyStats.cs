using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
	[Header("적 정보")]
	[Header("이름")]
	public string EnemyName;
	[Header("설명")]
	[TextArea]
	public string EnemyDescription;

	[Header("적 기본 스탯")]
	[Header("정찰 속도")]
	public float PatrolSpeed;
	[Header("플레이어 추격 속도")]
	public float ChaseSpeed;
	[Header("공격력")]
	public float Attack;
	[Header("체력")]
	public float HP;
}
