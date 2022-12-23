using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class PlayerModel
{
	string lowerModelName;			// This stores the file name for the lower.md3 model
	string upperModelName;			// This stores the file name for the upper.md3 model
	string headModelName;			// This stores the file name for the head.md3 model
	string lowerSkin;				// This stores the file name for the lower.md3 skin
	string upperSkin;				// This stores the file name for the upper.md3 skin
	string headSkin;                // This stores the file name for the head.md3 skin
	public MD3 head;
	public MD3 upper;
	public MD3 lower;
	public MD3 weapon;

	public List<ModelAnimation> upperAnim = new List<ModelAnimation>();
	public List<ModelAnimation> lowerAnim = new List<ModelAnimation>();

	private Dictionary<string, string> meshToSkin = new Dictionary<string, string>();
	private Dictionary<string, MD3> tagToModel = new Dictionary<string, MD3>();

	MD3UnityConverted lowerModel;
	MD3UnityConverted upperModel;
	MD3UnityConverted headModel;

	public class ModelAnimation
	{
		public int startFrame;
		public int endFrame;
		public int loopingFrames;
		public int fps;
		public string strName;
	}

	public bool LoadPlayer(string modelName)
	{
		string playerModelPath = "players/" + modelName;

		lowerModelName = playerModelPath + "/lower";
		upperModelName = playerModelPath + "/upper";
		headModelName = playerModelPath + "/head";
		string animationFile = playerModelPath + "/animation";

		lowerSkin = playerModelPath + "/lower_default";
		upperSkin = playerModelPath + "/upper_default";
		headSkin = playerModelPath + "/head_default";

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

		LinkModel(lower, upper, "tag_torso");
		LinkModel(upper, head, "tag_head");

		LoadAnimations(animationFile, upperAnim, lowerAnim);
		
		{
			GameObject playerModel = new GameObject(modelName);
			if (upper.readyMeshes.Count == 0)
				upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin);
			else
				upperModel = Mesher.FillModelFromProcessedData(upper);
			upperModel.go.name = "upper_body";
			upperModel.go.transform.SetParent(playerModel.transform);

			for (int i = 0; i < upper.meshes.Count; i++)
			{
				MD3Mesh currentMesh = upper.meshes[i];
				List<Vector3> currentVect = currentMesh.verts[152];
				upperModel.data[i].meshFilter.mesh.SetVertices(currentVect);
			}


			if (head.readyMeshes.Count == 0)
				headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin);
			else
				headModel = Mesher.FillModelFromProcessedData(head);

			headModel.go.name = "head";
			headModel.go.transform.SetParent(playerModel.transform);

			Vector3 currentOffset = upper.tagsbyName["tag_head"][152].origin;
			Quaternion currentRotation = upper.tagsbyName["tag_head"][152].rotation;

			for (int i = 0; i < head.meshes.Count; i++)
			{
				MD3Mesh currentMesh = head.meshes[i];
				List<Vector3> currentVect = currentMesh.verts[0];
				for (int j = 0; j < currentVect.Count; j++)
				{
					currentVect[j] =  currentRotation * currentVect[j];
					currentVect[j] += currentOffset;

				}
				headModel.data[i].meshFilter.mesh.SetVertices(currentVect);
			}


			if (lower.readyMeshes.Count == 0)
				lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin);
			else
				lowerModel = Mesher.FillModelFromProcessedData(lower);
			lowerModel.go.name = "lower_body";
			lowerModel.go.transform.SetParent(playerModel.transform);

			currentOffset = upper.tagsbyName["tag_torso"][152].origin;
			currentRotation = upper.tagsbyName["tag_torso"][152].rotation;

			currentOffset -= lower.tagsbyName["tag_torso"][166].origin;
			currentRotation *= lower.tagsbyName["tag_torso"][166].rotation;

			for (int i = 0; i < lower.meshes.Count; i++)
			{
				MD3Mesh currentMesh = lower.meshes[i];
				List<Vector3> currentVect = currentMesh.verts[166];
				for (int j = 0; j < currentVect.Count; j++)
				{
					currentVect[j] = currentRotation * currentVect[j];
					currentVect[j] += currentOffset;

				}
				lowerModel.data[i].meshFilter.mesh.SetVertices(currentVect);
			}
		}

		/*		{
					GameObject playerModel = new GameObject(modelName);
					GameObject tag_upper_torso = new GameObject("tag_upper_torso");
					GameObject tag_lower_torso = new GameObject("tag_lower_torso");
					GameObject tag_head = new GameObject("tag_head");

					if (upper.readyMeshes.Count == 0)
						upperModel = Mesher.GenerateModelFromMeshes(upper, meshToSkin);
					else
						upperModel = Mesher.FillModelFromProcessedData(upper);

					upperModel.go.name = "upper_body";
					upperModel.go.transform.SetParent(playerModel.transform);
					tag_upper_torso.transform.SetParent(upperModel.go.transform);
					tag_head.transform.SetParent(upperModel.go.transform);

					tag_upper_torso.transform.localPosition = upper.tagsbyName["tag_torso"][0].origin;
					tag_upper_torso.transform.localRotation = upper.tagsbyName["tag_torso"][0].rotation;

					tag_head.transform.localPosition = upper.tagsbyName["tag_head"][0].origin;
					tag_head.transform.localRotation = upper.tagsbyName["tag_head"][0].rotation;

					if (lower.readyMeshes.Count == 0)
						lowerModel = Mesher.GenerateModelFromMeshes(lower, meshToSkin);
					else
						lowerModel = Mesher.FillModelFromProcessedData(lower);

					lowerModel.go.name = "lower_body";
		//			lowerModel.go.transform.SetParent(tag_upper_torso.transform);
		//			lowerModel.go.transform.SetLocalPositionAndRotation(lower.tagsbyName["tag_torso"][0].origin, lower.tagsbyName["tag_torso"][0].rotation);
					lowerModel.go.transform.localScale = Vector3.one;
		//			tag_lower_torso.transform.SetParent(lowerModel.go.transform);

					tag_lower_torso.transform.localPosition = lower.tagsbyName["tag_torso"][0].origin;
					tag_lower_torso.transform.localRotation = lower.tagsbyName["tag_torso"][0].rotation;


					lowerModel.go.transform.position = tag_lower_torso.transform.InverseTransformPoint(tag_lower_torso.transform.position);

					if (head.readyMeshes.Count == 0)
						headModel = Mesher.GenerateModelFromMeshes(head, meshToSkin);
					else
						headModel = Mesher.FillModelFromProcessedData(head);

					headModel.go.name = "head";
					headModel.go.transform.SetParent(tag_head.transform);
					headModel.go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					headModel.go.transform.localScale = Vector3.one;

				}
				*/
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

			animations[currentAnim] = new ModelAnimation();
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


				lower.Add(animations[currentAnim]);
			}
			currentAnim++;
		}

		animFile.Close();
		return true;
	}
	public bool LinkModel(MD3 model, MD3 link, string linkTag)
	{
		if ((model == null) || (link == null) || (string.IsNullOrEmpty(linkTag)))
		{
			Debug.Log("Invalid data for linking");
			return false;
		}

		// Go through all of our tags and find which tag contains the linkTag, then link'em
		for (int i = 0; i < model.numTags; i++)
		{
			// If this current tag index has the tag name we are looking for
			if (string.Equals(model.tags[i].name, linkTag, StringComparison.OrdinalIgnoreCase))
			{
				tagToModel.Add(linkTag, link);
				return true;
			}
		}
		return false;
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
