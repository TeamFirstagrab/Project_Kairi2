using UnityEngine;

public class InteractionController : MonoBehaviour, IInteractionObject
{
	[Header("터지는 오브젝트")]
	public bool explosionObject;
	[Header("폭발 이펙트")]
	public GameObject explosionEffectPrefab;
	[Header("폭발 범위")]
	public float explosionRadiaus = 2f;
	[Header("부서지는 오브젝트")]
	public bool crackObject = false;
	[Header("크랙 스프라이트 단계")]
	public Sprite[] crackSprites;
	[Header("최대 내구도")]
	public int maxCount = 3;
	private int count = 0;      // 현재 내구도

	private void Update()
	{

	}

	public void UpdateSprite()
	{
		throw new System.NotImplementedException();
	}

	public void Destroy()
	{
		throw new System.NotImplementedException();
	}

	// Explode
	public void OnInteract()
	{

	}
}
