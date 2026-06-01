using UnityEngine;

[CreateAssetMenu(fileName = "KimEnemyStats", menuName = "Scriptable Objects/KimEnemyStats")]
public class KimEnemyStats : ScriptableObject
{
	[Header("적 정보")]
	[Header("이름")]
	public string EnemyName;
	[Header("설명")]
	[TextArea]
	public string EnemyDescription;

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
	public float SightRange;
	[Header("발견 시 추격/공격 쿨타임")]
	public float FindCoolTime;
	[Header("공격력")]
	public int Attack;
	[Header("공격 범위")]
	public float AttackRange;
	[Header("체력")]
	public float HP;
	[Header("부채꼴 시야 탐지 설정")]
	[Tooltip("적이 전방을 바라볼 때 플레이어를 감지할 수 있는 시야의 절반 각도 (예: 60도 설정 시 좌우 합쳐 총 120도 감지)")]
	public float SightAngle = 60f;
	[Tooltip("플레이어와 몬스터의 수직 높이 차이 한계 (미터, 이 수치보다 높이 차이가 크면 감지 안 함)")]
	public float SightHeightLimit = 1.5f;
}
