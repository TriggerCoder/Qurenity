using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.MultiAudioListener;

[RequireComponent(typeof(MultiAudioSource))]
public class PlayerThing : MonoBehaviour, Damageable
{
	[HideInInspector]
	public PlayerInfo playerInfo;
	[HideInInspector]
	public PlayerControls playerControls;
	[HideInInspector]
	public PlayerCamera playerCamera;

	MultiAudioSource audioSource;

	public string modelName = "sarge";
	public string skinName = "default";
	public GameObject player;
	public PlayerModel avatar;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	public int hitpoints = 100;
	public int armor = 0;
	public float painTime = 0f;
	public float lookTime = .5f;
	public bool finished = false;
	public bool radsuit = false;
	public bool invul = false;
	public bool ready = false;
	private enum LookType
	{
		Left = 0,
		Center = 1,
		Right = 2
	}
	private LookType whereToLook = LookType.Center;
	void Awake()
	{
		audioSource = GetComponent<MultiAudioSource>();
		playerInfo = GetComponentInChildren<PlayerInfo>();
		playerControls = GetComponentInChildren<PlayerControls>();
		playerCamera = GetComponentInChildren<PlayerCamera>();
	}

	public void InitPlayer()
	{
		player = new GameObject();
		avatar = player.AddComponent<PlayerModel>();
		player.transform.SetParent(transform);

		avatar.LoadPlayer(modelName, skinName, playerInfo.playerLayer, playerControls);

		gameObject.layer = playerInfo.playerLayer;
		Vector3 destination = SpawnerManager.FindSpawnLocation();
		TeleporterThing.TelefragEverything(destination, gameObject);
		transform.position = destination;

		playerControls.capsuleCollider.enabled = true;
		playerControls.controller.enabled = true;
		playerInfo.playerHUD.pickupFlashTime = 0f;
		playerInfo.playerHUD.painFlashTime = 0f;

		int playerLayer = ((1 << GameManager.Player1Layer) |
							(1 << GameManager.Player2Layer) |
							(1 << GameManager.Player3Layer) |
							(1 << GameManager.Player4Layer)) & ~(1 << (playerInfo.playerLayer));

		playerCamera.SkyholeCamera.cullingMask = (((1 << (GameManager.DefaultLayer)) |
													(1 << (GameManager.DebrisLayer)) |
													(1 << (GameManager.ThingsLayer)) |
													(1 << (GameManager.RagdollLayer)) |
													(1 << (GameManager.CombinesMapMeshesLayer)) |
													(1 << (playerInfo.playerLayer - 5)) |
													playerLayer));

		if (playerControls.playerWeapon == null)
			playerControls.SwapToBestWeapon();

		playerInfo.playerHUD.HUDUpdateHealthNum();
		playerInfo.playerHUD.HUDUpdateArmorNum();

		playerCamera.ChangeThirdPersonCamera(false);

		playerControls.enabled = true;
		ready = true;
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		if (Dead)
		{
			playerInfo.playerHUD.painFlashTime = 2f;
		}
		if (painTime > 0f)
			painTime -= Time.deltaTime;
	}

