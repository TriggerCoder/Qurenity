using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorCollider : MonoBehaviour
{
	public DoorController door;
	public int Damage { get { return door.damage; } }
	void OnCollisionEnter(Collision collision)
	{
		if (GameManager.Paused)
			return;

		if (door.CurrentState != DoorController.State.Closing)
			return;

		PlayerThing playerThing = collision.gameObject.GetComponent<PlayerThing>();
		if (playerThing == null)
			return;

		playerThing.Damage(Damage, DamageType.Crusher);
		if (!door.crusher)
			door.CurrentState = DoorController.State.Opening;
	}
}
