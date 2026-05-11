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
		public static readonly string bullet = "Bullet";
		// 오브젝트
		public static readonly string obj = "NormalObject";
		public static readonly string crackObj = "CrackObject";
		public static readonly string expObj = "ExplosionObject";
		public static readonly string door = "Door";
		// 플레이어 관련
		public static readonly string player = "Player";
		public static readonly string hook = "Hook";
		// 배경 요소
		public static readonly string ground = "Ground";
		public static readonly string wall = "Wall";
		public static readonly string oneWayPlatform = "OneWayPlatform";
		public static readonly string trigger = "Trigger";
		// NPC
		public static readonly string npc = "NPC";
		// 카메라
		public static readonly string cameraBound = "CameraBound";
	}

	public static class LayerName
	{
		public static readonly string ground = "Ground";
		public static readonly string player = "Player";
		public static readonly string enemy = "Enemy";
	}

	// 애니메이션 이름 관련 클래스
	public static class EnemyAnimName	// 적
	{
		public static readonly string idle = "Enemy_Idle";
		public static readonly string chase = "Enemy_Run";
		public static readonly string attack = "Enemy_Shot1";
		public static readonly string patrol = "Enemy_Walk";
		public static readonly string recharge = "Enemy_Recharge";
	}
	public static class PlayerAnimName   // 플레이어
	{
		public static readonly string idle = "Idle";
		public static readonly string run = "Run";
		public static readonly string attack = "Attack";
		public static readonly string landDown = "LandDown";
		public static readonly string landing = "Landing";
		public static readonly string landUp = "LandUp";
		public static readonly string slide = "Slide";
		public static readonly string roll = "Roll";
		public static readonly string redgeClimb = "RedgeClimb";
		public static readonly string climb = "Climb";
		public static readonly string climbSlide = "ClimbSlide";
	}

	// 프리펩 이름 관련 클래스
	public static class PrefabName
	{
		public static readonly string bullet = "Bullet";
	}
}