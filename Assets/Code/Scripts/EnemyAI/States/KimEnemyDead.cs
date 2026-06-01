using UnityEngine;
using EnumType; // 1단계에서 작성한 KimEnemyState 열거형 이름표를 쓰기 위해 연결합니다.
using Globals;   // 1단계에서 작성한 KimEnemyAnimName 이름표를 쓰기 위해 연결합니다.

/// <summary>
/// 김 전용 적 캐릭터의 죽음(Dead) 상태 행동을 지휘하는 클래스입니다.
/// 
/// 작동 원리 설명:
///  - 체력이 0이 되는 순간 본체의 사령탑(TakeDamage)에 의해 이 상태로 홱 변경됩니다.
///  - 상태에 진입하자마자 모든 X, Y축 물리 이동 속도를 즉각 0으로 강제 잠금하여 시체가 제자리에서 얌전히 쓰러지게 만듭니다.
///  - 1단계 Globals에 등록한 죽음 글자("Enemy_Die")를 틀어 적 캐릭터가 지면에 으악 쓰러져 죽는 그래픽 연출을 재생시킵니다.
///  - [매우 중요] 적의 물리 충돌 상자(Collider2D)를 즉시 비활성화(False)하여, 시체 껍데기가 플레이어의 앞길을 꽉 막아 지나가지 못하게 방해하는 유니티 물리 에러를 완벽하게 차단합니다.
///  - 1.5초 동안 쓰러진 모션을 유저에게 확실히 보여준 뒤, 기존에 잘 짜여있는 풀 매니저(PoolManager) 시스템을 안전하게 호출하여 씬에서 소환 해제시킵니다.
/// </summary>
public class KimEnemyDead : IKimEnemyState
{
    private float fadeDelay = 1.5f; // 쓰러진 모습을 플레이어에게 1.5초간 보인 뒤 소멸시키도록 딜레이 시간을 세팅합니다.
    private float fadeTimer = 0f;    // 실시간으로 시체가 쓰러져 대기한 경과 시간을 누적하는 카운터 타이머입니다.

    // [상태 입장 규칙 구현]
    // 적이 플레이어 칼에 맞아 숨을 거두는 첫 프레임 순간에 딱 한 번 작동합니다.
    public void EnterState(KimEnemy enemy)
    {
        Debug.Log("Kim 적 캐릭터의 목숨이 끊어져 사망 처리 및 사망 연출을 집행합니다.");

        // 1단계 Globals에 등록해 둔 오타 없는 정답 죽음 글자("Enemy_Die")를 호출해 애니메이터 엔진에게 쓰러지는 모션을 재생시킵니다.
        enemy.anim.Play(KimEnemyAnimName.dead);

        // 시체가 물리적으로 미끄러지거나 허공답보하지 않도록 수평/수직 속도를 완전 0으로 제동합니다.
        enemy.rb.linearVelocity = Vector2.zero;

        // [시체 충돌체 비활성화 연산]
        // 적 몸에 붙어있는 2D 물리 충돌 상자(Collider2D)를 찾아 꺼버립니다.
        // 이 덕분에 플레이어는 쓰러진 적의 시체를 발로 딛거나 튕겨 나가지 않고, 스펙터클하게 시체를 슥 밟고 통과해 지나갈 수 있게 됩니다!
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 실시간 소멸 딜레이 타이머를 0초로 셋업합니다.
        fadeTimer = 0f;
    }

    // [상태 유지 규칙 구현]
    // 시체가 바닥에 누워있는 1.5초 동안 매 프레임 실시간 무한 작동합니다.
    public void UpdateState(KimEnemy enemy)
    {
        // 시체가 바닥에 누워있는 상태에서도 관성 이동이 일어나지 않게 물리 속도를 매 순간 0으로 붙잡아 줍니다.
        enemy.rb.linearVelocity = Vector2.zero;

        // 실시간 딜레이 타이머 시계를 흘려보냅니다.
        fadeTimer += Time.deltaTime;

        // [소멸 및 풀 매니저 반환 공식 작동]
        // 1.5초의 시각적 사망 연출 대기가 완료되는 시점 프레임!
        if (fadeTimer >= fadeDelay)
        {
            // [예외 에러를 물리 치료하는 이중 안전망 장치]
            // 만약 풀 매니저(PoolManager)가 런타임 상에 안전하게 켜져 있다면 풀에 반환시키고, 
            // 만약 나홀로 테스트 씬이라 풀 매니저가 없다면 그냥 유니티 순정 파괴 공장(Destroy)을 사용해 안전하게 씬에서 파괴 소멸시킵니다.
            if (GameManager.Instance != null && GameManager.Instance.poolManager != null)
            {
                GameManager.Instance.poolManager.ReturnToPool(enemy.gameObject); // 원래 풀로 반환
            }
            else
            {
                GameObject.Destroy(enemy.gameObject); // 테스트용 강제 삭제
            }
        }
    }

    // [상태 퇴장 규칙 구현]
    // 적이 씬에서 완전히 사라지기 직전에 단 한 번 작동합니다.
    public void ExitState(KimEnemy enemy)
    {
        Debug.Log("Kim 적 캐릭터가 소멸 처리되어 게임 세상에서 완전히 퇴장했습니다.");

        // 사용한 타이머 변수를 0으로 초기화합니다.
        fadeTimer = 0f;
    }
}