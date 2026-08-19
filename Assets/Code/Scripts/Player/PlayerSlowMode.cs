using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerSlowMode : MonoBehaviour
{
	[Header("Audio Mixer")]
	public AudioMixer mixer;
	[Header("슬로우 배경 Panel")]
	public GameObject panel;
	//[Header("슬로우 게이지 UI")]
	//public Slider slowGaugeSlider;
	[Header("슬로우 비율")]
	public const float slowFactor = 0.01f;
	[Header("슬로우 게이지 최대치")]
	public float slowMaxGauge = 3f;
	//[Header("슬로우 게이지 현재치")]
	//public float slowGauge = 3f;
	[Header("슬로우 게이지 감소 속도")]
	public float slowDecreaseRate = 1f;
	[Header("슬로우 게이지 회복 속도")]
	public float slowRecoverRate = 0.5f;
	[Header("슬로우 상태 여부")]
	private bool isPlayerSlow = false;

	[Header("슬로우 쿨다운")]
	[SerializeField] private float slowCooldown = 5f; // 슬로우 재사용 대기시간 (5초)
	private float cooldownTimer = 0f; // 현재 쿨다운 타이머
	public bool IsCooldown => cooldownTimer > 0f; // 현재 쿨다운 중인지 판단

	[Header("슬로우 쿨다운 UI 연동")]
	[SerializeField] private PlayerSlowCooldownUI slowCooldownUI; // 슬로우 UI 스크립트 참조

	[Header("슬로우 적 강조 색상")]
	[SerializeField] private Color enemyHighlightColor = new Color(0f, 0f, 1f, 1f); // 유니티 에디터에서 설정 가능

	private Silhouette solihoutte;  // 잔상효과
	private float slowTime = 0.5f;  // 슬로우 지속 시간

	private struct EnemyHighlightState
	{
		public SpriteRenderer spriteRenderer;
		public Color originalColor;
		public int originalSortingOrder;
		public Material originalMaterial;
	}
	private List<EnemyHighlightState> highlightedEnemies = new List<EnemyHighlightState>();

	private void Update()
	{
		if (cooldownTimer > 0f)
		{
			cooldownTimer -= Time.unscaledDeltaTime;
			if (slowCooldownUI != null)
			{
				slowCooldownUI.UpdateCooldown(cooldownTimer, slowCooldown);
			}

			if (cooldownTimer <= 0f && slowCooldownUI != null)
			{
				slowCooldownUI.UpdateCooldown(0f, slowCooldown); // 쿨다운이 끝나면 0 상태로 업데이트
			}
		}
	}


	private void Awake()
	{
		solihoutte = GetComponent<Silhouette>();
	}

	private void Start()
	{
		panel?.SetActive(false);

		//if (globalVolume == null)
		//{
		//	Debug.LogError("Global Volume이 할당되지 않음");
		//	return;
		//}

		//if (!globalVolume.profile.TryGet(out colorAdjustments))
		//	Debug.LogError("Volume Profile에 없음");
		//if (!globalVolume.profile.TryGet(out bloom))
		//	Debug.LogError("Volume Profile에 없음");
	}

	public void EnterSlow(float factor = slowFactor)
	{
		// 쿨다운 중이면 슬로우 모드를 실행하지 않고 리턴
		if (IsCooldown) return;
		print($"slow duration: {factor}");
		if (!isPlayerSlow)
		{
			isPlayerSlow = true;
			panel?.SetActive(true);
			StartSlow(factor);
			solihoutte.Active = true;
			HighlightEnemies();
		}
	}

	public void EnterOnlySlow(float factor = slowFactor)
	{
		// 쿨다운 중이면 실행 차단
		if (IsCooldown) return;
		if (!isPlayerSlow)
		{
			isPlayerSlow = true;
			StartSlow(factor);
			solihoutte.Active = true;
			HighlightEnemies();
		}
	}

	public void ExitSlow(bool triggerCooldown = true)
	{
		if (isPlayerSlow)
		{
			isPlayerSlow = false;
			solihoutte.Active = false;
			panel?.SetActive(false);
			StopSlow();
			RestoreEnemies();

			if (triggerCooldown)
			{
				cooldownTimer = slowCooldown;

				// [추가] 쿨다운 활동이 시작되었으니 UI를 켜면 됩니다.
				if (slowCooldownUI != null)
				{
					slowCooldownUI.ShowCooldown(true);
				}
			}
		}
	}

	private void StartSlow(float factor)    // 슬로우 효과 시작
	{
        Time.timeScale = factor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;
		mixer.SetFloat("MasterCutoff", 1000f);   // 먹먹
	}

	private void StopSlow()     // 슬로우 효과 종료
	{
		if (isPlayerSlow)
			return;
		Time.timeScale = 1f;            // 시간 원래대로
		Time.fixedDeltaTime = 0.02f;
		mixer.SetFloat("MasterCutoff", 22000f); // 원래 소리
		solihoutte.DefaultSet();		// 실루엣 기본상태로 변경
	}

	public void EnterHitStop()		// 시간 멈추기
	{
		Time.timeScale = 0f;
		Time.fixedDeltaTime = 0f;
	}

	public void ExitHitStop()	// 원래대로
	{
		Time.timeScale = 1f;
		Time.fixedDeltaTime = 1f;
	}

	private void HighlightEnemies()
	{
		// 기본 전체 강조는 수행하지 않고, 조준선 동적 강조만 수행합니다.
		highlightedEnemies.Clear();
	}

	public void UpdateDynamicHighlights(HashSet<Enemy> targetEnemies)
	{
		if (!isPlayerSlow) return;

		// 1. 더 이상 조준선에 닿지 않는 적들의 강조 해제 및 복구
		for (int i = highlightedEnemies.Count - 1; i >= 0; i--)
		{
			var state = highlightedEnemies[i];
			if (state.spriteRenderer == null)
			{
				highlightedEnemies.RemoveAt(i);
				continue;
			}

			// 현재 닿은 타겟 목록에 존재하는지 확인
			bool isStillTargeted = false;
			foreach (var enemy in targetEnemies)
			{
				if (enemy != null && enemy.GetComponentInChildren<SpriteRenderer>() == state.spriteRenderer)
				{
					isStillTargeted = true;
					break;
				}
			}

			if (!isStillTargeted)
			{
				state.spriteRenderer.color = state.originalColor;
				state.spriteRenderer.sortingOrder = state.originalSortingOrder;
				state.spriteRenderer.material = state.originalMaterial;
				highlightedEnemies.RemoveAt(i);
			}
		}

		// 2. 새롭게 조준선에 닿은 적들 강조 적용
		foreach (var enemy in targetEnemies)
		{
			if (enemy == null || enemy.currentHP <= 0) continue;

			SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
			if (sr != null)
			{
				// 이미 강조된 적인지 확인
				bool alreadyHighlighted = false;
				foreach (var state in highlightedEnemies)
				{
					if (state.spriteRenderer == sr)
					{
						alreadyHighlighted = true;
						break;
					}
				}

				if (!alreadyHighlighted)
				{
					highlightedEnemies.Add(new EnemyHighlightState
					{
						spriteRenderer = sr,
						originalColor = sr.color,
						originalSortingOrder = sr.sortingOrder,
						originalMaterial = sr.material
					});

					// 완전히 단색 실루엣으로 덮어 강렬하게 강조 처리
					sr.color = enemyHighlightColor;
					sr.sortingOrder = 10;

					// 검은색 외곽선까지 덮어버리는 GUI/Text Shader(단색 실루엣) 동적 적용
					Shader solidShader = Shader.Find("GUI/Text Shader");
					if (solidShader != null)
					{
						sr.material = new Material(solidShader);
					}
				}
			}
		}
	}

	private void RestoreEnemies()
	{
		foreach (var state in highlightedEnemies)
		{
			if (state.spriteRenderer != null)
			{
				// 원래 상태로 원복
				state.spriteRenderer.color = state.originalColor;
				state.spriteRenderer.sortingOrder = state.originalSortingOrder;
				state.spriteRenderer.material = state.originalMaterial;
			}
		}
		highlightedEnemies.Clear();
	}

	//void UpdateSlowGauge()      // 슬로우 게이지 업데이트
	//{
	//	if (slowGaugeSlider == null) return;
	//	if (isPlayerSlow)
	//	{
	//		slowGauge -= slowDecreaseRate * Time.unscaledDeltaTime;

	//		if (slowGauge <= 0f)
	//		{
	//			slowGauge = 0f;
	//			StopSlow();
	//		}
	//	}
	//	else
	//	{
	//		slowGauge += slowRecoverRate * Time.unscaledDeltaTime;
	//		if (slowGauge > slowMaxGauge)
	//			slowGauge = slowMaxGauge;
	//	}
	//	slowGaugeSlider.value = slowGauge / slowMaxGauge;
	//}
}
