using Globals;
using System.Collections;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using tagName = Globals.TagName;

/// <summary>
/// ���� ������Ʈ
/// ���� �浹�ϰų� TakeDamage ȣ�� �� ���� ����
/// </summary>
public class ExplosionObject : BaseObject
{
	[Header("���� ����Ʈ")]
	public GameObject explosionEffectPrefab;

	[Header("���� ����")]
	public float explosionRadius = 2f;

	// ������������������������������������������������������������������������������������
	//  �ʱ�ȭ
	// ������������������������������������������������������������������������������������

	protected override void Awake()
	{
		base.Awake();
	}

	//protected override void Start()
	//{
	//	base.Start();
	//}

	// ������������������������������������������������������������������������������������
	//  �浹 ó��
	// ������������������������������������������������������������������������������������

	//protected override void OnCollisionEnter2D(Collision2D collision)
	//{
	//	base.OnCollisionEnter2D(collision);     // �ٴ� üũ

	//	if (collision.gameObject.CompareTag(tagName.enemy) &&
	//		collision.gameObject.TryGetComponent<Enemy>(out _))
	//	{
	//		Explode();
	//	}
	//}

	// ������������������������������������������������������������������������������������
	//  ���� ����
	// ������������������������������������������������������������������������������������

	public void Explode()
	{
		//GameManager.Instance.audioManager.ObjectExplosionSound(1f);
		//GameManager.Instance.cameraShake.ShakeForSeconds(1f);

		Vector2 explosionPos = transform.position;
		Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);

		foreach (var hit in hits)
		{
			if (hit.CompareTag(tagName.enemy) &&
				hit.TryGetComponent<Enemy>(out var target))
			{
				target.TakeDamage(1, (target.transform.position - transform.position).normalized);
			}
		}

		GameManager.Instance.StartCoroutine(SpawnExplosionEffect(explosionPos));
		GameManager.Instance.poolManager.ReturnToPool(gameObject);
	}

	private IEnumerator SpawnExplosionEffect(Vector2 position)
	{
		GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
		yield return new WaitForSeconds(1.07f);
		Destroy(effect);
	}

	// ������������������������������������������������������������������������������������
	//  Gizmo
	// ������������������������������������������������������������������������������������

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, explosionRadius);
	}
}
