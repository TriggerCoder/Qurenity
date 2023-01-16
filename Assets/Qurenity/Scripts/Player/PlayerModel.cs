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

	public bool enableOffset { get { return _enableOffset; } set { _enableOffset = value; } }

	private bool _enableOffset = true;

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
	private bool ownerDead = false;
	public class ModelAnimation
	{
		public int index;
		public int startFrame;
		public int endFrame;
		public int loopingFrames;
		public int fps;
		public string strName;
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
		Back,
		Swim,
		Jump,
		Land,
		JumpBack,
		LandBack,
		Idle,
		IdleCR,
		Turn
	}

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
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return true; } }
	public BloodType BloodColor { get { return BloodType.Red; } }

	List<Vector3> currentVect = new List<Vector3>();
	List<Vector3> nextVect = new List<Vector3>();
	private void Update()
	{
		if (!loaded)
			return;

		if (GameManager.Paused)
			return;

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
					switch (nextUpper.index)
					{
						default:
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = currentUpper.startFrame;
						break;
						case (int)UpperAnimation.Death3:
							upperAnimation = UpperAnimation.Dead3;
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = currentUpper.startFrame;
							ChangeToRagDoll();
							return;
						break;
						case (int)UpperAnimation.Attack:
						case (int)UpperAnimation.Raise:
							upperAnimation = UpperAnimation.Stand;
							nextUpper = upperAnim[(int)upperAnimation];
							nextFrameUpper = currentUpper.startFrame;
						break;
						case (int)UpperAnimation.Drop:
							nextFrameUpper = currentUpper.endFrame;
						break;
					}
				}
			}
			else
				nextFrameUpper = nextUpper.startFrame;

			if (nextLower.index == currentLower.index)
			{
				nextFrameLower = currentFrameLower + 1;
				if (nextFrameLower >= currentLower.endFrame)
				{
					switch (nextLower.index)
					{
						default:
							
						break;
						case (int)LowerAnimation.Death3:
							lowerAnimation = LowerAnimation.Dead3;
						break;
						case (int)LowerAnimation.Jump:
							lowerAnimation = LowerAnimation.Land;
						break;
						case (int)LowerAnimation.JumpBack:
							lowerAnimation = LowerAnimation.LandBack;
						break;
						case (int)LowerAnimation.Turn:
						case (int)LowerAnimation.Land:
						case (int)LowerAnimation.LandBack:
							if (turnTo.sqrMagnitude > 0)
							{
								playerTransform.forward = turnTo;
								turnTo = Vector3.zero;
							}
							lowerAnimation = LowerAnimation.Idle;
							_enableOffset = true;
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
				/*
				 * Multiplying a vector by a Quaternion, rotates the vector by eulerangles
				 */
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
				}

				Quaternion baseRotation = lowerTorsoRotation;
				currentOffset = baseRotation * upperHeadOrigin;
				currentRotation = baseRotation * upperHeadRotation;

				tagHeadTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);

				currentOffset = baseRotation * weaponOrigin;
				currentRotation = baseRotation * weaponRotation;

				weaponTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);

				if (_enableOffset)
					playerTransform.localPosition = lowerTorsoOrigin;
				else
					playerTransform.localPosition = Vector3.zero;

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

		float vView = viewDirection.x;
		float hView = viewDirection.y;
/*
		int vAngle = (int)Mathf.Round((vView) / (360) * 16) % 16;
		int hAngle = (int)Mathf.Round((hView + 90) / (360) * 16) % 16;
		headTransform.rotation = Quaternion.Slerp(headTransform.rotation, Quaternion.Euler(0, 22.5f * hAngle, 20 * vAngle), rotationFPS * deltaTime);
*/
		headTransform.rotation = Quaternion.Slerp(headTransform.rotation, Quaternion.Euler(0, hView + 90, vView), rotationFPS * deltaTime);

		int vAngle = (int)Mathf.Round((vView) / (360) * 32) % 32;
		int hAngle = (int)Mathf.Round((hView + 90) / (360) * 32) % 32;

