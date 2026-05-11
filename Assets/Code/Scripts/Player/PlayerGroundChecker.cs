using Globals;
using UnityEngine;

public class PlayerGroundChecker : MonoBehaviour
{
	[SerializeField] private Transform groundCheckObj;
	public float checkRadius = 0.1f;
	private LayerMask oneWayPlatformMask;

	public bool isGrounded = true;
	public bool isGroundedSpecial = false;

	private void Awake()
	{
		oneWayPlatformMask = LayerMask.GetMask(TagName.oneWayPlatform);
	}

	public void Check()
	{
		isGrounded = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius);
		isGroundedSpecial = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, oneWayPlatformMask);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(groundCheckObj.position, checkRadius);
	}
}