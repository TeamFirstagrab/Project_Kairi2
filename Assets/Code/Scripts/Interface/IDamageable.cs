using UnityEngine;

// 데미지 관련 인터페이스
public interface IDamageable
{
	public void TakeDamage(int attack);    // 데미지 입기
	public void Die();	// 죽음 처리
}