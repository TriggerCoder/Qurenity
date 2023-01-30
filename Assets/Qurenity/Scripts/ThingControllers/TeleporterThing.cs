using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterThing : MonoBehaviour
{
	public string TeleportInSound;
	public string TeleportOutSound;

	private Vector3 destination;
	private List<PlayerThing> toTeleport = new List<PlayerThing>();
	public void Init(Vector3 dest)
	{
		destination = dest;
	}
	public static void TelefragEverything(Vector3 position, GameObject go)
	{
		Collider[] hits = new Collider[10];

		int max = Physics.OverlapSphereNonAlloc(position, 2, hits, ((1 << GameManager.DamageablesLayer) |
													(1 << GameManager.Player1Layer) |
													(1 << GameManager.Player2Layer) |
													(1 << GameManager.Player3Layer) |
													(1 << GameManager.Player4Layer)), QueryTriggerInteraction.Ignore);

		if (max > hits.Length)
			max = hits.Length;

		for (int i = 0; i < max; i++)
		{
			Collider hit = hits[i];
			if (hit.gameObject == go)
				continue;

			Damageable d = hit.GetComponent<Damageable>();
			if (d != null)
				d.Damage(10000, DamageType.Telefrag, go);
		}
		return;
	}
	public void TeleportToDestination(Transform otherTransform)
	{
/*
		GameObject effect1 = PoolManager.GetObjectFromPool("TeleportEffect", false);
		effect1.transform.position = transform.position;
		effect1.SetActive(true);
*/
		otherTransform.position = destination;

/*
		GameObject effect2 = PoolManager.GetObjectFromPool("TeleportEffect", false);
		effect2.transform.position = transform.position;
		effect2.SetActive(true);
*/
	}
	void OnTriggerEnter(Collider other)
	{
		if (GameManager.Paused)
			return;

		PlayerThing playerThing = other.GetComponent<PlayerThing>();
		if (playerThing == null)
			return;

		if (!toTeleport.Contains(playerThing))
			toTeleport.Add(playerThing);
	}

	private void FixedUpdate()
	{
		if (GameManager.Paused)
			return;

		if (toTeleport.Count == 0)
			return;

		for (int i = 0; i < toTeleport.Count; i++)
		{
			PlayerThing playerThing = toTeleport[i];
			Transform otherTransform = playerThing.transform;

			if (!string.IsNullOrEmpty(TeleportOutSound))
				AudioManager.Create3DSound(otherTransform.position, TeleportOutSound, 1f);
		
			playerThing.playerInfo.CheckPVS(Time.frameCount, destination);
			TelefragEverything(destination,gameObject);
			TeleportToDestination(otherTransform);

			if (!string.IsNullOrEmpty(TeleportInSound))
				AudioManager.Create3DSound(destination, TeleportInSound, 1f);
//			playerThing.playerControls.viewDirection.y = teleporter.viewAngle;
		}
		toTeleport = new List<PlayerThing>();
	}
}
