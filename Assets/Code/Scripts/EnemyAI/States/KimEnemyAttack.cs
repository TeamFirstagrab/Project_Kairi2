using UnityEngine;
using EnumType; // 1단계에서 작성한 KimEnemyState 열거형 이름표를 쓰기 위해 연결합니다.
using Globals;   // 1단계에서 작성한 KimEnemyAnimName 및 TagName 이름표들을 쓰기 위해 연결합니다.

/// <summary>
/// 김 전용 적 캐릭터의 공격(Attack) 상태 행동을 지휘하는 클래스입니다.
/// 
/// 작동 원리 설명:
///  - 2단계 인터페이스 약속(IKimEnemyState)에 맞춰 적 캐릭터가 제자리에 멈춰 서서 강력한 공격 모션을 연출합니다.
///  - 1단계 Globals에 등록해 둔 오타 없는 정답 공격 애니메이션 문자열("Enemy_Shot1")을 유니티 엔진에 주입하여 모션을 틀어줍니다.
///  - 공격 동작 시간(0.6초) 동안은 자리에 발이 완전히 달라붙도록 수평 이동 속도를 0으로 완전히 잠금합니다.
///  - 공격이 성공적으로 다 수행된 직후(0.6초 뒤), 만약 플레이어가 여전히 맵에 살아있다면 즉시 다시 맹렬한 추격(CHASE) 상태로 상황 판을 홱 변경합니다.
/// </summary>
public class KimEnemyAttack : IKimEnemyState
{
    private float attackDuration = 0.6f; // 한 번 칼을 크게 휘두르는 진짜 모션 동작 시간을 0.6초로 설정합니다.
    private float attackTimer = 0f;      // 실시간으로 휘두르며 경과한 공격 시간을 누적 측정하는 카운터 타이머입니다.

    // [상태 입장 규칙 구현]
    // 3단계 본체 사령탑이 나를 공격 상태(ATTACK)로 전이시키는 첫 순간에 딱 한 번 작동합니다.
    public void EnterState(KimEnemy enemy)
    {
        Debug.Log("Kim 적 캐릭터가 플레이어에게 근접 공격을 개시합니다!");

        // 1단계 Globals에 등록한 정답 공격 글자("Enemy_Shot1")를 꺼내 유니티 애니메이터 컴포넌트에게 공격 동작 재생을 지시합니다.
        enemy.anim.Play(KimEnemyAnimName.attack);

        // 공격하는 찰나 동안 뒤로 미끄러지지 않도록, 발바닥을 본드로 붙이듯이 수평 이동 속도를 즉각 0으로 강제 원복합니다.
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // 실시간 공격 경과 시간을 0초로 초기화합니다.
        attackTimer = 0f;
    }

    // [상태 유지 규칙 구현]
    // 공격 애니메이션을 수행하고 있는 0.6초의 시간 동안 매 프레임 실시간 무한 반복됩니다.
    public void UpdateState(KimEnemy enemy)
    {
        // 실시간 경과 시간 타이머를 현실 시간 속도(Time.deltaTime)만큼 덧셈해 나갑니다.
        attackTimer += Time.deltaTime;

        // 공격하는 동작을 수행하고 있는 도중에도 미끄러지지 않게 수평 물리 속도를 매 프레임 0으로 꽉 묶어줍니다.
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // [공격 완료 및 추격 재개 분기 조건식]
        // 우리가 약속한 한 번의 칼질 동작 시간(0.6초)이 마침내 전부 종료 완료되었다면!
        if (attackTimer >= attackDuration)
        {
            // 공격을 무사히 마쳤으므로, 다시 적 본체 사령탑(enemy)에게 "상대가 아직 살아있다면 쫓아갈 수 있게 즉시 추격(CHASE) 상태로 교환해라!" 하고 명령을 전송합니다.
            enemy.ChangeState(KimEnemyState.CHASE);
        }
    }

    // [상태 퇴장 규칙 구현]
    // 한 번의 칼질 공격이 끝나고 이 상태를 완전히 빠져나가기 직전 마지막 순간에 단 한 번 작동합니다.
    public void ExitState(KimEnemy enemy)
    {
        Debug.Log("Kim 적 캐릭터가 공격을 완수하고 상태를 안전하게 종료합니다.");

        // 사용한 임시 공격 타이머 변수를 0으로 깨끗이 정리 정돈합니다.
        attackTimer = 0f;
    }
}