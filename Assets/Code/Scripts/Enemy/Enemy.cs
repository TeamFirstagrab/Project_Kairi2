using UnityEngine;
using EnumType;
using System.Collections.Generic;
using Globals;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// [Enemy 본체 스크립트]
/// 
/// 1. MonoBehaviour를 상속받아 게임 오브젝트에 부착되어 동작합니다.
/// 2. IDamageable 인터페이스를 구현합니다.
/// 3. FSM 상태 패턴을 사용하여 IDLE / PATROL / CHASE / ATTACK / DEAD 상태를 관리합니다.
/// 4. EnemyStats ScriptableObject를 통해 능력치를 관리합니다.
/// 5. 동일한 Enemy가 앞에 있을 경우 이동 방향을 반전시켜 서로 겹치는 것을 방지합니다.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
	[Header("에너미 능력치 데이터 (ScriptableObject)")]

	[Tooltip("에너미의 기본 스펙 데이터")]
	public EnemyStats enemyStats;

	[HideInInspector]
	public Rigidbody2D rb;

	[HideInInspector]
	public Animator anim;

	// FSM 상태 목록
	public Dictionary<EnemyState, IEnemyState> stateList;

	// 현재 FSM 상태
	private EnemyState currentEnemyState;

	// 현재 HP
	[HideInInspector]
	public int currentHP;


	// =========================================================
	// 원거리 공격 설정
	// =========================================================

	[Header("원거리 공격 설정")]

	[Tooltip("체크하면 원거리 공격을 사용합니다.")]
	public bool isRanged = false;

	[Tooltip("원거리 적이 사용할 총알 프리팹")]
	public GameObject bulletPrefab;

	[Tooltip("총알이 생성될 발사 위치")]
	public Transform firePoint;


	// =========================================================
	// 순찰 설정
	// =========================================================

	[Header("적 순찰 여부")]

	public bool isPatrol = true;


	// =========================================================
	// 피 이펙트
	// =========================================================

	private BloodEffect effect;


	// =========================================================
	// 동일 Enemy 회피 설정
	// =========================================================

	[Header("동일 적 충돌 회피")]

	[Tooltip("앞에 있는 같은 Enemy를 감지할 거리")]
	[SerializeField]
	private float enemyAvoidDistance = 1.0f;

	[Tooltip("같은 Enemy가 속한 레이어")]
	[SerializeField]
	private LayerMask enemyLayer;

	[Tooltip("같은 Enemy를 발견한 후 다시 방향을 바꾸기까지의 시간")]
	[SerializeField]
	private float avoidCooldown = 0.5f;

	// 마지막으로 방향을 전환한 시간
	private float lastAvoidTime = -999f;


	// =========================================================
	// 사망 이펙트
	// =========================================================

	[Header("Kill Slash Effect")]

	[SerializeField]
	private GameObject killSlashEffectPrefab;

	[SerializeField]
	private GameObject bloodEffectPrefab;

	[SerializeField]
	private float bloodEffectOffset = 0.5f;

	[SerializeField]
	private float killSlashOffset = 200.0f;


	// =========================================================
	// Awake
	// =========================================================

	private void Awake()
	{
		// Rigidbody2D 캐싱
		rb = GetComponent<Rigidbody2D>();

		// Animator 캐싱
		anim = GetComponent<Animator>();

		// BloodEffect 캐싱
		effect = GetComponent<BloodEffect>();
	}


	// =========================================================
	// OnEnable
	// =========================================================

	private void OnEnable()
	{
		// 풀에서 부활할 때 콜라이더와 물리 상태를 초기화
		if (rb != null)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
		}

		Collider2D col = GetComponent<Collider2D>();
		if (col != null)
		{
			col.enabled = true;
		}
	}


	// =========================================================
	// Start
	// =========================================================

	private void Start()
	{
		// EnemyStats가 등록되어 있다면 해당 HP 사용
		if (enemyStats != null)
		{
			currentHP = (int)enemyStats.HP;
		}
		else
		{
			// EnemyStats가 없을 경우 기본 HP
			currentHP = 1;

			Debug.LogWarning(
				$"{gameObject.name}에 EnemyStats 에셋이 등록되지 않아 기본 체력 1로 세팅되었습니다."
			);
		}

		// FSM 초기화
		InitStateList();
	}


	// =========================================================
	// Update
	// =========================================================

	private void Update()
	{
		if (stateList == null)
			return;

		if (stateList.ContainsKey(currentEnemyState))
		{
			stateList[currentEnemyState]?.UpdateState(this);
		}
	}


	// =========================================================
	// FSM 초기화
	// =========================================================

	private void InitStateList()
	{
		stateList = new Dictionary<EnemyState, IEnemyState>();

		// 기본 상태
		stateList[EnemyState.IDLE] = new EnemyIdle();

		stateList[EnemyState.PATROL] = new EnemyPatrol();

		stateList[EnemyState.CHASE] = new EnemyChase();

		stateList[EnemyState.DEAD] = new EnemyDead();


		// 원거리 / 근거리 공격 분기
		if (isRanged)
		{
			stateList[EnemyState.ATTACK] = new EnemyRangedAttack();
		}
		else
		{
			stateList[EnemyState.ATTACK] = new EnemyAttack();
		}


		// 최초 상태
		currentEnemyState = EnemyState.IDLE;

		ChangeState(currentEnemyState);
	}


	// =========================================================
	// FSM 상태 변경
	// =========================================================

	public void ChangeState(EnemyState nextState)
	{
		// 이미 동일한 상태이면 상태 전환 생략
		if (currentEnemyState == nextState)
		{
			return;
		}

		// 순찰이 비활성화되어 있는데 PATROL로 변경하려는 경우
		if (nextState == EnemyState.PATROL && !isPatrol)
		{
			return;
		}


		// 이전 상태 Exit
		if (stateList != null &&
			stateList.ContainsKey(currentEnemyState))
		{
			stateList[currentEnemyState]?.ExitState(this);
		}


		// 상태 변경
		currentEnemyState = nextState;


		// 새 상태 Enter
		if (stateList != null &&
			stateList.ContainsKey(currentEnemyState))
		{
			stateList[currentEnemyState]?.EnterState(this);
		}
	}


	// =========================================================
	// 현재 상태 확인
	// =========================================================

	public EnemyState GetCurrentState()
	{
		return currentEnemyState;
	}


	// =========================================================
	// 현재 바라보는 방향
	// =========================================================

	/// <summary>
	/// 현재 Enemy가 바라보고 있는 방향을 반환합니다.
	/// localScale.x 기준으로 좌우를 판단합니다.
	/// </summary>
	public Vector2 GetFacingDirection()
	{
		if (transform.localScale.x >= 0f)
		{
			return Vector2.right;
		}

		return Vector2.left;
	}


	// =========================================================
	// 같은 Enemy 감지
	// =========================================================

	/// <summary>
	/// 현재 Enemy가 바라보고 있는 방향에
	/// 동일한 Enemy가 존재하는지 검사합니다.
	/// </summary>
	public bool IsEnemyAhead()
	{
		Vector2 facingDirection = GetFacingDirection();


		RaycastHit2D hit = Physics2D.Raycast(
			transform.position,
			facingDirection,
			enemyAvoidDistance,
			enemyLayer
		);


		if (hit.collider == null)
		{
			return false;
		}


		// 충돌한 오브젝트에서 Enemy 컴포넌트를 검색
		Enemy otherEnemy = hit.collider.GetComponentInParent<Enemy>();


		// Enemy가 아니면 무시
		if (otherEnemy == null)
		{
			return false;
		}


		// 자기 자신이면 무시
		if (otherEnemy == this)
		{
			return false;
		}


		// 살아있는 Enemy만 감지
		if (otherEnemy.currentHP <= 0)
		{
			return false;
		}


		return true;
	}


	// =========================================================
	// 같은 Enemy 회피
	// =========================================================

	/// <summary>
	/// 앞에 같은 Enemy가 있다면 방향을 반전합니다.
	/// 
	/// avoidCooldown을 이용해 매 프레임 방향이
	/// 좌우로 반복해서 뒤집히는 현상을 방지합니다.
	/// </summary>
	public bool TryAvoidEnemy()
	{
		// 쿨타임 중이면 방향 전환하지 않음
		if (Time.time < lastAvoidTime + avoidCooldown)
		{
			return false;
		}


		// 앞에 Enemy가 없다면 아무것도 하지 않음
		if (!IsEnemyAhead())
		{
			return false;
		}


		// 방향 전환 시간 기록
		lastAvoidTime = Time.time;


		// 방향 반전
		TurnAround();


		return true;
	}


	// =========================================================
	// 방향 반전
	// =========================================================

	/// <summary>
	/// Enemy의 좌우 방향을 반전합니다.
	/// </summary>
	public void TurnAround()
	{
		Vector3 scale = transform.localScale;

		scale.x *= -1f;

		transform.localScale = scale;
	}


	// =========================================================
	// 피격 방향을 고려한 방향 설정
	// =========================================================

	/// <summary>
	/// 공격 방향을 기준으로 Enemy가 바라보는 방향을 설정합니다.
	/// 필요할 경우 피격 이펙트나 넉백 연출에 사용할 수 있습니다.
	/// </summary>
	public void FaceDirection(Vector2 direction)
	{
		if (direction.x == 0f)
		{
			return;
		}


		Vector3 scale = transform.localScale;

		if (direction.x > 0f)
		{
			scale.x = Mathf.Abs(scale.x);
		}
		else
		{
			scale.x = -Mathf.Abs(scale.x);
		}

		transform.localScale = scale;
	}


	// =========================================================
	// Blood Effect
	// =========================================================

	private void SpawnBloodEffect(Vector2 dir)
	{
		if (bloodEffectPrefab == null)
		{
			return;
		}


		// 공격 방향 쪽으로 약간 떨어진 위치에 생성
		Vector3 spawnPosition =
			transform.position +
			(Vector3)(dir.normalized * bloodEffectOffset);


		GameObject effect =
			Instantiate(
				bloodEffectPrefab,
				spawnPosition,
				Quaternion.identity
			);


		// 공격 방향에 맞춰 회전
		float angle =
			Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;


		effect.transform.rotation =
			Quaternion.Euler(0f, 0f, angle);
	}


	// =========================================================
	// Kill Slash Effect
	// =========================================================

	private void SpawnKillSlash(Vector2 dir)
	{
		if (killSlashEffectPrefab == null)
		{
			return;
		}

		// 공격 방향의 반대 방향(플레이어 쪽 뒤편)으로 오프셋을 주어 시작 위치 설정
		Vector3 spawnPosition =
			transform.position -
			(Vector3)(dir.normalized * killSlashOffset);

		GameObject effect =
			Instantiate(
				killSlashEffectPrefab,
				spawnPosition,
				Quaternion.identity
			);


		// 공격 방향에 맞춰 회전
		float angle =
			Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;


		effect.transform.rotation =
			Quaternion.Euler(0f, 0f, angle);
	}


	// =========================================================
	// 일반 데미지
	// =========================================================

	public void TakeDamage(int attackDamage)
	{
		// 이미 죽은 Enemy는 추가 공격을 받지 않음
		if (currentHP <= 0 ||
			currentEnemyState == EnemyState.DEAD)
		{
			return;
		}

		// 데미지 적용
		currentHP -= attackDamage;


		// 사망
		if (currentHP <= 0)
		{
			currentHP = 0;

			ChangeState(EnemyState.DEAD);
		}
	}


	// =========================================================
	// 방향을 포함한 데미지
	// =========================================================

	public void TakeDamage(
		int attackDamage,
		Vector2 attackDirection)
	{
		TakeDamage(
			attackDamage,
			attackDirection,
			true
		);
	}


	// =========================================================
	// 방향 + Slash 여부를 포함한 데미지
	// =========================================================

	public void TakeDamage(
		int attackDamage,
		Vector2 attackDirection,
		bool isSlash)
	{
		// 이미 죽은 Enemy는 추가 공격을 받지 않음
		if (currentHP <= 0 ||
			currentEnemyState == EnemyState.DEAD)
		{
			return;
		}


		// HP 감소
		currentHP -= attackDamage;


		// =====================================================
		// 사망
		// =====================================================

		if (currentHP <= 0)
		{
			currentHP = 0;


			// Slash 공격일 경우 Slash Effect 생성
			if (isSlash)
			{
				SpawnKillSlash(attackDirection);
			}


			// Blood Effect 생성
			SpawnBloodEffect(attackDirection);


			// DEAD 상태 전환
			ChangeState(EnemyState.DEAD);


			// BloodEffect 활성화
			effect?.ActiveBloodEffect(
				Random.insideUnitCircle.normalized
			);


			// =================================================
			// PlayerSkillAttack 쿨타임 감소
			// =================================================

			Debug.Log(
				$"[Enemy] {gameObject.name} died! " +
				$"HP: {currentHP}. " +
				$"Notifying PlayerSkillAttack..."
			);


			var playerSkillAttack =
				FindAnyObjectByType<PlayerSkillAttack>();


			if (playerSkillAttack != null)
			{
				playerSkillAttack.ReduceCooldownOnKill();
			}
			else
			{
				Debug.LogWarning(
					"[Enemy] PlayerSkillAttack component not found in scene!"
				);
			}
		}
	}


	// =========================================================
	// Gizmos
	// =========================================================

	private void OnDrawGizmosSelected()
	{
		// EnemyStats가 없으면 종료
		if (enemyStats == null)
		{
			return;
		}


#if UNITY_EDITOR

		// =====================================================
		// 시야 범위
		// =====================================================

		Handles.color =
			new Color(
				1f,
				0.92f,
				0.016f,
				0.12f
			);


		// 현재 바라보는 방향
		Vector3 facingDir;

		if (transform.localScale.x >= 0f)
		{
			facingDir = Vector3.right;
		}
		else
		{
			facingDir = Vector3.left;
		}


		// 시야 시작 방향
		Vector3 startDir =
			Quaternion.Euler(
				0f,
				0f,
				-enemyStats.SightAngle
			) * facingDir;


		// 시야 부채꼴
		Handles.DrawSolidArc(
			transform.position,
			Vector3.forward,
			startDir,
			enemyStats.SightAngle * 2f,
			enemyStats.SightRange
		);


		// 시야 외곽선
		Handles.color = Color.yellow;


		Handles.DrawWireArc(
			transform.position,
			Vector3.forward,
			startDir,
			enemyStats.SightAngle * 2f,
			enemyStats.SightRange
		);


		// 시야 끝 방향
		Vector3 endDir =
			Quaternion.Euler(
				0f,
				0f,
				enemyStats.SightAngle
			) * facingDir;


		Gizmos.color = Color.yellow;


		// 시야 좌측 경계
		Gizmos.DrawLine(
			transform.position,
			transform.position +
			startDir * enemyStats.SightRange
		);


		// 시야 우측 경계
		Gizmos.DrawLine(
			transform.position,
			transform.position +
			endDir * enemyStats.SightRange
		);

#endif


		// =====================================================
		// 공격 범위
		// =====================================================

		Gizmos.color =
			new Color(
				1f,
				0f,
				0f,
				0.2f
			);


		Gizmos.DrawSphere(
			transform.position,
			enemyStats.AttackRange
		);


		// 공격 범위 외곽선
		Gizmos.color = Color.red;


		Gizmos.DrawWireSphere(
			transform.position,
			enemyStats.AttackRange
		);


		// =====================================================
		// 동일 Enemy 감지 범위
		// =====================================================

		Gizmos.color = Color.cyan;


		Vector2 facingDirection = GetFacingDirection();


		Gizmos.DrawLine(
			transform.position,
			transform.position +
			(Vector3)(
				facingDirection *
				enemyAvoidDistance
			)
		);


		// 감지 끝 지점
		Gizmos.DrawWireSphere(
			transform.position +
			(Vector3)(
				facingDirection *
				enemyAvoidDistance
			),
			0.05f
		);
	}

	[HideInInspector]
	public bool isIgnoringPlatform = false;

	public void IgnorePlatformTemporarily(GameObject platformObj)
	{
		if (isIgnoringPlatform) return;
		StartCoroutine(IgnorePlatformRoutine(platformObj));
	}

	private System.Collections.IEnumerator IgnorePlatformRoutine(GameObject platformObj)
	{
		if (platformObj == null) yield break;
		isIgnoringPlatform = true;

		Collider2D[] platformColliders = platformObj.GetComponents<Collider2D>();
		Collider2D[] enemyColliders = GetComponents<Collider2D>();

		foreach (var platformCol in platformColliders)
		{
			foreach (var enemyCol in enemyColliders)
			{
				Physics2D.IgnoreCollision(enemyCol, platformCol, true);
			}
		}

		yield return new WaitForSeconds(1.5f);

		if (platformObj != null)
		{
			foreach (var platformCol in platformColliders)
			{
				if (platformCol != null)
				{
					foreach (var enemyCol in enemyColliders)
					{
						Physics2D.IgnoreCollision(enemyCol, platformCol, false);
					}
				}
			}
		}
		isIgnoringPlatform = false;
	}
}