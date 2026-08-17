using Globals;
using UnityEngine;

/// <summary>
/// 플레이어의 움직임 상태(이동, 점프, 벽 슬라이드 등)에 맞춰 애니메이션 전환을 제어하는 클래스입니다.
/// 매 프레임 스트링 검색 대신 Hash값을 사용해 성능 최적화를 제공합니다.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
	// 플레이어의 애니메이션 상태 정의
	private enum AnimationState
	{
		Idle,       // 대기 상태
		Run,        // 달리기 상태
		Jump,       // 공중 점프/낙하 상태
		Wall,       // 벽 슬라이드/벽 점프 상태
		Down,       // 웅크리기 상태
		Roll,       // 구르기
		Attack,     // 공격
		Skill,      // 스킬 공격
		Die         // 사망
	}

	// 캐싱할 컴포넌트 레퍼런스
	private Animator anim;
	private PlayerMovement movement;
	private PlayerGroundChecker groundChk;
	private PlayerAttack attack;
	private PlayerSkillAttack skillAttack;
	private PlayerHealth health;

	// 현재 재생 중인 상태 저장 (중복 상태 전환 방지 목적)
	private AnimationState currentAnimState = AnimationState.Idle;

	// 🌟 애니메이션 매개변수/클립 이름을 해시값으로 캐싱하여 문자열 비교 오버헤드를 최적화합니다.
	private static readonly int HashIdle = Animator.StringToHash(PlayerAnimName.idle);
	private static readonly int HashRun = Animator.StringToHash(PlayerAnimName.run);
	private static readonly int HashJump = Animator.StringToHash(PlayerAnimName.jump);
	private static readonly int HashWall = Animator.StringToHash(PlayerAnimName.climb);
	private static readonly int HashDown = Animator.StringToHash(PlayerAnimName.down);
	private static readonly int HashRoll = Animator.StringToHash(PlayerAnimName.roll);
	private static readonly int HashAttack = Animator.StringToHash(PlayerAnimName.attack);
	private static readonly int HashSkill = Animator.StringToHash(PlayerAnimName.skill);
	private static readonly int HashDie = Animator.StringToHash(PlayerAnimName.die);

	private void Awake()
	{
		anim = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
		groundChk = GetComponent<PlayerGroundChecker>();
		attack = GetComponent<PlayerAttack>();
		skillAttack = GetComponent<PlayerSkillAttack>();
		health = GetComponent<PlayerHealth>();
	}

	private void Start()
	{
		// 시작 시 기본 대기 애니메이션을 호출합니다.
		PlayAnimation(AnimationState.Idle);
	}

	private void Update()
	{
		// 실시간 상태 분석 후 최적화된 애니메이션을 갱신합니다.
		UpdateAnimationState();
	}

	/// <summary>
	/// 플레이어 이동 및 접지 상태 컴포넌트로부터 현재 캐릭터의 실시간 물리/논리 상태를 파악하여 애니메이션 상태를 도출합니다.
	/// 우선순위: 사망 -> 벽 점프 -> 공격, 스킬 공격 -> 구르기 -> 웅크리기 -> 달리기 -> 기본
	/// </summary>
	private void UpdateAnimationState()
	{
		// 1. 사망
		if (health != null && health.isDead)
		{
			PlayAnimation(AnimationState.Die);
			return;
		}

		// 2. 벽 점프 / 벽 슬라이딩
		if (movement != null && (movement.IsWallSliding || movement.IsWallJumping))
		{
			PlayAnimation(AnimationState.Wall);
			return;
		}

		// 3. 공격 / 스킬 공격
		if (attack != null && attack.IsAttacking)
		{
			PlayAnimation(AnimationState.Attack);
			return;
		}
		if (skillAttack != null && skillAttack.IsSkillAttacking)
		{
			PlayAnimation(AnimationState.Skill);
			return;
		}

		// 4. 구르기 (구르는 도중 모든 키를 떼도 구르는 시간이 지속되고 있을 때)
		if (movement != null && movement.isDash)
		{
			PlayAnimation(AnimationState.Roll);
			return;
		}

		// 5. 웅크리기 (웅크리기를 유지하고 있을 때)
		if (movement != null && movement.isCrouchPressed && groundChk != null && groundChk.isGrounded)
		{
			PlayAnimation(AnimationState.Down);
			return;
		}

		// 6. 공중 점프/낙하
		if (groundChk != null && !groundChk.isGrounded)
		{
			PlayAnimation(AnimationState.Jump);
			return;
		}

		// 7. 달리기
		if (movement != null && Mathf.Abs(movement.inputVec.x) > 0.1f)
		{
			PlayAnimation(AnimationState.Run);
			return;
		}

		// 8. 기본 (Idle)
		PlayAnimation(AnimationState.Idle);
	}

	/// <summary>
	/// 실제 애니메이터 컨트롤러에 애니메이션 클립을 실행하도록 명령하는 핵심 메서드입니다.
	/// 이전 상태와 신규 상태가 같으면 작동하지 않도록 방어 설계되었습니다.
	/// </summary>
	/// <param name="newState">전환하려는 목표 상태</param>
	private void PlayAnimation(AnimationState newState)
	{
		if (currentAnimState == newState) return; // 동일 상태면 불필요하게 클립 재설정 차단

		currentAnimState = newState;

		// 해싱된 고유 정수 ID를 사용하여 즉시 플레이(Play)를 구동합니다.
		switch (currentAnimState)
		{
		case AnimationState.Idle:
			anim.Play(HashIdle);
			break;
		case AnimationState.Run:
			anim.Play(HashRun);
			break;
		case AnimationState.Jump:
			anim.Play(HashJump);
			break;
		case AnimationState.Wall:
			anim.Play(HashWall);
			break;
		case AnimationState.Down:
			anim.Play(HashDown);
			break;
		case AnimationState.Roll:
			anim.Play(HashRoll);
			break;
		case AnimationState.Attack:
			anim.Play(HashAttack);
			break;
		case AnimationState.Skill:
			anim.Play(HashSkill);
			break;
		case AnimationState.Die:
			anim.Play(HashDie);
			break;
		}
	}
}