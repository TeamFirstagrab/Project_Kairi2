using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 🧠 [플레이어의 머리 역할 - 조종 대장!]
/// 키보드나 마우스에서 신호가 들어오면 그걸 감지해서 "움직여라!", "공격해라!" 하고
/// 실제 몸통(Movement)이나 팔(Attack)에 명령을 전달해 주는 똑똑한 비서입니다.
/// </summary>
public class Test_KatanaPlayerController : MonoBehaviour
{
    // 💡 우리 몸의 각 부분을 부려먹기 위해 연결할 하위 스크립트 연결고리입니다.
    private Test_KatanaMovement movement; // 달리기와 점프를 하는 진짜 다리/몸통 역할
    private Test_KatanaAttack attack;     // 칼을 마우스 방향으로 휘두르는 진짜 팔 역할

    private void Awake()
    {
        // 💡 게임이 처음 딱 켜지는 순간(Awake), 내 몸에 붙어있는 이동 스크립트와 공격 스크립트를 찾아서 바인딩합니다.
        movement = GetComponent<Test_KatanaMovement>();
        attack = GetComponent<Test_KatanaAttack>();
    }

    /// <summary>
    /// ⌨️ [이벤트 함수 - 키보드 AD(좌우)나 WS(위아래) 키를 누를 때 실행돼요!]
    /// </summary>
    /// <param name="value">키보드 입력 정보가 들어있는 주머니 (Vector2 x, y 좌표)</param>
    public void OnMove(InputValue value)
    {
        // 💡 value 주머니 안에서 진짜 방향 데이터(좌우 AD는 X축, 위아래 WS는 Y축)를 꺼내서 가공합니다.
        Vector2 inputVec = value.Get<Vector2>();

        if (movement != null)
        {
            // 💡 [지능형 구르기 감지 공식 - 초등학생 버전!]
            // 조건 1: 이전 프레임에 좌우 키를 안 누르고 얌전히 서 있었는가? (X축 속도가 0에 가깝니?)
            // 조건 2: 이번 프레임에 새롭게 방향키(A 혹은 D)를 딱 누르기 시작했는가? (X축 값이 생겼니?)
            // 조건 3: 유저가 엎드려 있으려고 'S(아래)' 키를 꾹 누르고 있었는가? (쪼그려 앉은 상태인가?)
            // 결론: 이 세 가지 조건이 동시에 맞으면 ➡️ "쪼그려 앉아 있다가 팍 튕겨 나가면서 구르고 싶구나!"로 인지하여 구르기를 트리거합니다!
            
            // 🔍 [함수와 물리 공식 상세 설명]
            // 1. Mathf.Abs()란?
            //  - 수학의 '절대값(Absolute Value)'을 구하는 공식 함수입니다.
            //  - 숫자가 양수(+)이든 음수(-)이든 상관없이 마이너스 부호를 떼어 버리고 무조건 0 이상의 양수로 통일해 줍니다. (예: -1도 1로 변환)
            //  - 왼쪽(A키 = -1)과 오른쪽(D키 = +1) 중 어느 쪽으로 누르든 상관없이 '이동 힘의 크기' 자체만 깔끔하게 비교하고 싶어서 사용해요!
            //
            // 2. 0.01f란?
            //  - 컴퓨터의 소수 연산 물리 오차(부동 소수점 오차) 때문에 가만히 멈춰있어도 0.000001 같은 찌꺼기 숫자가 생기는 것을 방지하는 안전지대(역치/한계값)입니다.
            //  - 계산된 절대값 크기가 이 얇은 마법 장막(0.01)보다 작다면 "사실상 멈춰서 안 움직인 상태"로 취급하고, 이 선을 넘으면 "진짜 이동 버튼을 누른 움직임 상태"라고 안심하고 판별할 수 있어요!
            //
            // 3. hadNoHorizontal (과거형: 직전 프레임까지 수평 움직임 조작 없이 가만히 멈춰있었는가?)
            //  - movement.InputVector.x(바로 직전까지 기억된 좌우 이동 정보)의 절대값이 0.01보다 작다면 ➡️ '참(true)'
            //  - 직전 프레임까지 이동하지 않고 서 있거나 쪼그려 앉아 대기하고 있었다는 확실한 수학적 보증서입니다.
            //
            // 4. hasHorizontal (현재형: 바로 지금 이 순간 새로운 수평 이동을 개시하려 방향키를 눌렀는가?)
            //  - inputVec.x(방금 막 키보드로 누른 파릇파릇한 최신 입력 정보)의 절대값이 0.01보다 크다면 ➡️ '참(true)'
            //  - 지금 당장 새로운 좌우 이동 시도가 시작되었다는 것을 감지하는 레이더 센서입니다.
            bool hadNoHorizontal = Mathf.Abs(movement.InputVector.x) < 0.01f;
            bool hasHorizontal = Mathf.Abs(inputVec.x) > 0.01f;

            if (hadNoHorizontal && hasHorizontal && movement.IsCrouchPressed)
            {
                movement.TriggerRollInput(); // 구르기 준비 땅! (무브먼트에 신호를 쏴요)
            }

            // 💡 키보드 입력축 정보를 몸통(Movement)에 전달해서 진짜 캐릭터가 수평 이동하도록 전달합니다.
            movement.SetInputVector(inputVec);
        }
    }

