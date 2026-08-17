using UnityEngine;

// 데미지 관련 인터페이스
public interface IDamageable
{
	public void TakeDamage(int attack, Vector2 attackDirection);    // 데미지 입기
}