using UnityEngine;
using Assets.MultiAudioListener;
public class PlayerControls : MonoBehaviour
{
	public MultiAudioSource audioSource;
	[HideInInspector]
	public PlayerInfo playerInfo;
	[HideInInspector]
	public PlayerWeapon playerWeapon;
	[HideInInspector]
	public PlayerCamera playerCamera;
	[HideInInspector]
	public PlayerThing playerThing;

	private Vector2 centerHeight = new Vector2(0.2f, -.05f); // character controller center height, x standing, y crouched
	private Vector2 height = new Vector2(2.0f, 1.5f); // character controller height, x standing, y crouched
	private float camerasHeight = .15f;
	private float ccHeight = .05f;

	public Vector2 viewDirection = new Vector2(0, 0);

	public Vector3 lastPosition = new Vector3(0, 0, 0);

	public Vector3 impulseVector = Vector3.zero;

	public Vector3 jumpPadVel = Vector3.zero;

	public float impulseDampening = 4f;
	[HideInInspector]
	public CharacterController controller;
	[HideInInspector]
	public CapsuleCollider capsuleCollider;

	// Movement stuff
	public float crouchSpeed = 3.0f;                // Crouch speed
	public float walkSpeed = 5.0f;					// Walk speed
	public float runSpeed = 7.0f;                   // Run speed
	private float oldSpeed = 0;						// Previous move speed

	public float moveSpeed;							// Ground move speed
	public float runAcceleration = 14.0f;			// Ground accel
	public float runDeacceleration = 10.0f;			// Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;			// Air accel
	public float airDecceleration = 2.0f;			// Deacceleration experienced when ooposite strafing
	public float airControl = 0.3f;					// How precise air control is
	public float sideStrafeAcceleration = 50.0f;	// How fast acceleration occurs to get up to sideStrafeSpeed when
	public float sideStrafeSpeed = 1.0f;			// What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;					// The speed at which the character's up axis gains when hitting jump
	public bool holdJumpToBhop = false;				// When enabled allows player to just hold jump button to keep on bhopping perfectly. Beware: smells like casual.

	public Vector3 playerVelocity = Vector3.zero;

	private bool wishJump = false;

	private float deathTime = 0;
	private float respawnDelay = 1.7f;
	struct currentMove
	{
		public float forwardSpeed;
		public float sidewaysSpeed;
	}

	private currentMove cMove;

	public int CurrentWeapon = -1;
	public int SwapWeapon = -1;