    /// <summary>
    /// 🚀 [이벤트 함수 - 스페이스바(점프) 키를 누르거나 뗄 때 실행돼요!]
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (movement != null)
        {
            // 💡 value.isPressed는 키를 꾹 누르고 있으면 true(참), 떼면 false(거짓)가 됩니다.
            // 이 신호를 몸통에 그대로 토스해서 가변 점프(살짝 누르면 조금 뛰고, 꾹 누르면 높게 뛰는 것)를 만듭니다.
            movement.SetJumpInput(value.isPressed);
        }
    }

    /// <summary>
    /// ⚔️ [이벤트 함수 - 마우스 왼쪽 버튼(공격)을 딸깍 클릭할 때 실행돼요!]
    /// </summary>
    public void OnAttack(InputValue value)
    {
        // 💡 마우스 클릭 신호가 참(Pressed)인 경우에만
        if (value.isPressed)
        {
            if (attack != null)
            {
                // 💡 진짜 팔(Attack) 스크립트에 "칼 휘둘러서 적을 베어버려!"라고 명령을 내립니다.
                attack.TryAttack();
            }
        }
    }

    /// <summary>
    /// 🛡️ [컴포넌트 간의 대화 창구 - 플레이어가 지금 공격 모션 중인지 물어봐요]
    /// </summary>
    /// <returns>공격을 하는 중이라면 true(참), 아니면 false(거짓)</returns>
    public bool IsAttacking()
    {
        if (attack != null)
        {
            return attack.IsAttacking; // 팔에 물어봐서 현재 베고 있는 상태인지 대답해 줍니다.
        }
        return false;
    }

    /// <summary>
    /// 🙇‍♂️ [이벤트 함수 - 키보드 S 키(쪼그려 앉기)를 누르거나 뗄 때 실행돼요!]
    /// </summary>
    public void OnCrouch(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (movement != null)
        {
            // 💡 S키 입력 상태(누름/뗌)를 몸통에 토스하여 캐릭터가 바닥에 수그리거나 일어나도록 명령합니다.
            movement.SetCrouchInput(isPressed);
        }
    }

    /// <summary>
    /// 🙇‍♂️ [세이프티 이벤트 함수 - S 키를 확실하게 뗐을 때의 안전장치!]
    /// </summary>
    public void OnReleaseCrouch(InputValue value)
    {
        if (movement != null)
        {
            // 💡 키를 확실히 뗐으므로 몸통에 "이제 쪼그려 앉지 말고 일어서!"(false)라고 쏴줍니다.
            movement.SetCrouchInput(false);
        }
    }
}
