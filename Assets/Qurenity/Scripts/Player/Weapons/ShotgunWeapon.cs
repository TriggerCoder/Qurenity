using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .049f; } } // tan(4º) / 2
	public override float maxDispersion { get { return .062f; } } //tan(7.1º) / 2

	public string caseName;

	public float vDispersion = .7f;
	public float maxRange = 400f;
	public float pushForce = 350;

	protected override void OnUpdate()
	{
		if (playerInfo.Ammo[1] <= 0 && fireTime < .1f)
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

		if (playerInfo.Ammo[1] <= 0)
			return false;

		playerInfo.Ammo[1]--;

		if (GameOptions.UseMuzzleLight)
			if (muzzleLight != null)
			{
				muzzleLight.intensity = 1;
				muzzleLight.enabled = true;
				if (muzzleObject != null)
					if (!muzzleObject.activeSelf)
					{
						muzzleObject.SetActive(true);
						playerInfo.playerThing.avatar.MuzzleFlashSetActive(true);
					}
			}

		//maximum fire rate 20/s, unless you use negative number (please don't)
		fireTime = _fireRate + .05f;
		coolTimer = 0f;

		if (Sounds.Length > 0)
		{
			audioSource.AudioClip = Sounds[0];
			audioSource.Play();
		}

		//Hitscan attack
		Vector3 d = playerInfo.playerCamera.MainCamera.transform.forward;
		for (int i = 0; i < 11; i++)
		{
			Vector2 r = GetDispersion();
			d += playerInfo.playerCamera.MainCamera.transform.right * r.x + playerInfo.playerCamera.MainCamera.transform.up * r.y;
			d.Normalize();

			Ray ray = new Ray(playerInfo.playerCamera.MainCamera.transform.position, d);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, maxRange, ~((1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.PlayerLayer) | (1 << GameManager.RagdollLayer)), QueryTriggerInteraction.Ignore))
			{
				Damageable target = hit.collider.gameObject.GetComponent<Damageable>();
				if (target != null)
				{
					target.Damage(Random.Range(DamageMin, DamageMax + 1), DamageType.Generic, playerInfo.gameObject);

					if (target.Bleed)
					{
						GameObject blood;
						switch (target.BloodColor)
						{
							default:
							case BloodType.Red:
								blood = PoolManager.GetObjectFromPool("BloodRed");
								break;
							case BloodType.Green:
								blood = PoolManager.GetObjectFromPool("BloodGreen");
								break;
							case BloodType.Blue:
								blood = PoolManager.GetObjectFromPool("BloodBlue");
								break;
						}
						blood.transform.position = hit.point - ray.direction * .2f;
					}
					else
					{
						GameObject puff = PoolManager.GetObjectFromPool("BulletHit");
						puff.transform.position = hit.point - ray.direction * .2f;
					}
				}
				else
				{
					GameObject puff = PoolManager.GetObjectFromPool("BulletHit");
					puff.transform.position = hit.point - ray.direction * .2f;
					puff.transform.right = -hit.normal;
					//puff.transform.right = -ray.direction;
					if (Sounds.Length > 3)
						AudioManager.Create3DSound(puff.transform.position, Sounds[Random.Range(3, Sounds.Length)], 5f, 1);

					//Check if collider can be marked
					if (!MapLoader.noMarks.Contains(hit.collider))
					{
						GameObject mark = PoolManager.GetObjectFromPool("BulletMark");
						mark.transform.position = hit.point + hit.normal * .05f;
						mark.transform.forward = hit.normal;
					}
					Debug.Log(hit.collider.name);
				}
			}

		}

		//Case Drop
		if (!string.IsNullOrEmpty(caseName))
		{
			for (int i = 0; i < 2; i++)
			{
				PoolObject<Rigidbody> ammocase = PoolManager.GetRigidBodyFromPool(caseName);
				Rigidbody rb = (Rigidbody)ammocase.data;
				ammocase.go.transform.position = transform.position;
				ammocase.go.transform.SetParent(GameManager.Instance.BaseThingsHolder);
				Vector3 force = new Vector3(Random.Range(-100f, 100f), Random.Range(100f, 200f), Random.Range(0, 100f));
				rb.AddForce(force);
			}
		}

		return true;
	}
}
