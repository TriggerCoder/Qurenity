using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlasmagunWeapon : PlayerWeapon
{
	public GameObject AttackProjectile;
	public string AttackProjectileName;
	public Vector3 spawnPos;
	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[6] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioSource.AudioClip = Sounds[1];
				audioSource.Play();
			}
			putAway = true;
		}
	}
	protected override void OnInit()
	{
		if (AttackProjectile != null)
		{
			if (!PoolManager.HasObjectPool(AttackProjectileName))
				PoolManager.CreateProjectilePool(AttackProjectileName, AttackProjectile, 20);
		}
		if (Sounds.Length > 2)
		{
			audioSource.AudioClip = Sounds[2];
			audioSource.Play();
		}
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[6] <= 0)
			return false;

		playerInfo.Ammo[6]--;

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
			if (audioSource.AudioClip != Sounds[0])
				audioSource.AudioClip = Sounds[0];
			audioSource.Play();
		}

		//Projectile attack
		{
			Vector3 d = playerInfo.playerCamera.MainCamera.transform.forward;
			Vector2 r = GetDispersion();
			d += playerInfo.playerCamera.MainCamera.transform.right * r.x + playerInfo.playerCamera.MainCamera.transform.up * r.y;
			d.Normalize();

			PoolObject<Projectile> projectile = PoolManager.GetProjectileFromPool(AttackProjectileName);
			Projectile plasma = (Projectile)projectile.data;
			plasma.owner = playerInfo.gameObject;
			if (muzzleObject != null)
				plasma.transform.position = muzzleObject.transform.position;
			else
				plasma.transform.position = playerInfo.playerCamera.MainCamera.transform.position + (playerInfo.playerCamera.MainCamera.transform.right * spawnPos.x) + (playerInfo.playerCamera.MainCamera.transform.up * spawnPos.y) + (playerInfo.playerCamera.MainCamera.transform.forward * spawnPos.z);
			plasma.transform.rotation = Quaternion.LookRotation(d);
			plasma.transform.SetParent(GameManager.Instance.BaseThingsHolder);
		}

		return true;
	}
}
