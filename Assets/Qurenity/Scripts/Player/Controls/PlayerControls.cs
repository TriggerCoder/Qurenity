using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Assets.MultiAudioListener;

public class PlayerControls : MonoBehaviour, ControlsInterface
{
	public AnimationCurve axisAnimationCurve;
	public MultiAudioSource audioSource;
	[HideInInspector]
	public PlayerInfo playerInfo;
	[HideInInspector]
	public PlayerWeapon playerWeapon;
	[HideInInspector]
	public PlayerThing playerThing;
	[HideInInspector]
	public InterpolationObjectController interpolationController;

	protected Vector2 centerHeight = new Vector2(0.2f, -.05f); // character controller center height, x standing, y crouched
	protected Vector2 height = new Vector2(2.0f, 1.5f); // character controller height, x standing, y crouched
	protected float camerasHeight = .65f;
	protected float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.zero;

	public Vector3 jumpPadVel = Vector3.zero;

	public Vector3 playerVelocity = Vector3.zero;

	[HideInInspector]
	public CapsuleCollider capsuleCollider;

	protected bool wishJump = false;
	protected bool wishFire = false;
	protected bool controllerIsGrounded = true;

	protected float deathTime = 0;
	protected float respawnDelay = 1.7f;

	public int CurrentWeapon = -1;
	public int SwapWeapon = -1;
	protected struct currentMove
	{
		public float forwardSpeed;
		public float sidewaysSpeed;
	}

	protected currentMove cMove;

	public MoveType currentMoveType = MoveType.Run;

	public Vector3 teleportDest = Vector3.zero;

	//Cached Transform
	public Transform cTransform;
	public enum MoveType
	{
		Crouch,
		Walk,
		Run
	}
	public virtual void SetCameraBobActive(bool active) { }
	public virtual bool JumpPressedThisFrame { get { return false; } }
	public virtual bool JumpReleasedThisFrame { get { return false; } }
	public virtual bool JumpPressed { get { return false; } }
	public virtual bool FirePressedThisFrame { get { return false; } }
	public virtual bool FirePressed { get { return false; } }
	public virtual void CheckCameraChange() { }
	public virtual void SetViewDirection() { }
	public virtual Vector2 Look { get { return Vector2.zero; } }
	public virtual Vector3 ForwardDir { get { return Vector3.forward; } }
	public virtual bool IsControllerGrounded { get { return true; } }
	public virtual bool CrouchPressedThisFrame { get { return false; } }
	public virtual bool CrouchReleasedThisFrame { get { return false; } }
	public virtual void CrouchChangePlayerSpeed(bool Standing)
	{
		if (Standing)
		{
			currentMoveType = MoveType.Run;
			return;
		}
		currentMoveType = MoveType.Crouch;
	}
	public virtual void ChangeHeight(bool Standing)
	{
		float newCenter = centerHeight.y;
		float newHeight = height.y;

		if (Standing)
		{
			newCenter = centerHeight.x;
			newHeight = height.x;
		}
		capsuleCollider.center = new Vector3(0, newCenter, 0);
		capsuleCollider.height = newHeight + ccHeight;
	}
	public virtual void CheckIfRunning() { }
	public virtual void QueueJump() { }
	public virtual void CheckMouseWheelWeaponChange() { }
	public virtual void CheckWeaponChangeByIndex() { }
	public virtual void ApplyMove() { }
	public virtual void ApplySimpleGravity() { }
	public virtual void CheckMovements() { }
	public virtual void EnableColliders(bool enable) { capsuleCollider.enabled = enable; }
	public virtual Vector2 GetBobDelta(float hBob, float vBob, float lerp) { return Vector2.zero; }
	public virtual void RotateTorwardDir() { }
	public virtual void SetInput(bool[] bInputs, Vector3 vForward) { }
	public virtual void SetMove(Vector3 vPosition, Vector3 vForward) { }
	public void OnAwake()
	{
		capsuleCollider = GetComponentInParent<CapsuleCollider>();
		audioSource = GetComponentInParent<MultiAudioSource>();
		playerInfo = GetComponent<PlayerInfo>();
		playerThing = GetComponentInParent<PlayerThing>();
		interpolationController = GetComponent<InterpolationObjectController>();

		playerWeapon = null;
		currentMoveType = MoveType.Run;
		//cache transform
		cTransform = transform;
	}

