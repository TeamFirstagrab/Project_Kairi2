using UnityEngine;

public class PlayerSkillAttack : MonoBehaviour
{
	[Header("스킬 시전 시 슬로우 비율")]
	[SerializeField] private float slowFactor = 0.2f;
	[Header("플레이어 스킬 사용 최소 거리")]
	[SerializeField] private float skillMinRadius = 5f;
	[Header("플레이어 스킬 사용 최대 거리")]
	[SerializeField] private float skillMaxRadius = 50f;
	[Header("스킬 사용 시 보이는 점")]
	[SerializeField] private GameObject Dot;
	[Header("선 두께")]
	[SerializeField] private float lineWidth = 0.5f;
	private GameObject DotObj;
	private LineRenderer line;
	private PlayerSlowMode slowMode;
	private Camera mainCam;
	private Vector2 targetPos;
	public bool isActive = false;

	private void Awake()
	{
		slowMode = GetComponent<PlayerSlowMode>();
	}

	private void Start()
	{
		mainCam = Camera.main;
		DotObj = Instantiate(Dot);
		DotObj.SetActive(false);
		SetLine();
	}

	private void SetLine()
	{
		line = DotObj.GetComponentInChildren<LineRenderer>();
		line.positionCount = 2;
		line.startWidth = lineWidth;
		line.endWidth = lineWidth;
		line.startColor = Color.white;
		line.endColor = Color.white;
	}

	public void EnterSkill()
	{
		if (Dot == null)
		{
			Debug.LogWarning("Dot 오브젝트 없음");
			return;
		}
		isActive = true;
		line.enabled = true;
		slowMode.EnterSlow(slowFactor);
		DotObj.SetActive(true);
	}

	private void FixedUpdate()
	{
		if (!isActive) return;

		targetPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		DotObj.transform.position = targetPos;
		if (Vector2.Distance(transform.position, targetPos) > skillMinRadius)
		{
			ShowLine();     // 선 보이기
		}
		else
		{
			HideLine();     // 선 숨기기
		}
	}

	private void ShowLine()
	{
		line.SetPosition(0, transform.position);
		line.SetPosition(1, DotObj.transform.position);
	}

	private void HideLine()
	{

	}

	private void SkillAttack()
	{
		transform.position = targetPos;
	}

	public void ExitSkill()
	{
		SkillAttack();
		isActive = false;
		line.enabled = false;
		DotObj.SetActive(false);
		slowMode.ExitSlow();
	}
}
