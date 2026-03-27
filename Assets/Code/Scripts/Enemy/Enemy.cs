using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
	public void TakeDamage(int attack)
	{
		Debug.Log("Damaged");
	}

	public void Die()
	{
		throw new System.NotImplementedException();
	}
}
