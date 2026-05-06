using UnityEngine;
using tagName = Globals.TagName;

public class EnemyBullet : MonoBehaviour
{
    [Header("속도")]
    public float speed = 8f;
    [Header("생존 시간")]
    public float lifeTime = 2f;
    private Vector2 moveDir;
	private bool isReverseDir = false;	// 튕겨졌는지 여부

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
        if (other.CompareTag(tagName.player))
        {
            Debug.Log("플레이어 Bullet 피격");

			IDamageable damage = other.GetComponent<IDamageable>();
			if (damage != null)
			{
				damage.TakeDamage(GameManager.Instance.playerStatsRuntime.attack);  // 데미지 주기
			}

        }
		if(isReverseDir && other.CompareTag(tagName.enemy))
		{
			// 데미지 주기
			other.GetComponent<IDamageable>()?.TakeDamage(GameManager.Instance.playerStatsRuntime.attack);
		}
        if (!other.isTrigger && !other.CompareTag(tagName.enemy) && !other.CompareTag(tagName.bullet))
            ReturnToPool();
    }

    void ReturnToPool()
    {
        GameManager.Instance.poolManager.ReturnToPool(gameObject);
    }

    void OnDisable()
    {
        CancelInvoke();     // 풀에서 다시 꺼낼 때 중복 Invoke 방지
    }

	public void DeflectBullet()     // 튕겨져나가기
	{
		moveDir = moveDir.x > 0 ? Vector2.left : Vector2.right;
		isReverseDir = true;
	}
}
