using System.Collections;
using UnityEngine;

public class BaseEffect : MonoBehaviour
{
	[Header("발동할 시간")]
	public float activeTime = 1.0f;

	private void OnEnable()
	{
		Invoke(nameof(DestroySelf), activeTime);
	}

	private void DestroySelf()
	{
		Destroy(gameObject);
	}
}
