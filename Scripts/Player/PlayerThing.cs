using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerThing : MonoBehaviour
{
	public GameObject player;
	public PlayerModel avatar;
	void Start()
	{
		player = new GameObject();
		avatar = player.AddComponent<PlayerModel>();
		player.transform.SetParent(transform.parent);
		avatar.LoadPlayer("sarge");
	}
}
