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
	private LayerMask oneWayGroundMask;
	private Collider2D coll;

	// 바닥 체크
	public bool isGrounded { get; set; }
	public bool isGroundedOneway { get; set; }
	public bool isSlope;

	private float ungroundedTimer;
	private const float ungroundedBufferTime = 0.08f; // 미세 튕김 보정용 시간 (약 5프레임)

	public void ForceUnground()
	{
		isGrounded = false;
		ungroundedTimer = ungroundedBufferTime;
	}

	// 경사 체크
	private Vector2 slopeNormal;    // 경사면 방향 체크
	private float slopeAngle;       // 경사 각도

	public float distance;
	public float angle;

	[Header("바닥체크 감지선 길이")]
	public float checkDist = 0.25f;     // 바닥 체크 거리

	private void Awake()
	{
		coll = GetComponent<Collider2D>();
		groundMask = LayerMask.GetMask(LayerName.ground);
		oneWayGroundMask = LayerMask.GetMask(LayerName.oneWayPlatform);
	}

	public void CheckGround()
	{
		if (GlobalUtil.IsNullScript(coll))
			return;

		bool currGrounded = CheckGroundLayer(groundMask, true);

		// OneWay 플랫폼 접지 여부
		isGroundedOneway = CheckGroundLayer(oneWayGroundMask, false);

		// 일반 바닥 또는 OneWay 플랫폼에 닿아 있으면 접지 상태
		if (currGrounded || isGroundedOneway)
		{
			isGrounded = true;
			ungroundedTimer = 0f;
		}
		else
		{
			ungroundedTimer += Time.deltaTime;

			if (ungroundedTimer >= ungroundedBufferTime)
				isGrounded = false;
		}
	}

	private bool CheckGroundLayer(LayerMask mask, bool checkSlope)
	{
		float offset = 0.05f;
		float totalDistance = offset + checkDist;

		// 세 개의 범위를 나눠서 체크
		float sideMargin = 0.02f;
		Vector2 centerOrigin = new Vector2(coll.bounds.center.x, coll.bounds.min.y + offset);
		Vector2 leftOrigin = new Vector2(coll.bounds.min.x + sideMargin, coll.bounds.min.y + offset);
		Vector2 rightOrigin = new Vector2(coll.bounds.max.x - sideMargin, coll.bounds.min.y + offset);

		// 세 범위에 맞는 레이케스트 생성 (전달받은 mask로 체크)
		RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, Vector2.down, totalDistance, mask);
		RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, totalDistance, mask);
		RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, totalDistance, mask);

		// DEBUG
		Debug.DrawRay(centerOrigin, Vector2.down * totalDistance, hitCenter.collider != null ? Color.green : Color.red);
		Debug.DrawRay(leftOrigin, Vector2.down * totalDistance, hitLeft.collider != null ? Color.green : Color.red);
		Debug.DrawRay(rightOrigin, Vector2.down * totalDistance, hitRight.collider != null ? Color.green : Color.red);

		// Center -> Left -> Right 순서로 유효한 Hit 선택
		RaycastHit2D hit = hitCenter ? hitCenter : (hitLeft ? hitLeft : hitRight);

		// 땅에 닿지 않을 경우 false 리턴
		if (hit.collider == null)
		{
			if (checkSlope)
			{
				isSlope = false;
				slopeAngle = 0f;
			}

			return false;
		}

		if (checkSlope)
		{
			// 경사 정보 갱신
			slopeNormal = hit.normal;
			slopeAngle = Vector2.Angle(Vector2.up, slopeNormal);

			// 오를 수 있는 경사인지 확인
			if (slopeAngle > 0.05f && slopeAngle < maxSlopeAngle)
			{
				if (!isSlope)
				{
					Debug.Log(
						$"[PlayerGroundChecker] 경사면 감지됨! " +
						$"각도: {slopeAngle}도, 노멀: {slopeNormal}"
					);
				}

				isSlope = true;
			}
			else
			{
				isSlope = false;
				slopeAngle = 0f;
			}
		}

		// 경사면에서만 접지 판정 여유거리를 증가시킨다.
		// oneWayGroundMask(checkSlope == false)는 항상 일반 margin 사용.
		float margin;

		if (checkSlope && slopeAngle > 5f)
			margin = 0.35f;
		else
			margin = 0.15f;

		float strictLandingDist = offset + margin;

		return hit.distance <= strictLandingDist;
	}

	public bool CheckMaxSlope(float angle)
	{
		return angle <= maxSlopeAngle;
	}

	public void CheckSlope()
	{
		RaycastHit2D hit = Physics2D.Raycast(groundCheckObj.position, Vector2.down, distance, groundMask);

		angle = hit ? Vector2.Angle(hit.normal, Vector2.up) : 0f;

		Debug.DrawLine(hit.point, hit.point + hit.normal, Color.blue);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(groundCheckObj.position, checkRadius);
	}
}