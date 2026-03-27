using UnityEngine;
using Globals;
using System.Collections;

public class ExplosionObject : MonoBehaviour, IInteractionObject
{
	[Header("Æø¹ß ÀÌÆåÆ® ÇÁ¸®Æé")]
	public GameObject explosionEffectPrefab;
	[Header("Æø¹ß ¹üÀ§")]
	public float explosionRadius = 2f;
	public bool isExplosion = false;  // Æø¹ß ¿©ºÎ

	public void OnInteract()
	{
		if (isExplosion) Explode();
	}

	private void Explode()
	{
		//GameManager.Instance.audioManager.ObjectExplosionSound(1f);
		//GameManager.Instance.cameraShake.ShakeForSeconds(1f);
		Vector2 explosionPos = transform.position;  // ÅÍÁö´Â À§Ä¡
		Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);

		foreach (var hit in hits)
		{
			if (hit.CompareTag(TagName.enemy) || hit.CompareTag(TagName.throwingEnemy))
			{
				if (hit.TryGetComponent<Enemy>(out var target))
				{
					//Vector2 hitDir = (target.transform.position - transform.position).normalized;
					//target.SetHitDirection(hitDir);
					target.TakeDamage(1);
				}
			}
		}
		StartCoroutine(SpawnExplosionEffect(explosionPos));
		//ownerSpawner?.OnObjectDestroyed(this);
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		if(!collision.transform.CompareTag(TagName.player) 
			&& !collision.transform.CompareTag(TagName.ground))
		{
			isExplosion = true;
		}
	}


	/// <summary>
	/// Coroutine
	/// </summary>
	IEnumerator SpawnExplosionEffect(Vector2 position)
	{
		GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
		yield return new WaitForSeconds(1.07f);
		Destroy(effect);
	}
}
