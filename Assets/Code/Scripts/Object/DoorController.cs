using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorController : MonoBehaviour
{
	[HideInInspector] public Animator animator;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

}
