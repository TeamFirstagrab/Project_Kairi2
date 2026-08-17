using UnityEngine;

public class ThrownProjectile : MonoBehaviour
{
    private Rigidbody2D rigid;
    private float speed;
    private Vector2 direction;
    private bool isInitialized = false;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir, float launchSpeed)
    {
        direction = dir.normalized;
        speed = launchSpeed;

        rigid.gravityScale = 0f; // ߷  
        rigid.linearVelocity = direction * speed;

        //  ⿡  Ʈ ȸ  
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        isInitialized = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInitialized) return;

        // 1. (Enemy) 浹     Ʈ ı
        if (collision.CompareTag("Enemy"))
        {
            //  Ʈ Enemy Ʈ   Լ ȣ
            if (collision.TryGetComponent<Enemy>(out var enemy))
            {
                // 100  ְ ô (direction) 
                enemy.TakeDamage(100, direction, false);
            }

            BreakObject(); // ɺ 
        }
        // 2. (Ground), (Wall), ׸ (Door) 浹  Ʈ ı
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall") || collision.CompareTag("Door"))
        {
            BreakObject(); // ɺ 
        }
    }

    private void BreakObject()
    {
        // TODO:  ƼŬ̳  Ҹ    
        Destroy(gameObject);
    }
}