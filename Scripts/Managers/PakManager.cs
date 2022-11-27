using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
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
		FileInfo[] info = dir.GetFiles("*.PK3");
		foreach (FileInfo zipfile in info)
		{
			string FileName = path + zipfile.Name;
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			foreach (ZipEntry e in zip)
			{
				//Only Files
				if (e.FileName.Contains("."))
					ZipFiles.Add(e.FileName.ToUpper(), FileName);
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
