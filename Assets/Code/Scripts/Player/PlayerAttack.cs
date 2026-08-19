using Globals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
	[Header("전투 설정")]
	[Tooltip("칼을 한 번 베고 나서 다음 공격을 할 수 있을 때까지 기다리는 쿨타임 대기시간 (초)")]
	[SerializeField] private float attackCooldown = 0.05f;

	[Tooltip("플레이어가 공격한 후 타격감을 위해 잠시 멈출 시간")]
	[SerializeField] private float afterAttackStopTime = 0.07f;

	[Tooltip("칼날이 쓸고 지나가는 타격 범위의 둥근 원(Radius) 반경 크기")]
	[SerializeField] private float attackRadius = 1.8f;

	[Header("이펙트")]
	[SerializeField] private GameObject attackEffectPref;

	private Rigidbody2D rigid;
	private PlayerStatsRuntime stats;
	private Camera mainCam;
	private PlayerSlowMode slowMode;
	private PlayerGroundChecker groundChk;

	private float attackTimer;

	public bool canAttack = true;

	public bool IsAttacking { get; private set; }


	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		slowMode = GetComponent<PlayerSlowMode>();
		groundChk = GetComponent<PlayerGroundChecker>();
	}


	private void Start()
	{
		mainCam = Camera.main;
	}


	public void TryAttack()
	{
		// 공격 중이거나 공격할 수 없는 상태라면 공격하지 않는다.
		if (!canAttack) return;
		if (IsAttacking) return;

		IsAttacking = true;

		StartCoroutine(Attack());
	}


	private IEnumerator Attack()
	{
		IsAttacking = true;

		// 공격 시작 시 타이머 초기화
		attackTimer = 0f;

		stats = GameManager.Instance.playerStatsRuntime;

		Vector2 startPos = rigid.position;
		Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);

		// 마우스 방향으로 공격 방향 계산
		Vector2 dir = (mousePos - startPos).normalized;

		// 공격 거리
		float targetDist =
			groundChk.isGrounded || groundChk.isSlope
				? stats.attackDist
				: 1.5f;

		Vector2 targetPos = startPos + dir * targetDist;

		bool isSlow = false;

		float skin = 0.05f;

		// 플레이어 바로 앞에서부터 판정하도록 약간 앞으로 이동
		Vector2 castStart = startPos + dir * skin;


		// ============================================================
		// 1. CrackObject / Door 판정
		// ============================================================

		LayerMask objMask = LayerMask.GetMask(
			LayerName.crackObj,
			LayerName.door
		);

		RaycastHit2D objHit = Physics2D.Raycast(
			castStart,
			dir,
			stats.attackDist,
			objMask
		);


		// ============================================================
		// 2. 공격 범위의 모든 충돌체 탐색
		//
		// 기존 BoxCast는 가장 먼저 맞은 물체 하나만 반환한다.
		//
		// 하지만 원하는 동작은
		//
		// Door -> Enemy
		//
		// 처럼 Door 뒤에 있는 Enemy까지 공격해야 하기 때문에
		// BoxCastAll을 사용한다.
		// ============================================================

		LayerMask attackMask = ~LayerMask.GetMask(
			LayerName.player,
			LayerName.oneWayPlatform,
			LayerName.crackObj
		);

		BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

		Vector2 boxSize = Vector2.Scale(
			boxCollider.size,
			transform.lossyScale
		);

		RaycastHit2D[] allHits = Physics2D.BoxCastAll(
			castStart,
			boxSize,
			transform.eulerAngles.z,
			dir,
			stats.attackDist,
			attackMask
		);


		Debug.DrawRay(
			rigid.position,
			dir * stats.attackDist,
			Color.red,
			0.5f
		);


		// ============================================================
		// 3. Enemy와 Door를 각각 수집
		// ============================================================

		List<Collider2D> enemyColliders = new List<Collider2D>();

		DoorController door = null;

		float doorDistance = Mathf.Infinity;


		foreach (RaycastHit2D hit in allHits)
		{
			if (hit.collider == null)
				continue;


			// --------------------------------------------------------
			// Enemy
			// --------------------------------------------------------

			if (hit.collider.CompareTag(TagName.enemy))
			{
				// 같은 Enemy의 여러 Collider가 잡히는 경우를 방지
				if (!enemyColliders.Contains(hit.collider))
				{
					enemyColliders.Add(hit.collider);
				}

				continue;
			}


			// --------------------------------------------------------
			// Door
			// --------------------------------------------------------

			if (hit.collider.CompareTag(TagName.door))
			{
				// 공격 방향에서 가장 가까운 Door만 사용
				if (hit.distance < doorDistance)
				{
					doorDistance = hit.distance;

					door = hit.collider.GetComponent<DoorController>();
				}
			}
		}


		// ============================================================
		// 4. Enemy가 Door보다 앞에 있는지 확인
		//
		// Enemy -> Door
		//
		// 이런 구조라면 Enemy가 먼저 공격을 받기 때문에
		// Door는 열리지 않는다.
		//
		// Door -> Enemy
		//
		// 이 구조라면 Door가 먼저 공격을 받으므로
		// Door도 열리고 Enemy도 공격받는다.
		// ============================================================

		bool enemyBeforeDoor = false;

		if (door != null)
		{
			foreach (Collider2D enemyCollider in enemyColliders)
			{
				float enemyDistance = Vector2.Distance(
					castStart,
					enemyCollider.ClosestPoint(castStart)
				);

				if (enemyDistance < doorDistance)
				{
					enemyBeforeDoor = true;
					break;
				}
			}
		}


		// ============================================================
		// 5. Door 처리
		// ============================================================

		if (door != null)
		{
			// Enemy가 Door보다 앞에 있다면
			// Door는 열지 않는다.
			if (!enemyBeforeDoor)
			{
				GameManager.Instance.cameraShake.ShakeForSeconds();

				door.OnOpen();

				GameManager.Instance.cameraShake.ShakeForSeconds();
			}
		}


		// ============================================================
		// 6. CrackObject 처리
		//
		// CrackObject는 기존처럼 공격 방향에서 가장 가까운
		// CrackObject 하나를 기준으로 처리한다.
		// ============================================================

		if (objHit)
		{
			Collider2D hitCol = objHit.collider;

			if (hitCol.CompareTag(TagName.crackObj))
			{
				targetDist = objHit.distance * 1.5f;

				hitCol
					.GetComponent<CrackObject>()
					?.Crack();
			}
		}


		// ============================================================
		// 7. 공격 대상에 Enemy가 있다면
		//    타격감용 Slow Mode 실행
		// ============================================================

		if (enemyColliders.Count > 0)
		{
			isSlow = true;
		}


		if (isSlow)
		{
			slowMode.EnterOnlySlow();

			yield return new WaitForSecondsRealtime(
				afterAttackStopTime
			);

			slowMode.ExitSlow();

			// 적을 공격했을 때 카메라 흔들림 + 슬로우
			GameManager.Instance.cameraShake.ShakeForSeconds();
		}


		// ============================================================
		// 8. 플레이어 공격 이동 거리 결정
		//
		// Enemy가 있다면 가장 가까운 Enemy 위치까지 이동한다.
		//
		// 단, Enemy가 Door보다 뒤에 있어도 Door가 먼저 있으므로
		// Door까지 이동한 뒤 Enemy를 공격할 수 있도록 한다.
		// ============================================================

		if (enemyColliders.Count > 0)
		{
			float nearestEnemyDistance = Mathf.Infinity;

			foreach (Collider2D enemyCollider in enemyColliders)
			{
				float distance = Vector2.Distance(
					rigid.position,
					enemyCollider.transform.position
				);

				if (distance < nearestEnemyDistance)
				{
					nearestEnemyDistance = distance;
				}
			}


			// 기존 코드의 공격 거리 제한 로직 유지
			if (groundChk.isGrounded)
			{
				targetDist = Mathf.Min(
					targetDist,
					nearestEnemyDistance
				);
			}
		}


		// 최종 이동 위치 계산
		targetPos = rigid.position + dir * targetDist;


		// ============================================================
		// 9. 공격 이펙트 생성
		// ============================================================

		GameObject attackObj = SpawnAttackEffect(dir);


		// ============================================================
		// 10. 공격 거리만큼 대쉬
		// ============================================================

		while (
			Vector2.Distance(rigid.position, targetPos) > 0.5f &&
			attackTimer < stats.attackDuration
		)
		{
			attackTimer += Time.deltaTime;

			float t = attackTimer / 0.5f;

			rigid.MovePosition(
				Vector2.Lerp(
					rigid.position,
					targetPos,
					t
				)
			);


			// 공격 이펙트를 플레이어 위치에 따라 이동
			if (attackObj != null)
			{
				attackObj.transform.position = rigid.position;
			}

			yield return null;
		}


		// 최종 위치 보정
		rigid.MovePosition(targetPos);


		// ============================================================
		// 11. Enemy 데미지 처리
		//
		// BoxCastAll로 찾은 모든 Enemy를 공격한다.
		//
		// 따라서
		//
		// Door -> Enemy -> Enemy
		//
		// 구조라면 Door도 열리고 Enemy 두 마리도 공격받는다.
		// ============================================================

		foreach (Collider2D enemyCollider in enemyColliders)
		{
			if (enemyCollider == null)
				continue;


			if (enemyCollider.TryGetComponent<IDamageable>(
				out IDamageable damageable
			))
			{
				damageable.TakeDamage(
					stats.attack,
					dir
				);
			}
		}


		// ============================================================
		// 12. 공격 쿨타임
		// ============================================================

		yield return new WaitForSeconds(
			stats.attackCoolTime
		);


		attackTimer = 0f;

		ResetAttackState();
	}


	/// <summary>
	/// 공격 이펙트를 생성하고 공격 방향에 맞게 회전시킨다.
	/// </summary>
	private GameObject SpawnAttackEffect(Vector3 p_dir)
	{
		// 방향 벡터를 각도로 변환
		float angle =
			Mathf.Atan2(
				p_dir.y,
				p_dir.x
			) * Mathf.Rad2Deg;


		// Z축 회전값 생성
		Quaternion spawnRotation =
			Quaternion.Euler(
				0f,
				0f,
				angle
			);


		// 플레이어 위치에서 공격 이펙트 생성
		return Instantiate(
			attackEffectPref,
			transform.position,
			spawnRotation
		);
	}


	/// <summary>
	/// 공격 상태를 초기화한다.
	/// </summary>
	private void ResetAttackState()
	{
		IsAttacking = false;
	}


