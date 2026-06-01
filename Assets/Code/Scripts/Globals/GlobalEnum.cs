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

	enum PlayerDashType		// 대쉬
	{ 
		NONE = 0, 
		NORMAL, 
		READY, 
		DOWN
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



	///  - 이 이름표는 '열거형(Enum)'이라고 부릅니다.
	///  - C# 스크립트 코드 내부에서 적이 "지금 멈춰서 쉴까? 순찰을 돌까? 쫓아갈까?"를 스스로 생각하고 비교할 때 사용해요!
	///  - 겉보기에는 IDLE, PATROL 같은 영어 단어로 보이지만, 컴퓨터 내부적으로는 숫자인 0, 1, 2, 3으로 변환되어 처리됩니다.
	///  - 컴퓨터는 글자보다 숫자를 수백 배 더 빠르게 비교할 수 있기 때문에, 이 Enum을 사용하면 게임에 렉이 걸리지 않고 엄청나게 가볍게 작동합니다!
	public enum KimEnemyState
	{
		// 💡 IDLE은 대기 상태를 뜻하며, 내부적으로 숫자 0으로 취급됩니다.
		IDLE = 0,
		// 💡 PATROL은 경계 순찰 상태를 뜻하며, 내부적으로 숫자 1로 취급됩니다.
		PATROL,
		// 💡 CHASE는 눈에 불을 켜고 플레이어를 쫓는 추격 상태를 뜻하며, 내부적으로 숫자 2로 취급됩니다.
		CHASE,
		// 💡 ATTACK은 플레이어를 타격하는 공격 상태를 뜻하며, 내부적으로 숫자 3로 취급됩니다.
		ATTACK,
		// 💀 적이 대미지를 다 입고 쓰러지는 죽음 상태! (내부적으로 숫자 4)
		DEAD,
		// 💡 MAX는 상태의 총 개수(여기서는 5개)를 똑똑하게 파악하기 위해 적어두는 마지막 한계선 표시판입니다.
		MAX
	}






}