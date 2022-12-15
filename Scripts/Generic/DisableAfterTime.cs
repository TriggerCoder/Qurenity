using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableAfterTime : MonoBehaviour
{

	public float _lifeTime = 1;
	float time = 0f;

	void Update()
	{
		if (GameManager.Paused)
			return;

		time += Time.deltaTime;

		if (time >= _lifeTime)
		{
			//Reset Timer
			time = 0f;
			gameObject.SetActive(false);
		}
	}
}
