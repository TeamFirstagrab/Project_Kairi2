using UnityEngine;

/// <summary>
/// 글로벌 변수를 관리하는 파일
/// **반드시 불변하는 값(읽기전용)만 지정할 것!!!**
/// </summary>
namespace Globals
{
	// 태그 이름(string) 관련 클래스
	public static class TagName
	{
		// 적
		public static readonly string enemy = "Enemy";
		public static readonly string throwingEnemy = "ThrowingEnemy";
		public static readonly string bullet = "Bullet";
		// 오브젝트
		public static readonly string obj = "NormalObject";
		public static readonly string throwingObj = "ThrowingObject";
		public static readonly string crackObj = "CrackObject";
		public static readonly string expObj = "ExplosionObject";
		// 플레이어 관련
		public static readonly string player = "Player";
		public static readonly string hook = "Hook";
		// 배경 요소
		public static readonly string ground = "Ground";
		public static readonly string groundSpecial = "OneWayPlatform";
		public static readonly string trigger = "Trigger";
		// NPC
		public static readonly string npc = "NPC";
		// 카메라
		public static readonly string cameraBound = "CameraBound";
	}

	// 적 관련 데이터
	public static class EnemyData
	{
		public static readonly float findPlayerDist = 5f;	// 플레이어 인식 범위
		public static readonly float aggroOffDist = 10f;	// 플레이어 어그로 풀리는 범위
		public static readonly float attackDist = 1.5f;		// 플레이어 공격 범위
	}

	// 애니메이션 이름 관련 클래스
	public static class EnemyAnimName	// 적
	{
		public static readonly string Idle = "Enemy_Idle";
		public static readonly string Chase = "Enemy_Run";
		public static readonly string Attack = "Enemy_Shot1";
		public static readonly string Patrol = "Enemy_Walk";
	}
}