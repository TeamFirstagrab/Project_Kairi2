using Globals;
using System.Collections;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using tagName = Globals.TagName;

/// <summary>
/// Æø¹ß ¿ÀºêÁ§Æ®
/// Àû°ú Ãæµ¹ÇÏ°Å³ª TakeDamage È£Ãâ ½Ã ¹üÀ§ Æø¹ß
/// </summary>
public class ExplosionObject : BaseObject
{
	[Header("Æø¹ß ÀÌÆåÆ®")]
	public GameObject explosionEffectPrefab;

	[Header("Æø¹ß ¹üÀ§")]
	public float explosionRadius = 2f;

	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
	//  ÃÊ±âÈ­
	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

	protected override void Awake()
	{
		base.Awake();
	}

	//protected override void Start()
	//{
	//	base.Start();
	//}

	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
	//  Ãæµ¹ Ã³¸®
	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

	//protected override void OnCollisionEnter2D(Collision2D collision)
	//{
	//	base.OnCollisionEnter2D(collision);     // ¹Ù´Ú Ã¼Å©

	//	if (collision.gameObject.CompareTag(tagName.enemy) &&
	//		collision.gameObject.TryGetComponent<Enemy>(out _))
	//	{
	//		Explode();
	//	}
	//}

	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
	//  Æø¹ß ·ÎÁ÷
	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
				target.TakeDamage(1);
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

	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
	//  IDamageable
	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

	public override void TakeDamage(int attack)
	{
		Explode();
	}

	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
	//  Gizmo
	// ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, explosionRadius);
	}
}
