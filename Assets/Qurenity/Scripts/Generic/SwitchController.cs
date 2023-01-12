using System.Collections;
using System.Collections.Generic;
using Assets.MultiAudioListener;
using UnityEngine;
public class SwitchController : DoorController
{
	public TriggerController internalSwitch;
	public override float waitTime { get { return internalSwitch.AutoReturnTime; } set { internalSwitch.AutoReturnTime = value; } }
	public override bool Activated { get { return internalSwitch.activated; } set { internalSwitch.activated = value; } }
}
