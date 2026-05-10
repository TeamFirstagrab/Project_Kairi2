using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorController : MonoBehaviour, IInteractionObject
{
	private Animator animator;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	public void OnInteract()	// 상호작용: 문 열기
	{
		GameManager.Instance.cameraShake.ShakeForSeconds();     // 카메라 쉐이킹
		animator.Play("Door_Open");
		GetComponent<Collider2D>().enabled = false;
	}
}
