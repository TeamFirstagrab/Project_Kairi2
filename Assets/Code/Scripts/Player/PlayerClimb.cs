using Globals;
using UnityEngine;

public class PlayerClimb : MonoBehaviour
{
	private Rigidbody2D rigid;
	private Collider2D coll;
	private Animator animator;
	private PlayerStatsRuntime playerStats;
	private PlayerMovement movement;
	private PlayerGroundChecker groundChk;

	[Header("벽점프 & 벽슬라이딩 설정")]
	[Tooltip("벽을 발로 차고 점프할 때 밀어내는 폭발적인 탄력 힘 (X축: 벽 반대로 튕겨나감, Y축: 솟구쳐 오름)")]
	[SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);
	[Tooltip("벽점프 발동 직후, 아주 미세한 순간 동안 유저의 좌우 조작(A/D)을 임시 격리하여 반동 튕김이 가로막히지 않게 잠그는 프레임 시간")]
	[SerializeField] private float wallJumpDuration = 0.15f;
	[Tooltip("벽과의 거리를 정밀하게 낚아챌 둥근 레이저 사거리 길이")]
	[SerializeField] private float wallCheckDistance = 0.5f;

	// 벽 체크
	[Header("벽 체크")]
	public Transform wallChk;
	public bool isWall;
	[Header("체크 레이어")]      // TODO: Globals에 넣기
	[SerializeField] private LayerMask w_Layer;
	[SerializeField] private bool isWallJump;		// 벽에서 점프 중인지 여부

	private float isRight = 1;  // 바라보는 방향 (1: 오른쪽, -1: 왼쪽)
	public bool isWallLeft;
	public bool isWallRight;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		coll = GetComponent<Collider2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
        groundChk = GetComponent<PlayerGroundChecker>();
    }

    private void Update()
	{
		playerStats = GameManager.Instance.playerStatsRuntime;

		CheckWall();

		bool isCurrWall = isWallLeft || isWallRight;    // 현재 붙어있는 벽 위치 (왼, 오른)
		bool isGrounded = groundChk ? groundChk.isGrounded : false;

		//if (movement.inputVec.x > 0)
		//	isRight = 1;
		//else if (movement.inputVec.x < 0)
		//	isRight = -1;

		//  isWall = Physics2D.Raycast(
		//	wallChk.position,
		//	Vector2.right * isRight,
		//	playerStats.wallChkDist, 
		//	w_Layer
		//);
	}

	private void CheckWall() // 벽 체크
	{
		if (!coll) return;

		float offsetY = 0.2f;	// y축 보정값
		float offsetX = 0.05f;  // x축 보정값
		float dist = offsetX + wallCheckDistance;

		// 좌측 하단, 우측 하단에서 각 레이저 발사
		Vector2 leftVec2 = new Vector2(coll.bounds.min.x + offsetX, coll.bounds.min.y + offsetY);
		Vector2 rightVec2 = new Vector2(coll.bounds.max.x - offsetX, coll.bounds.min.y + offsetY);

		// 좌우 레이캐스트 생성
		RaycastHit2D hitLeft = Physics2D.Raycast(leftVec2, Vector2.left, dist, w_Layer);
		RaycastHit2D hitRight = Physics2D.Raycast(rightVec2, Vector2.right, dist, w_Layer);

		// DEBUG: 레이저 표시
		Debug.DrawRay(leftVec2, Vector2.left * dist, hitLeft.collider != null ? Color.blue : Color.yellow);
		Debug.DrawRay(rightVec2, Vector2.right * dist, hitRight.collider != null ? Color.blue : Color.yellow);

		// 벽에 닿았을 경우 처리
		isWallLeft = hitLeft.collider != null && hitLeft.collider.gameObject != gameObject;
		isWallRight = hitRight.collider != null && hitRight.collider.gameObject != gameObject;
	}


    public void WallJump()
	{
		isWallJump = true;
		Invoke("FreezeX", 0.3f);	// 0.3초 후에 FreezeX 함수 실행

		PlayerStatsRuntime stats = GameManager.Instance.playerStatsRuntime;
        isRight *= -1;  // 방향 전환
		rigid.linearVelocity = new Vector2(isRight * stats.wallJumpPower, 0.5f * stats.wallJumpPower);
		movement.UpdateSprite(new Vector2(isRight, 0));
		movement.inputVec = new Vector2(isRight, 0);
    }

	private void FreezeX()
	{
		isWallJump = false;
		movement.inputVec = Vector2.zero;
	}

	private void FixedUpdate()
	{
		if(isWall && !groundChk.isGrounded)
		//if (isWall && !isWallJump)
		{
			isWallJump = false;

			animator.Play(PlayerAnimName.climbSlide);
			rigid.linearVelocity = new Vector2(
				rigid.linearVelocityX,
				rigid.linearVelocityY * playerStats.climbSlidingSpeed
			);
		}
    }

	private void OnDrawGizmos()
	{
		if(wallChk != null)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(
				wallChk.position, 
				Vector2.right * isRight * playerStats.wallChkDist
			);
		}
	}
}
