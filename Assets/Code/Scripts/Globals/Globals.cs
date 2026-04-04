using UnityEngine;

/// <summary>
/// 글로벌 변수를 관리하는 파일
/// **반드시 불변하는 값(읽기전용)만 지정할 것!!!**
/// </summary>
namespace Globals
{
	// 애니메이션 이름(string) 관련 클래스
	public static class AnimationVarName
	{
		public static readonly string playerState = "playerState";  // 플레이어 상태
		public static readonly string enemyState = "enemyState";  // 플레이어 상태
	}

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
}