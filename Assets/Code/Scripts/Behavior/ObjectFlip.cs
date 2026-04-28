using UnityEngine;

public class ObjectFlip : MonoBehaviour
{
	// 스프라이트 반전 함수
	public void Flip()
	{
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
	}
}
