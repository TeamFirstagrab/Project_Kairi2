using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 입력을 감지하고 다른 컴포넌트에서 읽을 수 있도록 제공하는 클래스입니다.
/// 유니티 6의 새로운 Input System(전역 InputSystem.actions)을 사용하여 입력을 처리합니다.
/// </summary>
public class KatanaPlayerInputReader : MonoBehaviour
{
    /// <summary>
    /// 외부 컴포넌트에서 플레이어의 현재 가로축 입력(A/D, 왼쪽/오른쪽 화살표 등)을 읽을 수 있는 읽기 전용 프로퍼티입니다.
    /// 범위는 -1.0(왼쪽)에서 1.0(오른쪽) 사이입니다.
    /// </summary>
    public float HorizontalInput { get; private set; }
    
    /// <summary>
    /// 외부 컴포넌트에서 이번 프레임에 점프 키(Space 등)가 새로 눌렸는지 여부를 확인할 수 있는 프로퍼티입니다.
    /// Update 프레임 내에서 한 번 참(true)이 된 뒤 다음 프레임에 거짓(false)이 됩니다.
    /// </summary>
    public bool JumpPressedThisFrame { get; private set; }

    // 유니티 신규 인풋 시스템의 액션 객체들을 보관하는 변수
    private InputAction moveAction;
    private InputAction jumpAction;
	private void Start()
	{
		// 유니티 6의 전역 입력 시스템(InputSystem.actions)에서 "Move"와 "Jump" 액션을 자동으로 찾아 바인딩합니다.
		moveAction = InputSystem.actions.FindAction("Move");
		jumpAction = InputSystem.actions.FindAction("Jump");
	}

	private void Update()
    {
        // 1. 좌우 이동 입력값 갱신
        // moveAction이 유효한 경우, Vector2 값 중 x축(가로 입력) 값을 가져와 HorizontalInput에 저장합니다.
        if (moveAction != null)
        {
            HorizontalInput = moveAction.ReadValue<Vector2>().x;
        }

        // 2. 점프 입력 상태 감지
        // jumpAction이 유효한 경우, 이번 프레임에 점프 버튼이 처음 눌렸는지(WasPressedThisFrame) 감지하여 반영합니다.
        if (jumpAction != null)
        {
            JumpPressedThisFrame = jumpAction.WasPressedThisFrame();
        }
    }
}

