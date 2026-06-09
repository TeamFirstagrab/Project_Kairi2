using Globals;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
	[Header("검광 이펙트")]
	[Tooltip("칼을 벨 때 화면에 소환되어 나타날 멋진 검광 스프라이트 프리팹")]
	[SerializeField] private GameObject slashEffectPrefab;
	[Tooltip("캐릭터 중심에서 몇 미터 앞에 검광을 띄워서 소환할지 조절하는 거리 오프셋")]
	[SerializeField] private Vector3 effectOffset = new Vector3(1f, 0f, 0f);

	[Header("타격 판정 설정 (C# 실시간 레이저 캐스팅)")]
	[Tooltip("우리가 휘두른 칼에 맞아 대미지를 입힐 대상 필터링 레이어 (Enemy 레이어)")]
	[SerializeField] private LayerMask enemyLayer;
	[Tooltip("칼날이 쓸고 지나가는 타격 범위의 둥근 원(Radius) 반경 크기")]
	[SerializeField] private float attackRadius = 1.8f;
	[Header("패링 설정")]
	[Tooltip("에너미 총알(Bullet) 오브젝트들이 속한 레이어 마스크를 등록합니다.")]
	[SerializeField] private LayerMask bulletLayer;
	private float lastAttackTime; // 마지막으로 공격한 시간 기록 장치 (연타 방지용)
	private Animator anim;        // 캐릭터 팔다리 모션을 바꿀 애니메이터 컴포넌트

	private Rigidbody2D rigid;
	private PlayerStatsRuntime stats;
	private Camera mainCam;
	private PlayerSlowMode slowMode;
	private float attackTimer;

	public bool IsAttacking { get; private set; }

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		slowMode = GetComponent<PlayerSlowMode>();
		anim = GetComponent<Animator>();

	}

	private void Start()
	{
		mainCam = Camera.main;
	}

	public void TryAttack()
	{
		if(!IsAttacking)
			StartCoroutine(Attack());
	}

	private IEnumerator Attack()
	{
		IsAttacking = true;
		stats = GameManager.Instance.playerStatsRuntime;
		Vector2 startPos = transform.position;
		Vector2 currPos = startPos;
		Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		Vector2 dir = (mousePos - startPos).normalized;
		Vector2 targetPos = startPos + dir * stats.attackDist;

		LayerMask mask = ~LayerMask.GetMask(LayerName.player, LayerName.oneWayPlatform);
		Vector2 boxSize = Vector2.Scale(GetComponent<BoxCollider2D>().size, transform.lossyScale);
		RaycastHit2D hit = Physics2D.BoxCast(
			transform.position,
			boxSize,
			transform.eulerAngles.z,
			dir,
			stats.attackDist,
			mask
		);
		LayerMask crackMask = LayerMask.GetMask(LayerName.crackObj);
		RaycastHit2D crackHit = Physics2D.Raycast(
				transform.position,
				dir,
				stats.attackDist,
				crackMask
			);

		Debug.DrawRay(transform.position, dir, Color.red, stats.attackDist);

		// TODO: 공격 시 무언가가 맞으면 때리는 위치에서 딜레이 + 카메라 쉐이킹 있음

		if (crackHit)
		{
			print($"hit obj: {crackHit.transform.name}");
			GameManager.Instance.poolManager.ReturnToPool(crackHit.transform.gameObject);
		}

		if (hit)
		{
			Collider2D hitCol = hit.collider;
			if (hitCol.CompareTag(TagName.enemy)
			|| hitCol.CompareTag(TagName.crackObj))
			{
				if (hitCol.TryGetComponent<IDamageable>(out IDamageable coll))
				{
					coll.TakeDamage(stats.attack);  // 공격력만큼 데미지 주기

					// TODO: 공격 이펙트
					print($"Attack to {hit.collider.tag}");
				}
			}
			else if (hitCol.CompareTag(TagName.door))
			{
				hit.transform.GetComponent<IInteractionObject>()?.OnInteract();
				print($"Interact {hit.collider.tag}");
			}
			else if (hitCol.CompareTag(TagName.bullet))
			{
				// TODO: 총알 패링
				if (hit.transform.TryGetComponent<EnemyBullet>(out var bullet))
				{
					print($"Hit Bullet {bullet.ToString()}");
					bullet.DeflectBullet(mousePos);     // 패링
				}
			}
		}

		// 공격 거리만큼 대쉬
		while (Vector2.Distance(transform.position, targetPos) > 0.1f
			&& stats.attackDuration > attackTimer)
		{
			attackTimer += Time.deltaTime;
			float t = attackTimer / 0.1f;
			transform.position = Vector3.Lerp(transform.position, targetPos, t);
			yield return null;  // 다음 프레임까지 대기
		}
		transform.position = targetPos;

		yield return new WaitForSeconds(stats.attackCoolTime);
		attackTimer = 0f;

		Invoke(nameof(ResetAttackState), stats.attackCoolTime);
	}

	private void ResetAttackState()
	{
		IsAttacking = false;
	}

	private void OnDrawGizmos()
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
}