	public MoveType currentMoveType = MoveType.Run;
	public enum MoveType
	{
		Crouch,
		Walk,
		Run
	}
	void Awake()
	{
		controller = GetComponentInParent<CharacterController>();
		capsuleCollider = GetComponentInParent<CapsuleCollider>();
		audioSource = GetComponentInParent<MultiAudioSource>();
		playerCamera = GetComponentInChildren<PlayerCamera>();
		playerInfo = GetComponent<PlayerInfo>();
		playerThing = GetComponentInParent<PlayerThing>();
		playerWeapon = null;
		moveSpeed = runSpeed;
		currentMoveType = MoveType.Run;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (Input.GetKeyDown(KeyCode.Escape))
			Application.Quit();

		if (playerThing.Dead)
		{
			if (deathTime < respawnDelay)
				deathTime += Time.deltaTime;
			else
			{
				if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
				{
					deathTime = 0;
					viewDirection = Vector2.zero;

					if (playerWeapon != null)
					{
						Destroy(playerWeapon.gameObject);
						playerWeapon = null;
					}

					playerInfo.Reset();
					playerThing.InitPlayer(0);
				}
			}
			return;
		}

		if (!playerThing.ready)
			return;

		if (Input.GetKeyDown(KeyCode.Q))
			playerCamera.ChangeThirdPersonCamera(!playerCamera.ThirdPerson.enabled);

		viewDirection.y += Input.GetAxis("Mouse X") * GameOptions.MouseSensitivity.x;
		viewDirection.x -= Input.GetAxis("Mouse Y") * GameOptions.MouseSensitivity.y;

		//so you don't fall when no-clipping
		bool outerSpace = false;

		if (gameObject.layer != GameManager.PlayerLayer)
			outerSpace = true;



		//read input
		if (Input.GetKey(KeyCode.LeftArrow))
			viewDirection.y -= Time.deltaTime * 90;

		if (Input.GetKey(KeyCode.RightArrow))
			viewDirection.y += Time.deltaTime * 90;

		if (viewDirection.y < -180) viewDirection.y += 360;
		if (viewDirection.y > 180) viewDirection.y -= 360;

		//restricted up/down looking angle as sprites look really bad when looked at steep angle
		//also the game doesn't really require such as originally there was no way to rotate camera pitch
		if (viewDirection.x < -85) viewDirection.x = -85;
		if (viewDirection.x > 85) viewDirection.x = 85;

		transform.rotation = Quaternion.Euler(0, viewDirection.y, 0);

		playerThing.avatar.ChangeView(viewDirection, Time.deltaTime);
		playerThing.avatar.CheckLegTurn(playerCamera.MainCamera.transform.forward);

		
		if (Input.GetKey(KeyCode.C))
		{
			if (oldSpeed == 0)
				oldSpeed = moveSpeed;
			moveSpeed = crouchSpeed;
			currentMoveType = MoveType.Crouch;
			ChangeHeight(false);
		}
		else if (Input.GetKeyUp(KeyCode.C))
		{
			moveSpeed = oldSpeed;
			if (moveSpeed == walkSpeed)
				currentMoveType = MoveType.Walk;
			else
				currentMoveType = MoveType.Run;
			ChangeHeight(true);
			oldSpeed = 0;
		}
		else //CheckRun
		{
			if (GameOptions.runToggle)
			{
				if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
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
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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

		playerThing.avatar.isGrounded = controller.isGrounded;

		//Movement Checks
		if (currentMoveType != MoveType.Crouch)
			QueueJump();
		if (controller.isGrounded)
			GroundMove();
		else
			AirMove();

		//apply move
		lastPosition = transform.position;
		controller.Move((playerVelocity + impulseVector + jumpPadVel) * Time.deltaTime);

		//dampen impulse
		if (impulseVector.sqrMagnitude > 0)
		{
			impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseDampening * Time.deltaTime);
			if (impulseVector.sqrMagnitude < 1f)
				impulseVector = Vector3.zero;
		}

		//dampen jump pad impulse
		if (jumpPadVel.sqrMagnitude > 0)
		{
			jumpPadVel.y -= (GameManager.Instance.gravity * Time.deltaTime);
			if (controller.isGrounded)
				jumpPadVel = Vector3.zero;
		}

		if (playerCamera.MainCamera.activeSelf)
		{
			//use weapon
			if (Input.GetMouseButton(0))
				if (playerWeapon.Fire())
				{
					playerInfo.playerHUD.HUDUpdateAmmoNum();
					playerThing.avatar.Attack();
				}
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
		float wheel = Input.GetAxis("Mouse ScrollWheel");

		if ((wheel > 0) || (Input.GetKeyDown(KeyCode.Plus)))
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

		if ((wheel < 0) || (Input.GetKeyDown(KeyCode.Minus)))
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


		if (Input.GetKeyDown(KeyCode.Alpha0))
			TrySwapWeapon(0);

		if (Input.GetKeyDown(KeyCode.Alpha1))
			TrySwapWeapon(1);

		if (Input.GetKeyDown(KeyCode.Alpha2))
			TrySwapWeapon(2);

		if (Input.GetKeyDown(KeyCode.Alpha3))
			TrySwapWeapon(3);

		if (Input.GetKeyDown(KeyCode.Alpha4))
			TrySwapWeapon(4);

		if (Input.GetKeyDown(KeyCode.Alpha5))
			TrySwapWeapon(5);

		if (Input.GetKeyDown(KeyCode.Alpha6))
			TrySwapWeapon(6);

		if (Input.GetKeyDown(KeyCode.Alpha7))
			TrySwapWeapon(7);

		if (Input.GetKeyDown(KeyCode.Alpha8))
			TrySwapWeapon(8);

		if (Input.GetKeyDown(KeyCode.Alpha9))
			TrySwapWeapon(9);

	}

	public void ChangeHeight(bool Standing)
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

		playerCamera.MainCamera.transform.localPosition = new Vector3(0, newCenter + camerasHeight, 0);
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
	private void SetMovementDir()
	{
		cMove.forwardSpeed = 0f;
		cMove.sidewaysSpeed = 0f;

		//qwerty and dvorak combatible =^-^=
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Comma) || Input.GetKey(KeyCode.UpArrow))
			cMove.forwardSpeed += 1f;
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.O) || Input.GetKey(KeyCode.DownArrow))
			cMove.forwardSpeed -= 1f;

		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.E))
			cMove.sidewaysSpeed += 1f;
		if (Input.GetKey(KeyCode.A))
			cMove.sidewaysSpeed -= 1f;
	}
	private void QueueJump()
	{
		if (holdJumpToBhop)
		{
			wishJump = Input.GetKey(KeyCode.Space);
			return;
		}

		if (Input.GetKeyDown(KeyCode.Space) && !wishJump)
			wishJump = true;
		if (Input.GetKeyUp(KeyCode.Space))
			wishJump = false;
	}

	private void GroundMove()
	{
		Vector3 wishdir;

		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f);
		else
			ApplyFriction(0);

		SetMovementDir();

		if (playerThing.avatar.enableOffset)
		{
			playerThing.avatar.TurnLegs((int)currentMoveType,cMove.sidewaysSpeed, cMove.forwardSpeed);
		}

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = transform.TransformDirection(wishdir);
		wishdir.Normalize();

		var wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		Accelerate(wishdir, wishspeed, runAcceleration, runSpeed);

		// Reset the gravity velocity
		playerVelocity.y = -GameManager.Instance.gravity * Time.deltaTime;

		if (wishJump)
		{
			AnimateLegsOnJump();
			playerVelocity.y = jumpSpeed;
			wishJump = false;
		}
	}

	private void ApplyFriction(float t)
	{
		Vector3 vec = playerVelocity;
		float speed;
		float newspeed;
		float control;
		float drop;

		vec.y = 0.0f;
		speed = vec.magnitude;
		drop = 0.0f;

		/* Only if the player is on the ground then apply friction */
		if (controller.isGrounded)
		{
			control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * GameManager.Instance.friction * Time.deltaTime * t;
		}

		newspeed = speed - drop;

		if (newspeed < 0)
			newspeed = 0;
		if (speed > 0)
			newspeed /= speed;

		playerVelocity.x *= newspeed;
		playerVelocity.z *= newspeed;
	}
	private void Accelerate(Vector3 wishdir, float wishspeed, float accel, float wishaccel = 0)
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
		accelspeed = accel * Time.deltaTime * wishaccel;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.x += accelspeed * wishdir.x;
		playerVelocity.z += accelspeed * wishdir.z;
	}

	private void AirMove()
	{
		Vector3 wishdir;
		float accel;

		SetMovementDir();

		playerThing.avatar.TurnLegsOnJump(cMove.sidewaysSpeed);

		wishdir = new Vector3(cMove.sidewaysSpeed, 0, cMove.forwardSpeed);
		wishdir = transform.TransformDirection(wishdir);

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

		Accelerate(wishdir, wishspeed, accel);
		if (airControl > 0)
			AirControl(wishdir, wishspeed2);

		// Apply gravity
		if(jumpPadVel.sqrMagnitude > 0)
			playerVelocity.y = 0;
		else
			playerVelocity.y -= GameManager.Instance.gravity * Time.deltaTime;
	}

	private void AirControl(Vector3 wishdir, float wishspeed)
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
		k *= airControl * dot * dot * Time.deltaTime;

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
