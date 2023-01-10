using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThingController : MonoBehaviour
{
	[HideInInspector]
	public Vector3 location;
	[HideInInspector]
	public Quaternion angularrotation;

	public string thingName;
	public string respawnSound = "items/respawn1";
	public float respawnTime;

	[System.Serializable]
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		JumpPad,
		TargetDestination,
		Trigger,
		Door,
		Player
	}

	public ThingType thingType = ThingType.Decor;

	void OnDisable()
	{
		if (GameManager.Instance.ready)
			if (respawnTime > 0)
				ThingsManager.AddItemToRespawn(gameObject, respawnSound, respawnTime);
	}
}
