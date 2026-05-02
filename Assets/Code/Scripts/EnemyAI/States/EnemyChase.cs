using EnumType;
using Globals;
using UnityEngine;

public class EnemyChase : IEnemyState
{
	float _chaseTime = 2f;      // 플레이어 찾으려고 시도하는 시간
	float _chaseTimer = 0f;		// 시도한 시간
	public void EnterState(Enemy p_enemy)
	{
		Debug.Log("Enter Chase");

		p_enemy.GetComponent<Animator>().Play(EnemyAnimName.Chase);    // 탐색 애니메이션
		_chaseTime = 0f;
	}

	public void UpdateState(Enemy p_enemy)
	{
		Debug.Log("Chasing...");

		_chaseTimer += Time.deltaTime;
		Transform _player = GameManager.Instance.playerObj.transform;

		// 플레이어 체크
		bool _playerVisible = p_enemy.GetComponent<EnemySight>().IsPlayerInRange();
		if (_playerVisible) _chaseTimer = 0f;

		// 시야 밖으로 벗어났고, 시간이 끝났을 경우
		if(!_playerVisible && _chaseTimer >= _chaseTime)
		{
			// 기본 상태로 전환
			p_enemy.ChangeState(EnemyState.IDLE);
			return;
		}

		if(_player != null)
		{
			Vector2 dir = (_player.position - p_enemy.transform.position).normalized;   // 플레이어 방향

			// 추적 속도만큼 이동
			p_enemy.rb.linearVelocity = new Vector2(dir.x * p_enemy.GetComponent<EnemyDataManager>()._enemyStats.ChaseSpeed, 0f);

			// 적 이동 방향에 맞게 캐릭터 Flip
			if(dir.x != 0f)
			{
				Vector3 scale = p_enemy.transform.localScale;
				scale.x = -Mathf.Sign(dir.x) * Mathf.Abs(scale.x);
				p_enemy.transform.localScale = scale;
			}

			// 공격 가능 범위에 들어왔을 경우 공격 상태로 전환
			float dist = Vector2.Distance(p_enemy.transform.position, _player.position);
			if (dist <= p_enemy.GetComponent<EnemyDataManager>()._enemyStats.AttackRange)
			{
				p_enemy.rb.linearVelocity = Vector2.zero;
				p_enemy.ChangeState(EnemyState.ATTACK);
			}
		}
	}

	public void ExitState(Enemy p_enemy)
	{
		Debug.Log("Exit Chase");
	}

}
