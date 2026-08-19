using UnityEngine;
using EnumType;
using Globals;

/// <summary>
/// [Kim 에너미 근거리 공격(Attack) 상태 클래스]
/// 플레이어가 공격 사정거리에 도달하면 0.5초 동안 멈춰 선 뒤 근거리 공격을 시도합니다.
/// </summary>
public class EnemyAttack : IEnemyState
{
	private float attackDelay = 0.5f;    // 플레이어 앞에 서서 공격 전 멈추는 대기 시간 (0.5초)
	private float attackDuration = 0.6f; // 실제 공격 애니메이션/판정 유지 시간
	private float attackTimer = 0f;      // 상태 진입 후 누적 시간
	private bool isAttacking = false;    // 0.5초 대기 후 실제 공격 애니메이션 시작 여부

	// 플레이어가 속한 레이어
	private LayerMask playerLayer;

	// 이미 데미지를 입혔는지 여부
	private bool hasDealtDamage = false;

	public void EnterState(Enemy enemy)
	{
		if (enemy.currentHP <= 0) return; // 이미 사망한 경우 제외
		Debug.Log("Kim 에너미가 플레이어를 발견하여 공격 위치에서 0.5초 대기합니다!");

		// 플레이어 앞에 서서 이동 멈춤
		enemy.rb.linearVelocity = new Vector2(0f, enemy.rb.linearVelocity.y);

		// 0.5초 멈춤 대기 동안 Idle 상태 모션 재생
		enemy.anim.Play(EnemyAnimName.idle);

		attackTimer = 0f;
		isAttacking = false;
		hasDealtDamage = false;

		// Player 레이어 가져오기
		playerLayer = LayerMask.GetMask(LayerName.player);
	}

	public void UpdateState(Enemy enemy)
	{
		if (enemy.currentHP <= 0) return; // 이미 사망한 경우 제외
		attackTimer += Time.deltaTime;

		// 멈춤 상태 유지
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

		// 0.5초 멈춰 선 후 실제 공격 동작 개시
		if (!isAttacking && attackTimer >= attackDelay)
		{
			isAttacking = true;
			enemy.anim.Play(EnemyAnimName.attack);
		}

		// 공격 동작 진행 중 타격 판정 및 종료 처리
		if (isAttacking)
		{
			if (!hasDealtDamage)
			{
				CheckAttackHit(enemy);
			}

			// 공격 시간 종료 후 다시 추격(CHASE) 상태로 전환
			if (attackTimer >= attackDelay + attackDuration)
			{
				enemy.ChangeState(EnemyState.CHASE);
			}
		}
	}

	private void CheckAttackHit(Enemy enemy)
	{
		if (enemy.currentHP <= 0) return; // 이미 사망한 경우 제외
		
		// 공격 사정거리 범위 내 플레이어 검사
		Collider2D hitPlayer = Physics2D.OverlapCircle(
			enemy.transform.position,
			enemy.enemyStats.AttackRange,
			playerLayer
		);

		// 플레이어가 공격 범위 내에 있으면 데미지 전달
		if (hitPlayer != null)
		{
			PlayerHealth playerHealth = hitPlayer.GetComponent<PlayerHealth>();

			if (playerHealth != null)
			{
				playerHealth.TakeDamage(enemy.enemyStats.Attack);

				// 한 번 공격 시 중복 데미지 방지
				hasDealtDamage = true;
			}
		}
	}

	public void ExitState(Enemy enemy)
	{
		Debug.Log("Kim 에너미가 공격 상태를 종료합니다.");

		attackTimer = 0f;
		isAttacking = false;
		hasDealtDamage = false;
	}
}