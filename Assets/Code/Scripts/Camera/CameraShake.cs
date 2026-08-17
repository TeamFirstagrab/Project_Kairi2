using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
	private CinemachineImpulseSource impulseSource;

	[SerializeField]
	private float magnitude = 0.05f;	// 세기

	[SerializeField]
	private float roughness = 0.05f;	// 빠르기

	private CinemachineCamera virtualCamera;
	private CinemachineBasicMultiChannelPerlin noise;

	private void Awake()
	{
		impulseSource = GetComponent<CinemachineImpulseSource>();
		virtualCamera = GetComponent<CinemachineCamera>();
		if (virtualCamera != null)
			noise = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
	}

	public void GenerateImpulse()
	{
		impulseSource.GenerateImpulse();
	}

	public void ShakeForSeconds(float m = 0.05f, float r = 0.05f, float d = 0.5f)
	{
		if (noise != null)
		{
			StopAllCoroutines();
			StartCoroutine(ShakeCoroutine(m, r, d));
		}
		else
			GenerateImpulse();
	}

	private IEnumerator ShakeCoroutine(float m, float r, float d)
	{
		noise.AmplitudeGain = m;
		noise.FrequencyGain = r;

		float elapsed = 0f;
		while (elapsed < d)
		{
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		noise.AmplitudeGain = 0f;
		noise.FrequencyGain = 0f;
	}
}