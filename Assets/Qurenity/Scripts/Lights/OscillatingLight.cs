using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class OscillatingLight : MonoBehaviour
{
	Light l;
	public float lowIntensity = 0f;
	public float highIntensity = 1f;

	public float speed = 4;

	void Awake()
	{
		l = GetComponent<Light>();
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		l.intensity = Mathf.Lerp(lowIntensity, highIntensity, Mathf.Sin(Time.time * speed) * .5f + .5f);
	}
}
