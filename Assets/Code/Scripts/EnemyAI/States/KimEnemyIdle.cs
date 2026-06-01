using UnityEngine;
using EnumType;
using Globals;

/// <summary>
/// [Kim 에너미 대기(Idle) 상태 클래스]
/// 일정 시간 동안 자리에 가만히 멈춰 서서 휴식을 취하는 행동 패턴입니다.
/// 실시간으로 플레이어가 시야 범위 내로 다가오는지를 탐지하여 추격(CHASE) 상태로 즉시 돌입합니다.
/// </summary>
public class KimEnemyIdle : IKimEnemyState
{
    private float waitTime = 0f;    // 자리에 머무르며 휴식을 취할 목표 시간 (초)
    private float waitTimer = 0f;   // 타이머 카운터

    public void EnterState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 대기(Idle) 상태에 들어가 휴식을 취합니다.");

        // 애니메이터에 지정된 대기 모션 재생
        enemy.anim.Play(KimEnemyAnimName.idle);

        // 자리에 멈춰 서도록 물리 속도를 수평 0으로 고정합니다.
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // 1.5초에서 2.5초 사이의 무작위 대기 시간 할당
        waitTimer = 0f;
        waitTime = Random.Range(1.5f, 2.5f);
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
                            return; // 상태 전환 시 즉시 Update 종료
                        }
                    }
                }
            }
        }

        // 실시간 경과 시간 계산
        waitTimer += Time.deltaTime;

        // 2. [목표 대기 시간 종료 판정]
        // 시간이 다 차면 순찰(PATROL) 상태로 돌입해 주변 지형을 정찰하도록 명령을 토스합니다.
        if (waitTimer >= waitTime)
        {
            enemy.ChangeState(KimEnemyState.PATROL);
        }
    }

    public void ExitState(KimEnemy enemy)
    {
        Debug.Log("Kim 에너미가 대기 상태를 마무리하고 일어섭니다.");
        waitTimer = 0f;
    }
}