using System.ComponentModel.Design;
using UnityEngine;

public class LaserObject : MonoBehaviour
{
	[SerializeField]
	private GameObject LaserPrefeb;
	private GameObject Laser;
	public bool isActive = true;

	private void Start()
	{
		Laser = Instantiate(LaserPrefeb);
	}

	private void Update()
	{
		if(isActive)
		{
			Laser.SetActive(true);
		}
		else
		{
			Laser.SetActive(false);
		}
	}
}
