using EnumType;
using Globals;
using UnityEngine;

public class EnemyPatrol : IEnemyState
{
	private float _moveTime = 0f;			// 움직일 시간 지정
	private float _moveTimer = 0f;			// 움직인 시간 체크
	private bool _isPatrolling = false;		// 이동 시작 여부

	public void EnterState(Enemy p_enemy)
	{
		Debug.Log($"Enter Patrol");

		p_enemy.GetComponent<Animator>().Play(EnemyAnimName.patrol);	// 애니메이션 지정
		_isPatrolling = false;
		_moveTimer = 0f;
		_moveTime = Random.Range(2f, 3f);   // 움직일 시간 랜덤 지정
	}

	public void UpdateState(Enemy p_enemy)
	{
		// 시야 내에 플레이어가 있을 경우 추격 상태로 변경
		if (p_enemy.GetComponent<EnemySight>().IsPlayerInRange())
		{
			p_enemy.ChangeState(EnemyState.CHASE);
			return;
		}

		if (!_isPatrolling)	// 추적 중인 경우
		{
			p_enemy.GetComponent<ObjectFlip>().Flip();
			_isPatrolling = true;
		}
		Vector2 dir = p_enemy.transform.localScale.x < 0 ? Vector2.right : Vector2.left;
		_moveTimer += Time.deltaTime;
		p_enemy.rb.linearVelocity = new Vector2(dir.x * p_enemy.GetComponent<EnemyDataManager>()._enemyStats.PatrolSpeed, 0f);

		if(_moveTimer >= _moveTime)
		{
			p_enemy.rb.linearVelocity = Vector2.zero;
			p_enemy.ChangeState(EnemyState.IDLE);
		}
	}
	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Patrol");
	}

}
