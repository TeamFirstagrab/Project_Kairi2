using EnumType;
using Globals;
using UnityEngine;

public class EnemyIdle : IEnemyState
{
	// TODO: Coroutine 사용하기
	private float _idleTime;    // 가만히 있을 시간
	private float _timer;		// 가만히 있는 시간 체크

	public void EnterState(Enemy p_enemy)
	{
		p_enemy.GetComponent<Animator>().Play(EnemyAnimName.Idle);      // 애니메이션 실행
		_idleTime = Random.Range(3f, 4f);	// 랜덤 값 지정
		_timer = 0f;

		Debug.Log($"Enter Idle (idleTime: {_idleTime})");
	}

	public void UpdateState(Enemy p_enemy)
	{
		Debug.Log("Idle...");

		_timer += Time.deltaTime;
		if(_timer >= _idleTime)
		{
			p_enemy.ChangeState(EnemyState.PATROL);
			return;
		}
	}
	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Idle");
	}

}
