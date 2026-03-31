using UnityEngine;
using System.Collections;
using tagName = Globals.TagName;    // 태그
using Globals;

public class ObjectController : MonoBehaviour
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
	public int count;          // 현재 내구도
	public bool isGrounded;
	public bool hasCollided = false;
	Rigidbody2D rigid;
	SpriteRenderer sprite;


	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		sprite = GetComponent<SpriteRenderer>();
		count = maxCount;
		UpdateCrackSprite();
	}

	void Start()
	{
		isGrounded = true;
	}
	void Update()
	{
		UpdateCrackSprite();

		//// 현재 태그에 따라 레이어마스크 대상 변경하기
		//if (gameObject.CompareTag(tagName.throwingObj))
		//	gameObject.layer = LayerMask.NameToLayer(tagName.throwingObj);
		//else if (gameObject.CompareTag(tagName.obj))
		//	gameObject.layer = LayerMask.NameToLayer(tagName.obj);
	}
	public void Init()
	{
		// 재사용 풀링 대비 초기화
		count = maxCount;
		UpdateCrackSprite();
	}

	public void CheckGround(Collision2D collision)
	{
		foreach (var contact in collision.contacts)     // 바닥 체크
		{
			if (contact.normal.y > 0.7f &&
				contact.point.y < transform.position.y)
			{
				isGrounded = true;
				break;
			}
		}

		// 충돌 체크
		hasCollided = true;

		// y값 보정 (바닥 뚫림 방지)
		if (isGrounded && rigid.linearVelocityY < 0f)
			rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
	}

	void UpdateCrackSprite()
	{
		if (!crackObject || crackSprites == null || crackSprites.Length == 0)
			return;

		if (count <= 0)
		{
			if (explosionObject)
				Explode();
			else
			{
				GameManager.Instance.poolManager.ReturnToPool(gameObject);
			}
			return;
		}

		// 내구도 비율 스프라이트 인덱스
		float ratio = (float)count / maxCount;
		int index = Mathf.Clamp(Mathf.FloorToInt((1f - ratio) * crackSprites.Length), 0, crackSprites.Length - 1);

		sprite.sprite = crackSprites[index];
	}
	private void OnDrawGizmosSelected()
	{
		if (explosionObject)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, explosionRadiaus); // 반경 2
		}
	}

	void OnTriggerEnter2D(Collider2D collision)
	{
		if (crackObject && collision.CompareTag(tagName.bullet))
		{
			--count;
			UpdateCrackSprite();
		}
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		CheckGround(collision);     // 바닥 체크

		if (gameObject.CompareTag(tagName.throwingObj) && collision.gameObject.CompareTag(tagName.enemy))
		{
			if (collision.gameObject.TryGetComponent<Enemy>(out var target))
			{
				//// 첫 번째 접촉점 기준
				//ContactPoint2D contact = collision.contacts[0];

				//// normal은 "맞은 대상 기준으로 바깥 방향"
				//Vector2 hitDir = -contact.normal;
				//target.SetHitDirection(hitDir);
				target.TakeDamage(1);       // 닿은 적에게 데미지 주기
			}
		}

		if (CompareTag(tagName.throwingObj) && explosionObject && collision.gameObject.CompareTag(tagName.enemy))
		{
			if (collision.gameObject.TryGetComponent<Enemy>(out var target))
			{
				//// 첫 번째 접촉점 기준
				//ContactPoint2D contact = collision.contacts[0];

				//// normal은 "맞은 대상 기준으로 바깥 방향"
				//Vector2 hitDir = -contact.normal;
				//target.SetHitDirection(hitDir);
				Explode();
			}
		}
	}

	public void Explode()
	{
		//GameManager.Instance.audioManager.ObjectExplosionSound(1f);
		//GameManager.Instance.cameraShake.ShakeForSeconds(1f);
		Vector2 explosionPos = transform.position;
		Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadiaus);

		foreach (var hit in hits)
		{
			if (hit.CompareTag(tagName.enemy))
			{
				if (hit.TryGetComponent<Enemy>(out var target))
				{
					//Vector2 hitDir = (target.transform.position - transform.position).normalized;
					//target.SetHitDirection(hitDir);
					target.TakeDamage(1);
				}
			}
		}
		GameManager.Instance.StartCoroutine(SpawnExplosionEffect(explosionPos));
		GameManager.Instance.poolManager.ReturnToPool(gameObject);
	}
	IEnumerator SpawnExplosionEffect(Vector2 position)
	{
		GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
		yield return new WaitForSeconds(1.07f);
		Destroy(effect);
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		CheckGround(collision);     // 바닥 체크
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag(tagName.ground))
			isGrounded = false;
	}

}