#if UNITY_EDITOR

	private void OnDrawGizmosSelected()
	{
		// 에디터에서 공격 판정 범위를 확인하기 위한 Gizmo
		float maxDistance = 100f;


		// Start에서 초기화되기 전에는 Camera가 없을 수 있으므로
		// null 체크
		if (mainCam == null)
			mainCam = Camera.main;

		if (mainCam == null)
			return;


		Vector2 startPos = transform.position;

		Vector2 mousePos =
			mainCam.ScreenToWorldPoint(
				Input.mousePosition
			);


		Vector2 dir =
			(mousePos - startPos).normalized;


		LayerMask mask = ~LayerMask.GetMask(
			LayerName.player,
			LayerName.oneWayPlatform
		);


		BoxCollider2D boxCollider =
			GetComponent<BoxCollider2D>();


		Vector2 boxSize =
			Vector2.Scale(
				boxCollider.size,
				transform.lossyScale
			);


		// BoxCastAll로 실제 공격 범위와 비슷하게 표시
		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(
				startPos,
				boxSize,
				transform.eulerAngles.z,
				dir,
				maxDistance,
				mask
			);


		Gizmos.color = Color.red;


		// 공격 방향 표시
		Gizmos.DrawRay(
			startPos,
			dir * maxDistance
		);


		// 감지된 물체들을 표시
		foreach (RaycastHit2D hit in hits)
		{
			if (hit.collider == null)
				continue;


			Gizmos.DrawWireCube(
				hit.point,
				boxSize
			);
		}
	}

#endif
}