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
    public void Deflect(Vector3 mousePosition)
    {
        // 1. 튕겨 나간 상태로 속성을 전환합니다.
        isDeflected = true;

        // 2. 조준된 마우스 방향으로 날아가도록 새로운 비행 방향 벡터를 연산합니다.
        Vector2 deflectDirection = (Vector2)(mousePosition - transform.position);
        deflectDirection.Normalize();

        // 3. 패링의 쾌감을 높이기 위해 비행 속도를 기존 속도보다 1.5배 빠르게 향상시킵니다.
        rb.linearVelocity = deflectDirection * (speed * 1.5f);

        // 4. 새로운 비행 벡터에 부합하게 총알의 2D 회전각을 재설정합니다.
        float angle = Mathf.Atan2(deflectDirection.y, deflectDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 5. 패링되었음을 시각적으로 알려주기 위해 총알의 색상을 연한 푸른색(하늘색)으로 변경합니다.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.cyan;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 상황 A: 정상 상황 (적군이 발사하여 아직 플레이어 칼에 맞기 전)
        if (!isDeflected)
        {
            if (collision.CompareTag("Player"))
            {
                IDamageable playerDamageable = collision.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.TakeDamage(damageValue, rb.linearVelocity.normalized);
                }
                Destroy(gameObject);
            }
        }
        // 상황 B: 패링 반사 상황 (플레이어가 쳐내어 적을 향해 역으로 날아가는 상태)
        else
        {
            if (collision.CompareTag("Enemy"))
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
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
