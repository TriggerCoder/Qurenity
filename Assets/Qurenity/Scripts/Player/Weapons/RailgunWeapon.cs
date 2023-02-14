using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailgunWeapon : PlayerWeapon
{
	public override float avgDispersion { get { return .005f; } }
	public override float maxDispersion { get { return .01f; } }

	public Color railColor;

	public float maxRange = 400f;

	public GameObject AttackProjectile;
	public string AttackProjectileName;

	Material railMaterial = null;
	protected override void OnUpdate()
	{

		if ((railMaterial != null) && (fireTime >= 0))
		{
			Color color = Color.Lerp(railColor, Color.white, fireTime);
			railMaterial.SetColor("_Color", color);
		}

		if (playerInfo.Ammo[5] <= 0 && fireTime < .1f)
		{
			if ((!putAway) && (Sounds.Length > 1))
			{
				audioSource.AudioClip = Sounds[1];
				audioSource.Loop = false;
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
				PoolManager.CreateObjectPool(AttackProjectileName, AttackProjectile, 5);
		}
		if (Sounds.Length > 2)
		{
			GameObject sound = AudioManager.Create3DSound(transform.position, Sounds[2], 5f, 1);
			sound.transform.SetParent(playerInfo.WeaponHand);
		}
		if (Sounds.Length > 3)
		{
			audioSource.AudioClip = Sounds[3];
			audioSource.Loop = true;
			audioSource.Play();
		}
		if (muzzleLight != null)
		{
			muzzleLight.color = railColor;
		}
		Renderer[] renderers = GetComponentsInChildren<Renderer>();

		// Get the material that has the color change for the railgun
		foreach (Renderer renderer in renderers)
		{
			Material[] materials = renderer.sharedMaterials;
			foreach(Material material in materials)
			{
				if (material.HasProperty("_Color"))
					railMaterial = material;
			}
		}
		if (railMaterial != null)
			railMaterial.SetColor("_Color", railColor);
	}
	public override bool Fire()
	{
		if (LowerAmount > .2f)
			return false;

		//small offset to allow continous fire animation
		if (fireTime > 0.05f)
			return false;

		if (playerInfo.Ammo[5] <= 0)
			return false;

		playerInfo.Ammo[5]--;

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
			GameObject sound = AudioManager.Create3DSound(transform.position, Sounds[0], 5f, 1);
			sound.transform.SetParent(playerInfo.WeaponHand);
		}

		//Change Color
		if (railMaterial != null)
			railMaterial.SetColor("_Color", Color.white);

		//Hitscan attack
		Vector3 d = playerInfo.playerCamera.MainCamera.transform.forward;
		{
			Vector2 r = GetDispersion();
			d += playerInfo.playerCamera.MainCamera.transform.right * r.x + playerInfo.playerCamera.MainCamera.transform.up * r.y;
			d.Normalize();

			Ray ray = new Ray(playerInfo.playerCamera.MainCamera.transform.position, d);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, maxRange, ~((1 << GameManager.InvisibleBlockerLayer) | (1 << GameManager.ThingsLayer) | (1 << playerInfo.playerLayer)), QueryTriggerInteraction.Ignore))
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
						GameObject puff = PoolManager.GetObjectFromPool("SlugMark");
						puff.transform.position = hit.point - ray.direction * .2f;
					}
				}
				else
				{
					//Check if collider can be marked
					if (!MapLoader.noMarks.Contains(hit.collider))
					{
						GameObject mark = PoolManager.GetObjectFromPool("SlugMark");
						mark.transform.position = hit.point - ray.direction * .05f;
						mark.transform.forward = hit.normal;
						mark.transform.Rotate(Vector3.forward, Random.Range(0, 360));
					}
					Debug.Log(hit.collider.name);
				}

				//railgun effect attack
				{
					GameObject go = PoolManager.GetObjectFromPool(AttackProjectileName);
					Vector3 start = muzzleObject.transform.position;
					Vector3 end = hit.point - ray.direction * .05f;
					go.transform.position = (end + start) / 2;
					go.transform.up = d;
					go.transform.localScale = new Vector3(.1f, Vector3.Distance(start, end) / 2, .1f);
				}

			}
			//Max Range hit, Railgun need to stop so it should have hit sky or something
			else if(Physics.Raycast(ray, out hit, maxRange, ~((1 << playerInfo.playerLayer)), QueryTriggerInteraction.Ignore))
			{
				GameObject go = PoolManager.GetObjectFromPool(AttackProjectileName);
				Vector3 start = muzzleObject.transform.position;
				Vector3 end = hit.point + hit.normal * .05f;
				go.transform.position = (end + start) / 2;
				go.transform.up = d;
				go.transform.localScale = new Vector3(.1f, Vector3.Distance(start, end) / 2, .1f);
			}
		}
		return true;
	}
}
