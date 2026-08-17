using UnityEngine;
using EnumType;
using Globals;

public class EnemyRangedAttack : IEnemyState
{
    private float attackDuration = 0.8f; // 원거리 공격 모션 전체 수행 시간 (초)
    private float attackTimer = 0f;      // 시간 계산용 타이머
    private bool hasFired = false;       // 한 번의 공격 주기 동안 총알을 발사했는지 여부

    public void EnterState(Enemy enemy)
    {
        Debug.Log("Kim 에너미가 원거리 공격 상태에 돌입했습니다.");

        // 에너미의 공격 애니메이션 재생 (원격 공격 애니메이션 이름으로 교체 가능)
        enemy.anim.Play(EnemyAnimName.attack);

        // 공격하는 동안은 제자리에 고정되도록 강제 속도 0 설정
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        attackTimer = 0f;
        hasFired = false;
    }

    public void UpdateState(Enemy enemy)
    {
        attackTimer += Time.deltaTime;
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // 원리 설명: 공격 모션이 실행된 후, 프레임이 일정 시간(예: 0.2초) 흘렀을 때 총알을 소환합니다.
        // 이렇게 함으로써 모션과 발사 타이밍을 자연스럽게 싱크시킬 수 있습니다.
        if (!hasFired && attackTimer >= 0.2f)
        {
            FireBullet(enemy);
            hasFired = true;
        }

        // 공격 전체 시간이 완료되면 다시 플레이어를 쫓아다니는 CHASE 상태로 강제 복귀합니다.
        if (attackTimer >= attackDuration)
        {
            enemy.ChangeState(KimEnemyState.CHASE);
        }
    }

    public void ExitState(Enemy enemy)
    {
        Debug.Log("Kim 에너미가 원거리 공격 상태를 종료합니다.");
        attackTimer = 0f;
        hasFired = false;
    }

    /// <summary>
    /// 실제 총알을 소환하고 방향을 지시하는 핵심 물리 함수입니다.
    /// </summary>
    private void FireBullet(Enemy enemy)
    {
        if (enemy.bulletPrefab == null)
        {
            Debug.LogError($"{enemy.gameObject.name}: 발사할 총알 프리팹이 등록되지 않았습니다.");
            return;
        }

        // 발사구 위치 설정 (지정된 firePoint가 없다면 에너미 본체의 중심점에서 발사)
        Vector3 spawnPosition = enemy.firePoint != null ? enemy.firePoint.position : enemy.transform.position;

        // 총알 실시간 생성
        GameObject bulletObj = Object.Instantiate(enemy.bulletPrefab, spawnPosition, Quaternion.identity);

        // 생성된 총알 컴포넌트에 플레이어 조준 정보 주입
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            // 플레이어의 위치를 찾아 총알에 대상을 전달합니다.
            GameObject player = GameObject.FindWithTag(TagName.player);
            if (player != null)
            {
                // 플레이어의 중앙을 조준하도록 타겟 벡터 전달
                bullet.Launch(player.transform.position, enemy.enemyStats.Attack);
            }
            else
            {
                // 플레이어가 없다면 자신이 바라보는 방향으로 직진 발사
                Vector3 facingDir = enemy.transform.eulerAngles.y > 90f ? Vector3.left : Vector3.right;
                bullet.Launch(enemy.transform.position + facingDir, enemy.enemyStats.Attack);
            }
        }
    }
}