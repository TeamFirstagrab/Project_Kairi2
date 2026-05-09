using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[SerializeField] float acceleration = 15f;
	[SerializeField] float deceleration = 10f;

	private Rigidbody2D rigid;
	private float maxSpeed;

	public Vector2 inputVec;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
	}

	public void Init()
	{
		maxSpeed = GameManager.Instance.playerStatsRuntime.speed;
	}

	public void HandleMovement()
	{
		float targetSpeed = inputVec.x * maxSpeed;
		float speedDiff = targetSpeed - rigid.linearVelocity.x;
		float accelRate = (Mathf.Abs(inputVec.x) > 0.01f) ? acceleration : deceleration;

		rigid.AddForce(Vector2.right * speedDiff * accelRate);
		rigid.linearVelocity = new Vector2(
			Mathf.Clamp(rigid.linearVelocity.x, -maxSpeed, maxSpeed),
			rigid.linearVelocity.y);
	}

	public void UpdateSprite()
	{
		if (inputVec.x > 0) transform.eulerAngles = Vector3.zero;
		else if (inputVec.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
	}

	public void UpdateSprite(Vector2 dir)
	{
		if (dir.x > 0) transform.eulerAngles = Vector3.zero;
		else if (dir.x < 0) transform.eulerAngles = new Vector3(0f, 180f, 0f);
	}
}