//		upperTransform.rotation = Quaternion.Slerp(upperTransform.rotation, Quaternion.Euler(0, hView, .7f * vView), rotationFPS * deltaTime);
		upperTransform.rotation = Quaternion.Slerp(upperTransform.rotation, Quaternion.Euler(0, 11.25f * hAngle, 7.5f * vAngle), rotationFPS * deltaTime);
	}

	public void CheckLegTurn(Vector3 direction)
	{
		if (ownerDead)
			return;

		Vector3 forward = playerTransform.forward;
		int angle = (int)Mathf.Round((Mathf.Atan2(direction.x, direction.z)) / (Mathf.PI * 2) * 8) % 8;

		//Player Models are rotated 90�
		angle += 2;
		direction = Quaternion.Euler(0f, angle * 45f, 0f) * Vector3.forward;

		angle = (int)Mathf.Round(((Mathf.Atan2((forward.z * direction.x) - (direction.z * forward.x), (forward.x * direction.x) + (forward.z * direction.z)))) / (Mathf.PI * 2) * 8) % 8;

		if (angle != 0)
		{
			turnTo = direction;
			if (_enableOffset)
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
		playerTransform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
		playerTransform.SetPositionAndRotation(currentPosition, currentRotation);
		gameObject.layer = GameManager.DefaultLayer;

		var transformChildren = playerTransform.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (var child in transformChildren)
			child.gameObject.layer = GameManager.DefaultLayer;

		var meshFilterChildren = playerTransform.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
		for (var i = 0; i < combine.Length; i++)
			combine[i].mesh = meshFilterChildren[i].mesh;

		var mesh = new Mesh();
		mesh.CombineMeshes(combine, true, false, false);

		MeshCollider mc = playerTransform.gameObject.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;
		mc.convex = true;
		rb = playerTransform.gameObject.AddComponent<Rigidbody>();
		rb.useGravity = false;
		rb.isKinematic = true;

		loaded = false;
	}

	public void Die()
	{
		upperAnimation = UpperAnimation.Death3;
		lowerAnimation = LowerAnimation.Death3;

		ownerDead = true;
	}
	public void TurnLegsOnJump(float sideMove)
	{
		Quaternion rotate = Quaternion.identity;

		if (lowerAnimation != LowerAnimation.Idle)
			return;

		if (sideMove > 0)
			rotate = Quaternion.AngleAxis(30f, playerTransform.up);
		else if (sideMove < 0)
			rotate = Quaternion.AngleAxis(-30f, playerTransform.up);

		lowerTransform.localRotation = rotate;
	}
	public void TurnLegs(float sideMove, float forwardMove)
	{
		if (ownerDead)
			return;

		Quaternion rotate = Quaternion.identity;
		if (forwardMove > 0)
		{
			lowerAnimation = LowerAnimation.Run;
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(30f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(-30f, playerTransform.up);
		}
		else if (forwardMove < 0)
		{
			lowerAnimation = LowerAnimation.Back;
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(-30f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(30f, playerTransform.up);
		}
		else if (sideMove != 0)
		{
			lowerAnimation = LowerAnimation.Run;
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(50f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(-50f, playerTransform.up);
		}
		// else if (lowerAnimation != LowerAnimation.Turn)
		// 	lowerAnimation = LowerAnimation.Idle;

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

	public void LoadWeapon(MD3 newWeapon, string completeModelName, string muzzleModelName)
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
			Vector3 OffSet = Vector3.zero;
			List<MD3Tag> weaponTags;
			muzzleFlash = new GameObject("muzzle_flash");
			MD3 muzzle = ModelsManager.GetModel(muzzleModelName, true);

			if (muzzle == null)
				return;

			if (muzzle.readyMeshes.Count == 0)
				Mesher.GenerateModelFromMeshes(muzzle, muzzleFlash, true);
			else
				Mesher.FillModelFromProcessedData(muzzle, muzzleFlash);
			muzzleFlash.layer = weaponModel.go.layer;

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
	public bool LoadPlayer(string modelName, string SkinName = "default")
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
			upperBody.transform.SetParent(playerModel.transform);

			GameObject tag_head = new GameObject("tag_head");
			tagHeadTransform = tag_head.transform;
			tagHeadTransform.SetParent(upperBody.transform);

			GameObject tag_weapon = new GameObject("tag_weapon");
			weaponTransform = tag_weapon.transform;
			weaponTransform.SetParent(upperBody.transform);

			headBody = new GameObject("Head");
			headTransform = headBody.transform;
			headBody.transform.SetParent(tag_head.transform);

			if (upper.readyMeshes.Count == 0)
				upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin);
			else
				upperModel = Mesher.FillModelFromProcessedData(upper, meshToSkin);
			upperModel.go.name = "upper_body";
			upperModel.go.transform.SetParent(upperBody.transform);

			if (head.readyMeshes.Count == 0)
				headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin);
			else
				headModel = Mesher.FillModelFromProcessedData(head, meshToSkin);

			headModel.go.name = "head";
			headModel.go.transform.SetParent(headBody.transform);

			if (lower.readyMeshes.Count == 0)
				lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin);
			else
				lowerModel = Mesher.FillModelFromProcessedData(lower, meshToSkin);
			lowerModel.go.name = "lower_body";
			lowerTransform = lowerModel.go.transform;
			lowerModel.go.transform.SetParent(playerModel.transform);

			loaded = true;
		}
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
		ModelAnimation[] animations = new ModelAnimation[25];

		if (animFile.EndOfStream)
		{
			return false;
		}

		string strWord;
		int currentAnim = 0;
		int torsoOffset = 0;
		int legsOffset = 7;
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
					torsoOffset = animations[13].startFrame - animations[6].startFrame;

				animations[currentAnim].startFrame -= torsoOffset;
				animations[currentAnim].endFrame -= torsoOffset;
				animations[currentAnim].index -= legsOffset;
				lower.Add(animations[currentAnim]);
			}
			currentAnim++;
		}

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
