using UnityEngine;

/// <summary>
/// [플레이어의 멋진 조준경 UI - 마우스 추적 및 회전 제어기!]
/// 밋밋한 윈도우 마우스 포인터를 멋지게 숨기고, 
/// 마우스의 실시간 물리 위치를 정밀하게 추적하면서 
/// 360도로 회전하여 에임을 직관적으로 보여주는 멋진 특수 에임 컴포넌트입니다.
/// </summary>
public class Crosshair : MonoBehaviour
{
	[Tooltip("에임 움직임 감도 조절 필터 (현재는 기본 1배속 적용)")]
	[SerializeField] private float mouseSensitivity = 1f;

	[Header("시각적 회전 피드백")]
	[Tooltip("참(True)으로 켜두면 크로스헤어가 조준 방향 각도에 맞춰 꼬리를 휘돌리며 자동 정렬합니다.")]
	[SerializeField] private bool rotateTowardCenter = true;

	private Transform playerTrans; // 기준점이 되어 줄 우리 플레이어 몸통 좌표 저장소
	private Camera mainCam;        // 화면 좌표를 게임좌표로 바꿔줄 메인 카메라 캐싱

	private void Start()
	{
		// 1. [하드웨어 마우스 커서 숨기기]
		// 윈도우 기본 화살표 포인터가 둥둥 떠다니면 몰입감이 깨지므로 화면 상에서 안 보이게 숨깁니다.
		Cursor.visible = false;

		// 2. [마우스 가두기]
		// 마우스 포인터가 전체 게임 창 밖으로 휙 나가버리지 않도록 윈도우 창 안에 가둬둡니다.
		Cursor.lockState = CursorLockMode.Confined;

		mainCam = Camera.main;

		// 무조건 Player 태그를 가진 플레이어 본체를 찾도록 셋업합니다.
		GameObject playerObj = GameObject.FindWithTag("Player");
		if (playerObj != null)
		{
			playerTrans = playerObj.transform;
		}
	}

	private void Update()
	{
		if (playerTrans == null)
		{
			// 만약 플레이어 캐릭터가 씬 상에서 아직 부서지지 않고 살아있는지 실시간으로 재서칭하여 널 에러를 방지합니다.
			GameObject playerObj = GameObject.FindWithTag("Player");
			if (playerObj != null)
			{
				playerTrans = playerObj.transform;
			}
			return;
		}

		// 1. [마우스 스크린좌표에서 월드좌표 변환]
		// 유저가 조준한 마우스 커서 위치(픽셀)를 메인 카메라의 시야각과 깊이를 계산하여 실제 2차원 게임 월드의 위치로 환산합니다.
		// Z축 거리는 메인 카메라와 플레이어 평면 간의 거리를 동적으로 계산하여 오차를 줄입니다.
		float zDistance = Mathf.Abs(mainCam.transform.position.z - playerTrans.position.z);
		Vector3 mouseScreenPos = Input.mousePosition;
		mouseScreenPos.z = zDistance;
		Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
		mouseWorldPos.z = playerTrans.position.z; // 2D 평면상 조준을 위해 Z축 평탄화

		// 2. [플레이어와 마우스 사이의 조준선 벡터 계산]
		// 플레이어 몸통 중심점(playerTrans.position)에서 마우스 월드 좌표를 감산하여 플레이어에게서 마우스로 뻗어 나가는 조준선 벡터를 획득합니다.
		Vector3 direction = mouseWorldPos - playerTrans.position;

		// 에임 제한(aimRadius)을 해제하여 마우스 월드 좌표를 크로스헤어의 최종 목표 위치로 설정합니다.
		Vector3 targetWorldPos = mouseWorldPos;

		// 3. [하이브리드 UI/월드 위치 주입 시스템]
		// 크로스헤어가 캔버스 내부의 UI 요소(RectTransform)인지, 아니면 일반 게임 내 스프라이트(SpriteRenderer)인지 자동 감지합니다.
		// UI 요소인 경우, 스크린 스페이스(Overlay 또는 Camera) 모드에 따라 픽셀 좌표로 변환하여 배치하여 떨림이나 오차가 없게 보정합니다.
		RectTransform rectTrans = GetComponent<RectTransform>();
		if (rectTrans != null)
		{
			Canvas canvas = GetComponentInParent<Canvas>();
			if (canvas != null)
			{
				if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					// Screen Space Overlay 모드인 경우:
					// transform.position에 화면상의 픽셀 좌표를 직접 대입하면 정확하게 마킹됩니다.
					Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);
					screenPos.z = 0f;
					rectTrans.position = screenPos;
				}
				else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
				{
					// Screen Space Camera 모드인 경우:
					// 카메라 깊이 영역에 맞게 로컬 좌표계로 치환해서 RectTransform의 좌표를 조정합니다.
					Vector2 localPoint;
					Vector2 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);
					RectTransformUtility.ScreenPointToLocalPointInRectangle(
						canvas.transform as RectTransform,
						screenPos,
						canvas.worldCamera != null ? canvas.worldCamera : mainCam,
						out localPoint
					);
					rectTrans.anchoredPosition = localPoint;
				}
				else
				{
					// World Space Canvas 모드인 경우:
					// 월드 좌표계를 직접 대입하여 지정합니다.
					rectTrans.position = targetWorldPos;
				}
			}
			else
			{
				rectTrans.position = targetWorldPos;
			}
		}
		else
		{
			// UI 컴포넌트가 아닌 일반 2D 월드 스프라이트 오브젝트인 경우:
			// 월드 포지션을 다이렉트로 대입해 조준경을 매끄럽게 포지셔닝합니다.
			transform.position = targetWorldPos;
		}

		// 4. [조준경 회전 연출 적용]
		// 아크탄젠트(Atan2) 공식을 활용해 크로스헤어 자체가 캐릭터 중심에서 뻗어 나가는 사선 각도를 그대로 바라보며 
		// 360도로 매끄럽게 회전하도록 회전값을 대입합니다.
		//
		// 수학 공식 상세 설명:
		//  - Mathf.Atan2(y, x) 란?
		//    삼각함수의 역함수 중 하나인 아크탄젠트(ArcTangent) 함수입니다.
		//    가로 밑변 길이(x)와 세로 높이(y)를 이 함수에 주입하면, 두 기둥이 이루는 경사각을 라디안(Radian) 단위 숫자로 계산해서 뱉어줍니다.
		//    나눗셈 분모가 0이 되어 나누기 에러가 나는 현상(수직 정렬 시)을 수학적으로 알아서 우회 판정해 주기 때문에 에러가 발생하지 않습니다.
		//
		//  - Mathf.Rad2Deg 란?
		//    컴퓨터 삼각학에서 사용하는 각도 단위인 호도법(Radian)을 일상적인 각도(Degree, 0~360도) 단위로 바꾸어주는 비율 상수입니다.
		//    유니티 트랜스폼 회전 주입구는 도(Degree) 단위를 읽으므로, 이 변환율을 꼭 곱해 주어야 정방향으로 정렬이 됩니다.
		if (rotateTowardCenter)
		{
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, 0f, angle);
		}
	}

	private void OnDisable()
	{
		// [세심한 유저 배려 정책]
		// 게임 오버가 되거나 씬을 빠져나가 이 스크립트가 꺼질 때는, 숨겨두었던 윈도우 마우스 포인터 화살표를 
		// 다시 정상적으로 활성화하고 해제시켜 주어 윈도우 조작이 잘 되도록 원래 상태로 돌려놓습니다.
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}
}
