using UnityEngine;
using Globals;
using EnumType;
using System.Collections.Generic;

public class Enemy : MonoBehaviour, IDamageable
{
	[HideInInspector] public Rigidbody2D rb;
	[HideInInspector] public Animator anim;
	public Dictionary<EnemyState, IEnemyState> stateList;
	private EnemyState enemyState;
	private EnemyStatsRuntime enemyStatsRuntime;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		anim = GetComponent<Animator>();
	}

	private void Start()
	{
		InitStateList();    // 상태 리스트 초기화

		// 플레이어 스탯
		enemyStatsRuntime = new EnemyStatsRuntime(GetComponent<EnemyDataManager>()._enemyStats);   // 스탯 값 복제
	}

	private void Update()
	{
		print($"state: {enemyState}");
		stateList[enemyState]?.UpdateState(this);
	}

	private void InitStateList()
	{
		stateList = new Dictionary<EnemyState, IEnemyState>();
		stateList[EnemyState.IDLE] = new EnemyIdle();
		stateList[EnemyState.CHASE] = new EnemyChase();
		stateList[EnemyState.ATTACK] = new EnemyLongRangeAttack();
		stateList[EnemyState.PATROL] = new EnemyPatrol();

		enemyState = EnemyState.IDLE;
		ChangeState(enemyState);	// 설정한 상태로 진입
	}

	public void ChangeState(EnemyState p_state) // 상태 변경
	{
		Debug.Log($"{enemyState.ToString()} -> {p_state.ToString()} 상태 변경");

		stateList[enemyState]?.ExitState(this);
		enemyState = p_state;
		stateList[enemyState].EnterState(this);
	}

	// 인터페이스 상속
	public void TakeDamage(int attack)
	{
		enemyStatsRuntime.CurrentHP -= attack;

		if (enemyStatsRuntime.CurrentHP < 0)
		{
			GameManager.Instance.poolManager.ReturnToPool(gameObject);
			Debug.Log($"적 사망 (데미지: {attack})");
		}
	}
}
