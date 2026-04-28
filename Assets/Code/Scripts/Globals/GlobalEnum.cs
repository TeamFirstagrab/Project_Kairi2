using UnityEngine;

namespace EnumType
{
	// 플레이어 상태
	public enum PlayerState
	{
		Idle = 0,       // 기본
		Run,            // 달리기
		Jump,           // 점프
		Dash,			// 대쉬
		Land,           // 착지
		Attack,			// 공격
		Damaged,        // 데미지 받은 상태
	}

	public enum EnemyState
	{
		IDLE = 0,
		CHASE,
		ATTACK,
		PATROL,

		MAX,
	}

	// 오브젝트 타입
	public enum ObjectType
	{

	}
}