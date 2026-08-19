using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
	private Animator animator;

	// afterTouchDuration이 지난 후 true
	private bool canOpen = false;

	[Tooltip("첫 번째 상호작용 후, 이 시간이 지나면 두 번째 상호작용으로 문을 열 수 있음")]
	[SerializeField] private float afterTouchDuration = 0.5f;

	[Tooltip("문을 열 수 있는 상태가 된 후, 이 시간 동안 상호작용하지 않으면 초기화")]
	[SerializeField] private float deleteTouchDuration = 1f;

	[Header("문 강조 설정")]
	[SerializeField] private float highlightDistance = 4.5f; // 감지 범위
	[SerializeField] private Color highlightColor = new Color(0f, 1f, 1f, 1f); // 테두리 색상 (민트/시안색)
	[SerializeField] private float outlineThickness = 0.05f; // 테두리 두께 (기본값 0.05)
	[SerializeField] private int sortingOrderOffset = -1; // -1: 문 뒤로 배치(기본값)
	private SpriteRenderer spriteRenderer;
	private Color originalColor;
	private Transform playerTransform;
	private GameObject[] outlineObjs;
	private SpriteRenderer[] outlineSrs;
	private bool isHighlighted = false; // 실시간 상태 추적용 변수

	private Coroutine touchCoroutine;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer != null)
		{
			originalColor = spriteRenderer.color;
			CreateOutline();
		}
	}

	private void CreateOutline()
	{
		// 4방향 미세 오프셋을 사용한 아웃라인 생성 (피벗 정렬 문제를 완벽하게 회피하고 순수 테두리만 그리는 기법)
		outlineObjs = new GameObject[4];
		outlineSrs = new SpriteRenderer[4];

		Vector3[] offsets = new Vector3[]
		{
			new Vector3(-outlineThickness, 0f, 0.05f), // 좌
			new Vector3(outlineThickness, 0f, 0.05f),  // 우
			new Vector3(0f, -outlineThickness, 0.05f), // 하
			new Vector3(0f, outlineThickness, 0.05f)   // 상
		};

		string[] directions = new string[] { "Left", "Right", "Down", "Up" };

		for (int i = 0; i < 4; i++)
		{
			outlineObjs[i] = new GameObject("DoorOutline_" + directions[i]);
			outlineObjs[i].transform.SetParent(transform);
			outlineObjs[i].transform.localPosition = offsets[i];
			outlineObjs[i].transform.localRotation = Quaternion.identity;
			outlineObjs[i].transform.localScale = Vector3.one; // 스케일을 1로 고정하여 피벗 왜곡 차단

			SpriteRenderer sr = outlineObjs[i].AddComponent<SpriteRenderer>();
			sr.sprite = spriteRenderer.sprite;
			sr.color = highlightColor;
			sr.sortingLayerID = spriteRenderer.sortingLayerID;
			sr.sortingOrder = spriteRenderer.sortingOrder + sortingOrderOffset;
			sr.flipX = spriteRenderer.flipX;
			sr.flipY = spriteRenderer.flipY;

			// 검은색 외곽선도 강제로 설정한 강조 색상으로 칠하기 위해 GUI/Text Shader(단색 실루엣) 적용
			Shader solidShader = Shader.Find("GUI/Text Shader");
			if (solidShader != null)
			{
				sr.material = new Material(solidShader);
			}

			outlineSrs[i] = sr;
			outlineObjs[i].SetActive(false);
		}
	}

	private void Start()
	{
		// 씬 내의 Player 찾기
		var player = FindAnyObjectByType<PlayerController>();
		if (player != null)
		{
			playerTransform = player.transform;
		}
	}

	private void Update()
	{
		if (playerTransform == null)
		{
			var player = FindAnyObjectByType<PlayerController>();
			if (player != null)
			{
				playerTransform = player.transform;
			}
			return;
		}

		if (spriteRenderer != null && outlineObjs != null)
		{
			float distance = Vector2.Distance(transform.position, playerTransform.position);
			bool shouldHighlight = distance <= highlightDistance;

			if (shouldHighlight != isHighlighted)
			{
				isHighlighted = shouldHighlight;
				foreach (var obj in outlineObjs)
				{
					if (obj != null) obj.SetActive(shouldHighlight);
				}

				if (shouldHighlight)
				{
					Debug.Log($"[DoorController] Highlight ON: Player is close to {gameObject.name} (Distance: {distance:F2}m / Threshold: {highlightDistance}m)");
				}
				else
				{
					Debug.Log($"[DoorController] Highlight OFF: Player walked away from {gameObject.name}");
				}
			}

			// 실시간 이미지 상태 동기화 및 두께(두께 조절 시 반영) 업데이트
			if (shouldHighlight)
			{
				Vector3[] offsets = new Vector3[]
				{
					new Vector3(-outlineThickness, 0f, 0.05f),
					new Vector3(outlineThickness, 0f, 0.05f),
					new Vector3(0f, -outlineThickness, 0.05f),
					new Vector3(0f, outlineThickness, 0.05f)
				};

				for (int i = 0; i < 4; i++)
				{
					if (outlineSrs[i] != null && outlineObjs[i] != null)
					{
						outlineSrs[i].sprite = spriteRenderer.sprite;
						outlineSrs[i].color = highlightColor; // 인스펙터에서 실시간으로 색상 조절이 가능하도록 추가 동기화
						outlineSrs[i].flipX = spriteRenderer.flipX;
						outlineSrs[i].flipY = spriteRenderer.flipY;
						outlineObjs[i].transform.localPosition = offsets[i];
					}
				}
			}
		}
	}

	/// <summary>
	/// 플레이어의 상호작용 요청을 처리한다.
	/// </summary>
	public bool TryOpen()
	{
		// 두 번째 상호작용이 가능한 상태
		if (canOpen)
		{
			OnOpen();
			return true;
		}

		// 아직 타이머가 시작되지 않았다면
		// 첫 번째 상호작용으로 타이머 시작
		if (touchCoroutine == null)
		{
			StartFirstTouchTimer();
		}

		return false;
	}

	/// <summary>
	/// 첫 번째 상호작용 타이머를 시작한다.
	/// </summary>
	private void StartFirstTouchTimer()
	{
		// 이미 실행 중인 코루틴이 있다면 다시 시작하지 않는다.
		if (touchCoroutine != null)
		{
			return;
		}

		touchCoroutine = StartCoroutine(FirstTouchTimer());
	}

	/// <summary>
	/// 실제로 문을 여는 처리.
	/// </summary>
	public void OnOpen()
	{
		canOpen = false;

		// 타이머 종료
		if (touchCoroutine != null)
		{
			StopCoroutine(touchCoroutine);
			touchCoroutine = null;
		}

		// 문 강조 아웃라인들 삭제
		if (outlineObjs != null)
		{
			foreach (var obj in outlineObjs)
			{
				if (obj != null) Destroy(obj);
			}
		}

		// 문 열기 연출
		GameManager.Instance.cameraShake.ShakeForSeconds();

		animator.Play("Door_Open");

		GetComponent<Collider2D>().enabled = false;
		Destroy(this);
	}

	public bool CanOpen()
	{
		return canOpen;
	}

	private IEnumerator FirstTouchTimer()
	{
		Debug.Log("Start FirstTouch CoolTime");

		// 첫 번째 상호작용 후 대기
		yield return new WaitForSecondsRealtime(afterTouchDuration);

		Debug.Log("Can Open Door");

		// 이제 두 번째 상호작용 가능
		canOpen = true;

		float elapsedTime = 0f;

		// 문을 열 수 있는 상태에서 일정 시간 동안 기다림
		while (elapsedTime < deleteTouchDuration)
		{
			elapsedTime += Time.unscaledDeltaTime;

			yield return null;
		}

		// 일정 시간 동안 TryOpen이 호출되지 않았다면 초기화
		canOpen = false;
		touchCoroutine = null;

		Debug.Log("FirstTouch Reset");
	}
}