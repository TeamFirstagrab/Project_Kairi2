// PlayerController.cs
using Globals;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	private Rigidbody2D rigid;
	private PlayerMovement movement;
	private PlayerAttack attack;
	private PlayerGroundChecker groundChecker;
	private PlayerSlowMode slowMode;
	private PlayerSkillAttack skillAttack;
	private float originalGravity;

	private Collision2D collidedObj;     // 플레이어와 상호작용 할 오브젝트

	// 1. 클래스 상단 변수 선언부에 추가
	private PlayerThrow throwModule;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		movement = GetComponent<PlayerMovement>();
		attack = GetComponent<PlayerAttack>();
		slowMode = GetComponent<PlayerSlowMode>();
		groundChecker = GetComponent<PlayerGroundChecker>();
		skillAttack = GetComponent<PlayerSkillAttack>();

		throwModule = GetComponent<PlayerThrow>();
	}

	private void Update()
	{
		// 문 상호작용
		if (collidedObj != null)
		{
			if (collidedObj.transform.TryGetComponent(out DoorController door))
			{
				if (movement.inputVec.x != 0)
				{
					print($"Try Open");
					if (door.TryOpen())     // 문 열기 시도해서 성공할 경우 오브젝트 없애기
					{
						collidedObj = null;
					}
				}
			}
		}
	}

	private void Start()
	{
		originalGravity = rigid.gravityScale;
	}

	private void OnMove(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;

		Vector2 inputVec = val.Get<Vector2>();

		bool hadNoHorizontal = Mathf.Abs(movement.inputVec.x) < 0.01f;
		bool hasHorizontal = Mathf.Abs(inputVec.x) > 0.01f;

		// 구르기
		if (hadNoHorizontal && hasHorizontal && movement.isCrouchPressed)
		{
			movement.TriggerRollInput();
		}

		movement.inputVec = inputVec;
	}

	private void OnJump(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;

		movement.SetJumpInput(val.isPressed);
	}

	private void OnCrouch(InputValue val)
	{
		if (GlobalUtil.IsNullScript(movement)) return;

		if (val.isPressed)
		{
			movement.SetCrouchInput(val.isPressed);
			movement.TriggerRollInput();
		}
		else
		{
			movement.SetCrouchInput(false);
		}
	}

	private void OnAttack(InputValue val)
	{
		if (GlobalUtil.IsNullScript(attack)) return;
		if (val.isPressed)
		{
			rigid.gravityScale = 1f;
			attack.TryAttack();
			rigid.gravityScale = originalGravity;
		}
	}

	// 3. 기존 OnSkillAttack 메서드를 아래와 같이 수정
	private void OnSkillAttack(InputValue val)
	{
		// [우선순위 1] 플레이어가 아이템을 들고 있거나, 발 밑에 주울 수 있는 아이템이 있는 경우
		if (throwModule != null && (throwModule.HasItem() || throwModule.HasNearbyPickup()))
		{
			// 줍기/던지기는 누르는 시점(Down)에 한 번만 즉시 처리되도록 합니다.
			if (val.isPressed)
			{
				throwModule.ExecuteThrowAction();
			}
			return; // 기존 스킬이 발동하지 않도록 리턴 처리
		}
		// [우선순위 2] 들고 있는 아이템이나 주울 아이템이 없으면 기존 스킬 정상 발동
		if (val.isPressed)
		{
			skillAttack.EnterSkill();
		}
		else
		{
			skillAttack.ExitSkill();
		}
	}

	private void OnSlow(InputValue val)
	{
		if (GameManager.Instance.playerStatsRuntime.currentHP <= 0)
			return;
		if (val.isPressed)
		{
			// 쿨타임 중이면 아예 슬로우 시도를 막음
			if (slowMode.IsCooldown) return;
			slowMode.EnterSlow();
		}
		else
		{
			// 현재 슬로우 모드가 켜진 상태였을 때만 떼기(종료) 처리 작동
			slowMode.ExitSlow();
		}
	}
}