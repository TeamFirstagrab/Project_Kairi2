using UnityEngine;
using tagName = Globals.TagName;
using Globals;

/// <summary>
/// 오브젝트 공통 기반 클래스
/// 바닥 체크 / 충돌 상태 / 풀링 초기화 등 공유 로직 담당
/// </summary>
public abstract class BaseObject : MonoBehaviour, IDamageable
{
	//public bool isGrounded;
	public bool hasCollided = false;

	protected Rigidbody2D rigid;
	protected SpriteRenderer spriteRenderer;

	protected virtual void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	//protected virtual void Start()
	//{
	//	isGrounded = true;
	//}

	/// <summary>
	/// 오브젝트 풀 재사용 시 초기화
	/// </summary>
	public virtual void Init() { }

	public abstract void TakeDamage(int attack);

	// ──────────────────────────────────────────
	//  바닥 / 충돌 체크
	// ──────────────────────────────────────────

	//protected void CheckGround(Collision2D collision)
	//{
	//	foreach (var contact in collision.contacts)
	//	{
	//		if (contact.normal.y > 0.7f &&
	//			contact.point.y < transform.position.y)
	//		{
	//			isGrounded = true;
	//			break;
	//		}
	//	}

	//	hasCollided = true;

	//	// y값 보정 (바닥 뚫림 방지)
	//	if (isGrounded && rigid.linearVelocityY < 0f)
	//		rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0f);
	//}

	//protected virtual void OnCollisionEnter2D(Collision2D collision)
	//{
	//	CheckGround(collision);
	//}

	//protected virtual void OnCollisionStay2D(Collision2D collision)
	//{
	//	CheckGround(collision);
	//}

	//protected virtual void OnCollisionExit2D(Collision2D collision)
	//{
	//	if (collision.gameObject.CompareTag(tagName.ground))
	//		isGrounded = false;
	//}
}
