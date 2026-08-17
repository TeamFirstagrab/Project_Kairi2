using UnityEngine;

/// <summary>
/// 플레이어의 좌우에 벽(Wall)이 닿아 있는지 감지하는 컴포넌트입니다.
/// 캐릭터 콜라이더의 좌측과 우측에 BoxCast를 쏘아 벽과의 충돌을 검출합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerWallDetector : MonoBehaviour
{
	[Header("벽 감지 레이어")]
	[Tooltip("벽으로 판정할 레이어(LayerMask)를 지정합니다.")]
	[SerializeField] private LayerMask wallLayer;

	[Header("벽 감지 오프셋")]
	[Tooltip("캐릭터 콜라이더 옆면으로부터 얼마나 떨어진 곳까지 벽으로 감지할지 결정합니다.")]
	[SerializeField] private float wallCheckDistance = 0.3f; // 사용자의 0.3 설정값 반영

	[Header("전방 벽 감지")]
	[Tooltip("기존 벽 감지보다 조금 더 먼 거리까지 검사합니다.")]
	[SerializeField] private float wallAheadCheckDistance = 0.6f;

	// 플레이어의 콜라이더 레퍼런스 (크기 및 범위 측정을 위해 사용)
	private Collider2D playerCollider;

	// 벽 감지 변수 (public)
	// 현재 벽에 접촉해 있는지 여부를 나타내는 프로퍼티
	public bool IsTouchingWall { get; private set; }

	// 접촉해 있는 벽의 방향 (1.0f = 오른쪽 벽, -1.0f = 왼쪽 벽, 0.0f = 접촉 없음)
	public float WallDirection { get; private set; }

	// 조금 더 먼 거리 내에 벽이 있는지 여부
	public bool IsWallAhead { get; private set; }

	// 먼 거리에서 감지된 벽 방향 (1 = 오른쪽, -1 = 왼쪽, 0 = 없음)
	public float WallAheadDirection { get; private set; }

	private void Awake()
	{
		playerCollider = GetComponent<Collider2D>();
	}

	private void Update()
	{
		// 매 프레임마다 좌우의 벽 유무를 물리 엔진으로 검사합니다.
		IsTouchingWall = (WallAheadDirection = CheckWall(wallCheckDistance)) != 0;
		IsWallAhead = (WallDirection = CheckWall(wallAheadCheckDistance)) != 0;
	}

	/// <summary>
	/// Physics2D.BoxCast를 좌측 및 우측으로 각각 발사하여 벽과의 충돌 및 그 방향을 감지합니다.
	/// </summary>
	private float CheckWall(float checkDist)
	{
		Bounds bounds = playerCollider.bounds;
		Vector2 checkSize = new Vector2(0.1f, bounds.size.y * 0.8f);

		// 우측 벽 감지
		// 콜라이더 중심에서 우측으로 (가로 폭의 절반 + 감지 오프셋) 거리만큼 쏩니다.
		// 이때 위아래 구석 부분에서 오작동하는 것을 방지하기 위해 높이를 콜라이더 실측 높이의 80%로 축소하여 검사합니다.
		RaycastHit2D rightHit = Physics2D.BoxCast(
			bounds.center,                              // 발사 시작 위치 (콜라이더 중심)
			checkSize,   // 캐스팅할 박스의 크기 (가로는 매우 좁게, 세로는 콜라이더 높이의 80%)
			0f,                                         // 회전 각도 (회전 없음)
			Vector2.right,                              // 발사 방향 (우측)
			(bounds.size.x / 2f) + checkDist,   // 검사 거리 (콜라이더 반경 + 오프셋)
			wallLayer                                   // 충돌을 감지할 레이어
		);

		// 좌측 벽 감지
		// 콜라이더 중심에서 좌측으로 동일한 사양의 박스캐스트를 쏩니다.
		RaycastHit2D leftHit = Physics2D.BoxCast(
			bounds.center,
			checkSize,
			0f,
			Vector2.left,
			(bounds.size.x / 2f) + checkDist,
			wallLayer
		);

		// 결과 판정
		if (rightHit.collider != null)
		{
			IsTouchingWall = true;
			return 1f; // 우측 벽
		}
		else if (leftHit.collider != null)
		{
			IsTouchingWall = true;
			return -1f; // 좌측 벽
		}
		else
		{
			IsTouchingWall = false;
			return 0f; // 벽 없음
		}
	}

	/// <summary>
	/// 씬(Scene) 뷰에서 좌우 벽 감지 영역을 기즈모로 시각화합니다.
	/// </summary>
	private void OnDrawGizmos()
	{
		if (playerCollider == null)
		{
			playerCollider = GetComponent<Collider2D>();
			if (playerCollider == null) return;
		}

		Bounds bounds = playerCollider.bounds;
		Vector2 checkSize = new Vector2(0.1f, bounds.size.y * 0.8f);

		// 벽 접촉 상태에 따라 노란색(접촉됨) 또는 회색(비접촉)으로 표시합니다.
		Gizmos.color = IsTouchingWall ? Color.yellow : Color.gray;

		// 우측 감지 영역 기즈모 그리기
		Vector3 rightPos = bounds.center + Vector3.right * ((bounds.size.x / 2f) + wallCheckDistance);
		Gizmos.DrawWireCube(rightPos, checkSize);

		// 좌측 감지 영역 기즈모 그리기
		Vector3 leftPos = bounds.center + Vector3.left * ((bounds.size.x / 2f) + wallCheckDistance);
		Gizmos.DrawWireCube(leftPos, checkSize);
	}
}