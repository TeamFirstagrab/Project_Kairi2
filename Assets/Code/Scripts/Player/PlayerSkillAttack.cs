using Globals;
using System.Collections;
using System.Collections.Generic;
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
	[SerializeField] private bool recoverTimeBased = false; // 시간 경과에 따른 자동 회복 여부 (기본값: false, 처치로만 차도록 설정)
	private float cooldownTimer = 0f; // 현재 남은 쿨타임 계산용
	public bool IsCooldown => cooldownTimer > 0f; // 쿨타임 중인지 판단

	private float aimTimer = 0f; // 현재 조준 시간 계산용
	private const float maxAimTime = 3f; // 최대 조준 제한 시간 (3초)
	private bool isFailCooldown = false; // 현재 쿨타임이 2초 패널티(시간 충전) 쿨타임인지 여부

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

	private SpriteRenderer playerSpriteRenderer;
	private GameObject[] outlineObjs;

	private void Update()
	{
		// 1. 조준 시간 제한 체크 (3초)
		if (isActive)
		{
			aimTimer -= Time.unscaledDeltaTime; // 슬로우 중에도 3초 정상 감산을 위해 unscaled 사용
			if (aimTimer <= 0f)
			{
				Debug.Log("[PlayerSkillAttack] Aim time limit exceeded (3s)! Auto-canceling skill...");
				ExitSkill(true); // 자동 취소 강제 호출 (isTimeout = true)
			}
		}

		// 2. 쿨타임 감산 처리
		if (cooldownTimer > 0f)
		{
			// 패널티 쿨타임이거나(isFailCooldown == true), 시간 충전 설정이 켜져 있을 때만 시간 경과로 충전
			if (recoverTimeBased || isFailCooldown)
			{
				cooldownTimer -= Time.unscaledDeltaTime;
			}
			if (cooldownUI != null)
			{
				cooldownUI.UpdateCooldown(cooldownTimer, isFailCooldown ? 2f : skillCooldown);
			}
			if (cooldownTimer <= 0f && cooldownUI != null)
			{
				isFailCooldown = false; // 패널티 쿨타임 종료
				cooldownUI.UpdateCooldown(0f, skillCooldown); // 쿨타임 끝나면 0 상태로 업데이트
			}
		}
	}

	private void LateUpdate()
	{
		UpdateOutline();
	}

	private void UpdateOutline()
	{
		bool showOutline = cooldownTimer <= 0f && !isActive && !IsSkillAttacking;

		if (showOutline)
		{
			if (playerSpriteRenderer == null) return;

			if (outlineObjs == null)
			{
				outlineObjs = new GameObject[4];
				string[] directions = { "Left", "Right", "Down", "Up" };
				Vector3[] offsets = { Vector3.left * 0.05f, Vector3.right * 0.05f, Vector3.down * 0.05f, Vector3.up * 0.05f };

				for (int i = 0; i < 4; i++)
				{
					GameObject outlineObj = new GameObject("PlayerOutline_" + directions[i]);
					outlineObj.transform.SetParent(transform);
					outlineObj.transform.localPosition = offsets[i];
					outlineObj.transform.localScale = Vector3.one;
					outlineObj.transform.localRotation = Quaternion.identity;

					SpriteRenderer sr = outlineObj.AddComponent<SpriteRenderer>();
					sr.sortingLayerName = playerSpriteRenderer.sortingLayerName;
					sr.sortingOrder = playerSpriteRenderer.sortingOrder - 1;

					Shader textShader = Shader.Find("GUI/Text Shader");
					if (textShader != null)
					{
						sr.material = new Material(textShader);
						sr.material.color = Color.red; // 빨간색 테두리
					}
					outlineObjs[i] = outlineObj;
				}
			}

			// 실시간 스프라이트 애니메이션 동기화
			foreach (var obj in outlineObjs)
			{
				if (obj != null)
				{
					obj.SetActive(true);
					SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
					sr.sprite = playerSpriteRenderer.sprite;
					sr.flipX = playerSpriteRenderer.flipX;
					sr.flipY = playerSpriteRenderer.flipY;
					sr.sortingOrder = playerSpriteRenderer.sortingOrder - 1;
				}
			}
		}
		else
		{
			if (outlineObjs != null)
			{
				foreach (var obj in outlineObjs)
				{
					if (obj != null) obj.SetActive(false);
				}
			}
		}
	}

	private void Awake()
	{
		slowMode = GetComponent<PlayerSlowMode>();
		anim = GetComponent<Animator>();
		playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
		aimTimer = maxAimTime; // 조준 제한 시간 설정 (3초)
		line.enabled = true;
		slowMode.EnterSlow();
		SetActiveObj(true);
	}

	private void FixedUpdate()
	{
		if (!isActive) return;

		targetPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
		targetPos.z = DotObj.transform.position.z;

		// Dot 방향 변환
		Vector2 dir = (targetPos - transform.position).normalized;
		Vector3 scale = DotObj.transform.localScale;

		if (dir.x > 0)
			scale.x = Mathf.Abs(scale.x);      // 오른쪽
		else if (dir.x < 0)
			scale.x = -Mathf.Abs(scale.x);     // 왼쪽

		DotObj.transform.localScale = scale;

		// 목표 거리 계산
		float dotDist = Vector2.Distance(transform.position, targetPos);

		if (dotDist < skillMinRadius)   // 최소거리 미만인 경우 숨김
		{
			HideAll();
		}
		else
		{
			// Dot 위치를 BoxCast 결과로 계산
			DotObj.transform.position = GetSkillTargetPosition(targetPos);
			ShowLine();
		}

		// -라인오브젝트 보정
		if (LineObj.transform.position != transform.position)
			LineObj.transform.position = transform.position;

		// 조준선 닿은 적 실시간 강조 처리
		UpdateTargetEnemiesHighlight();
	}

	private void UpdateTargetEnemiesHighlight()
	{
		if (DotObj == null || !DotObj.activeSelf)
		{
			slowMode.UpdateDynamicHighlights(new HashSet<Enemy>());
			return;
		}

		Vector2 startPos = transform.position;
		Vector2 endPos = DotObj.transform.position;
		Vector2 direction = (endPos - startPos).normalized;
		float distance = Vector2.Distance(startPos, endPos);

		LayerMask mask = LayerMask.GetMask(LayerName.enemy);

		RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, distance, mask);
		
		HashSet<Enemy> targetEnemies = new HashSet<Enemy>();
		foreach (var hit in hits)
		{
			if (hit.collider != null)
			{
				Enemy enemy = hit.collider.GetComponent<Enemy>();
				if (enemy != null)
				{
					targetEnemies.Add(enemy);
				}
			}
		}

		slowMode.UpdateDynamicHighlights(targetEnemies);
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
		slowMode.UpdateDynamicHighlights(new HashSet<Enemy>());
	}

	private void SetActiveObj(bool active)
	{
		DotObj.SetActive(active);
		LineObj.SetActive(active);
	}

	// 스킬 사용
	private IEnumerator SkillAttack()
	{
		IsSkillAttacking = true;

		Vector2 targetPos = GetSkillTargetPosition(DotObj.transform.position);	// Dot 위치를 목표 위치로
		Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

		LayerMask mask = LayerMask.GetMask(
			LayerName.enemy,
			LayerName.crackObj,
			LayerName.door);

		float distance = Vector2.Distance(transform.position, targetPos);

		RaycastHit2D[] hits = Physics2D.RaycastAll(
			transform.position,
			dir,
			distance,
			mask);

		// 적 처치 성공 여부 계산 (현재 체력이 내 공격력 이하인 적이 1명이라도 있는지)
		int killCount = 0;
		int playerAttack = GameManager.Instance.playerStatsRuntime.attack;

		foreach (RaycastHit2D hit in hits)
		{
			if (hit.transform.TryGetComponent<Enemy>(out var enemy))
			{
				if (enemy.currentHP > 0 && enemy.currentHP <= playerAttack)
				{
					killCount++;
				}
			}
		}

		if (killCount > 0)
		{
			// 성공: 정식 쿨타임(3초) 설정 및 차감 없음 알림
			isFailCooldown = false;
			cooldownTimer = skillCooldown;
			if (cooldownUI != null)
			{
				cooldownUI.ShowCooldown(true);
				cooldownUI.UpdateCooldown(cooldownTimer, skillCooldown);
			}

			// 공격 판정
			foreach (RaycastHit2D hit in hits)
			{
				if (hit.transform.TryGetComponent<DoorController>(out var door))
					door.OnOpen();

				if (hit.transform.TryGetComponent<IDamageable>(out var damage))
					damage.TakeDamage(playerAttack, dir);

				if (hit.transform.TryGetComponent<CrackObject>(out var obj))
					obj.Crack();
			}

			slowMode.panel?.SetActive(false);   // 슬로우 화면 제거

			// 이동 완료할때까지 기다림 (성공 시에만 목표 위치로 이동!)
			yield return StartCoroutine(MoveTargetPos(targetPos));

			// 슬로우 + 카메라 셰이크
			if (hits.Length > 0f)
			{
				GameManager.Instance.cameraShake.ShakeForSeconds();
				slowMode.EnterOnlySlow();
				yield return new WaitForSecondsRealtime(hitStopTime);
				slowMode.ExitSlow(false); // Shift 슬로우 쿨타임 작동 방지 (false 전달)
			}
		}
		else
		{
			// 실패: 이동하지 않고 공격만 수행하며 2초의 연속사용 방지 쿨타임 발동
			isFailCooldown = true;
			cooldownTimer = 2f;
			if (cooldownUI != null)
			{
				cooldownUI.ShowCooldown(true);
				cooldownUI.UpdateCooldown(cooldownTimer, 2f);
			}

			// 공격 판정 (이동은 안 하지만 맞닿은 문이나 데미지는 입힘)
			foreach (RaycastHit2D hit in hits)
			{
				if (hit.transform.TryGetComponent<DoorController>(out var door))
					door.OnOpen();

				if (hit.transform.TryGetComponent<IDamageable>(out var damage))
					damage.TakeDamage(playerAttack, dir);

				if (hit.transform.TryGetComponent<CrackObject>(out var obj))
					obj.Crack();
			}

			slowMode.panel?.SetActive(false);   // 슬로우 화면 제거

			// 제자리 타격 시에도 히트스톱 연출을 주어 타격감 보존
			if (hits.Length > 0f)
			{
				GameManager.Instance.cameraShake.ShakeForSeconds();
				slowMode.EnterOnlySlow();
				yield return new WaitForSecondsRealtime(hitStopTime);
				slowMode.ExitSlow(false);
			}
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

	/// <summary>
	/// 적 처치 시 호출되어 스킬 공격 쿨타임을 25% (4칸 중 1칸)만큼 즉시 충전합니다.
	/// </summary>
	public void ReduceCooldownOnKill()
	{
		if (cooldownTimer > 0f)
		{
			float reduction = skillCooldown * 0.25f; // 25% 단축
			cooldownTimer = Mathf.Max(0f, cooldownTimer - reduction);

			if (cooldownUI != null)
			{
				cooldownUI.UpdateCooldown(cooldownTimer, isFailCooldown ? 2f : skillCooldown);
			}

			Debug.Log($"[PlayerSkillAttack] Enemy Killed! Cooldown reduced by {reduction:F2}s. Remaining Cooldown: {cooldownTimer:F2}s");
		}
	}

	// 마우스 뗌과 동시에 스킬 나가기 및 사용
	public void ExitSkill(bool isTimeout = false)
	{
		// 조준 상태가 아니거나(쿨다운 등으로 인해), 쿨다운 중이면 스킬 발사를 차단합니다.
		if (!isActive || IsCooldown) return;

		isActive = false;
		line.enabled = false;
		SetActiveObj(false);
		slowMode.ExitSlow(false);

		GetComponent<PlayerMovement>().canMove = true;
		GetComponent<PlayerAttack>().canAttack = true;

		if (isTimeout)
		{
			// 3초 조준 제한 시간 만료: 이동하지 않고 2초의 패널티 쿨타임 발동
			isFailCooldown = true;
			cooldownTimer = 2f;
			if (cooldownUI != null)
			{
				cooldownUI.ShowCooldown(true);
				cooldownUI.UpdateCooldown(cooldownTimer, 2f);
			}
		}
		else
		{
			anim.Play("Dragon_Skill");   // 애니메이션
			if (canUseSkill) StartCoroutine(SkillAttack());
		}
	}
}
