using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class ClientLocalControl : PlayerControls
{
	public AnimationCurve axisAnimationCurve;
	public PlayerCamera playerCamera;
	[HideInInspector]
	public PlayerInput playerInput;
	[HideInInspector]
	public Rigidbody rb;

	private Vector3 receivedPosition = Vector3.zero;
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

	const int Forward = 0;
	const int Backwards = 1;
	const int Left = 2;
	const int Right = 3;
	const int Fire = 4;
	const int Run = 5;
	const int Jump = 6;
	const int Crouch = 7;
	const int Weapon_Plus = 8;
	const int Weapon_Minus = 9;
	const int Weapon = 10;

	bool[] inputs = new bool[20];
	void Awake()
	{
		rb = GetComponentInParent<Rigidbody>();
		playerInput = GetComponentInParent<PlayerInput>();
		OnAwake();
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

	void GetInputToSend()
	{
		//Move
		Vector2 move = Action_Move.ReadValue<Vector2>();
		if (move.y > 0)
			inputs[Forward] = true;
		else
			inputs[Forward] = false;
		if (move.y < 0)
			inputs[Backwards] = true;
		else
			inputs[Backwards] = false;
		if (move.x < 0)
			inputs[Left] = true;
		else
			inputs[Left] = false;
		if (move.x > 0)
			inputs[Right] = true;
		else
			inputs[Right] = false;
		//Fire
		if (Action_Fire.IsPressed())
			inputs[Fire] = true;
		else
			inputs[Fire] = false;
		//Run
		if (Action_Run.IsPressed())
			inputs[Run] = true;
		else
			inputs[Run] = false;
		//Jump
		if (Action_Jump.IsPressed())
			inputs[Jump] = true;
		else
			inputs[Jump] = false;
		//Crouch
		if (Action_Crouch.IsPressed())
			inputs[Crouch] = true;
		else
			inputs[Crouch] = false;
		//Weapon +-
		float wheel = Action_WeaponSwitch.ReadValue<float>();
		if (wheel > 0)
			inputs[Weapon_Plus] = true;
		else
			inputs[Weapon_Plus] = false;
		if (wheel < 0)
			inputs[Weapon_Minus] = true;
		else
			inputs[Weapon_Minus] = false;
		//Weapons
		for (int i = 0; i < 10; i++)
		{
			if (Action_Weapon[i].IsPressed())
				inputs[Weapon + i] = true;
			else
				inputs[Weapon + i] = false;
		}
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (Action_Close.WasPressedThisFrame())
			Application.Quit();

		GetInputToSend();

		if (playerThing.Dead)
		{
			if (playerCamera != null)
				playerCamera.bopActive = false;

			if (deathTime < respawnDelay)
				deathTime += Time.deltaTime;
			else
			{
				if (Action_Jump.WasPressedThisFrame() || Action_Fire.WasPressedThisFrame())
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

		if (Action_CameraSwitch.WasPressedThisFrame())
			playerCamera.ChangeThirdPersonCamera(!playerCamera.ThirdPerson.enabled);

		OnUpdate();
	}

	public override Vector2 Move { get { return Action_Move.ReadValue<Vector2>(); } }
	public override Vector2 View
	{
		get
		{
			Vector2 onLook = Action_Look.ReadValue<Vector2>();
			Vector2 retValue = Vector2.zero;
			if (playerInput.currentControlScheme == "Keyboard&Mouse")
			{
				retValue.x = onLook.x * Time.smoothDeltaTime * GameOptions.MouseSensitivity.x;
				retValue.y = onLook.y * Time.smoothDeltaTime * GameOptions.MouseSensitivity.y;
			}
			else
			{
				retValue.x = onLook.x * GameOptions.GamePadSensitivity.x * axisAnimationCurve.Evaluate(Mathf.Abs(onLook.x));
				retValue.y = onLook.y * GameOptions.GamePadSensitivity.y * axisAnimationCurve.Evaluate(Mathf.Abs(onLook.y));
			}
			return retValue;
		}
	}
	public override Vector2 Look { get { return Action_Look.ReadValue<Vector2>(); } }
	public override Vector3 ForwardDir { get { return playerCamera.cTransform.forward; } }
	public override bool CrouchPressedThisFrame { get { return ((Action_Crouch.WasPressedThisFrame()) && (controllerIsGrounded)); } }
	public override bool CrouchReleasedThisFrame { get { return (Action_Crouch.WasReleasedThisFrame()); } }
	public override void SetCameraOffsetY(float offset)
	{
		playerCamera.yOffset = offset;
	}
	public override void ApplyBobAndCheckFire()
	{
		if (playerCamera.MainCamera.activeSelf)
		{
			if ((cTransform.position - lastPosition).sqrMagnitude > .0001f)
			{
				if (playerCamera != null)
					playerCamera.bopActive = true;
			}
			else if (playerCamera != null)
				playerCamera.bopActive = false;

			//use weapon
			if (Action_Fire.IsPressed())
				wishFire = true;
		}
	}
	public override bool RunReleasedThisFrame { get { return (Action_Run.WasReleasedThisFrame()); } }
	public override bool RunPressed { get { return (Action_Run.IsPressed()); } }
	public override bool JumpPressedThisFrame { get { return (Action_Jump.WasPressedThisFrame()); } }
	public override bool JumpReleasedThisFrame { get { return (Action_Jump.WasReleasedThisFrame()); } }
	public override bool JumpPressed { get { return (Action_Jump.IsPressed()); } }
	public override void CheckMouseWheel()
	{
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

		if (wheel < 0)
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
	public override void CheckWeaponChangeByIndex()
	{
		for (int i = 0; i < 10; i++)
		{
			if (Action_Weapon[i].WasPressedThisFrame())
			{
				TrySwapWeapon(i);
				break;
			}
		}
	}

	public override bool IsControllerGrounded { get { return Physics.CheckSphere(lastPosition, .5f, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore);}
	}
	public override void ApplyMove()
	{
		float deltaTime = Time.fixedDeltaTime;
		lastPosition = cTransform.position;
		rb.MovePosition(receivedPosition);
	}

	public override void ApplySimpleGravity()
	{
		if (capsuleCollider.enabled)
		{
			// Reset the gravity velocity
			playerVelocity = Vector3.down * GameManager.Instance.gravity;
			ApplyMove();
		}
	}
	public override void CheckMovements()
	{

	}
	public override void ChangeColliderHeight(Vector3 center, float height)
	{
		capsuleCollider.center = center;
		capsuleCollider.height = height + ccHeight;
	}

	public override void EnableColliders(bool enable)
	{
		capsuleCollider.enabled = enable;
	}
}