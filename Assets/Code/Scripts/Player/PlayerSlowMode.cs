using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerSlowMode : MonoBehaviour
{
	[Header("Audio Mixer")]
	public AudioMixer mixer;
	[Header("Global Volume 오브젝트")]
	public Volume globalVolume;
	//[Header("슬로우 게이지 UI")]
	//public Slider slowGaugeSlider;
	[Header("슬로우 비율")]
	public const float slowFactor = 0.3f;
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

    private Silhouette solihoutte;  // 잔상효과
	private float slowTime = 0.5f;  // 슬로우 지속 시간

	// 흑백효과 처리
	private ColorAdjustments colorAdjustments;
	private Bloom bloom;

	private void Awake()
	{
		solihoutte = GetComponent<Silhouette>();
	}

	private void Start()
	{
		if (globalVolume == null)
		{
			Debug.LogError("Global Volume이 할당되지 않았음");
			return;
		}

		if (!globalVolume.profile.TryGet(out colorAdjustments))
			Debug.LogError("Volume Profile에 없음");
		if (!globalVolume.profile.TryGet(out bloom))
			Debug.LogError("Volume Profile에 없음");
	}

	public void EnterSlow(float factor = slowFactor)
	{
		print($"slow duration: {factor}");
		if (!isPlayerSlow)
		{
			// 슬로우 코루틴 시작
			isPlayerSlow = true;
			StartSlow(factor);

			solihoutte.Active = true;
		}
	}

	public void ExitSlow()
	{
		if(isPlayerSlow)
		{
			isPlayerSlow = false;
			solihoutte.Active = false;
			StopSlow();
		}
	}

	private void StartSlow(float factor)    // 슬로우 효과 시작
	{
        Time.timeScale = factor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;
		//transform.Translate(Vector3.forward * GameManager.Instance.playerStats.speed * Time.deltaTime * 0.5f);
		if (colorAdjustments != null)
			colorAdjustments.saturation.value = -100f;
		if (bloom != null)
			bloom.intensity.value = 3;
		mixer.SetFloat("MasterCutoff", 1000f);   // 먹먹
	}

	private void StopSlow()     // 슬로우 효과 종료
	{
		if (isPlayerSlow)
			return;
		Time.timeScale = 1f;            // 시간 원래대로
		Time.fixedDeltaTime = 0.02f;
		if (colorAdjustments != null)
			colorAdjustments.saturation.value = 0f;
		if (bloom != null)
			bloom.intensity.value = 0.8f;
		mixer.SetFloat("MasterCutoff", 22000f); // 원래 소리
		solihoutte.DefaultSet();		// 실루엣 기본상태로 변경
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
