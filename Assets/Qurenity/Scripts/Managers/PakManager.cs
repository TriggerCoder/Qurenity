using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Pathfinding.Ionic.Zip;

public class PakManager : MonoBehaviour
{
	public static PakManager Instance;
	public static Dictionary<string, string> ZipFiles = new Dictionary<string, string>();
	void Awake()
	{
		Instance = this;
		LoadPK3Files();
	}

	public static void LoadPK3Files()
	{
		string path = Application.streamingAssetsPath + "/";

		DirectoryInfo dir = new DirectoryInfo(path);
		var info = dir.GetFiles("*.PK3").OrderBy(file =>	Regex.Replace(file.Name, @"\d+", match => match.Value.PadLeft(4, '0')));
		foreach (FileInfo zipfile in info)
		{
			string FileName = path + zipfile.Name;
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			foreach (ZipEntry e in zip)
			{
				//Only Files
				if (e.FileName.Contains("."))
				{
					string logName = e.FileName.ToUpper();
					if (ZipFiles.ContainsKey(logName))
					{
						Debug.Log("Updating pak file with name " + logName);
						ZipFiles[logName] = FileName;
					}
					else
						ZipFiles.Add(logName, FileName);
				}
			}
			zip.Dispose();
			stream.Close();
		}
	}
	public static byte[] ZipToByteArray(string name, ref ZipFile zip)
	{
		MemoryStream stream = new MemoryStream();
		if (zip[name] == null)
		{
			zip.Dispose();
			stream.Close();
			return null;
		}

		zip[name].Extract(stream);
		byte[] bytes = stream.GetBuffer();
		zip.Dispose();
		stream.Close();
		return bytes;
	}
}
