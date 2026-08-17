using Globals;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
	[Header("전투 설정")]
	[Tooltip("칼을 한 번 베고 나서 다음 공격을 할 수 있을 때까지 기다리는 쿨타임 대기시간 (초)")]
	[SerializeField] private float attackCooldown = 0.05f;
	[Tooltip("플레이어가 공격한 후 타격감을 위해 잠시 멈출 시간")]
	[SerializeField] private float afterAttackStopTime = 0.05f;

	[Tooltip("칼날이 쓸고 지나가는 타격 범위의 둥근 원(Radius) 반경 크기")]
	[SerializeField] private float attackRadius = 1.8f;

	[Header("이펙트")]
	[SerializeField]
	private GameObject attackEffectPref;

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
		//if(!IsAttacking)
		//	StartCoroutine(Attack());

		// 공격 중이거나 공격 쿨타임이 다 안 찼으면 리턴
		if (!canAttack) return;
		if (IsAttacking) return;

		IsAttacking = true;

		StartCoroutine(Attack());
	}

	private IEnumerator Attack()
	{
		IsAttacking = true;
		stats = GameManager.Instance.playerStatsRuntime;
		Vector2 startPos = rigid.position;
		Vector2 currPos = startPos;
		Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		Vector2 dir = (mousePos - startPos).normalized;
		float targetDist = groundChk.isGrounded || groundChk.isSlope ? stats.attackDist : 1.5f;
		Vector2 targetPos = startPos + dir * targetDist;
		// -> 바닥이나 경사에 있을 경우에는 공격 거리만큼 이동
		// -> 공격을 하거나 점프해서 떠 있을 경우 매우 짧은 거리로 이동
		bool isSlow = false;    // 슬로우 여부
								
		float skin = 0.05f;

		Vector2 castStart = startPos + dir * skin;

		LayerMask mask = ~LayerMask.GetMask(LayerName.player, LayerName.oneWayPlatform, LayerName.crackObj);
		Vector2 boxSize = Vector2.Scale(GetComponent<BoxCollider2D>().size, transform.lossyScale);
		RaycastHit2D hit = Physics2D.BoxCast(
			castStart,
			boxSize,
			transform.eulerAngles.z,
			dir,
			stats.attackDist,
			mask
		);
		LayerMask crackMask = LayerMask.GetMask(LayerName.crackObj);
		RaycastHit2D crackHit = Physics2D.Raycast(
				castStart,
				dir,
				stats.attackDist,
				crackMask
			);

		Debug.DrawRay(rigid.position, dir, Color.red, stats.attackDist);

		// 공격 시 무언가가 맞으면 때리는 위치에서 딜레이 + 카메라 쉐이킹
		if (crackHit)   // 부서지는 오브젝트 (개별 처리)
		{
			targetDist = crackHit.distance * 1.5f;
			crackHit.collider.GetComponent<CrackObject>()?.Crack();
			GameManager.Instance.cameraShake.ShakeForSeconds(); // 카메라 쉐이킹
		}

		if(hit)
		{
			Collider2D hitCol = hit.collider;

			if (hitCol.CompareTag(TagName.enemy))    // 적
			{
				if (Vector2.Distance(rigid.position, hitCol.transform.position) >= stats.attackDist
					|| !groundChk.isGrounded)
				{
					targetDist = Vector2.Distance(rigid.position, hitCol.transform.position);
				}
				isSlow = true;
			}
			else if (hitCol.CompareTag(TagName.door))   // 문
			{
				GameManager.Instance.cameraShake.ShakeForSeconds(); // 카메라 쉐이킹

				if (hitCol.TryGetComponent(out DoorController door))
				{
					door.OnOpen();  // 상호작용
				}
			}
			else if (hitCol.CompareTag(TagName.bullet)) // 총알
			{
				GameManager.Instance.cameraShake.ShakeForSeconds(); // 카메라 쉐이킹

				// 총알 패링
				if (hit.transform.TryGetComponent<EnemyBullet>(out var bullet))
				{
					print($"Hit Bullet {bullet.ToString()}");
					//bullet.DeflectBullet(mousePos);     // TODO: 패링

					targetDist = 0f;
				}
			}
			else    // 그 외
			{
				targetDist = hit.distance;
			}

		}

		targetPos = rigid.position + dir * targetDist;

		// 공격 이펙트
		GameObject attackObj = SpawnAttackEffect(dir);

		// 슬로우
		if (isSlow)
		{
			print($"슬로우모드 (hit: {hit.collider.tag}, crackHit?: {crackHit.collider != null})");
			slowMode.EnterOnlySlow();
			yield return new WaitForSecondsRealtime(afterAttackStopTime);
			slowMode.ExitSlow();
		}

		// 공격 거리만큼 대쉬
		while (Vector2.Distance(rigid.position, targetPos) > 0.5f
			&& stats.attackDuration > attackTimer)
		{
			attackTimer += Time.deltaTime;
			float t = attackTimer / 0.5f;
			rigid.MovePosition(Vector3.Lerp(rigid.position, targetPos, t));
			if(attackObj != null) attackObj.transform.position = rigid.position;
			yield return null;  // 다음 프레임까지 대기
		}

		rigid.MovePosition(targetPos);

		if (hit)	// 적 데미지
		{
			if (hit.collider.TryGetComponent<IDamageable>(out IDamageable coll))
			{
				coll.TakeDamage(stats.attack, dir);  // 공격력만큼 데미지
			}
		}

		yield return new WaitForSeconds(stats.attackCoolTime);
		attackTimer = 0f;

		ResetAttackState();
	}

	private GameObject SpawnAttackEffect(Vector3 p_dir)
	{
		// 삼각함수 Atan2 각도 변환
		float angle = Mathf.Atan2(p_dir.y, p_dir.x) * Mathf.Rad2Deg;

		// 각도 주입 Quaternion 획득
		Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);

		return Instantiate(attackEffectPref, transform.position, spawnRotation);
	}

	private void ResetAttackState()     // 공격 설정 초기화
	{
		IsAttacking = false;
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		float maxDistance = 100f;

		RaycastHit2D hit;

		Vector2 startPos = transform.position;
		Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		LayerMask mask = ~LayerMask.GetMask(LayerName.player, LayerName.oneWayPlatform);
		Vector2 dir = (mousePos - startPos).normalized;
		Vector2 boxSize = GetComponent<BoxCollider2D>().size;

		// BoxCast
		// (중심 위치, 박스 크기, 회전 각도(z축), 방향, 거리)
		hit = Physics2D.BoxCast(
			transform.position,
			boxSize,
			transform.eulerAngles.z,
			dir,
			maxDistance,
			mask
		);

		Gizmos.color = Color.red;

		if (hit.collider != null)
		{
			Gizmos.DrawRay(
				transform.position,
				(Vector3)dir * hit.distance
			);

			Gizmos.DrawWireCube(
				transform.position + (Vector3)dir * hit.distance,
				boxSize
			);
		}
		else
		{
			Gizmos.DrawRay(
				transform.position,
				(Vector3)dir * maxDistance
			);
		}
	}
#endif
}