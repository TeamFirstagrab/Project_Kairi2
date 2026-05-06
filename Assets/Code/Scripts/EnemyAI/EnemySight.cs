using Globals;
using UnityEngine;
using UnityEngine.UI;

public class EnemySight : MonoBehaviour
{
	private Transform _player;
	private EnemyDataManager _dataMng;
	private EnemyStats _enemyData;

	private void Start()
	{
		_dataMng = GetComponent<EnemyDataManager>();
		_player = GameManager.Instance.playerObj.transform;
		_enemyData = _dataMng._enemyStats;
	}

	// 플레이어가 바라보고 있는 방향 시야에 있는지
	public bool IsPlayerInRange()
	{
		LayerMask mask = ~LayerMask.GetMask(LayerName.enemy);
		Vector2 forwardDir = (_player.position - transform.position).normalized;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, forwardDir, _enemyData.SightRange, mask);
		Collider2D col = Physics2D.OverlapCircle(transform.position, _enemyData.SightRoundRange, mask);

		Vector2 dir = (_player.position - transform.position).normalized;
		float dx = _player.position.x - transform.position.x;
		float forward = -Mathf.Sign(transform.localScale.x);
		bool isFacing = dx * forward > 0f;

		return ((col != null && col.CompareTag(TagName.player))) || (isFacing && hit.transform.CompareTag(TagName.player));
	}

	void OnDrawGizmos()
	{
		// 근접 시야 범위
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, GetComponent<EnemyDataManager>()._enemyStats.SightRoundRange);
	}
}
