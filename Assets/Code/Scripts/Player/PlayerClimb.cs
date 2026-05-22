using Globals;
using UnityEngine;

public class PlayerClimb : MonoBehaviour
{
	private Rigidbody2D rigid;
	private Animator animator;
	private PlayerStatsRuntime playerStats;
	private PlayerMovement movement;
	private PlayerGroundChecker groundChk;

	// 벽 체크
	[Header("벽 체크")]
	public Transform wallChk;
	public bool isWall;
	[Header("체크 레이어")]		// TODO: Globals에 넣기
	public LayerMask w_Layer;
	public bool isWallJump;		// 벽에서 점프 중인지 여부

	private float isRight = 1;  // 바라보는 방향 (1: 오른쪽, -1: 왼쪽)

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		movement = GetComponent<PlayerMovement>();
        groundChk = GetComponent<PlayerGroundChecker>();

    }

    private void Update()
	{
		playerStats = GameManager.Instance.playerStatsRuntime;

		if (movement.inputVec.x > 0)
			isRight = 1;
		else if (movement.inputVec.x < 0)
			isRight = -1;

        isWall = Physics2D.Raycast(
			wallChk.position,
			Vector2.right * isRight,
			playerStats.wallChkDist, 
			w_Layer
		);
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