	protected void OnUpdate()
	{
		if (playerThing.Dead)
		{
			SetCameraBobActive(false);

			if (deathTime < respawnDelay)
				deathTime += Time.deltaTime;
			else
			{
				if (JumpPressedThisFrame || FirePressedThisFrame)
				{
					deathTime = 0;
					viewDirection = Vector2.zero;

					if (playerWeapon != null)
					{
						Destroy(playerWeapon.gameObject);
						playerWeapon = null;
					}

					playerInfo.Reset();
					playerThing.InitPlayer();
				}
			}
			return;
		}

		if (!playerThing.ready)
			return;

		CheckCameraChange();

		SetViewDirection();

		//so you don't fall when no-clipping
		bool outerSpace = false;

		if (gameObject.layer != playerInfo.playerLayer)
			outerSpace = true;

		if (viewDirection.y < -180) viewDirection.y += 360;
		if (viewDirection.y > 180) viewDirection.y -= 360;

		//restricted up/down looking angle
		if (viewDirection.x < -85) viewDirection.x = -85;
		if (viewDirection.x > 85) viewDirection.x = 85;

		playerThing.avatar.ChangeView(viewDirection, Time.deltaTime);
		playerThing.avatar.CheckLegTurn(ForwardDir);

		controllerIsGrounded = IsControllerGrounded;
		playerThing.avatar.isGrounded = controllerIsGrounded;

		//Player can only crounch if it is grounded
		if ((CrouchPressedThisFrame) && (controllerIsGrounded))
		{
			CrouchChangePlayerSpeed(false);
			ChangeHeight(false);
		}
		else if (CrouchReleasedThisFrame)
		{
			CrouchChangePlayerSpeed(true);
			ChangeHeight(true);
		}

		if (currentMoveType != MoveType.Crouch)
		{
			CheckIfRunning();
			QueueJump();
		}

		//Movement Checks
		if (controllerIsGrounded)
		{
			if (playerThing.avatar.enableOffset)
				playerThing.avatar.TurnLegs((int)currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed);
			if (wishJump)
				AnimateLegsOnJump();
		}
		else
			playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed);

		//	if (playerCamera.MainCamera.activeSelf)
		{
			if ((cTransform.position - lastPosition).sqrMagnitude > .0001f)
				SetCameraBobActive(true);
			else
				SetCameraBobActive(false);

			//use weapon
			if (FirePressed)
				wishFire = true;
		}

		//swap weapon
		if (playerWeapon == null)
		{
			if (SwapWeapon == -1)
				SwapToBestWeapon();

			if (SwapWeapon > -1)
			{
				CurrentWeapon = SwapWeapon;
				playerWeapon = Instantiate(playerInfo.WeaponPrefabs[CurrentWeapon]);
				playerWeapon.Init(playerInfo);
				SwapWeapon = -1;
			}
		}

		CheckMouseWheelWeaponChange();

		CheckWeaponChangeByIndex();
	}

	protected void OnFixedUpdate()
	{
		if (playerThing.Dead)
		{
			ApplySimpleGravity();
			return;
		}

		if (!playerThing.ready)
			return;

		RotateTorwardDir();

		controllerIsGrounded = IsControllerGrounded;
		//Movement Checks
		CheckMovements();

		//apply move
		ApplyMove();

		if (wishFire)
		{
			wishFire = false;
			if (playerWeapon.Fire())
			{
				playerInfo.playerHUD.HUDUpdateAmmoNum();
				playerThing.avatar.Attack();
			}
		}
	}
	public void AnimateLegsOnJump()
	{
		if (cMove.forwardSpeed >= 0)
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.Jump;
		else if (cMove.forwardSpeed < 0)
			playerThing.avatar.lowerAnimation = PlayerModel.LowerAnimation.JumpBack;
		playerThing.avatar.enableOffset = false;
		playerThing.PlayModelSound("jump1");
	}

	public bool TrySwapWeapon(int weapon)
	{
		if (CurrentWeapon == weapon || SwapWeapon != -1)
			return false;

		if (weapon < 0 || weapon >= playerInfo.Weapon.Length)
			return false;

		if (!playerInfo.Weapon[weapon])
			return false;

		switch (weapon)
		{
			default:
				return false;

			case 0:
				break;

			case 1:
				if (playerInfo.Ammo[0] <= 0)
					return false;
				break;
			case 2:
				if (playerInfo.Ammo[1] <= 0)
					return false;
				break;

			case 3:
				if (playerInfo.Ammo[2] <= 0)
					return false;
				break;

			case 4:
				if (playerInfo.Ammo[3] <= 0)
					return false;
				break;

			case 5:
				if (playerInfo.Ammo[4] <= 0)
					return false;
				break;
			case 6:
				if (playerInfo.Ammo[5] <= 0)
					return false;
				break;
			case 7:
				if (playerInfo.Ammo[6] <= 0)
					return false;
				break;
			case 8:
				if (playerInfo.Ammo[7] <= 0)
					return false;
				break;
		}

		if (playerWeapon != null)
			playerWeapon.putAway = true;

		SwapWeapon = weapon;
		return true;
	}
	public void SwapToBestWeapon()
	{
		if (TrySwapWeapon(8)) return; //bfg10k
		if (TrySwapWeapon(5)) return; //lightning gun
		if (TrySwapWeapon(7)) return; //plasma gun
		if (TrySwapWeapon(6)) return; //railgun
		if (TrySwapWeapon(2)) return; //shotgun
		if (TrySwapWeapon(1)) return; //machinegun
		if (TrySwapWeapon(4)) return; //rocketlauncher
		if (TrySwapWeapon(3)) return; //grenade launcher
		if (TrySwapWeapon(0)) return; //gauntlet
	}
}
