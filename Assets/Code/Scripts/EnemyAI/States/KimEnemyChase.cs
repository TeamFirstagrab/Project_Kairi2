using UnityEngine;
using EnumType;
using Globals;

/// <summary>
/// [Kim 에너미 추격(Chase) 상태 클래스]
/// 시야 안에 들어온 플레이어 대상을 끈질기게 추적하여 달리는 상태입니다.
/// 스크립터블 오브젝트(SO)로부터 추격 속도, 공격 유효 범위, 추격 해제 범위를 받아와 연동 제어합니다.
/// </summary>
public class KimEnemyChase : IKimEnemyState
{
    private Transform playerTransform; // 실시간으로 추격 타깃이 될 플레이어의 위치 컴포넌트
    private float chaseSpeed = 5.0f;    // 플레이어 추격 시의 달리기 속도 (기본값)

    public void EnterState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 플레이어를 추격 상태(Chase)로 인지하여 달리기를 개시합니다!");

        // 애니메이터에 지정된 달리기 모션("Enemy_Run") 플레이
        enemy.anim.Play(KimEnemyAnimName.chase);

        // 스크립터블 오브젝트 에셋의 추격 속도를 대입합니다.
        if (enemy.enemyStats != null)
        {
            chaseSpeed = enemy.enemyStats.ChaseSpeed;
        }

        // 글로벌 태그 상수로 플레이어 본체를 찾아내어 타깃팅합니다.
        GameObject playerObj = GameObject.FindWithTag(TagName.player);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public void UpdateState(KimEnemy enemy)
    {
        // 쫓고 있던 플레이어가 파괴되거나 씬에서 사라지면 대기 상태로 복원합니다.
        if (playerTransform == null)
        {
            enemy.ChangeState(KimEnemyState.IDLE);
            return;
        }

        // 플레이어 본체와의 실제 실시간 거리를 계산합니다.
        float distanceToPlayer = Vector2.Distance(enemy.transform.position, playerTransform.position);

        // 1. [공격 거리 판정 (AttackRange 연동)]
        // 플레이어가 공격 사정거리 안으로 들어왔다면 멈춰 서서 공격(ATTACK) 상태로 즉시 전환합니다.
        float currentAttackRange = 1.8f;
        if (enemy.enemyStats != null)
        {
            currentAttackRange = enemy.enemyStats.AttackRange;
        }

        if (distanceToPlayer <= currentAttackRange)
        {
            enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y); // 즉각 제동
            enemy.ChangeState(KimEnemyState.ATTACK);
            return;
        }

        // 2. [추격 한계 범위 판정 (SightRange 연동)]
        // 플레이어가 너무 멀리 도망쳐 에셋에 적힌 감지 시야 한계(SightRange) 범위를 넘어가면 대기(IDLE) 상태로 돌려놓습니다.
        float currentSightRange = 8.0f;
        if (enemy.enemyStats != null)
        {
            currentSightRange = enemy.enemyStats.SightRange;
        }

        if (distanceToPlayer >= currentSightRange)
        {
            enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y); // 즉각 제동
            enemy.ChangeState(KimEnemyState.IDLE);
            return;
        }

        // 3. [플레이어 방향 감지 및 좌우 회전 조절]
        // 플레이어 좌표가 적의 우측에 있는지, 좌측에 있는지 분석하여 적의 Y축 각도를 0도 혹은 180도로 뒤집습니다.
        float directionX = playerTransform.position.x - enemy.transform.position.x;

        if (directionX > 0f)
        {
            enemy.transform.eulerAngles = Vector3.zero; // 오른쪽 방향 조준
        }
        else
        {
            enemy.transform.eulerAngles = new Vector3(0f, 180f, 0f); // 왼쪽 방향 조준
        }

        // 이동 진행 방향 결정 (-1 혹은 1)
        float moveSign = directionX > 0f ? 1f : -1f;

        // 물리 컴포넌트(Rigidbody2D)에 속력을 가하여 플레이어 방향으로 질주시킵니다.
        enemy.rb.linearVelocity = new Vector2(moveSign * chaseSpeed, enemy.rb.linearVelocity.y);
    }

    public void ExitState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 추격 상태를 종료하고 대상을 비워둡니다.");
        playerTransform = null;
    }
}