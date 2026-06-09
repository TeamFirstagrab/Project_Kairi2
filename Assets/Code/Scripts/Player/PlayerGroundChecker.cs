using Globals;
using UnityEngine;

public class PlayerGroundChecker : MonoBehaviour
{
	[SerializeField] private Transform groundCheckObj;

	[Header("경사 체크")]
	[Tooltip("플레이어가 오를 수 있는 언덕의 최대 각도")]
	[SerializeField] private float maxSlopeAngle = 60f;

	public float checkRadius = 0.1f;
	private LayerMask groundMask;
	private LayerMask oneWayPlatformMask;
	private Collider2D coll;

	// 바닥 체크
	public bool isGrounded { get; set; }
	public bool isGroundedOneway = false;
	public bool isSlope;

	// 경사 체크
	private Vector2 slopeNormal;    // 경사면 방향 체크
	private float slopeAngle;       // 경사 각도

	public float distance;
	public float angle;

	[Header("바닥체크 감지선 길이")]
	public float checkDist = 0.25f;		// 바닥 체크 거리

	private void Awake()
    {
		coll = GetComponent<Collider2D>();
		groundMask = LayerMask.GetMask(LayerName.ground);
		oneWayPlatformMask = LayerMask.GetMask(LayerName.oneWayPlatform);
	}

	public void CheckGround()
	{
		//isGrounded = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, groundMask);
		//isGroundedOneway = Physics2D.OverlapCircle(groundCheckObj.position, checkRadius, oneWayPlatformMask);

		if(GlobalUtil.IsNullScript(coll)) return;

		float offset = 0.05f;
		float totalDistance = offset + checkDist;

		// 세 개의 범위를 나눠서 체크
		float sideMargin = 0.02f;
		Vector2 centerOrigin = new Vector2(coll.bounds.center.x, coll.bounds.min.y + offset);
		Vector2 leftOrigin = new Vector2(coll.bounds.min.x + sideMargin, coll.bounds.min.y + offset);
		Vector2 rightOrigin = new Vector2(coll.bounds.max.x + sideMargin, coll.bounds.min.y + offset);

		// 세 범위에 맞는 레이케스트 생성
		RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, Vector2.down, totalDistance, groundMask);
		RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, totalDistance, groundMask);
		RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, totalDistance, groundMask);

		// DEBUG: 레이케스트 확인용
		Debug.DrawRay(centerOrigin, Vector2.down * totalDistance, hitCenter.collider != null ? Color.green : Color.red);
		Debug.DrawRay(leftOrigin, Vector2.down * totalDistance, hitLeft.collider != null ? Color.green : Color.red);
		Debug.DrawRay(rightOrigin, Vector2.down * totalDistance, hitRight.collider != null ? Color.green : Color.red);

		RaycastHit2D hit = hitCenter ? hitCenter : (hitLeft ? hitLeft : hitRight);

		bool currGrounded = false;
		if (hit)
		{
			slopeNormal = hit.normal;
			slopeAngle = Vector2.Angle(Vector2.up, slopeNormal);

			// 사선 빗면 틈새 공간(slopeAngle > 5)에서는 순간적으로 붕 떠서 접지 판정이 풀려 덜덜거리지 않게 
			// 감지선 거리를 0.35m로 넓혀주고, 평지에서는 0.15m로 엄밀하게 좁혀 물리 오차를 이중 스케일링
			float margin = (slopeAngle > 5f) ? 0.35f : 0.15f;
			float strictLandingDist = offset + margin;

			if (hit.distance <= strictLandingDist)
			{
				currGrounded = true;
			}

			// 오를 수 있는 경사면이라면 slope를 참으로
			if(slopeAngle > 0.05f && slopeAngle < maxSlopeAngle)
			{
				isSlope = true;
			}
			else
			{
				isSlope = false;
				slopeAngle = 0;		// 경사 각도 초기화
			}
		}
		else
		{
			isSlope = false;
			slopeAngle = 0;     // 경사 각도 초기화
		}

		if(isGrounded != currGrounded)
		{
			isGrounded = currGrounded;
		}
	}

	public bool CheckMaxSlope(float angle)
	{
		return angle < maxSlopeAngle;
	}

	public void CheckSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheckObj.position, Vector2.down, distance, groundMask);

		angle = Vector2.Angle(hit.normal, Vector2.up);

		Debug.DrawLine(hit.point, hit.point + hit.normal, Color.blue);
    }

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(groundCheckObj.position, checkRadius);
	}
}