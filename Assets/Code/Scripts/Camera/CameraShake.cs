using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
	[SerializeField]
	private CinemachineImpulseSource impulseSource;

	[SerializeField]
	private float magnitude = 0.05f;	// 세기

	[SerializeField]
	private float roughness = 0.05f;	// 빠르기

	private CinemachineCamera virtualCamera;
	private CinemachineBasicMultiChannelPerlin noise;

	private void Awake()
	{
		virtualCamera = GetComponent<CinemachineCamera>();
		if (virtualCamera != null)
			noise = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
	}

	public void GenerateImpulse()
	{
		impulseSource.GenerateImpulse();
	}

	public void ShakeForSeconds(float duration = 0.5f)
	{
		if (noise != null)
		{
			StopAllCoroutines();
			StartCoroutine(ShakeCoroutine(duration));
		}
		else
			GenerateImpulse();
	}

	private IEnumerator ShakeCoroutine(float duration)
	{
		noise.AmplitudeGain = magnitude;
		noise.FrequencyGain = roughness;

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		noise.AmplitudeGain = 0f;
		noise.FrequencyGain = 0f;
	}
}