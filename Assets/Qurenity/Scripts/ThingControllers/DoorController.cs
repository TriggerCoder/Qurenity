using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
	private bool activated = false;

	public int Damage = 2;
	public float Lip = 8 * GameManager.sizeDividor;
	public bool Repeatable = true;
	public bool AutoReturn = true;
	public float AutoReturnTime = 3f;

	public float time = 0f;

	Transform cTransform;
	void Awake()
	{
		cTransform = transform;
	}

	private void MoveDoor(bool open)
	{
		if (activated != open)
			return;

		activated = !activated;
	}
	public bool Activate()
	{
		if ((!Repeatable) || (AutoReturn))
			if (activated)
				return false;

		if (AutoReturn)
			time = AutoReturnTime;

		MoveDoor(true);

		return true;
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		if (time <= 0)
			return;
		else
		{
			time -= Time.deltaTime;
			if (time <= 0)
			{
				MoveDoor(false);
			}
		}
	}
}
