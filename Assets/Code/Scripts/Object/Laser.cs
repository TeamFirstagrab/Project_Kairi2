using Globals;
using UnityEngine;

public class Laser : MonoBehaviour
{
	private void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.TryGetComponent<IDamageable>(out var damage))
		{
			damage.TakeDamage(99);      // 데미지 입히기
		}
	}
}