	public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
	{
		if (Dead)
			return;

		if ((damageType != DamageType.Environment) || (damageType != DamageType.Crusher))
			amount = Mathf.RoundToInt(amount * GameManager.Instance.PlayerDamageReceive);

		if (invul)
			if ((damageType != DamageType.Crusher) && (damageType != DamageType.Telefrag))
				amount = 0;

		if (amount <= 0)
			return;

		if (armor > 0)
		{
			int subjectiveToMega = Mathf.Min(Mathf.Max(armor - 100, 0), amount);
			int subjectiveToNormal = Mathf.Min(armor, amount - subjectiveToMega);
			int absorbed = Mathf.Max(subjectiveToMega / 2 + subjectiveToNormal / 3, 1);

			armor -= absorbed;
			amount -= absorbed;
		}

		hitpoints -= amount;

		//Cap Negative Damage
		if (hitpoints < -99)
			hitpoints = -99;

		playerInfo.playerHUD.HUDUpdateHealthNum();
		playerInfo.playerHUD.HUDUpdateArmorNum();

		if (attacker == null)
		{
			lookTime = 1f;
//			playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Pain, CalcPain(hitpoints), 1);
		}
		else
		{
			if ((attacker.name == "Player"))
			{
				lookTime = 1f;
				if (amount >= 20)
				{
//					playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Ouch, CalcPain(hitpoints));
				}
				else
				{
//					playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Pain, CalcPain(hitpoints), 1);
				}
			}
			else
			{
				float angleDir = AngleDir(playerCamera.MainCamera.transform.forward, attacker.transform.position, playerCamera.MainCamera.transform.up);
				lookTime = 1f;
				if (angleDir < 0)
				{
//					playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Pain, CalcPain(hitpoints), 0);
				}
				else if (angleDir > 0)
				{
//					playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Pain, CalcPain(hitpoints), 2);
				}
				else
				{
//					playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Pain, CalcPain(hitpoints), 1);
				}
			}
		}

		if (amount > 60)
			playerInfo.playerHUD.painFlashTime = 2.5f;
		else if (amount > 40)
			playerInfo.playerHUD.painFlashTime = 2f;
		else if (amount > 20)
			playerInfo.playerHUD.painFlashTime = 1.5f;
		else
			playerInfo.playerHUD.painFlashTime = 1f;

		if (hitpoints <= 0)
		{
//			playerInfo.doomHUD.HUDUpdateMugshot(DoomHUD.MugType.Dead);
	
			if (playerControls.playerWeapon != null)
				playerControls.playerWeapon.putAway = true;

			playerCamera.ChangeThirdPersonCamera(true);
			PlayModelSound("death" + Random.Range(1, 4));
			avatar.Die();
			ready = false;
		}
		else if (painTime <= 0f)
		{
			if (hitpoints > 75)
				PlayModelSound("pain100_1");
			else if (hitpoints > 50)
				PlayModelSound("pain75_1");
			else if (hitpoints > 25)
				PlayModelSound("pain50_1");
			else
				PlayModelSound("pain25_1");

			painTime = 1f;
		}
	}

	public void RemovePowerUps()
	{
		Debug.Log("Removing PowerUPS");
	}

	public void PlayModelSound(string soundName)
	{
		soundName = "player/" + modelName + "/" + soundName;
		audioSource.AudioClip = SoundLoader.LoadSound(soundName);
		audioSource.Play();
	}
	public void Impulse(Vector3 direction, float force)
	{
		float length = force / 80;

		//Gravity will be the only force down
		Vector3 impulse = direction * length;
		float magnitude = impulse.magnitude;
		impulse.y = 0;
		impulse = impulse.normalized * magnitude;

		playerControls.impulseVector += impulse;
	}

	public void JumpPadDest(Vector3 destination)
	{
		Vector3 position = transform.position;
		Vector3 horizontalVelocity = destination - position;
		float height = destination.y - position.y;

		if (height <= 0)
		{
			playerControls.jumpPadVel = Vector3.zero;
			return;
		}

		float time = Mathf.Sqrt((2 * height) / GameManager.Instance.gravity);
		float verticalVelocity = time * GameManager.Instance.gravity;

		horizontalVelocity.y = 0;
		float forward = horizontalVelocity.magnitude / time;
		horizontalVelocity = horizontalVelocity.normalized * forward;
		playerControls.jumpPadVel = horizontalVelocity;
		playerControls.jumpPadVel.y = verticalVelocity;
		playerControls.playerVelocity = Vector3.zero;
		playerControls.AnimateLegsOnJump();
	}



	public int CalcPain(int hitpoint)
	{
		if (hitpoint >= 80)
			return 0;
		else if (hitpoint >= 60)
			return 1;
		else if (hitpoint >= 40)
			return 2;
		else if (hitpoint >= 20)
			return 3;
		return 4;
	}
	public float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
	{
		Vector3 perp = Vector3.Cross(fwd, targetDir);
		float dir = Vector3.Dot(perp, up);

		if (dir > 0.0f)
		{
			return 1.0f;
		}
		else if (dir < 0.0f)
		{
			return -1.0f;
		}
		else
		{
			return 0.0f;
		}
	}
}
