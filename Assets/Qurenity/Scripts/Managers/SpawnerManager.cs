using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager
{
	public static List<GameObject> deathMatchSpawner = new List<GameObject>();

	public static string respawnSound = "world/telein";

	public static void AddToList(GameObject go)
	{
		deathMatchSpawner.Add(go);
	}

	public static Vector3 FindSpawnLocation()
	{
		int index = Random.Range(0, deathMatchSpawner.Count);
		Vector3 destination = deathMatchSpawner[index].transform.position;
		AudioManager.Create3DSound(destination, respawnSound, 1f);
		return destination;
	}
}
