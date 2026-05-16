using Globals;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAttack : MonoBehaviour
{
	private Rigidbody2D rigid;
	private PlayerStatsRuntime stats;
	private Camera mainCam;
	private float attackTimer;

	public bool isAttack;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
		mainCam = Camera.main;
		stats = GameManager.Instance.playerStatsRuntime;
	}

	public void TryAttack()
	{
		StartCoroutine(Attack());
	}

	private IEnumerator Attack()
	{
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

		if (hit)
			targetPos = startPos + dir * hit.distance;

		// 공격 거리만큼 대쉬
		while (Vector2.Distance(transform.position, targetPos) > 0.1f)
		{
			attackTimer += Time.deltaTime;
			float t = attackTimer;
			transform.position = Vector3.Lerp(transform.position, targetPos, t);
			yield return null;	// 다음 프레임까지 대기
		}

		yield return new WaitForSeconds(stats.attackCoolTime);
		attackTimer = 0f;
	}

	private void OnDrawGizmos()
	{
		float maxDistance = 100f;

		RaycastHit2D hit;
		LayerMask mask = ~LayerMask.GetMask(LayerName.player);
		Vector2 dir = transform.right;
		//Vector2 boxSize = Vector2.Scale(GetComponent<BoxCollider2D>().size, transform.lossyScale);
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