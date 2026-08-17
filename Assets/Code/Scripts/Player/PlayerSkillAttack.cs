using Globals;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class PlayerSkillAttack : MonoBehaviour
{
	[Header("스킬 사용 시 쉐이킹 및 슬로우 시간")]
	[SerializeField] private float hitStopTime = 0.5f;
	[Header("플레이어 스킬 사용 최소 거리")]
	[SerializeField] private float skillMinRadius = 1f;
	[Header("플레이어 스킬 사용 최대 거리")]
	[SerializeField] private float skillMaxRadius = 10.4f;
	[Header("스킬 사용 시 보이는 점")]
	[SerializeField] private GameObject Dot;
	[Header("스킬 사용 시 보이는 선")]
	[SerializeField] private GameObject LimitLine;
	[Header("선 두께")]
	[SerializeField] private float lineWidth = 0.05f;

	[Header("스킬 쿨타임")]
	[SerializeField] private float skillCooldown = 3f; // 쿨타임 설정 시간
	[SerializeField] private PlayerCooldownUI cooldownUI; // 머리 위 UI 스크립트 연결용
	private float cooldownTimer = 0f; // 현재 남은 쿨타임 계산용
	public bool IsCooldown => cooldownTimer > 0f; // 쿨타임 중인지 판단

	private Animator anim;
	private GameObject DotObj;
	private GameObject LineObj;
	private LineRenderer line;
	private PlayerSlowMode slowMode;
	private Camera mainCam;
	private Vector3 targetPos;
	public bool isActive = false;
	public bool canUseSkill = true;
	public bool IsSkillAttacking { get; private set; }

	private void Update()
	{
		if (cooldownTimer > 0f)
		{
			cooldownTimer -= Time.deltaTime;
			if (cooldownUI != null)
			{
				cooldownUI.UpdateCooldown(cooldownTimer, skillCooldown);
			}
			if (cooldownTimer <= 0f && cooldownUI != null)
			{
				cooldownUI.ShowCooldown(false); // 쿨타임 끝나면 UI 숨김
			}
		}
	}

	private void Awake()
	{
		slowMode = GetComponent<PlayerSlowMode>();
		anim = GetComponent<Animator>();
	}

	private void Start()
	{
		mainCam = Camera.main;
		DotObj = Instantiate(Dot);
		DotObj.SetActive(false);	// 점 안 보이게
		LineObj = Instantiate(LimitLine);
		LineObj.SetActive(false);	// 선 안 보이게
		SetLine();
	}

	private void SetLine()
	{
		line = DotObj.GetComponentInChildren<LineRenderer>();
		line.positionCount = 2;
		line.widthMultiplier = lineWidth;
		line.startColor = Color.white;
		line.endColor = Color.white;
	}

	public void EnterSkill()
	{
		// 쿨타임 중이거나 스킬을 쓸 수 없는 상태면 실행하지 않음
		if (IsCooldown || !canUseSkill) return;

		GetComponent<PlayerMovement>().canMove = false;		// 움직임 제한
		GetComponent<PlayerAttack>().canAttack = false;		// 공격 제한

		if (Dot == null)
		{
			Debug.LogWarning("Dot 오브젝트 없음");
			return;
		}
		isActive = true;
		line.enabled = true;
		slowMode.EnterSlow();
		SetActiveObj(true);
	}

	private void FixedUpdate()
	{
		if (!isActive) return;

		targetPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		targetPos.z = DotObj.transform.position.z;

		// Dot 방향 전환
		Vector2 dir = (targetPos - transform.position).normalized;
		Vector3 scale = DotObj.transform.localScale;

		if (dir.x > 0)
			scale.x = Mathf.Abs(scale.x);      // 오른쪽
		else if (dir.x < 0)
			scale.x = -Mathf.Abs(scale.x);     // 왼쪽

		DotObj.transform.localScale = scale;

		// 목표 거리 계산
		float dotDist = Vector2.Distance(transform.position, targetPos);

		if (dotDist < skillMinRadius)   // 최소거리 미만일 경우 숨김
		{
			HideAll();
		}
		else
		{
			// Dot 위치를 BoxCast 결과로 계산
			DotObj.transform.position = GetSkillTargetPosition(targetPos);
			ShowLine();
		}

		// 선 오브젝트 설정
		if (LineObj.transform.position != transform.position)
			LineObj.transform.position = transform.position;
	}

	private void ShowLine()
	{
		if (!DotObj.gameObject.activeSelf)
		{
			SetActiveObj(true);
		}
		line.SetPosition(0, transform.position);
		line.SetPosition(1, DotObj.transform.position);
		canUseSkill = true;
	}

	private void HideAll()
	{
		if (DotObj.gameObject.activeSelf)
		{
			SetActiveObj(false);
		}
		canUseSkill = false;
	}

	private void SetActiveObj(bool active)
	{
		DotObj.SetActive(active);
		LineObj.SetActive(active);
	}

	// 스킬 사용
	private IEnumerator SkillAttack()
	{
		// 스킬 쿨타임 작동 및 UI 활성화
		cooldownTimer = skillCooldown;
		if (cooldownUI != null)
		{
			cooldownUI.ShowCooldown(true);
		}
		IsSkillAttacking = true;

		Vector2 targetPos = GetSkillTargetPosition(DotObj.transform.position);	// Dot 위치를 목표 위치로
		Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

		LayerMask mask = LayerMask.GetMask(
			LayerName.enemy,
			LayerName.crackObj);

		float distance = Vector2.Distance(transform.position, targetPos);

		RaycastHit2D[] hits = Physics2D.RaycastAll(
			transform.position,
			dir,
			distance,
			mask);

		// 공격 판정
		foreach (RaycastHit2D hit in hits)
		{
			if (hit.transform.TryGetComponent<IDamageable>(out var damage))
				damage.TakeDamage(GameManager.Instance.playerStatsRuntime.attack, dir);

			if (hit.transform.TryGetComponent<CrackObject>(out var obj))
				obj.Crack();
		}

		slowMode.panel?.SetActive(false);   // 슬로우 화면 제거

		// 이동 완료까지 기다림
		yield return StartCoroutine(MoveTargetPos(targetPos));

		// 슬로우 + 카메라 쉐이크
		if(hits.Length > 0f)
		{
			// 쉐이크
			GameManager.Instance.cameraShake.ShakeForSeconds();

			slowMode.EnterOnlySlow();

			// 히트스톱(잠시 멈춤)
			yield return new WaitForSecondsRealtime(hitStopTime);

			slowMode.ExitSlow();
		}

		IsSkillAttacking = false;
	}

	private IEnumerator MoveTargetPos(Vector2 target)
	{
		float duration = 0.03f;   // 30ms
		float time = 0f;

		Vector2 start = transform.position;

		while (time < duration)
		{
			time += Time.deltaTime;
			transform.position = Vector2.Lerp(start, target, time / duration);
			yield return null;
		}

		transform.position = target;
	}

	// 마우스 위치를 기준으로 실제 이동 가능한 위치를 반환
	private Vector2 GetSkillTargetPosition(Vector2 desiredPos)
	{
		Vector2 startPos = transform.position;
		Vector2 dir = (desiredPos - startPos).normalized;

		float mouseDist = Vector2.Distance(startPos, desiredPos);

		// 최대거리 제한
		float castDist = Mathf.Min(mouseDist, skillMaxRadius);

		Vector2 boxSize = Vector2.Scale(
			GetComponent<BoxCollider2D>().size,
			transform.lossyScale);

		LayerMask obstacleMask = LayerMask.GetMask(
			LayerName.ground,
			LayerName.wall); 
		
		float skin = 0.05f;

		Vector2 castStart = startPos + dir * skin;

		RaycastHit2D hit = Physics2D.BoxCast(
			castStart,
			boxSize,
			transform.eulerAngles.z,
			dir,
			castDist,
			obstacleMask);

		// 벽이 있으면 벽 앞에서 멈춤
		if (hit)
		{
			Debug.Log($"Hit : {hit.collider.name}, distance : {hit.distance}");
			return startPos + dir * hit.distance;
		}

		// 벽이 없으면 최대거리 또는 마우스 위치
		return startPos + dir * castDist;
	}

	// 마우스 뗌과 동시에 스킬 나가기 및 사용
	public void ExitSkill()
	{
		// 조준 상태가 아니었거나(쿨타임 등으로 인해), 쿨타임 중이면 스킬 발사를 차단합니다.
		if (!isActive || IsCooldown) return;

		anim.Play("Dragon_Skill");   // 애니메이션

		if (canUseSkill) StartCoroutine(SkillAttack());
		isActive = false;
		line.enabled = false;
		SetActiveObj(false);
		slowMode.ExitSlow(false);

		GetComponent<PlayerMovement>().canMove = true;
		GetComponent<PlayerAttack>().canAttack = true;
	}
}
