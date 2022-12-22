using System.IO;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class PlayerModel
{
	string lowerModel;              // This stores the file name for the lower.md3 model
	string upperModel;				// This stores the file name for the upper.md3 model
	string headModel;				// This stores the file name for the head.md3 model
	string lowerSkin;				// This stores the file name for the lower.md3 skin
	string upperSkin;				// This stores the file name for the upper.md3 skin
	string headSkin;                // This stores the file name for the head.md3 skin
	MD3 head;
	MD3 upper;
	MD3 lower;
	MD3 weapon;

	private Dictionary<string, string> meshToSkin = new Dictionary<string, string>();

	public bool LoadPlayer(string modelName)
	{
		string playerModelPath = "players/" + modelName;

		lowerModel = playerModelPath + "/lower";
		upperModel = playerModelPath + "/upper";
		headModel = playerModelPath + "/head";

		lowerSkin = playerModelPath + "/lower_default";
		upperSkin = playerModelPath + "/upper_default";
		headSkin = playerModelPath + "/head_default";

		lower = ModelsManager.GetModel(lowerModel);
		if (lower == null)
			return false;
		upper = ModelsManager.GetModel(upperModel);
		if (upper == null)
			return false;

		head = ModelsManager.GetModel(headModel);
		if (head == null)
			return false;

		LoadSkin(lower, lowerSkin);
		LoadSkin(upper, upperSkin);
		LoadSkin(head, headSkin);

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
					meshToSkin.Add(model.meshes[i].name, fullName[0]);
				}
			}
		}
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
