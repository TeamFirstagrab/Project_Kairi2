using Globals;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("총알 비행 스펙")]
    public float speed = 10f;
    public float maxLifeTime = 5f;

    private Rigidbody2D rb;
    private int damageValue = 1;

    // 방법 B의 핵심: 플레이어 칼에 튕겨 나갔는지 여부를 판별하는 변수
    private bool isDeflected = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        Destroy(gameObject, maxLifeTime);
    }

    public void Launch(Vector3 targetPosition, int damage)
    {
        damageValue = damage;
        Vector2 direction = (Vector2)(targetPosition - transform.position);
        direction.Normalize();

        rb.linearVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

	/// <summary>
	/// 플레이어의 칼 공격에 맞았을 때, 플레이어 스크립트로부터 강제 호출되는 반사 연산 함수입니다.
	/// </summary>
	/// <param name="mousePosition">현재 플레이어의 마우스 월드 좌표</param>
	public void Deflect(Vector2 dir)
	{
		isDeflected = true;

		// 기존 진행 방향의 정확히 반대 방향
		Vector2 deflectDirection = -dir.normalized;

		// 반사된 총알 속도 설정
		rb.linearVelocity = deflectDirection * (speed * 1.5f);

		// 총알 스프라이트가 날아가는 방향을 바라보도록 회전
		float angle = Mathf.Atan2(deflectDirection.y, deflectDirection.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
	}

	private void OnTriggerEnter2D(Collider2D collision)
    {
        // 상황 A: 정상 상황 (적군이 발사하여 아직 플레이어 칼에 맞기 전)
        if (!isDeflected)
        {
            if (collision.CompareTag(TagName.player))
            {
                IDamageable playerDamageable = collision.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
					print($"player IDamageable takeDamage 호출");
                    playerDamageable.TakeDamage(damageValue);
                }
                Destroy(gameObject);
            }
        }
        // 상황 B: 패링 반사 상황 (플레이어가 쳐내어 적을 향해 역으로 날아가는 상태)
        else
        {
            if (collision.CompareTag(TagName.enemy))
            {
                IDamageable enemyDamageable = collision.GetComponent<IDamageable>();
                if (enemyDamageable != null)
                {
                    // 반사된 탄환이므로 통쾌한 액션 보상으로 2배의 피해량을 줍니다.
                    enemyDamageable.TakeDamage(damageValue * 2, rb.linearVelocity.normalized);
                }
                Destroy(gameObject);
            }
        }

        // 공통: 벽 지형 등에 닿으면 탄환 제거
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Wall") || 
            collision.CompareTag("Ground") || 
            collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
