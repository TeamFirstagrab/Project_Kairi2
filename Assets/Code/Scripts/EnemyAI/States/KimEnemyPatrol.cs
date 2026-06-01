using UnityEngine;
using EnumType;
using Globals;

/// <summary>
/// [Kim 에너미 순찰(Patrol) 상태 클래스]
/// 지정된 시간 동안 한 방향으로 걷다가 대기 상태(IDLE)로 복귀하는 순찰 행동 패턴입니다.
/// 스크립터블 오브젝트에 기입된 PatrolSpeed 수치에 맞춰 부드럽게 지면을 걸어 다닙니다.
/// </summary>
public class KimEnemyPatrol : IKimEnemyState
{
    private float patrolTime = 0f;      // 순찰을 지속할 랜덤 목표 시간 (초)
    private float patrolTimer = 0f;     // 실시간 경과 시간 측정을 위한 타이머
    private bool hasFlipped = false;    // 방향 전환을 실행했는지에 대한 플래그

    public void EnterState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 순찰(Patrol) 상태에 진입하여 걷기를 시작합니다.");

        // 애니메이터에 지정된 걷기 모션 재생
        enemy.anim.Play(KimEnemyAnimName.patrol);

        // 경과 시간 초기화 및 2초에서 3초 사이의 불규칙 순찰 시간 할당
        patrolTimer = 0f;
        patrolTime = Random.Range(2f, 3f);
        hasFlipped = false;
    }

    public void UpdateState(KimEnemy enemy)
    {
        // 1. [실시간 플레이어 인지 및 추격 전환 (부채꼴 시야각 판정)]
        // 플레이어의 상하 수직 높이 차이, 전방 시야각(부채꼴 영역), 레이캐스트를 통한 지형 가림 여부를 모두 종합 검사합니다.
        GameObject playerObj = GameObject.FindWithTag(TagName.player);
        if (playerObj != null && enemy.enemyStats != null)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 playerPos = playerObj.transform.position;

            // A단계: 상하 높이 한계 체크 (Height Limit)
            float heightDifference = Mathf.Abs(playerPos.y - enemyPos.y);
            if (heightDifference <= enemy.enemyStats.SightHeightLimit)
            {
                // B단계: 직선거리 범위 체크 (Range)
                Vector2 dirToPlayer = playerPos - enemyPos;
                float distance = dirToPlayer.magnitude;
                if (distance <= enemy.enemyStats.SightRange)
                {
                    // C단계: 전방 부채꼴 시야각 범위 체크 (Angle)
                    dirToPlayer.Normalize();
                    Vector2 facingDir = enemy.transform.eulerAngles.y > 90f ? Vector2.left : Vector2.right;
                    float angle = Vector2.Angle(facingDir, dirToPlayer);

                    if (angle <= enemy.enemyStats.SightAngle)
                    {
                        // D단계: 시야 장애물 차단 여부 체크 (Raycast)
                        // 에너미 자신(Enemy 레이어)을 제외하고 벽이나 지형에 레이저가 먼저 막히는지 검사합니다.
                        LayerMask mask = ~LayerMask.GetMask(Globals.LayerName.enemy);
                        RaycastHit2D hit = Physics2D.Raycast(enemyPos, dirToPlayer, distance, mask);

                        if (hit.collider == null || hit.collider.CompareTag(TagName.player))
                        {
                            enemy.ChangeState(KimEnemyState.CHASE);
                            return; // 상태가 바뀌었으므로 즉시 업데이트 종료
                        }
                    }
                }
            }
        }

        // 2. [순찰 방향 결정 (EulerAngles 회전 방식 통일)]
        // 기존의 scale.x 곱하기 방식 대신, 전체 게임 프로젝트 표준에 맞춰 Y축 180도 회전 방식으로 방향을 결정합니다.
        if (!hasFlipped)
        {
            // 50% 확률로 왼쪽(180도 회전) 혹은 오른쪽(0도)을 바라보고 걷도록 정합니다.
            if (Random.value > 0.5f)
            {
                enemy.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            }
            else
            {
                enemy.transform.eulerAngles = Vector3.zero;
            }
            hasFlipped = true;
        }

        // 3. [바라보는 방향으로 이동 벡터 산출]
        // Y축 회전각(eulerAngles.y)이 90도보다 크면 왼쪽을 보고 있는 상태이므로 Vector2.left로 이동하고,
        // 그렇지 않으면 오른쪽을 보고 있는 상태이므로 Vector2.right로 이동하여 뒤로 걷는 버그를 원천 해결합니다.
        bool isFacingLeft = enemy.transform.eulerAngles.y > 90f;
        Vector2 walkDirection = isFacingLeft ? Vector2.left : Vector2.right;

        patrolTimer += Time.deltaTime;

        // 4. [스크립터블 오브젝트 연동 속도 대입]
        float currentPatrolSpeed = 2.5f;
        if (enemy.enemyStats != null)
        {
            currentPatrolSpeed = enemy.enemyStats.PatrolSpeed;
        }

        enemy.rb.linearVelocity = new Vector2(walkDirection.x * currentPatrolSpeed, enemy.rb.linearVelocity.y);

        // 5. [순찰 시간 초과 판정]
        // 약속된 순찰 목표 시간에 도달한 경우, 물리 정지 제동을 건 뒤 대기(IDLE) 상태로 컴백합니다.
        if (patrolTimer >= patrolTime)
        {
            enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);
            enemy.ChangeState(KimEnemyState.IDLE);
        }
    }

    public void ExitState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 순찰 상태를 마치고 퇴장합니다.");
        patrolTimer = 0f;
    }
}