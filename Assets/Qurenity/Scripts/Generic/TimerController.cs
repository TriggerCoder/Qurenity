using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerController : MonoBehaviour
{
	public int waitTime;
	public int randomTime;
	private float nextActivateTime;
	private TriggerController trigger;
	float time = 0f;

	#if UNITY_EDITOR
		public GameObject target;
	#endif

	public void Init(int wait, int random, TriggerController tc)
	{
		waitTime = wait;
		randomTime = random;
		trigger = tc;

		nextActivateTime = Random.Range(waitTime - randomTime, waitTime + randomTime + 1);
		if (trigger == null)
			enabled = false;
		
		#if UNITY_EDITOR
			target = tc.gameObject;
		#endif
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		time += Time.deltaTime;

		if (time >= nextActivateTime)
		{
			time = 0f;
			nextActivateTime = Random.Range(waitTime - randomTime, waitTime + randomTime + 1);
			trigger.Activate(null);
		}
	}
}
