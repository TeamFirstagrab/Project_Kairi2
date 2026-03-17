using UnityEngine;
using UnityEngine.InputSystem;
using EnumType;		// GlobalEnum

public class PlayerController : MonoBehaviour
{
	// 플레이어 정보
	private Rigidbody2D rigid;
	private SpriteRenderer sprite;
	private PlayerState state;		// 플레이어 상태

	// 이동값
	private Vector2 inputVec;   // 입력된 플레이어 이동값 (-1, 0, 1)
	private float speed = 10f;      // 플레이어 이동 속도

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		state = PlayerState.Idle;
	}

	private void Update()
	{
		rigid.linearVelocity = new Vector2(inputVec.x * speed, rigid.linearVelocityY);

		if (inputVec.x > 0)
			sprite.flipX = false;
		else if (inputVec.x < 0)
			sprite.flipX = true;
	}

	// InputSystem
	void OnMove(InputValue val)
	{
		inputVec = val.Get<Vector2>();
	}
}
