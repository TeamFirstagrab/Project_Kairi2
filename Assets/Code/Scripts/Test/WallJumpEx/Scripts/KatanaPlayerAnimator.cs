using UnityEngine;

/// <summary>
/// 플레이어의 움직임 상태(이동, 점프, 벽 슬라이드 등)에 맞춰 애니메이션 전환을 제어하는 클래스입니다.
/// 매 프레임 스트링 검색 대신 Hash값을 사용해 성능 최적화를 제공합니다.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(KatanaPlayerMovement))]
[RequireComponent(typeof(KatanaPlayerGroundDetector))]
public class KatanaPlayerAnimator : MonoBehaviour
{
    // 플레이어의 애니메이션 상태 정의
    private enum AnimationState
    {
        Idle, // 대기 상태
        Run,  // 달리기 상태
        Jump, // 공중 점프/낙하 상태
        Wall  // 벽 슬라이드 상태
    }

    // 캐싱할 컴포넌트 레퍼런스
    private Animator anim;
    private KatanaPlayerMovement movement;
    private KatanaPlayerGroundDetector groundDetector;

    // 현재 재생 중인 상태 저장 (중복 상태 전환 방지 목적)
    private AnimationState currentAnimState = AnimationState.Idle;

    // 🌟 애니메이션 매개변수/클립 이름을 해시값으로 캐싱하여 문자열 비교 오버헤드를 최적화합니다.
    private static readonly int HashIdle = Animator.StringToHash("Player_Idle");
    private static readonly int HashRun = Animator.StringToHash("Player_Run");
    private static readonly int HashJump = Animator.StringToHash("Player_Jump");
    private static readonly int HashWall = Animator.StringToHash("Player_Wall");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<KatanaPlayerMovement>();
        groundDetector = GetComponent<KatanaPlayerGroundDetector>();
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
    /// </summary>
    private void UpdateAnimationState()
    {
        // 1순위: 벽에 매달려 미끄러지고 있는 경우
        if (movement.IsWallSliding)
        {
            PlayAnimation(AnimationState.Wall);
        }
        // 2순위: 지상 접지 상태가 아닌 경우 (점프 또는 추락)
        else if (!groundDetector.IsGrounded)
        {
            PlayAnimation(AnimationState.Jump);
        }
        // 3순위: 지상인 경우 속도에 맞춰 Run 또는 Idle 결정
        else
        {
            if (Mathf.Abs(movement.CurrentVelocity.x) > 0.1f)
            {
                PlayAnimation(AnimationState.Run);
            }
            else
            {
                PlayAnimation(AnimationState.Idle);
            }
        }
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

        // 필요 시 디버깅을 위해 애니메이션 전환 시점에 콘솔 출력을 켤 수 있습니다.
        // Debug.Log($"[PlayerAnimator] 애니메이션 전환됨: {currentAnimState}");

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
        }
    }
}