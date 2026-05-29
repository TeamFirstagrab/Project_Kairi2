using UnityEngine;

/// <summary>
/// 🎯 [플레이어의 멋진 조준경 UI - 마우스 가두기 및 회전 제어기!]
/// 밋밋한 윈도우 마우스 포인터를 멋지게 숨기고, 
/// 캐릭터 주변의 둥근 조준 반경 밖으로 나가지 못하게 '자석 가두기' 락을 건 채 
/// 360도로 회전하며 에임을 직관적으로 보여주는 멋진 특수 에임 컴포넌트입니다.
/// </summary>
public class Test_KatanaCrosshair : MonoBehaviour
{
    [Header("에임 제한 설정")]
    [Tooltip("크로스헤어가 플레이어 캐릭터 주변에서 맴돌 수 있는 최대 둥근 가두기 반경 거리 (미터)")]
    [SerializeField] private float aimRadius = 3f;

    [Tooltip("에임 움직임 감도 조절 필터 (현재는 기본 1배속 적용)")]
    [SerializeField] private float mouseSensitivity = 1f;

    [Header("시각적 회전 피드백")]
    [Tooltip("참(True)으로 켜두면 크로스헤어가 조준 방향 각도에 맞춰 꼬리를 휘돌리며 자동 정렬합니다.")]
    [SerializeField] private bool rotateTowardCenter = true;

    private Transform playerTrans; // 기준점이 되어 줄 우리 플레이어 몸통 좌표 저장소
    private Camera mainCam;        // 화면 좌표를 게임좌표로 바꿔줄 메인 카메라 캐싱

