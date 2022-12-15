using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .017f; } } // tan(2º) / 2
	public override float maxDispersion { get { return .049f; } } // tan(5.6º) / 2

	public float maxRange = 400f;

	public float barrelSpeed = 400;

	private float currentRotSpeed = 0;
	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[0] <= 0 && fireTime < .1f)
			putAway = true;
	}

	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[0] <= 0)
			return false;

		playerInfo.Ammo[0]--;

		if (GameOptions.UseMuzzleLight)
			if (muzzleLight != null)
			{
				muzzleLight.intensity = 1;
				muzzleLight.enabled = true;
				if (muzzleObject != null)
					if (!muzzleObject.activeSelf)
						muzzleObject.SetActive(true);
			}

		//maximum fire rate 20/s, unless you use negative number (please don't)
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		if (Sounds.Length > 0)
		{
			audioSource.AudioClip = Sounds[0];
			audioSource.Play();
		}

		return true;
	}
	protected override Quaternion GetRotate()
	{
		if (fireTime > 0f)
		{
			currentRotSpeed += barrelSpeed * Time.deltaTime;
			if (currentRotSpeed < -180)
				currentRotSpeed += 360;
			if (currentRotSpeed > 180)
				currentRotSpeed -= 360;
		}
		return Quaternion.AngleAxis(currentRotSpeed, Vector3.right);
	}
}
