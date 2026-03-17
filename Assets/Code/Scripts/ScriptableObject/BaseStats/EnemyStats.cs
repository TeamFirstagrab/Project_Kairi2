using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
	[Header("적 기본 스탯")]
	[Header("공격력")]
	public float attack;
	[Header("체력")]
	public float HP;
}
