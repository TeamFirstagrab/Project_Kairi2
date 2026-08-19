using UnityEngine;
using EnumType;
using Globals;

/// <summary>
/// [Kim 에너미 원거리 공격(Ranged Attack) 상태 클래스]
/// 플레이어가 공격 사정거리 안으로 들어오면 0.5초 동안 멈춰 선 뒤 총알을 발사합니다.
/// </summary>
public class EnemyRangedAttack : IEnemyState
{
    private float attackDelay = 0.5f;     // 플레이어 앞에 서서 멈추는 대기 시간 (0.5초)
    private float attackDuration = 0.8f;  // 공격 전체 유지 시간
    private float attackTimer = 0f;       // 상태 진입 후 누적 시간
    private bool isAttacking = false;     // 0.5초 대기 후 실제 공격 모션 시작 여부
    private bool hasFired = false;        // 총알 발사 여부

    public void EnterState(Enemy enemy)
    {
        Debug.Log("Kim 원거리 에너미가 플레이어를 발견하여 공격 위치에서 0.5초 대기합니다.");

        // 플레이어 앞에서 멈춤
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // 0.5초 대기 동안 Idle 상태 모션 재생
        enemy.anim.Play(EnemyAnimName.idle);

        attackTimer = 0f;
        isAttacking = false;
        hasFired = false;
    }

    public void UpdateState(Enemy enemy)
    {
        attackTimer += Time.deltaTime;
        enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

        // 플레이어를 향해 바라보는 방향 조절
        GameObject playerObj = GameObject.FindWithTag(TagName.player);
        if (playerObj != null)
        {
            float directionX = playerObj.transform.position.x - enemy.transform.position.x;
            if (directionX > 0f)
            {
                enemy.transform.eulerAngles = Vector3.zero;
            }
            else if (directionX < 0f)
            {
                enemy.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            }
        }

        // 0.5초 멈춰 선 후 실제 공격 애니메이션 개시
        if (!isAttacking && attackTimer >= attackDelay)
        {
            isAttacking = true;
            enemy.anim.Play(EnemyAnimName.attack);
        }

        // 공격 동작 중 총알 발사 및 상태 종료
        if (isAttacking)
        {
            // 공격 애니메이션 시작 후 0.2초 시점에 총알 발사
            if (!hasFired && attackTimer >= attackDelay + 0.2f)
            {
                FireBullet(enemy);
                hasFired = true;
            }

            // 공격 유지 시간 종료 시 추격 상태로 복귀
            if (attackTimer >= attackDelay + attackDuration)
            {
                enemy.ChangeState(EnemyState.CHASE);
            }
        }
    }

    public void ExitState(Enemy enemy)
    {
        Debug.Log("Kim 원거리 에너미가 공격 상태를 종료합니다.");
        attackTimer = 0f;
        isAttacking = false;
        hasFired = false;
    }

    /// <summary>
    /// 지정된 발사 위치에서 총알을 생성하고 플레이어 방향으로 발사합니다.
    /// </summary>
    private void FireBullet(Enemy enemy)
    {
        if (enemy.bulletPrefab == null)
        {
            Debug.LogError($"{enemy.gameObject.name}: 에너미에 총알 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 발사 위치 설정 (설정된 firePoint가 없으면 에너미 위치 사용)
        Vector3 spawnPosition = enemy.firePoint != null ? enemy.firePoint.position : enemy.transform.position;

        // 총알 프리팹 생성
        GameObject bulletObj = Object.Instantiate(enemy.bulletPrefab, spawnPosition, Quaternion.identity);

        // 생성된 총알의 컴포넌트를 가져와 플레이어 방향으로 조준 발사
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            GameObject player = GameObject.FindWithTag(TagName.player);
            if (player != null)
            {
                bullet.Launch(player.transform.position, enemy.enemyStats.Attack);
            }
            else
            {
                Vector3 facingDir = enemy.transform.eulerAngles.y > 90f ? Vector3.left : Vector3.right;
                bullet.Launch(enemy.transform.position + facingDir, enemy.enemyStats.Attack);
            }
        }
    }
}