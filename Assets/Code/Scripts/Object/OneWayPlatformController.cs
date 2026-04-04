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
		if(collision.CompareTag(TagName.player))
			colls[0].isTrigger = true;
	}
	private void OnTriggerStay2D(Collider2D collision)
	{
		if (collision.CompareTag(TagName.player))
			colls[0].isTrigger = true;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if(collision.CompareTag(TagName.player))
			colls[0].isTrigger = false;
	}
}
