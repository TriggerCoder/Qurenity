using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class PlayerModel : MonoBehaviour, Damageable
{
	public int rotationFPS = 15;

	private MD3 head;
	private MD3 upper;
	private MD3 lower;
	private MD3 weapon;

	public UpperAnimation upperAnimation = UpperAnimation.Stand;
	public LowerAnimation lowerAnimation = LowerAnimation.Idle;

	private int airFrames = 0;
	private const int readyToLand = 25;
	public bool enableOffset { get { return _enableOffset; } set { _enableOffset = value; } }
	public bool isGrounded { get { return _isGrounded; } set { if ((!_isGrounded) && (!value)) { airFrames++; if (airFrames > readyToLand) airFrames = readyToLand; } else airFrames = 0; _isGrounded = value; } }

	private bool _enableOffset = true;
	private bool _isGrounded = true;
	private List<ModelAnimation> upperAnim = new List<ModelAnimation>();
	private List<ModelAnimation> lowerAnim = new List<ModelAnimation>();

	private Dictionary<string, string> meshToSkin = new Dictionary<string, string>();

	private MD3UnityConverted lowerModel;
	private MD3UnityConverted upperModel;
	private MD3UnityConverted headModel;
	private MD3UnityConverted weaponModel;

	private ModelAnimation nextUpper;
	private ModelAnimation nextLower;

	private ModelAnimation currentUpper;
	private ModelAnimation currentLower;

	private int nextFrameUpper;
	private int nextFrameLower;

	private int currentFrameUpper;
	private int currentFrameLower;

	private bool loaded = false;
	private bool ragDoll = false;
	private bool ownerDead = false;
	public class ModelAnimation
	{
		public int index;
		public int startFrame;
		public int endFrame;
		public int loopingFrames;
		public int fps;
		public string strName;
		public int nextFrame = 1;
		public ModelAnimation(int index)
		{
			this.index = index;
		}
	}

	public enum UpperAnimation
	{
		Death1,
		Dead1,
		Death2,
		Dead2,
		Death3,
		Dead3,
		Gesture,
		Attack,
		Melee,
		Drop,
		Raise,
		Stand,
		Stand2
	}
	public enum LowerAnimation
	{
		Death1,
		Dead1,
		Death2,
		Dead2,
		Death3,
		Dead3,
		WalkCR,
		Walk,
		Run,
		RunBack,
		Swim,
		Jump,
		Land,
		JumpBack,
		LandBack,
		Idle,
		IdleCR,
		Turn,
		WalkCRBack,
		Fall,
		WalkBack,
		FallBack
	}

	public MoveType currentMoveType = MoveType.Run;
	private MoveType nextMoveType = MoveType.Run;
	public enum MoveType
	{
		Crouch,
		Walk,
		Run
	}
	private const int TotalAnimation = 29;

	private GameObject upperBody;
	private GameObject headBody;
	private GameObject barrel;
	private GameObject muzzleFlash;

	private Transform playerTransform;

	private Transform lowerTransform;
	private Transform upperTransform;
	private Transform headTransform;

	private Transform tagHeadTransform;
	private Transform weaponTransform;

	private float upperLerpTime = 0;
	private float upperCurrentLerpTime = 0;
	private float lowerLerpTime = 0;
	private float lowerCurrentLerpTime = 0;

	private Vector3 turnTo = Vector3.zero;

	private int hitpoints = 50;
	private Rigidbody rb;

	//Needed to keep impulse once it turn into ragdoll
	private PlayerControls playerControls;
	private float impulseDampening = 4f;
	private Vector3 impulseVector = Vector3.zero;

	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	List<Vector3> currentVect = new List<Vector3>();
	List<Vector3> nextVect = new List<Vector3>();

	void ApplySimpleMove()
	{
		float gravityAccumulator;
		Vector3 currentPosition = playerTransform.position;
		isGrounded = Physics.CheckSphere(currentPosition, .5f, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore);
		if (isGrounded)
			gravityAccumulator = 0f;
		else
			gravityAccumulator = GameManager.Instance.gravity;
		Vector3 gravity = Vector3.down *  gravityAccumulator;
		currentPosition += (gravity + impulseVector) * Time.deltaTime;

		//dampen impulse
		if (impulseVector.sqrMagnitude > 0)
		{
			impulseVector = Vector3.Lerp(impulseVector, Vector3.zero, impulseDampening * Time.deltaTime);
			if (impulseVector.sqrMagnitude < 1f)
				impulseVector = Vector3.zero;
		}
		rb.MovePosition(currentPosition);
	}

	private void Update()
	{
		if (GameManager.Paused)
			return;

		if (!loaded)
			return;

		if (ragDoll)
		{
			ApplySimpleMove();
			return;
		}

		if (turnTo.sqrMagnitude > 0)
		{
			playerTransform.forward = Vector3.Slerp(playerTransform.forward, turnTo, rotationFPS * Time.deltaTime);
		}

		{
			nextUpper = upperAnim[(int)upperAnimation];
			nextLower = lowerAnim[(int)lowerAnimation];

			if (nextUpper.index == currentUpper.index)
			{
				nextFrameUpper = currentFrameUpper + 1;
				if (nextFrameUpper >= currentUpper.endFrame)
				{
					switch ((UpperAnimation)nextUpper.index)
					{
						default:
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
						break;
						case UpperAnimation.Death1:
						case UpperAnimation.Death2:
						case UpperAnimation.Death3:
							upperAnimation++;
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
							ChangeToRagDoll();
							return;
						case UpperAnimation.Attack:
						case UpperAnimation.Raise:
							upperAnimation = UpperAnimation.Stand;
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = nextUpper.startFrame;
							break;
						case UpperAnimation.Drop:
							nextFrameUpper = currentUpper.endFrame;
						break;
					}
				}
			}
			else
				nextFrameUpper = nextUpper.startFrame;

			if (nextLower.index == currentLower.index)
			{
				nextFrameLower = currentFrameLower + currentLower.nextFrame;
				//Need to check if correct end frame depending on start frame
				if (((currentLower.nextFrame > 0)
				 && (nextFrameLower >= currentLower.endFrame)) ||
				 ((currentLower.nextFrame < 0)
				 && (nextFrameLower <= currentLower.endFrame)))
				{
					switch ((LowerAnimation)nextLower.index)
					{
						default:
							
						break;
						case LowerAnimation.Death1:
						case LowerAnimation.Death2:
						case LowerAnimation.Death3:
							lowerAnimation++;
						break;
						case LowerAnimation.Jump:
							lowerAnimation = LowerAnimation.Land;
						break;
						case LowerAnimation.JumpBack:
							lowerAnimation = LowerAnimation.LandBack;
						break;
						case LowerAnimation.Land:
						case LowerAnimation.LandBack:
							lowerAnimation += 7;
						break;
						case LowerAnimation.Turn:
						case LowerAnimation.Fall:
						case LowerAnimation.FallBack:
							if (_isGrounded)
							{
								if (turnTo.sqrMagnitude > 0)
								{
									playerTransform.forward = turnTo;
									turnTo = Vector3.zero;
								}
								lowerAnimation = LowerAnimation.Idle;
								_enableOffset = true;
							}
						break;
					}
					nextLower = lowerAnim[(int)lowerAnimation];
					nextFrameLower = currentLower.startFrame;
				}
			}
			else
				nextFrameLower = nextLower.startFrame;

			Quaternion upperTorsoRotation = Quaternion.Slerp(upper.tagsbyName["tag_torso"][currentFrameUpper].rotation, upper.tagsbyName["tag_torso"][nextFrameUpper].rotation, upperCurrentLerpTime);
			Quaternion upperHeadRotation = Quaternion.Slerp(upper.tagsbyName["tag_head"][currentFrameUpper].rotation, upper.tagsbyName["tag_head"][nextFrameUpper].rotation, upperCurrentLerpTime);
			Quaternion lowerTorsoRotation = Quaternion.Slerp(lower.tagsbyName["tag_torso"][currentFrameLower].rotation, lower.tagsbyName["tag_torso"][nextFrameLower].rotation, lowerCurrentLerpTime);
			Quaternion weaponRotation = Quaternion.Slerp(upper.tagsbyName["tag_weapon"][currentFrameUpper].rotation, upper.tagsbyName["tag_weapon"][nextFrameUpper].rotation, upperCurrentLerpTime);


			Vector3 upperTorsoOrigin = Vector3.Lerp(upper.tagsbyName["tag_torso"][currentFrameUpper].origin, upper.tagsbyName["tag_torso"][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 upperHeadOrigin = Vector3.Lerp(upper.tagsbyName["tag_head"][currentFrameUpper].origin, upper.tagsbyName["tag_head"][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 lowerTorsoOrigin = Vector3.Lerp(lower.tagsbyName["tag_torso"][currentFrameLower].origin, lower.tagsbyName["tag_torso"][nextFrameLower].origin, lowerCurrentLerpTime);
			Vector3 weaponOrigin = Vector3.Lerp(upper.tagsbyName["tag_weapon"][currentFrameUpper].origin, upper.tagsbyName["tag_weapon"][nextFrameUpper].origin, upperCurrentLerpTime);

			{
				Vector3 currentOffset = lowerTorsoRotation * upperTorsoOrigin;
				Quaternion currentRotation = lowerTorsoRotation * upperTorsoRotation;

				for (int i = 0; i < upper.meshes.Count; i++)
				{
					MD3Mesh currentMesh = upper.meshes[i];
					currentVect.Clear();
					nextVect.Clear();
					currentVect.AddRange(currentMesh.verts[currentFrameUpper]);
					nextVect.AddRange(currentMesh.verts[nextFrameUpper]);
					for (int j = 0; j < currentVect.Count; j++)
					{
						currentVect[j] = currentRotation * Vector3.Lerp(currentVect[j], nextVect[j], upperCurrentLerpTime);
						currentVect[j] += currentOffset;
					}

					upperModel.data[i].meshFilter.mesh.SetVertices(currentVect);
					upperModel.data[i].meshFilter.mesh.RecalculateNormals();
				}

				Quaternion baseRotation = lowerTorsoRotation;
				currentOffset = baseRotation * upperHeadOrigin;
				currentRotation = baseRotation * upperHeadRotation;

				tagHeadTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);

				currentOffset = baseRotation * weaponOrigin;
				currentRotation = baseRotation * weaponRotation;

				weaponTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);


//				if ((_enableOffset) || (ownerDead))
					playerTransform.localPosition = lowerTorsoOrigin;
//				else
//					playerTransform.localPosition = Vector3.zero;

				currentOffset = upperTorsoRotation * upperTorsoOrigin;
				currentOffset -= lowerTorsoOrigin;
				currentRotation = upperTorsoRotation;

				for (int i = 0; i < lower.meshes.Count; i++)
				{
					MD3Mesh currentMesh = lower.meshes[i];
					currentVect.Clear();
					nextVect.Clear();
					currentVect.AddRange(currentMesh.verts[currentFrameLower]);
					nextVect.AddRange(currentMesh.verts[nextFrameLower]);

					for (int j = 0; j < currentVect.Count; j++)
					{
						currentVect[j] = currentRotation * Vector3.Lerp(currentVect[j], nextVect[j], lowerCurrentLerpTime);
						currentVect[j] += currentOffset;

					}
					lowerModel.data[i].meshFilter.mesh.SetVertices(currentVect);
					lowerModel.data[i].meshFilter.mesh.RecalculateNormals();
				}
			}

			upperLerpTime = nextUpper.fps * Time.deltaTime;
			lowerLerpTime = nextLower.fps * Time.deltaTime;

			upperCurrentLerpTime += upperLerpTime;
			lowerCurrentLerpTime += lowerLerpTime;

			if (upperCurrentLerpTime >= 1.0f)
			{
				upperCurrentLerpTime -= 1.0f;
				currentUpper = nextUpper;
				currentFrameUpper = nextFrameUpper;
			}

			if (lowerCurrentLerpTime >= 1.0f)
			{
				lowerCurrentLerpTime -= 1.0f;
				currentLower = nextLower;
				currentFrameLower = nextFrameLower;
			}
		}
	}

	public void ChangeView(Vector2 viewDirection, float deltaTime)
	{
		if (ownerDead)
			return;

		//In order to keep proper animation and not offset it by looking at target, otherwise head could go Exorcist-like
		if (!_enableOffset)
			return;

		float vView = viewDirection.x;
		float hView = viewDirection.y;

		headTransform.rotation = Quaternion.Slerp(headTransform.rotation, Quaternion.Euler(0, hView + 90, vView), rotationFPS * deltaTime);

		int vAngle = (int)Mathf.Round((vView) / (360) * 32) % 32;
		int hAngle = (int)Mathf.Round((hView + 90) / (360) * 32) % 32;

		upperTransform.rotation = Quaternion.Slerp(upperTransform.rotation, Quaternion.Euler(0, 11.25f * hAngle, 7.5f * vAngle), rotationFPS * deltaTime);
	}

	public void CheckLegTurn(Vector3 direction)
	{
		if (ownerDead)
			return;

		Vector3 forward = playerTransform.forward;
		int angle = (int)Mathf.Round((Mathf.Atan2(direction.x, direction.z)) / (Mathf.PI * 2) * 8) % 8;

		//Player Models are rotated 90º
		angle += 2;
		direction = Quaternion.Euler(0f, angle * 45f, 0f) * Vector3.forward;

		angle = (int)Mathf.Round(((Mathf.Atan2((forward.z * direction.x) - (direction.z * forward.x), (forward.x * direction.x) + (forward.z * direction.z)))) / (Mathf.PI * 2) * 8) % 8;

		if (angle != 0)
		{
			turnTo = direction;
			if (lowerAnimation == LowerAnimation.Idle)
				lowerAnimation = LowerAnimation.Turn;
		}
	}
	public void Attack ()
	{
		if (ownerDead)
			return;

		upperAnimation = UpperAnimation.Attack;
	}

	private void ChangeToRagDoll()
	{
		Vector3 currentPosition = playerTransform.position;
		Quaternion currentRotation = playerTransform.rotation;

		//Need to change head mesh from transform position and rotation offsets to vertex to get a correct collider
		Vector3 headOffset = tagHeadTransform.localPosition;
		Quaternion headRotation = tagHeadTransform.localRotation;

		for (int i = 0; i < head.meshes.Count; i++)
		{
			MD3Mesh currentMesh = head.meshes[i];
			currentVect.Clear();
			nextVect.Clear();
			//Head has only 1 frame
			currentVect.AddRange(currentMesh.verts[0]);
			for (int j = 0; j < currentVect.Count; j++)
			{
				currentVect[j] = headRotation * currentVect[j];
				currentVect[j] += headOffset;
			}

			headModel.data[i].meshFilter.mesh.SetVertices(currentVect);
			headModel.data[i].meshFilter.mesh.RecalculateNormals();
		}

		playerTransform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
		playerTransform.SetPositionAndRotation(currentPosition, currentRotation);
		tagHeadTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		var meshFilterChildren = playerTransform.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
		for (var i = 0; i < combine.Length; i++)
			combine[i].mesh = meshFilterChildren[i].mesh;

		var mesh = new Mesh();
		mesh.CombineMeshes(combine, true, false, false);
		mesh.RecalculateNormals();

		MeshCollider mc = playerTransform.gameObject.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;
		mc.convex = true;
		rb = playerTransform.gameObject.AddComponent<Rigidbody>();
		rb.useGravity = false;
		rb.isKinematic = true;


		impulseVector = playerControls.impulseVector;
		playerControls.EnableColliders(false);
		ragDoll = true;

		DestroyAfterTime destroyAfterTime = playerTransform.gameObject.AddComponent<DestroyAfterTime>();
		destroyAfterTime._lifeTime = 10;
	}

	public void Die()
	{
		//Need to reset the torso and head view
		headTransform.localRotation = Quaternion.identity;
		upperTransform.localRotation = Quaternion.identity;

		int deathNum = 2 * UnityEngine.Random.Range(0, 3);
		upperAnimation = (UpperAnimation)deathNum;
		lowerAnimation = (LowerAnimation)deathNum;

		gameObject.layer = GameManager.RagdollLayer;
		GameManager.SetLayerAllChildren(playerTransform, GameManager.RagdollLayer);

		ownerDead = true;
	}

	public void TurnLegsOnJump(float sideMove)
	{
		Quaternion rotate = Quaternion.identity;

		if (airFrames < readyToLand)
			return;

		switch(lowerAnimation)
		{
			default:
				return;
			break;
			case LowerAnimation.Idle:
			case LowerAnimation.IdleCR:
			case LowerAnimation.Run:
			case LowerAnimation.Walk:
			case LowerAnimation.WalkCR:
				lowerAnimation = LowerAnimation.Land;
				return;
			break;
			case LowerAnimation.RunBack:
			case LowerAnimation.WalkBack:
			case LowerAnimation.WalkCRBack:
				lowerAnimation = LowerAnimation.LandBack;
				return;
			break;
			case LowerAnimation.Land:
			case LowerAnimation.LandBack:
			case LowerAnimation.Fall:
			case LowerAnimation.FallBack:
			break;
		}

		if (sideMove > 0)
			rotate = Quaternion.AngleAxis(30f, playerTransform.up);
		else if (sideMove < 0)
			rotate = Quaternion.AngleAxis(-30f, playerTransform.up);

		lowerTransform.localRotation = rotate;
	}
	public void TurnLegs(int moveType, float sideMove, float forwardMove)
	{
		if (ownerDead)
			return;

		nextMoveType = (MoveType)moveType;

		Quaternion rotate = Quaternion.identity;
		if (forwardMove > 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.Run;
				break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.Walk;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCR;
				break;
			}
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(30f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(-30f, playerTransform.up);
		}
		else if (forwardMove < 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.RunBack;
					break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.WalkBack;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCRBack;
					break;
			}
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(-30f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(30f, playerTransform.up);
		}
		else if (sideMove != 0)
		{
			switch (nextMoveType)
			{
				default:
				case MoveType.Run:
					lowerAnimation = LowerAnimation.Run;
					break;
				case MoveType.Walk:
					lowerAnimation = LowerAnimation.Walk;
					break;
				case MoveType.Crouch:
					lowerAnimation = LowerAnimation.WalkCR;
					break;
			}
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(50f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(-50f, playerTransform.up);
		}
		else if (lowerAnimation != LowerAnimation.Turn)
		{
			if (nextMoveType == MoveType.Crouch)
				lowerAnimation = LowerAnimation.IdleCR;
			else
				lowerAnimation = LowerAnimation.Idle;
		}

		lowerTransform.localRotation = rotate;
	}

	public void MuzzleFlashSetActive(bool active)
	{
		if (muzzleFlash == null)
			return;

		muzzleFlash.SetActive(active);
	}

	public void RotateBarrel(Quaternion rotation, float speed)
	{
		if (barrel == null)
			return;

		barrel.transform.localRotation = Quaternion.Slerp(barrel.transform.localRotation, rotation, speed);
	}

	public void LoadWeapon(MD3 newWeapon, string completeModelName, string muzzleModelName, int layer)
	{
		if (ownerDead)
			return;

		if (weaponModel != null)
			if (weaponModel.go != null)
				Destroy(weaponModel.go);

		if (!string.IsNullOrEmpty(completeModelName))
		{
			weapon = ModelsManager.GetModel(completeModelName);
			if (weapon == null)
				return;
		}
		else
			weapon = newWeapon;

		if (weapon.readyMeshes.Count == 0)
			weaponModel = Mesher.GenerateModelFromMeshes(weapon);
		else
			weaponModel = Mesher.FillModelFromProcessedData(weapon);
		weaponModel.go.name = "weapon";
		weaponModel.go.transform.SetParent(weaponTransform.transform);
		weaponModel.go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		if (!string.IsNullOrEmpty(completeModelName))
		{
			Vector3 OffSet = Vector3.zero;
			barrel = new GameObject("barrel_weapon");
			if (newWeapon.readyMeshes.Count == 0)
				Mesher.GenerateModelFromMeshes(newWeapon, barrel);
			else
				Mesher.FillModelFromProcessedData(newWeapon, barrel);
			barrel.transform.SetParent(weaponModel.go.transform);
			barrel.layer = weaponModel.go.layer;
			foreach (MD3Tag tag in weapon.tags)
			{
				if (string.Equals(tag.name, "tag_barrel"))
				{
					OffSet = tag.origin;
					break;
				}
			}
			barrel.transform.SetLocalPositionAndRotation(OffSet, Quaternion.identity);
		}

		upperAnimation = UpperAnimation.Raise;

		if (!string.IsNullOrEmpty(muzzleModelName))
		{
			MD3UnityConverted muzzleUnityConverted;
			Vector3 OffSet = Vector3.zero;
			List<MD3Tag> weaponTags;
			muzzleFlash = new GameObject("muzzle_flash");
			MD3 muzzle = ModelsManager.GetModel(muzzleModelName, true);

			if (muzzle == null)
				return;

			if (muzzle.readyMeshes.Count == 0)
				muzzleUnityConverted = Mesher.GenerateModelFromMeshes(muzzle, muzzleFlash, true);
			else
				muzzleUnityConverted = Mesher.FillModelFromProcessedData(muzzle, muzzleFlash);
			muzzleFlash.layer = weaponModel.go.layer;

			//Muzzle Flash never cast shadow
			for (int i = 0; i < muzzle.readyMeshes.Count; i++)
				muzzleUnityConverted.data[i].meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			
			//Such Vanity
			if (barrel == null)
			{
				muzzleFlash.transform.SetParent(weaponModel.go.transform);
				weaponTags = weapon.tags;
			}
			else
			{
				muzzleFlash.transform.SetParent(barrel.transform);
				weaponTags = newWeapon.tags;
			}
			foreach (MD3Tag tag in weaponTags)
			{
				if (string.Equals(tag.name, "tag_flash"))
				{
					OffSet = tag.origin;
					break;
				}
			}
			muzzleFlash.transform.SetLocalPositionAndRotation(OffSet, Quaternion.identity);
			muzzleFlash.SetActive(false);
		}

		GameManager.SetLayerAllChildren(weaponTransform, layer);
	}

	public void UnloadWeapon()
	{
		if (weapon == null)
			return;

		if (weaponModel != null)
			if (weaponModel.go != null)
				Destroy(weaponModel.go);

		weapon = null;
		barrel = null;
		muzzleFlash = null;

		if (ownerDead)
			return;
		
		upperAnimation = UpperAnimation.Drop;
	}
	public bool LoadPlayer(string modelName, string SkinName, int layer, PlayerControls control)
	{
		string playerModelPath = "players/" + modelName;

		string lowerModelName = playerModelPath + "/lower";
		string upperModelName = playerModelPath + "/upper";
		string headModelName = playerModelPath + "/head";
		string animationFile = playerModelPath + "/animation";

		string lowerSkin = playerModelPath + "/lower_" + SkinName;
		string upperSkin = playerModelPath + "/upper_" + SkinName;
		string headSkin = playerModelPath + "/head_" + SkinName;

		lower = ModelsManager.GetModel(lowerModelName);
		if (lower == null)
			return false;
		upper = ModelsManager.GetModel(upperModelName);
		if (upper == null)
			return false;

		head = ModelsManager.GetModel(headModelName);
		if (head == null)
			return false;

		if (!LoadSkin(lower, lowerSkin))
			return false;
		if (!LoadSkin(upper, upperSkin))
			return false;
		if (!LoadSkin(head, headSkin))
			return false;

		LoadAnimations(animationFile, upperAnim, lowerAnim);
		currentUpper = upperAnim[(int)UpperAnimation.Stand];
		currentFrameUpper = currentUpper.startFrame;
		currentLower = lowerAnim[(int)LowerAnimation.Idle];
		currentFrameLower = currentLower.startFrame;

		{
			GameObject playerModel = gameObject;
			playerModel.name = modelName;
			playerTransform = transform;

			upperBody = new GameObject("Upper Body");
			upperTransform = upperBody.transform;
			upperTransform.SetParent(playerModel.transform);

			GameObject tag_head = new GameObject("tag_head");
			tagHeadTransform = tag_head.transform;
			tagHeadTransform.SetParent(upperTransform);

			GameObject tag_weapon = new GameObject("tag_weapon");
			weaponTransform = tag_weapon.transform;
			weaponTransform.SetParent(upperTransform);

			headBody = new GameObject("Head");
			headTransform = headBody.transform;
			headTransform.SetParent(tag_head.transform);

			if (upper.readyMeshes.Count == 0)
				upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin, true);
			else
				upperModel = Mesher.FillModelFromProcessedData(upper, meshToSkin);
			upperModel.go.name = "upper_body";
			upperModel.go.transform.SetParent(upperTransform);

			if (head.readyMeshes.Count == 0)
				headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin, false);
			else
				headModel = Mesher.FillModelFromProcessedData(head, meshToSkin);

			headModel.go.name = "head";
			headModel.go.transform.SetParent(headTransform);

			if (lower.readyMeshes.Count == 0)
				lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin, true);
			else
				lowerModel = Mesher.FillModelFromProcessedData(lower, meshToSkin);
			lowerModel.go.name = "lower_body";
			lowerTransform = lowerModel.go.transform;
			lowerModel.go.transform.SetParent(playerModel.transform);

			loaded = true;
		}
		playerControls = control;
		GameManager.SetLayerAllChildren(playerTransform, layer);
		return true;
	}

	private bool LoadAnimations(string fileName, List<ModelAnimation> upper, List<ModelAnimation> lower)
	{
		StreamReader animFile;

		string path = Application.streamingAssetsPath + "/models/" + fileName + ".cfg";
		if (File.Exists(path))
			animFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + fileName + ".cfg").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			ZipEntry map = zip[path];
			MemoryStream ms = new MemoryStream();
			map.Extract(ms);
			animFile = new StreamReader(ms);
		}
		else
		{
			Debug.Log("Unable to load animation file: " + fileName);
			return false;
		}

		animFile.BaseStream.Seek(0, SeekOrigin.Begin);
		ModelAnimation[] animations = new ModelAnimation[TotalAnimation];

		if (animFile.EndOfStream)
		{
			return false;
		}

		string strWord;
		int currentAnim = 0;
		int torsoOffset = 0;
		int legsOffset = (int)LowerAnimation.WalkCR + 1;
		char[]	separators = new char[2] { '\t', '(' };
		while (!animFile.EndOfStream)
		{
			strWord = animFile.ReadLine();

			if (strWord.Length == 0)
				continue;

			if (!char.IsDigit(strWord[0]))
			{
				continue;
			}
			
			string[] values = new string[4] {"","","",""};
			bool lastDigit = true;
			for (int i = 0, j = 0; i < strWord.Length; i++)
			{
				if (char.IsDigit(strWord[i]))
				{
					if (lastDigit)
						values[j] += strWord[i];
					else
					{
						j++;
						values[j] += strWord[i];
						lastDigit = true;
					}
				}
				else
					lastDigit = false;

				if ((j == 3) && (!lastDigit))
					break;
			}

			int startFrame = int.Parse(values[0]);
			int numOfFrames = int.Parse(values[1]);
			int loopingFrames = int.Parse(values[2]);
			int fps = int.Parse(values[3]);

			animations[currentAnim] = new ModelAnimation(currentAnim);
			animations[currentAnim].startFrame = startFrame;
			animations[currentAnim].endFrame = startFrame + numOfFrames;
			animations[currentAnim].loopingFrames = loopingFrames;
			animations[currentAnim].fps = fps;

			string[] name = strWord.Split('/');
			strWord = name[name.Length - 1].Trim();
			name = strWord.Split(separators);
			animations[currentAnim].strName = name[0];

			if (IsInString(animations[currentAnim].strName, "BOTH"))
			{
				upper.Add(animations[currentAnim]);
				lower.Add(animations[currentAnim]);
			}
			else if (IsInString(animations[currentAnim].strName, "TORSO"))
			{
				upper.Add(animations[currentAnim]);
			}
			else if (IsInString(animations[currentAnim].strName, "LEGS"))
			{
				if (torsoOffset == 0)
					torsoOffset = animations[(int)UpperAnimation.Stand2 + 1].startFrame - animations[(int)LowerAnimation.WalkCR].startFrame;

				animations[currentAnim].startFrame -= torsoOffset;
				animations[currentAnim].endFrame -= torsoOffset;
				animations[currentAnim].index -= legsOffset;
				lower.Add(animations[currentAnim]);
			}
			currentAnim++;
		}
		//Add Walk Crounched Back 
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.WalkCRBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.WalkCR].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.WalkCR].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[(int)LowerAnimation.WalkCR].loopingFrames;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.WalkCR].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.Fall);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.Land].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.Land].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.Land].fps;
		lower.Add(animations[currentAnim++]);

		//Add Walk Back
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.WalkBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.Walk].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.Walk].startFrame - 1;
		animations[currentAnim].loopingFrames = lowerAnim[(int)LowerAnimation.Walk].loopingFrames;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.Walk].fps;
		animations[currentAnim].nextFrame = -1;
		lower.Add(animations[currentAnim++]);

		//Add Fall Back
		animations[currentAnim] = new ModelAnimation((int)LowerAnimation.FallBack);
		animations[currentAnim].startFrame = lowerAnim[(int)LowerAnimation.LandBack].endFrame - 1;
		animations[currentAnim].endFrame = lowerAnim[(int)LowerAnimation.LandBack].endFrame;
		animations[currentAnim].loopingFrames = 0;
		animations[currentAnim].fps = lowerAnim[(int)LowerAnimation.LandBack].fps;
		lower.Add(animations[currentAnim]);

		animFile.Close();
		return true;
	}
	public bool LoadSkin(MD3 model, string skinName)
	{
		StreamReader SkinFile;

		string path = Application.streamingAssetsPath + "/models/" + skinName + ".skin";
		if (File.Exists(path))
			SkinFile = new StreamReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + skinName + ".skin").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			ZipEntry map = zip[path];
			MemoryStream ms = new MemoryStream();
			map.Extract(ms);
			SkinFile = new StreamReader(ms);
		}
		else
		{
			Debug.Log("Unable to load skin for model: " + model.name);
			return false;
		}

		SkinFile.BaseStream.Seek(0, SeekOrigin.Begin);

		if (SkinFile.EndOfStream)
		{
			Debug.Log("Unable to load skin for model: "+ model.name);
			return false;
		}

		// These 2 variables are for reading in each line from the file, then storing
		// the index of where the bitmap name starts after the ',' character.
		string strLine;
		int textureNameStart = 0;

		// Go through every line in the .skin file
		while (!SkinFile.EndOfStream)
		{
			strLine = SkinFile.ReadLine();

			// Loop through all of our objects to test if their name is in this line
			for (int i = 0; i < model.meshes.Count; i++)
			{
				// Check if the name of this mesh appears in this line from the skin file
				if (IsInString(strLine, model.meshes[i].name))
				{
					// To abstract the texture name, we loop through the string, starting
					// at the end of it until we find a '/' character, then save that index + 1.
					for (int j = strLine.Length - 1; j > 0; j--)
					{
						// If this character is a ',', save the index + 1
						if (strLine[j] == ',')
						{
							// Save the index + 1 (the start of the texture name) and break
							textureNameStart = j + 1;
							break;
						}
					}
					string skin = strLine.Substring(textureNameStart);
					//Need to strip extension
					string[] fullName = skin.Split('.');

					//Check if skin texture exist, if not add it
					if (!TextureLoader.HasTexture(fullName[0]))
						TextureLoader.AddNewTexture(fullName[0], false);

					meshToSkin.Add(model.meshes[i].name, fullName[0]);
				}
			}
		}
		SkinFile.Close();
		return true;
	}

	private bool IsInString(string strString, string strSubString)
	{
		// Make sure both of these strings are valid, return false if any are empty
		if (string.IsNullOrEmpty(strString) || string.IsNullOrEmpty(strSubString))
			return false;

		// grab the starting index where the sub string is in the original string
		uint index = (uint)strString.IndexOf(strSubString);

		// Make sure the index returned was valid
		if (index >= 0 && index < strString.Length)
			return true;

		// The sub string does not exist in strString.
		return false;
	}

	public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
	{
		if (amount <= 0)
			return;

		hitpoints -= amount;

		if (Dead)
			Destroy(gameObject);
	}

	public void Impulse(Vector3 direction, float force)
	{

	}
}
