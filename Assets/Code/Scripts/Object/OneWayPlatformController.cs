using UnityEngine;

using Globals;

public class OneWayPlatformController : MonoBehaviour
{
	private Collider2D[] colls;

	private void Awake()
	{
		colls = GetComponents<Collider2D>();
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag(TagName.player))
		{
			Physics2D.IgnoreCollision(collision, colls[0], true);
		}
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		print($"[OneWayPlatform LOG]: Trigger Exit");
		if (collision.CompareTag(TagName.player))
		{
			Physics2D.IgnoreCollision(collision, colls[0], false);
		}
	}

	public void SetIgnoreCollisionForPlayer(Collider2D playerCollider)
	{
		StartCoroutine(IgnoreRoutine(playerCollider));
	}

	private System.Collections.IEnumerator IgnoreRoutine(Collider2D playerCollider)
	{
		Physics2D.IgnoreCollision(playerCollider, colls[0], true);
		yield return new WaitForSeconds(0.5f);
		if (playerCollider != null)
		{
			Physics2D.IgnoreCollision(playerCollider, colls[0], false);
		}
	}
}
