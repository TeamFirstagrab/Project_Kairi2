using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorController : MonoBehaviour
{
	private Animator animator;
	private Collider2D collider;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		collider = GetComponent<Collider2D>();
	}

	public void OpenDoor()
	{
		animator.Play("Door_Open");
		collider.isTrigger = true;
	}
}
