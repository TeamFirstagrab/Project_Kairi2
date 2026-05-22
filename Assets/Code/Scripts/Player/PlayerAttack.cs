using Globals;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
	private Rigidbody2D rigid;
	private PlayerStatsRuntime stats;
	private Camera mainCam;
	private Tweener tr;
	private float attackTimer;

	public bool isAttack;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
	}

    private void Start()
    {
        mainCam = Camera.main;
    }

	public void TryAttack()
    {
        StartCoroutine(Attack());
	}

	private IEnumerator Attack()
    {
        stats = GameManager.Instance.playerStatsRuntime;
        Vector2 startPos = transform.position;
		Vector2 currPos = startPos;
		Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		Vector2 dir = (mousePos - startPos).normalized;
		Vector2 targetPos = startPos + dir * stats.attackDist;

		LayerMask mask = ~LayerMask.GetMask(LayerName.player);
		Vector2 boxSize = Vector2.Scale(GetComponent<BoxCollider2D>().size, transform.lossyScale);
		RaycastHit2D hit = Physics2D.BoxCast(
			transform.position,
			boxSize,
			transform.eulerAngles.z,
			dir,
			stats.attackDist,
			mask
		);

		isAttack = true;

		if (hit)
		{
			Collider2D hitCol = hit.collider;
			if(hitCol.CompareTag(TagName.enemy) 
			|| hitCol.CompareTag(TagName.crackObj))
			{
				if(hitCol.TryGetComponent<IDamageable>(out IDamageable coll))
				{
					coll.TakeDamage(stats.attack);  // 공격력만큼 데미지 주기
					targetPos = startPos;
					// TODO: 공격 이펙트
					print($"Attack to {hit.collider.tag}");
				}
			}
			else if(hitCol.CompareTag(TagName.door))
			{
				hit.transform.GetComponent<IInteractionObject>()?.OnInteract();
				print($"Interact {hit.collider.tag}");
			}
			else if (hitCol.CompareTag(TagName.bullet))
			{
				// TODO: 총알 패링
				print($"Parrying");
			}
			else
				targetPos = startPos + dir * hit.distance;
		}

		// 공격 거리만큼 대쉬
		//while (Vector2.Distance(transform.position, targetPos) > 0.1f
		//	&& stats.attackDuration > attackTimer)
		//{
		//	attackTimer += Time.deltaTime;
		//	float t = attackTimer / 0.5f;
		//	transform.position = Vector3.Lerp(transform.position, targetPos, t);
		//	yield return null;	// 다음 프레임까지 대기
		//}
		transform.DOMove(targetPos, stats.attackDuration);

		yield return new WaitForSeconds(stats.attackCoolTime);
		attackTimer = 0f;
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