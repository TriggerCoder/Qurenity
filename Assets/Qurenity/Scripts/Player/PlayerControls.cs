using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Assets.MultiAudioListener;
using System.Collections.Generic;

public class PlayerControls : NetworkBehaviour
{
	public AnimationCurve axisAnimationCurve;
	public MultiAudioSource audioSource;
	[HideInInspector]
	public PlayerInfo playerInfo;
	[HideInInspector]
	public PlayerWeapon playerWeapon;
	[HideInInspector]
	public PlayerThing playerThing;

	public PlayerCamera playerCamera;
	[HideInInspector]
	public CapsuleCollider capsuleCollider;
	[HideInInspector]
	public CharacterController controller;
	[HideInInspector]
	private PlayerInput playerInput;
	[HideInInspector]
	public InterpolationObjectController interpolationController;

	private Vector2 centerHeight = new Vector2(0.2f, -.05f); // character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(2.0f, 1.5f); // character controller height, x standing, y crouched
	private float camerasHeight = .65f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.zero;

	public Vector3 jumpPadVel = Vector3.zero;

	public Vector3 playerVelocity = Vector3.zero;

	public float impulseDampening = 4f;

	// Movement stuff
	public float crouchSpeed = 3.0f;                // Crouch speed
	public float walkSpeed = 5.0f;                  // Walk speed
	public float runSpeed = 7.0f;                   // Run speed
	private float oldSpeed = 0;                     // Previous move speed

	public float moveSpeed;                         // Ground move speed
	public float runAcceleration = 14.0f;           // Ground accel
	public float runDeacceleration = 10.0f;         // Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;            // Air accel
	public float airDecceleration = 2.0f;           // Deacceleration experienced when ooposite strafing
	public float airControl = 0.3f;                 // How precise air control is
	public float sideStrafeAcceleration = 50.0f;    // How fast acceleration occurs to get up to sideStrafeSpeed when
	public float sideStrafeSpeed = 1.0f;            // What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;                  // The speed at which the character's up axis gains when hitting jump
	public bool holdJumpToBhop = false;             // When enabled allows player to just hold jump button to keep on bhopping perfectly. Beware: smells like casual.

	//playerInputActions
	InputAction Action_Move;
	InputAction Action_Look;
	InputAction Action_Fire;
	InputAction Action_Jump;
	InputAction Action_Crouch;
	InputAction Action_Close;
	InputAction Action_CameraSwitch;
	InputAction Action_Run;
	InputAction Action_WeaponSwitch;
	InputAction[] Action_Weapon = new InputAction[10];

	private bool wishJump = false;
	private bool wishFire = false;
	private bool controllerIsGrounded = true;

	private float deathTime = 0;
	private float respawnDelay = 1.7f;

	public int CurrentWeapon = -1;
	public int SwapWeapon = -1;
	private struct currentMove
	{
		public float forwardSpeed;
		public float sidewaysSpeed;
	}

	private currentMove cMove;

	public int currentMoveType = MoveType.Run;

	public Vector3 teleportDest = Vector3.zero;

	//Cached Transform
	public Transform cTransform;

	public static class MoveType
	{
		public const int Crouch = 0;
		public const int Walk = 1;
		public const int Run = 2;
	}

