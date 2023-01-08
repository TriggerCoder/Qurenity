using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPadThing : MonoBehaviour
{
	public string BoingSound;

	private Vector3 destination;
	private Vector3 position;
	public void Init(Vector3 dest)
	{
		destination = dest;
		position = transform.position;
	}

	void OnTriggerEnter(Collider other)
	{
		if (GameManager.Paused)
			return;

		Damageable d = other.GetComponent<Damageable>();
		if (d == null)
			return;

		if (!string.IsNullOrEmpty(BoingSound))
			AudioManager.Create3DSound(position, BoingSound, 1f);

		d.JumpPadDest(destination);
	}
}
