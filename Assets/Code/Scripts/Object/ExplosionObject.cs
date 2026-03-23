using Globals;
using UnityEngine;

public class ExplosionObject : MonoBehaviour
{
	[Header("Æø¹ß ÀÌÆåÆ® ÇÁ¸®Æé")]
	public GameObject explosionEffectPrefeb;
	[Header("Æø¹ß ¹üÀ§")]
	public float explosionRadius = 2f;

	private void Explode()  // Æø¹ß
	{
		Vector2 explosionPos = transform.position;
		Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);

		foreach(var hit in hits)
		{
			if(hit.CompareTag(TagName.enemy))
			{
				if(hit.TryGetComponent<Enemy>(out var target))
				{

				}
			}
		}
	}
}