	struct PlayerInputs : INetworkSerializable
	{
		public Vector2 Move;
		public Vector2 Look;
		public Vector2 ViewDirection;
		public Vector3 ForwardDir;
		public bool Fire;
		public bool Jump;
		public bool Crouch;
		public int CurrentWeapon;
		public int CurrentTick;
		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref Move);
			serializer.SerializeValue(ref Look);
			serializer.SerializeValue(ref ViewDirection);
			serializer.SerializeValue(ref ForwardDir);
			serializer.SerializeValue(ref Fire);
			serializer.SerializeValue(ref Jump);
			serializer.SerializeValue(ref Crouch);
			serializer.SerializeValue(ref CurrentWeapon);
			serializer.SerializeValue(ref CurrentTick);
		}
	}
	struct PlayerState : INetworkSerializable
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public Quaternion ViewAngles;
		public bool isGrounded;
		public bool isFiring;
		public int MoveType;
		public int Weapon;
		public int Tick;
		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref Position);
			serializer.SerializeValue(ref Velocity);
			serializer.SerializeValue(ref ViewAngles);
			serializer.SerializeValue(ref isGrounded);
			serializer.SerializeValue(ref MoveType);
			serializer.SerializeValue(ref isFiring);
			serializer.SerializeValue(ref Weapon);
			serializer.SerializeValue(ref Tick);
		}
	}

	private Queue<PlayerInputs> clientInputs = new Queue<PlayerInputs>();
	private PlayerInputs lastFrameInput;
	private PlayerInputs currentFrameInput;

	private PlayerState lastFramePlayerState;
	private PlayerState currentFramePlayerState;
	void Awake()
	{
		
		controller = GetComponentInParent<CharacterController>();
		moveSpeed = runSpeed;
		OnAwake();
	}

	void Start()
	{
		if (!IsOwner)
			return;

		playerInput = GetComponentInParent<PlayerInput>();
		//Set the actions
		SetPlayerAction();
	}
	void SetPlayerAction()
	{
		Action_Move = playerInput.actions["Move"];
		Action_Look = playerInput.actions["Look"];
		Action_Fire = playerInput.actions["Fire"];
		Action_Jump = playerInput.actions["Jump"];
		Action_Crouch = playerInput.actions["Crouch"];
		Action_Close = playerInput.actions["Close"];
		Action_CameraSwitch = playerInput.actions["CameraSwitch"];
		Action_Run = playerInput.actions["Run"];
		Action_WeaponSwitch = playerInput.actions["WeaponSwitch"];

		for (int i = 0; i < 10; i++)
			Action_Weapon[i] = playerInput.actions["Weapon" + i];
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (IsOwner)
			if (Action_Close.WasPressedThisFrame())
				Application.Quit();

		if (IsServer)
			DequeueClientInputs();

		if ((IsOwner) || (IsServer))
			OnUpdate();
	}

	void FixedUpdate()
	{
		if (GameManager.Paused)
			return;

		if (teleportDest.sqrMagnitude > 0)
		{
			cTransform.position = teleportDest;
			interpolationController.ResetTransforms();
			teleportDest = Vector3.zero;
			return;
		}

		OnFixedUpdate();
	}
	private void SetMovementDir()
	{
		Vector2 currentMove = Move();

		cMove.forwardSpeed = currentMove.y;
		cMove.sidewaysSpeed = currentMove.x;
	}
	private void GroundMove(float deltaTime)
	{
		Vector3 wishdir;

		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f, deltaTime);
		else
			ApplyFriction(0, deltaTime);

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = cTransform.TransformDirection(wishdir);
		wishdir.Normalize();

		var wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		Accelerate(wishdir, wishspeed, runAcceleration, deltaTime, runSpeed);

		// Reset the gravity velocity
		playerVelocity.y = -GameManager.Instance.gravity * deltaTime;

		if (wishJump)
		{
			playerVelocity.y = jumpSpeed;
			wishJump = false;
		}
	}

	private void ApplyFriction(float t, float deltaTime)
	{
		Vector3 vec = playerVelocity;
		float speed;
		float newspeed;
		float control;
		float drop;

		vec.y = 0.0f;
		speed = vec.magnitude;
		drop = 0.0f;

		//Player is always grounded when we are here, no need to re-check
		//if (controller.isGrounded)
		{
			control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * GameManager.Instance.friction * deltaTime * t;
		}

		newspeed = speed - drop;

		if (newspeed < 0)
			newspeed = 0;
		if (speed > 0)
			newspeed /= speed;

		playerVelocity.x *= newspeed;
		playerVelocity.z *= newspeed;
	}
	private void Accelerate(Vector3 wishdir, float wishspeed, float accel, float deltaTime, float wishaccel = 0)
	{
		float addspeed;
		float accelspeed;
		float currentspeed;

		currentspeed = Vector3.Dot(playerVelocity, wishdir);
		addspeed = wishspeed - currentspeed;
		if (addspeed <= 0)
			return;
		if (wishaccel == 0)
			wishaccel = wishspeed;
		accelspeed = accel * deltaTime * wishaccel;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.x += accelspeed * wishdir.x;
		playerVelocity.z += accelspeed * wishdir.z;
	}

	private void AirMove(float deltaTime)
	{
		Vector3 wishdir;
		float accel;

		SetMovementDir();

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = cTransform.TransformDirection(wishdir);

		float wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		wishdir.Normalize();

		//Aircontrol
		float wishspeed2 = wishspeed;
		if (Vector3.Dot(playerVelocity, wishdir) < 0)
			accel = airDecceleration;
		else
			accel = airAcceleration;
		// If the player is ONLY strafing left or right
		if ((cMove.forwardSpeed == 0) && (cMove.sidewaysSpeed != 0))
		{
			if (wishspeed > sideStrafeSpeed)
				wishspeed = sideStrafeSpeed;
			accel = sideStrafeAcceleration;
		}

		Accelerate(wishdir, wishspeed, accel, deltaTime);
		if (airControl > 0)
			AirControl(wishdir, wishspeed2, deltaTime);

		// Apply gravity
		if (jumpPadVel.sqrMagnitude > 0)
			playerVelocity.y = 0;
		else
			playerVelocity.y -= GameManager.Instance.gravity * deltaTime;
	}

	private void AirControl(Vector3 wishdir, float wishspeed, float deltaTime)
	{
		float zspeed;
		float speed;
		float dot;
		float k;

		// Can't control movement if not moving forward or backward
		if ((cMove.forwardSpeed == 0) || (Mathf.Abs(wishspeed) < 0.001))
			return;
		zspeed = playerVelocity.y;
		playerVelocity.y = 0;
		/* Next two lines are equivalent to idTech's VectorNormalize() */
		speed = playerVelocity.magnitude;
		playerVelocity.Normalize();

		dot = Vector3.Dot(playerVelocity, wishdir);
		k = 32;
		k *= airControl * dot * dot * deltaTime;

		// Change direction while slowing down
		if (dot > 0)
		{
			playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
			playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
			playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

			playerVelocity.Normalize();
		}

		playerVelocity.x *= speed;
		playerVelocity.y = zspeed; // Note this line
		playerVelocity.z *= speed;
	}
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

	private void OnUpdate()
	{
		if (playerThing.Dead)
		{
			SetCameraBobActive(false);

			if (deathTime < respawnDelay)
				deathTime += Time.deltaTime;
			else
			{
				if (JumpPressedThisFrame() || FirePressedThisFrame())
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
		playerThing.avatar.CheckLegTurn(ForwardDir());

		controllerIsGrounded = IsControllerGrounded();
		playerThing.avatar.isGrounded = controllerIsGrounded;

		//Player can only crouch if it is grounded
		if ((CrouchPressedThisFrame()) && (controllerIsGrounded))
		{
			CrouchChangePlayerSpeed(false);
			ChangeHeight(false);
		}
		else if (CrouchReleasedThisFrame())
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
				playerThing.avatar.TurnLegs(currentMoveType, cMove.sidewaysSpeed, cMove.forwardSpeed);
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
			if (FirePressed())
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

		if ((IsOwner) && (!IsHost))
			SendDataServerRpc(new PlayerInputs
			{
					Move = Move(),
					Look = Look(),
					ViewDirection = viewDirection,
					ForwardDir = ForwardDir(),
					Fire = FirePressed(),
					Jump = JumpPressed(),
					Crouch = CrouchPressed(),
					CurrentWeapon = CurrentWeapon,
					CurrentTick = 0
			}) ;
	}

	private void OnFixedUpdate()
	{
		if (playerThing.Dead)
		{
			ApplySimpleGravity();
			return;
		}

		if (!playerThing.ready)
			return;

		RotateTorwardDir();

		controllerIsGrounded = IsControllerGrounded();
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

	public void SetCameraBobActive(bool active)
	{
		if (!IsOwner)
			return;

		if (playerCamera != null)
			playerCamera.bopActive = active;
	}
	public bool JumpPressedThisFrame()
	{
		if (!IsOwner)
		{
			if (lastFrameInput.Jump)
				return false;
			if (currentFrameInput.Jump)
				return true;
			return false;
		}
		return (Action_Jump.WasPressedThisFrame()); 
	
	}
	public bool JumpReleasedThisFrame()
	{ 
		if (!IsOwner)
		{
			if (!lastFrameInput.Jump)
				return false;
			if (!currentFrameInput.Jump)
				return true;
			return false;
		}
		return (Action_Jump.WasReleasedThisFrame());
	}
	public bool JumpPressed()
	{ 
		if (!IsOwner)
			return currentFrameInput.Jump;
		
		return (Action_Jump.IsPressed());
	}
	public bool FirePressedThisFrame()
	{ 
		if (!IsOwner)
		{
			if (lastFrameInput.Fire)
				return false;
			if (currentFrameInput.Fire)
				return true;
			return false;
		}
		return (Action_Fire.WasPressedThisFrame());
	}
	public bool FirePressed()
	{ 
		if (!IsOwner)
		{
			if (IsHost)
				return currentFrameInput.Fire;
			else
				return currentFramePlayerState.isFiring;
		}
		return (Action_Fire.IsPressed());
	}

	public void CheckCameraChange()
	{
		if (!IsOwner)
			return;

		if (Action_CameraSwitch.WasPressedThisFrame())
			playerCamera.ChangeThirdPersonCamera(!playerCamera.ThirdPerson.enabled);
	}
	public void SetViewDirection()
	{
		if (!IsOwner)
		{
			if (IsHost)
				viewDirection = currentFrameInput.ViewDirection;
			else
				Debug.LogWarning("Don't Forget to add SetViewDirection");
			return;
		}
		Vector2 Look = Action_Look.ReadValue<Vector2>();

		if (playerInput.currentControlScheme == "Keyboard&Mouse")
		{
			viewDirection.y += Look.x * Time.smoothDeltaTime * GameOptions.MouseSensitivity.x;
			viewDirection.x -= Look.y * Time.smoothDeltaTime * GameOptions.MouseSensitivity.y;
		}
		else
		{
			viewDirection.y += Look.x * GameOptions.GamePadSensitivity.x * axisAnimationCurve.Evaluate(Mathf.Abs(Look.x));
			viewDirection.x -= Look.y * GameOptions.GamePadSensitivity.y * axisAnimationCurve.Evaluate(Mathf.Abs(Look.y));
		}
	}
	public Vector2 Move()
	{
		if (!IsOwner)
			return currentFrameInput.Move;

		return Action_Move.ReadValue<Vector2>();
	}
	public Vector2 Look()
	{ 
		if (!IsOwner) 
			return currentFrameInput.Look;
		
		return Action_Look.ReadValue<Vector2>(); 
	}
	public Vector3 ForwardDir()
	{ 
		if (!IsOwner)
			return currentFrameInput.ForwardDir;

		return playerCamera.cTransform.forward;
	}
	public bool IsControllerGrounded()
	{ 
		if (!IsOwner)
			if (!IsHost)
				return currentFramePlayerState.isGrounded;

		return controller.isGrounded; 
	}
	public bool CrouchPressedThisFrame()
	{ 
		if (!IsOwner)
		{
			if (IsHost)
			{
				if (lastFrameInput.Crouch)
					return false;
				if (currentFrameInput.Crouch)
					return true;
				return false;
			}
			else
			{
				if (lastFramePlayerState.MoveType == MoveType.Crouch)
					return false;
				if (currentFramePlayerState.MoveType == MoveType.Crouch)
					return true;
				return false;
			}
		}
		return (Action_Crouch.WasPressedThisFrame()); 
	}

	public bool CrouchPressed()
	{
		if (!IsOwner)
			return currentFrameInput.Crouch;
		return (Action_Crouch.IsPressed());
	}
	public bool CrouchReleasedThisFrame() 
	{ 
		if (!IsOwner)
		{
			if (IsHost)
			{
				if (!lastFrameInput.Crouch)
					return false;
				if (!currentFrameInput.Crouch)
					return true;
				return false;
			}
			else
			{
				if (lastFramePlayerState.MoveType != MoveType.Crouch)
					return false;
				if (currentFramePlayerState.MoveType != MoveType.Crouch)
					return true;
				return false;
			}
		}
		return (Action_Crouch.WasReleasedThisFrame());
	}
	public void CrouchChangePlayerSpeed(bool Standing)
	{
		if ((!IsOwner) && (!IsHost))
		{
			if (Standing)
			{
				currentMoveType = MoveType.Run;
				return;
			}
			currentMoveType = MoveType.Crouch;
			return;
		}

		if (Standing)
		{
			if (oldSpeed != 0)
				moveSpeed = oldSpeed;
			if (moveSpeed == walkSpeed)
				currentMoveType = MoveType.Walk;
			else
				currentMoveType = MoveType.Run;
			oldSpeed = 0;
			return;
		}
		if (oldSpeed == 0)
			oldSpeed = moveSpeed;
		moveSpeed = crouchSpeed;
		currentMoveType = MoveType.Crouch;
	}
	public void ChangeHeight(bool Standing)
	{
		float newCenter = centerHeight.y;
		float newHeight = height.y;

		if (!IsOwner)
		{
			if (Standing)
			{
				newCenter = centerHeight.x;
				newHeight = height.x;
			}
			capsuleCollider.center = new Vector3(0, newCenter, 0);
			capsuleCollider.height = newHeight + ccHeight;
			return;
		}

		if (Standing)
		{
			newCenter = centerHeight.x;
			newHeight = height.x;
		}
		controller.center = new Vector3(0, newCenter, 0);
		controller.height = newHeight;

		capsuleCollider.center = controller.center;
		capsuleCollider.height = newHeight + ccHeight;

		playerCamera.yOffset = newCenter + camerasHeight;
	}
	public void CheckIfRunning()
	{
		if (!IsOwner)
			return;

		if (GameOptions.runToggle)
		{
			if (Action_Run.WasReleasedThisFrame())
			{
				if (moveSpeed == walkSpeed)
				{
					moveSpeed = runSpeed;
					currentMoveType = MoveType.Run;
				}
				else
				{
					moveSpeed = walkSpeed;
					currentMoveType = MoveType.Walk;
				}
			}
		}
		else
		{
			if (Action_Run.IsPressed())
			{
				moveSpeed = runSpeed;
				currentMoveType = MoveType.Run;
			}
			else
			{
				moveSpeed = walkSpeed;
				currentMoveType = MoveType.Walk;
			}
		}
	}
	public void QueueJump()
	{
		if (!IsOwner)
			return;

		if (holdJumpToBhop)
		{
			wishJump = Action_Jump.IsPressed();
			return;
		}

		if (Action_Jump.WasPressedThisFrame() && !wishJump)
			wishJump = true;
		if (Action_Jump.WasReleasedThisFrame())
			wishJump = false;
	}

	public void CheckMouseWheelWeaponChange()
	{
		if (!IsOwner)
			return;

		float wheel = Action_WeaponSwitch.ReadValue<float>();
		if (wheel > 0)
		{
			bool gotWeapon = false;
			for (int NextWeapon = CurrentWeapon + 1; NextWeapon < 9; NextWeapon++)
			{
				gotWeapon = TrySwapWeapon(NextWeapon);
				if (gotWeapon)
					break;
			}
			if (!gotWeapon)
				TrySwapWeapon(0);
		}
		else if (wheel < 0)
		{
			bool gotWeapon = false;
			for (int NextWeapon = CurrentWeapon - 1; NextWeapon >= 0; NextWeapon--)
			{
				gotWeapon = TrySwapWeapon(NextWeapon);
				if (gotWeapon)
					break;
			}
			if (!gotWeapon)
				SwapToBestWeapon();
		}
	}
	public void CheckWeaponChangeByIndex()
	{
		if (!IsOwner)
			return;

		for (int i = 0; i < 10; i++)
		{
			if (Action_Weapon[i].WasPressedThisFrame())
			{
				TrySwapWeapon(i);
				break;
			}
		}
	}
	public void ApplySimpleGravity()
	{
		if (!IsOwner)
			return;

		if (controller.enabled)
		{
			// Reset the gravity velocity
			playerVelocity = Vector3.down * GameManager.Instance.gravity;
			ApplyMove();
		}
	}
	public void ApplyMove()
	{
		if (!IsOwner)
			return;

		float deltaTime = Time.fixedDeltaTime;
		lastPosition = cTransform.position;
		controller.Move((playerVelocity + impulseVector + jumpPadVel) * deltaTime);

		//dampen impulse
		if (impulseVector.sqrMagnitude > 0)
		{
			impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseDampening * deltaTime);
			if (impulseVector.sqrMagnitude < 1f)
				impulseVector = Vector3.zero;
		}

		//dampen jump pad impulse
		if (jumpPadVel.sqrMagnitude > 0)
		{
			jumpPadVel.y -= (GameManager.Instance.gravity * Time.fixedDeltaTime);
			if ((jumpPadVel.y < 0) && (controllerIsGrounded))
				jumpPadVel = Vector3.zero;
		}
	}
	public void CheckMovements()
	{
		if (!IsOwner)
			return;

		if (controllerIsGrounded)
			GroundMove(Time.fixedDeltaTime);
		else
			AirMove(Time.fixedDeltaTime);
	}
	public void EnableColliders(bool enable)
	{

		capsuleCollider.enabled = enable;
		if (!IsOwner)
			return;

		controller.enabled = enable;
	}
	public Vector2 GetBobDelta(float hBob, float vBob, float lerp)
	{
		Vector2 position;

		if (!IsOwner)
			return Vector2.zero;

		float speed = playerVelocity.magnitude;
		float moveSpeed = walkSpeed;
		if (moveSpeed != walkSpeed)
			moveSpeed = runSpeed;
		float delta = Mathf.Cos(Time.time * moveSpeed) * hBob * speed * lerp;
		if (moveSpeed == crouchSpeed) //Crouched
			delta *= 5;
		position.x = delta;

		delta = Mathf.Sin(Time.time * moveSpeed) * vBob * speed * lerp;
		if (moveSpeed == crouchSpeed) //Crouched
			delta *= 5;
		position.y = delta;
		return position;
	}
	public void RotateTorwardDir()
	{
		if (!IsOwner)
			return;

		cTransform.rotation = Quaternion.Euler(0, viewDirection.y, 0);
	}
	public void DequeueClientInputs()
	{
		lastFrameInput = currentFrameInput;
		if (clientInputs.Count != 0)
			currentFrameInput = clientInputs.Dequeue();
	}

	[ServerRpc]
	private void SendDataServerRpc(PlayerInputs playerInputs)
	{
		clientInputs.Enqueue(playerInputs);
	}
}
