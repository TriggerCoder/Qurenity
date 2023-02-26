using System;
using Riptide;
using UnityEngine;
using UnityEngine.InputSystem;

public class ServerRemoteControls : PlayerControls
{
	public float impulseDampening = 4f;
	[HideInInspector]
	public CharacterController controller;

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

	//RemoteInputs
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

	bool[] LastFrameInputs = new bool[20];
	bool[] ThisFrameInputs = new bool[20];

	Vector3 inputForward = Vector3.forward;
	Vector2 viewForward = Vector2.zero;
	void Awake()
	{
		controller = GetComponentInParent<CharacterController>();
		moveSpeed = runSpeed;
		OnAwake();
	}

	void Start()
	{
		InvalidateThisFrameInputs();
	}

	void InvalidateThisFrameInputs()
	{
		for (int i = 0; i < ThisFrameInputs.Length; i++)
		{
			LastFrameInputs[i] = ThisFrameInputs[i];
			ThisFrameInputs[i] = false;
		}
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

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
		InvalidateThisFrameInputs();
		SendMovement();
	}
	private void SetMovementDir()
	{
		Vector2 Move = Vector2.zero;
		if (ThisFrameInputs[Forward])
			Move.y += 1;
		if (ThisFrameInputs[Backwards])
			Move.y -= 1;
		if (ThisFrameInputs[Left])
			Move.x -= 1;
		if (ThisFrameInputs[Right])
			Move.x += 1;

		cMove.forwardSpeed = Move.y;
		cMove.sidewaysSpeed = Move.x;
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


	public override bool JumpPressedThisFrame { get { return ((ThisFrameInputs[Jump]) && (!LastFrameInputs[Jump])); } }
	public override bool JumpReleasedThisFrame { get { return ((ThisFrameInputs[Jump]) && (!LastFrameInputs[Jump])); } }
	public override bool JumpPressed { get { return (ThisFrameInputs[Jump]); } }
	public override bool FirePressedThisFrame { get { return ((ThisFrameInputs[Fire]) && (!LastFrameInputs[Fire])); } }
	public override bool FirePressed { get { return (ThisFrameInputs[Fire]); } }
	public override void SetViewDirection()
	{
		viewForward.x = Vector3.SignedAngle(Vector3.forward, ForwardDir, Vector3.up);
		viewForward.y = Vector3.SignedAngle(Vector3.forward, ForwardDir, Vector3.right);

		viewDirection.y += Look.x;
		viewDirection.x -= Look.y;
	}
	public override Vector2 Look { get { return viewForward; } }
	public override Vector3 ForwardDir { get { return inputForward; } }
	public override bool IsControllerGrounded { get { return controller.isGrounded; } }
	public override bool CrouchPressedThisFrame { get { return ((ThisFrameInputs[Crouch]) && (!LastFrameInputs[Crouch])); } }
	public override bool CrouchReleasedThisFrame { get { return ((ThisFrameInputs[Crouch]) && (!LastFrameInputs[Crouch])); } }
	public override void CrouchChangePlayerSpeed(bool Standing)
	{
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
	public override void ChangeHeight(bool Standing)
	{
		float newCenter = centerHeight.y;
		float newHeight = height.y;

		if (Standing)
		{
			newCenter = centerHeight.x;
			newHeight = height.x;
		}
		controller.center = new Vector3(0, newCenter, 0);
		controller.height = newHeight;

		capsuleCollider.center = controller.center;
		capsuleCollider.height = newHeight + ccHeight;
	}
	public override void CheckIfRunning()
	{
		if (GameOptions.runToggle)
		{
			if ((ThisFrameInputs[Run]) && (!LastFrameInputs[Run]))
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
			if (ThisFrameInputs[Run])
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
	public override void QueueJump()
	{
		if (holdJumpToBhop)
		{
			wishJump = JumpPressed;
			return;
		}

		if (JumpPressedThisFrame && !wishJump)
			wishJump = true;
		if (JumpReleasedThisFrame)
			wishJump = false;
	}

	public override void CheckMouseWheelWeaponChange()
	{
		int wheel = 0;
		if (ThisFrameInputs[Weapon_Plus])
			wheel += 1;
		if (ThisFrameInputs[Weapon_Minus])
			wheel -= 1;

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
			if ((ThisFrameInputs[Weapon + i]) && (!LastFrameInputs[Weapon + i]))
			{
				TrySwapWeapon(i);
				break;
			}
		}
	}

	public override void ApplySimpleGravity()
	{
		if (controller.enabled)
		{
			// Reset the gravity velocity
			playerVelocity = Vector3.down * GameManager.Instance.gravity;
			ApplyMove();
		}
	}
	public override void ApplyMove()
	{
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
	public override void CheckMovements()
	{
		if (controllerIsGrounded)
			GroundMove(Time.fixedDeltaTime);
		else
			AirMove(Time.fixedDeltaTime);
	}
	public override void EnableColliders(bool enable)
	{
		capsuleCollider.enabled = enable;
		controller.enabled = enable;
	}

	public override Vector2 GetBobDelta(float hBob, float vBob, float lerp)
	{
		Vector2 position;

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
	public override void RotateTorwardDir()
	{
		Vector3 dir = FlattenVector3(ForwardDir);
		cTransform.forward = dir.normalized;
	}

	public override void SetInput(bool[] bInputs, Vector3 vForward)
	{
		ThisFrameInputs = bInputs;
		inputForward = vForward;
	}
	Vector3 FlattenVector3(Vector3 vector)
	{
		vector.y = 0;
		return vector;
	}
	void SendMovement()
	{
		Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
		message.AddUShort(playerThing.playerId);
		message.AddVector3(cTransform.position);
		message.AddVector3(cTransform.forward);
		NetworkManager.Instance.server.SendToAll(message);
	}
}