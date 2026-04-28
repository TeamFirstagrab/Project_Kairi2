using UnityEngine;
using Globals;
using EnumType;
using System.Collections.Generic;

public class Enemy : MonoBehaviour, IDamageable
{
	public Rigidbody2D _rb;
	public Dictionary<EnemyState, IEnemyState> _stateList;
	private EnemyState _enemyState;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody2D>();
	}

	private void Start()
	{
		InitStateList();	// 상태 리스트 초기화
	}

	private void Update()
	{
		_stateList[_enemyState]?.UpdateState(this);

		Debug.Log($"main enemy state: {_enemyState}");
	}

	private void InitStateList()
	{
		_stateList = new Dictionary<EnemyState, IEnemyState>();
		_stateList[EnemyState.IDLE] = new EnemyIdle();
		_stateList[EnemyState.CHASE] = new EnemyChase();
		_stateList[EnemyState.ATTACK] = new EnemyAttack();
		_stateList[EnemyState.PATROL] = new EnemyPatrol();

		_enemyState = EnemyState.IDLE;
		ChangeState(_enemyState);	// 설정한 상태로 진입
	}

	public void ChangeState(EnemyState p_state) // 상태 변경
	{
		Debug.Log($"{_enemyState.ToString()} -> {p_state.ToString()} 상태 변경");

		_stateList[_enemyState]?.ExitState(this);
		_enemyState = p_state;
		_stateList[_enemyState].EnterState(this);
	}

	public void ChangStateDebug(EnemyState p_state)
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			ChangeState(p_state);
		}
	}

	// 인터페이스 상속
	public void TakeDamage(int attack)
	{
		Debug.Log("Damaged");
	}

	public void Die()
	{
	}
}
