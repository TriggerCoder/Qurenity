using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPosition : MonoBehaviour
{
	void OnEnable()
	{
		SpawnerManager.AddToList(gameObject);
	}
}
