using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
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

	private ModelAnimation nextUpper;
	private ModelAnimation nextLower;

	private ModelAnimation currentUpper;
	private ModelAnimation currentLower;

	private int nextFrameUpper;
	private int nextFrameLower;

	private int currentFrameUpper;
	private int currentFrameLower;

	private bool loaded = false;
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

	GameObject upperBody;
	GameObject headBody;
	Transform playerTransform;
	Transform lowerTransform;
	Transform upperTransform;
	Transform headTransform;

	private float upperLerpTime = 0;
	private float upperCurrentLerpTime = 0;
	private float lowerLerpTime = 0;
	private float lowerCurrentLerpTime = 0;

	private void Update()
	{
		if (!loaded)
			return;

		if (GameManager.Paused)
			return;

		{
			nextUpper = upperAnim[(int)upperAnimation];
			nextLower = lowerAnim[(int)lowerAnimation];

			if (nextUpper.index == currentUpper.index)
			{
				nextFrameUpper = currentFrameUpper + 1;
				if (nextFrameUpper >= currentUpper.endFrame)
				{
					nextFrameUpper = currentUpper.startFrame;
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
						case (int)LowerAnimation.Jump:
							lowerAnimation = LowerAnimation.Land;
						break;
						case (int)LowerAnimation.JumpBack:
							lowerAnimation = LowerAnimation.LandBack;
						break;
						case (int)LowerAnimation.Land:
						case (int)LowerAnimation.LandBack:
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

			Vector3 upperTorsoOrigin = Vector3.Lerp(upper.tagsbyName["tag_torso"][currentFrameUpper].origin, upper.tagsbyName["tag_torso"][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 upperHeadOrigin = Vector3.Lerp(upper.tagsbyName["tag_head"][currentFrameUpper].origin, upper.tagsbyName["tag_head"][nextFrameUpper].origin, upperCurrentLerpTime);
			Vector3 lowerTorsoOrigin = Vector3.Lerp(lower.tagsbyName["tag_torso"][currentFrameLower].origin, lower.tagsbyName["tag_torso"][nextFrameLower].origin, lowerCurrentLerpTime);

			{
				Vector3 currentOffset = lowerTorsoRotation * upperTorsoOrigin;
				Quaternion currentRotation = lowerTorsoRotation * upperTorsoRotation;

				for (int i = 0; i < upper.meshes.Count; i++)
				{
					MD3Mesh currentMesh = upper.meshes[i];
					Vector3[] currentVect = currentMesh.verts[currentFrameUpper].ToArray();
					Vector3[] nextVect = currentMesh.verts[nextFrameUpper].ToArray();
					for (int j = 0; j < currentVect.Length; j++)
					{
						currentVect[j] = currentRotation * Vector3.Lerp(currentVect[j], nextVect[j], upperCurrentLerpTime);
						currentVect[j] += currentOffset;
					}

					upperModel.data[i].meshFilter.mesh.SetVertices(currentVect);
				}

				Quaternion baseRotation = lowerTorsoRotation;
				currentOffset = baseRotation * upperHeadOrigin;
				currentRotation = baseRotation * upperHeadRotation;

				headTransform.SetLocalPositionAndRotation(currentOffset, currentRotation);

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
					Vector3[] currentVect = currentMesh.verts[currentFrameLower].ToArray();
					Vector3[] nextVect = currentMesh.verts[nextFrameLower].ToArray();

					for (int j = 0; j < currentVect.Length; j++)
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

	public void TurnLegs(float sideMove, float forwardMove)
	{
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
		else
		{
			lowerAnimation = LowerAnimation.Run;
			if (sideMove > 0)
				rotate = Quaternion.AngleAxis(50f, playerTransform.up);
			else if (sideMove < 0)
				rotate = Quaternion.AngleAxis(-50f, playerTransform.up);
			else
				lowerAnimation = LowerAnimation.Idle;
		}
		lowerTransform.rotation = rotate;
	}

	public bool LoadPlayer(string modelName)
	{
		string playerModelPath = "players/" + modelName;

		string lowerModelName = playerModelPath + "/lower";
		string upperModelName = playerModelPath + "/upper";
		string headModelName = playerModelPath + "/head";
		string animationFile = playerModelPath + "/animation";

		string lowerSkin = playerModelPath + "/lower_default";
		string upperSkin = playerModelPath + "/upper_default";
		string headSkin = playerModelPath + "/head_default";

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
			headTransform = tag_head.transform;
			headTransform.SetParent(upperBody.transform);

			headBody = new GameObject();
			headBody.name = "Head";
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
}
