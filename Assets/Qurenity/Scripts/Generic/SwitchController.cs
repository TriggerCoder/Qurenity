using System.Collections;
using System.Collections.Generic;
using Assets.MultiAudioListener;
using UnityEngine;
public class SwitchController : DoorController
{
	public TriggerController internalSwitch;
	public override float waitTime { get { return internalSwitch.AutoReturnTime; } set { internalSwitch.AutoReturnTime = value; } }
	public override bool Activated { get { return internalSwitch.activated; } set { internalSwitch.activated = value; } }
	public override void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
	{
		if (Dead)
			return;

		if (Activated)
			return;

		if (tc == null)
		{
			TriggerController swTrigger;
			if (!ThingsManager.triggerToActivate.TryGetValue(internalSwitch.triggerNum, out swTrigger))
				return;
			tc = swTrigger;
		}
		CurrentState = State.Opening;
		tc.Activate(null);
	}
}
