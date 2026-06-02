using UnityEditorInternal;
using UnityEngine;
using tagName = Globals.TagName;

public class EnemyBullet : MonoBehaviour
{
	[Header("속도")]
	public float speed = 8f;
	[Header("총알 생존 시간")]
	public float lifeTime = 2f;
	[Header("패링 시 향상시킬 발사 속도")]
	public float parryingSpeed = 1.5f;
	private Vector2 moveDir;
	private Rigidbody2D rb;
	private bool isReverseDir = false;  // 튕겨졌는지 여부
	private int damageval = 1;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		if (TryGetComponent<Collider2D>(out var col))
			col.isTrigger = true;
	}

	private void Start()
	{
		Invoke("ReturnToPool", lifeTime);
	}

	public void Launch(Vector3 targetPos)
	{
		Vector2 dir = (Vector2)(targetPos = transform.position).normalized;

		rb.linearVelocity = dir * speed;

		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);   // 총알 날아가는 방향 설정

	}

	void OnEnable()
	{
		GameObject player = GameManager.Instance.playerObj;       // 발사 순간 플레이어 방향 고정
		float dirX = player.transform.position.x - transform.position.x;

		if (player != null)
			moveDir = dirX > 0 ? Vector2.right : Vector2.left;
		else
			moveDir = transform.right;

		Invoke(nameof(ReturnToPool), lifeTime);     // 생존 시간 후 풀로 반환
	}

	void Update()
	{
		transform.Translate(moveDir * speed * Time.deltaTime, Space.World);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		// 플레이어 패링 전 플레이어가 총알에 맞았을 때
		if (!isReverseDir)
		{
			if (other.CompareTag(tagName.player))
			{
				Debug.Log("플레이어 Bullet 피격");

				IDamageable damage = other.GetComponent<IDamageable>();
				if (damage != null)
				{
					damage.TakeDamage(damageval);  // 데미지 주기
				}
			}
		}
		// 패링된 총알에 적이 맞았을 때
		else
		{
			if (other.CompareTag(tagName.enemy))
			{
				// 적에게 데미지 주기
				if (other.TryGetComponent<IDamageable>(out var damageable))
				{
					damageable.TakeDamage(GameManager.Instance.playerStatsRuntime.attack);
				}
				ReturnToPool();		// 오브젝트 회수
			}
		}

		// 벽, 땅 등 지형에 닿으면 제거
		if (other.CompareTag(tagName.ground) || other.CompareTag(tagName.wall))
		{
			ReturnToPool();
		}
	}

	void ReturnToPool()
	{
		Debug.Log($"Return bullet");
		GameManager.Instance.poolManager.ReturnToPool(gameObject);
	}

	void OnDisable()
	{
		CancelInvoke();     // 풀에서 다시 꺼낼 때 중복 Invoke 방지
	}

	public void DeflectBullet(Vector3 mousePos)     // 튕겨져나가기
	{
		isReverseDir = true;

		// 조준된 마우스 방향으로 날아가도록 새로운 방향 벡터 연산
		Vector2 deflectDir = (Vector2)(mousePos - transform.position).normalized;

		// 패링 시 속도 향상
		rb.linearVelocity = deflectDir * (speed * parryingSpeed);

		// 패링 시 날아갈 방향 재설정 (튕겨져 나가게)
		float angle = Mathf.Atan2(deflectDir.y, deflectDir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);

		// DEBUG: 패링 확인용 총알 색상 변경
		if (TryGetComponent<SpriteRenderer>(out var sr))
		{
			sr.color = Color.cyan;
		}
	}
}
