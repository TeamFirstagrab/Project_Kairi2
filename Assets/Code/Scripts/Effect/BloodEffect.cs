using System.Collections.Generic;
using UnityEngine;

public class BloodEffect : MonoBehaviour
{
	[SerializeField] private List<Sprite> bloodSprites;
	[Range(0f, 30f)] public float dist;			// 피 뿌려지는 거리 범위
	[Range(0f, 1f)] private float bloodDist;	// 피 간격
	[SerializeField] private bool isActive = false;		// 활성화 여부
	[SerializeField] private float activeTime = 0.5f;   // 뿌려지는 시간


	private SpriteRenderer spriteRenderer;
	private float activeTimer;
	private int spriteIdx; // 스프라이트 출력 인덱스

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void ActiveBloodEffect(Vector2 dir)
	{
		Debug.Log($"Active blood effect");
		spriteIdx = Random.Range(0, bloodSprites.Count);
		spriteRenderer.sprite = bloodSprites[spriteIdx];
	}

	private void Update()
	{
		if(isActive)
			ActiveBloodEffect(Vector2.up);
	}
}
