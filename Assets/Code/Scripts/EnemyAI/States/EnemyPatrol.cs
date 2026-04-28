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
		p_enemy.GetComponent<Animator>().Play(EnemyAnimName.Patrol);	// 애니메이션 지정
		_isPatrolling = false;
		_moveTimer = 0f;
		_moveTime = Random.Range(2f, 3f);   // 움직일 시간 랜덤 지정

		Debug.Log($"Enter Patrol (moveTime: {_moveTime})");
	}

	public void UpdateState(Enemy p_enemy)
	{
		Debug.Log("Patroling...");

		if(!_isPatrolling)	// 추적 중인 경우
		{
			p_enemy.GetComponent<ObjectFlip>().Flip();
			_isPatrolling = true;
		}
		Vector2 dir = p_enemy.transform.localScale.x < 0 ? Vector2.right : Vector2.left;
		_moveTimer += Time.deltaTime;
		p_enemy._rb.linearVelocity = new Vector2(dir.x * p_enemy.GetComponent<EnemyDataManager>()._enemyStats.PatrolSpeed, 0f);

		if(_moveTimer >= _moveTime)
		{
			p_enemy._rb.linearVelocity = Vector2.zero;
			p_enemy.ChangeState(EnemyState.IDLE);
		}
	}
	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Patrol");
	}

}
