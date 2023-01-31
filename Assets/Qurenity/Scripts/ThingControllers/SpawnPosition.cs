using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPosition : MonoBehaviour
{
	void Start()
	{
		SpawnerManager.AddToList(gameObject);
	}
}
