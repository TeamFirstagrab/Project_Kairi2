using UnityEngine;
using tagName = Globals.TagName;
using Globals;

/// <summary>
/// 부서지는 오브젝트
/// 총알 트리거 충돌마다 내구도 감소 → 스프라이트 변경 → 내구도 0 시 풀 반환
/// </summary>
public class CrackObject : BaseObject
{
	[Header("크랙 스프라이트 단계")]
	public Sprite[] crackSprites;

	[Header("최대 내구도")]
	public int maxCount = 3;

	public int count;   // 현재 내구도

	// ──────────────────────────────────────────
	//  초기화
	// ──────────────────────────────────────────

	protected override void Awake()
	{
		base.Awake();
		count = maxCount;
		UpdateCrackSprite();
	}

	//protected override void Start()
	//{
	//	base.Start();
	//}

	public override void Init()
	{
		count = maxCount;
		UpdateCrackSprite();
	}

	// ──────────────────────────────────────────
	//  스프라이트 갱신
	// ──────────────────────────────────────────

	private void UpdateCrackSprite()
	{
		if (crackSprites == null || crackSprites.Length == 0)
			return;

		if (count <= 0)
		{
			GameManager.Instance.poolManager.ReturnToPool(gameObject);
			return;
		}

		// 내구도 비율로 스프라이트 인덱스 결정
		float ratio = (float)count / maxCount;
		int index = Mathf.Clamp(
			Mathf.FloorToInt((1f - ratio) * crackSprites.Length),
			0, crackSprites.Length - 1);

		spriteRenderer.sprite = crackSprites[index];
	}

	// ──────────────────────────────────────────
	//  트리거 / 충돌 처리
	// ──────────────────────────────────────────

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag(tagName.bullet))
		{
			--count;
			UpdateCrackSprite();
		}
	}

	// ──────────────────────────────────────────
	//  IDamageable
	// ──────────────────────────────────────────

	public override void TakeDamage(int attack)
	{
		Destroy(gameObject);
	}
}
