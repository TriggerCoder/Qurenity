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

	[System.Serializable]
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		Player
	}

	public ThingType thingType = ThingType.Decor;

}