    private void Start()
    {
        // 💡 1. [하드웨어 마우스 커서 숨기기]
        // 윈도우 기본 화살표 포인터가 둥둥 떠다니면 몰입감이 깨지므로 화면 상에서 안 보이게 숨깁니다.
        Cursor.visible = false;
        
        // 💡 2. [마우스 가두기]
        // 마우스 포인터가 전체 게임 창 밖으로 휙 나가버리지 않도록 윈도우 창 안에 가둬둡니다.
        Cursor.lockState = CursorLockMode.Confined;

        mainCam = Camera.main;
        
        // 💡 3. [플레이어 추적 장치]
        // 크로스헤어가 내 캐릭터 머리 위에 잘 붙을 수 있도록 부모 객체나 씬 안에서 "Player" 태그를 가진 진짜 플레이어의 뼈대를 찾습니다.
        if (transform.parent != null)
        {
            playerTrans = transform.parent;
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTrans = playerObj.transform;
            }
        }


    }

    private void Update()
    {
        if (playerTrans == null)
        {
            // 만약 플레이어 캐릭터가 씬 상에서 아직 부서지지 않고 살아있는지 실시간으로 재서칭하여 널 에러를 물리 치료합니다.
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTrans = playerObj.transform;
            }
            return;
        }

        // 💡 1. [마우스 스크린좌표 ➡️ 월드좌표 낚아채기]
        // 마우스의 윈도우 모니터 상의 픽셀 좌표를 게임 월드 속 진짜 2D 위치 좌표로 번역합니다.
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // 2D 평면이므로 Z축 깊이는 완전히 소거

        // 💡 2. [플레이어와 마우스 사이의 거리 및 사선 계산]
        // 플레이어 몸통 중심에서 마우스가 위치한 곳까지 길게 뻗어 나가는 방향 벡터(direction)와 그 거리 길이(distance)를 연산합니다.
        Vector3 direction = mouseWorldPos - playerTrans.position;

        // 🔍 [수학 공식 상세 설명]
        // 1. direction.magnitude 란?
        //  - 중학교 수학 시간에 배우는 '피타고라스 정리'(가로제곱 + 세로제곱 = 빗변제곱)를 2차원/3차원 공간 좌표계 상에서 편하게 계산해 주는 기능입니다.
        //  - 가로(X축) 거리와 세로(Y축) 거리를 각각 곱해서 더한 뒤 루트(Square Root)를 씌워 "플레이어 중심에서 내 마우스 포인터까지의 진짜 대각선 직선거리(m)"를 실수 숫자로 구합니다.
        float distance = direction.magnitude;

        // 💡 3. [조준 반경 가두기 (Vector Distance Clamp) - 최고 중요!]
        // 만약 마우스 거리가 우리가 설정해 둔 최대 반경(aimRadius: 3미터)을 초과해서 저 멀리 도망가려 한다면 ➡️
        // 마우스 조준 사선 방향은 그대로 유지한 채, 벡터의 길이를 3미터 크기로 싹둑 잘라 고정시킵니다.
        // 이 덕분에 마우스가 화면 구석 끝에 가도 크로스헤어는 항상 플레이어 조준 반경 3미터 테두리에 착 달라붙어 가둡니다!

        // 🔍 [수학 공식 상세 설명]
        // 2. direction.normalized 란?
        //  - 벡터의 '정규화(Normalization)'라고 부릅니다.
        //  - 실제 빗변 대각선 거리가 5미터이든 10미터이든 상관없이, 조준하고 있는 방향 비율은 그대로 보존하되 "순수 빗변 대각선 길이만 정확히 1미터"짜리로 작게 압축 축소시킵니다.
        //  - 여기에 우리가 가둘 반경인 aimRadius(3미터)를 곱해 주면, 마우스가 도망친 방향을 정확하게 가리키는 예쁜 3미터 길이의 가두기용 조준선 벡터가 완성됩니다!
        if (distance > aimRadius)
        {
            direction = direction.normalized * aimRadius;
        }

        // 💡 4. 최종 고정 및 가두기가 완료된 좌표를 조준경 UI의 위치로 주입합니다.
        transform.position = playerTrans.position + direction;

        // 💡 5. [조준경 회전 연출 적용]
        // 아크탄젠트(Atan2) 공식을 활용해 크로스헤어 자체가 캐릭터 중심에서 뻗어 나가는 사선 각도를 그대로 바라보며 
        // 360도로 매끄럽게 회전하도록 Quaternion 회전각을 주입합니다.

        // 🔍 [수학 공식 상세 설명]
        // 3. Mathf.Atan2(y, x) 란?
        //  - 삼각함수의 역함수 중 하나인 '아크탄젠트(ArcTangent)' 함수입니다.
        //  - 가로 밑변 길이(x)와 세로 높이(y)를 이 함수에 주입하면, 두 기둥이 이루는 경사각을 라디안(Radian) 단위 숫자로 계산해서 똑똑하게 뱉어줍니다.
        //  - 일반 Atan() 함수와 달리 나눗셈 분모가 0이 되어 부러지는 현상(예: 수직선)을 수학적으로 알아서 우회 판정해 주기 때문에 크래시(에러)가 전혀 나지 않는 안전한 전천후 회전각 탐색기입니다.
        //
        // 4. Mathf.Rad2Deg 란?
        //  - 컴퓨터 삼각학에서 사용하는 각도 단위인 '호도법(Radian, π 파이 기준)'을 우리가 일상에서 흔히 쓰는 '도(Degree, 0도~360도)' 단위로 교환해 주는 비율 상수(약 57.2958)입니다.
        //  - 유니티 트랜스폼 회전(Rotation) 주입구는 호도법이 아니라 도(Degree) 단위를 읽으므로, 이 변환율을 꼭 곱해 주어야 회전각이 엉뚱한 방향으로 돌지 않고 정방향 정렬이 됩니다!
        if (rotateTowardCenter)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnDisable()
    {
        // 💡 [세심한 유저 배려 정책]
        // 게임 오버가 되거나 씬을 빠져나가 이 스크립트가 꺼질 때는, 숨겨두었던 윈도우 마우스 포인터 화살표를 
        // 다시 정상적으로 활성화하고 해제시켜 주어 윈도우 조작이 잘 되도록 원래 상태로 친절하게 돌려놓습니다.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
