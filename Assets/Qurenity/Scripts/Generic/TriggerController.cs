using System;
using UnityEngine;
using UnityEngine.Events;
public class TriggerController : MonoBehaviour
{
	public string triggerName = "";
	public bool activated = false;
	private Action<PlayerThing> OnActivate = new Action<PlayerThing>((p) => { return; });

	public bool Repeatable = false;
	public bool AutoReturn = false;
	public float AutoReturnTime = 1f;

	public Func<bool> PreReq = new Func<bool>(() => { return true; });

	public float time = 0f;

	public void SetController(string name, Action<PlayerThing> activeAction)
	{
		triggerName = name;
		OnActivate = activeAction;
	}
	public bool Activate(PlayerThing playerThing)
	{
		if (!PreReq())
		{
			Debug.Log("TriggerController: Prereq False for: "+ triggerName);
			return false;
		}

		if ((!Repeatable) || (AutoReturn))
			if (activated)
				return false;

		if (AutoReturn)
			time = AutoReturnTime;

		activated = !activated;
		OnActivate.Invoke(playerThing);

		return true;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (time <= 0)
			return;
		else
		{
			time -= Time.deltaTime;
			if (time <= 0)
			{
				activated = !activated;
			}
		}
	}
	void OnTriggerEnter(Collider other)
	{
		if (GameManager.Paused)
			return;

		PlayerThing playerThing = other.GetComponent<PlayerThing>();
		if (playerThing == null)
			return;

		//Dead player don't activate stuff
		if (playerThing.Dead)
			return;

		Activate(playerThing);
	}
}
