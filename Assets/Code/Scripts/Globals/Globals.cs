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
		public const string enemy = "Enemy";
		public const string bullet = "Bullet";
		// 오브젝트
		public const string obj = "NormalObject";
		public const string crackObj = "CrackObject";
		public const string expObj = "ExplosionObject";
		public const string door = "Door";
		// 플레이어 관련
		public const string player = "Player";
		public const string hook = "Hook";
		// 배경 요소
		public const string ground = "Ground";
		public const string wall = "Wall";
		public const string oneWayPlatform = "OneWayPlatform";
		public const string trigger = "Trigger";
		// NPC
		public const string npc = "NPC";
		// 카메라
		public const string cameraBound = "CameraBound";
	}

	public static class LayerName
	{
		public const string ground = "Ground";
		public const string oneWayPlatform = "OneWayPlatform";
		public const string crackObj = "CrackObject";
		public const string player = "Player";
		public const string enemy = "Enemy";
		public const string wall = "Wall";
	}

	// 애니메이션 이름 관련 클래스
	public static class EnemyAnimName	// 적
	{
		public const string idle = "Enemy_Idle";
		public const string patrol = "Enemy_Walk";
		public const string attack = "Enemy_Attack";
		public const string chase = "Enemy_Run";
		public const string dead = "Enemy_Die";
	}
	public static class PlayerAnimName   // 플레이어
	{
		public const string idle		= "Player_Idle";
		public const string run			= "Player_Run";
		public const string jump		= "Player_Jump";
		public const string attack		= "Player_Attack";
		public const string down		= "Player_Down";
		public const string landing		= "Player_Landing";
		public const string landUp		= "Player_LandUp";
		public const string slide		= "Player_Slide";
		public const string roll		= "Player_Roll";
		public const string redgeClimb	= "Player_RedgeClimb";
		public const string climb		= "Player_Climb";
		public const string climbSlide	= "Player_ClimbSlide";
		public const string die			= "Player_Die";
		public const string skill		= "Player_SkillAttack";
	}

	// 프리펩 이름 관련 클래스
	public static class PrefabName
	{
		public const string bullet = "Bullet";
	}
}