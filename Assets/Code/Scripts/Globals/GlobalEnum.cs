using UnityEngine;

namespace EnumType
{
	// 플레이어 상태
	public enum PlayerState
	{
		Idle = 0,       // 기본
		Run,            // 달리기
		Jump,           // 점프
		Land,           // 착지
		Damaged,        // 데미지 받은 상태
	}
}