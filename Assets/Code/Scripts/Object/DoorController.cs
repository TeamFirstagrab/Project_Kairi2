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

	private Coroutine touchCoroutine;

	private void Awake()
	{
		animator = GetComponent<Animator>();
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