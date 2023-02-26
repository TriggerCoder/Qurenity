using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class ClientLocalControls : PlayerControls
{
	public PlayerCamera playerCamera;
	[HideInInspector]
	public Rigidbody rb;
	[HideInInspector]
	public PlayerInput playerInput;

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

	Vector3 newPosition = Vector3.zero;

	void Awake()
	{
		playerInput = GetComponentInParent<PlayerInput>();
		rb = GetComponentInParent<Rigidbody>();
		//Set the actions
		SetPlayerAction();
		OnAwake();
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
		OnUpdate();
	}

	void FixedUpdate()
	{
		if (GameManager.Paused)
			return;

		OnFixedUpdate();
	}

	public override void SetCameraBobActive(bool active)
	{
		if (playerCamera != null)
			playerCamera.bopActive = active;
	}

	public override bool JumpPressedThisFrame { get { return (Action_Jump.WasPressedThisFrame()); } }
	public override bool JumpReleasedThisFrame { get { return (Action_Jump.WasReleasedThisFrame()); } }
	public override bool JumpPressed { get { return (Action_Jump.IsPressed()); } }
	public override bool FirePressedThisFrame { get { return (Action_Fire.WasPressedThisFrame()); } }
	public override bool FirePressed { get { return (Action_Fire.IsPressed()); } }
	public override void CheckCameraChange()
	{
		if (Action_CameraSwitch.WasPressedThisFrame())
			playerCamera.ChangeThirdPersonCamera(!playerCamera.ThirdPerson.enabled);
	}
	public override void SetViewDirection()
	{
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
	public override Vector3 ForwardDir { get { return playerCamera.cTransform.forward; } }
	public override bool IsControllerGrounded { get { return Physics.CheckSphere(lastPosition, .5f, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore); }}
	public override bool CrouchPressedThisFrame { get { return (Action_Crouch.WasPressedThisFrame()); } }
	public override bool CrouchReleasedThisFrame { get { return (Action_Crouch.WasReleasedThisFrame()); } }
	public override void CrouchChangePlayerSpeed(bool Standing)
	{
		if (Standing)
		{
			currentMoveType = MoveType.Run;
			return;
		}
		currentMoveType = MoveType.Crouch;
	}
	public override void ChangeHeight(bool Standing)
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

		playerCamera.yOffset = newCenter + camerasHeight;
	}
	public override void CheckIfRunning()
	{
		if (GameOptions.runToggle)
		{
			if (Action_Run.WasReleasedThisFrame())
			{
				if (currentMoveType == MoveType.Walk)
					currentMoveType = MoveType.Run;
				else
					currentMoveType = MoveType.Walk;
			}
		}
		else
		{
			if (Action_Run.IsPressed())
				currentMoveType = MoveType.Run;
			else
				currentMoveType = MoveType.Walk;
		}
	}

	public override void CheckMouseWheelWeaponChange()
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
	public override void ApplySimpleGravity()
	{
		lastPosition = cTransform.position;
		rb.MovePosition(newPosition);
	}
	public override void ApplyMove()
	{
		lastPosition = cTransform.position;
		rb.MovePosition(newPosition);
	}
	public override void EnableColliders(bool enable)
	{
		capsuleCollider.enabled = enable;
	}
	public override void RotateTorwardDir()
	{
		cTransform.rotation = Quaternion.Euler(0, viewDirection.y, 0);
	}
	public override void SetMove(Vector3 vPosition, Vector3 vForward)
	{
		newPosition = vPosition;
	}
}
