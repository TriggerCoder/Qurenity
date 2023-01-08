using System;
using System.IO;
using System.Collections.Generic;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class SoundLoader : MonoBehaviour
{
	public static SoundLoader Instance;
	public static Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();
	public static Dictionary<string, AudioClip> OverrideSounds = new Dictionary<string, AudioClip>();
	public SoundOverride[] _OverrideSounds = new SoundOverride[0];

	[System.Serializable]
	public struct SoundOverride
	{
		public string SoundName;
		public AudioClip Sound;
	}

	void Awake()
	{
		Instance = this;

		foreach (SoundOverride so in _OverrideSounds)
			Sounds.Add(so.SoundName, so.Sound);
	}

	public static AudioClip LoadSound(string soundName)
	{
		if (OverrideSounds.ContainsKey(soundName))
			return OverrideSounds[soundName];

		if (Sounds.ContainsKey(soundName))
			return Sounds[soundName];

		byte[] WavSoudFile;
		string path = Application.streamingAssetsPath + "/sound/" + soundName + ".wav";
		if (File.Exists(path))
			WavSoudFile = File.ReadAllBytes(path);
		else if (PakManager.ZipFiles.ContainsKey(path = ("sound/" + soundName + ".wav").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			WavSoudFile = PakManager.ZipToByteArray(path, ref zip);
		}
		else
			return null;

		string[] soundFileName = path.Split('/');
		AudioClip clip = ToAudioClip(WavSoudFile, 0, soundFileName[soundFileName.Length - 1]);

		if (clip == null)
			return null;

		Sounds.Add(soundName, clip);
		if (FindHDSound(soundName))
			return OverrideSounds[soundName];
		return clip;
	}
	private static bool FindHDSound(string soundName)
	{
		AudioClip Sound = Resources.Load<AudioClip>("sounds/" + soundName);
		if (Sound == null)
			return false;
		if (Sound != null)
			OverrideSounds.Add(soundName, Sound);
		return true;
	}
	private static AudioClip ToAudioClip(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
	{
		int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
		ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);

		string formatCode = FormatCode(audioFormat);
		if ((audioFormat != 1) && (audioFormat != 65534))
		{
			Debug.LogWarning("Detected format code '" + audioFormat + "' " + formatCode + ", but only PCM and WaveFormatExtensable uncompressed formats are currently supported.");
			return null;
		}

		ushort channels = BitConverter.ToUInt16(fileBytes, 22);
		int sampleRate = BitConverter.ToInt32(fileBytes, 24);
		ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

		int headerOffset = 16 + 4 + subchunk1 + 4;
		int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);

		float[] data;
		switch (bitDepth)
		{
			case 8:
				data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 16:
				data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 24:
				data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			case 32:
				data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
				break;
			default:
				Debug.LogWarning(bitDepth + " bit depth is not supported.");
				return null;
		}

		if (data == null)
			return null;

		AudioClip audioClip = AudioClip.Create(name, data.Length, channels, sampleRate, false);
		audioClip.SetData(data, 0);
		return audioClip;
	}

	private static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			Debug.LogWarning("Failed to get valid 8-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		float[] data = new float[wavSize];
		sbyte maxValue = sbyte.MaxValue;
		sbyte minValue = sbyte.MinValue;

		int i = 0;
		while (i < wavSize)
		{
			data[i] = (source[i + headerOffset] + minValue) / (float)maxValue;
			++i;
		}

		return data;
	}

	private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			Debug.LogWarning("Failed to get valid 16-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		int x = sizeof(ushort); // block size = 2
		int convertedSize = wavSize / x;

		float[] data = new float[convertedSize];

		ushort maxValue = ushort.MaxValue;

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
			++i;
		}

		return data;
	}
	private static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			Debug.LogWarning("Failed to get valid 24-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}
		
		int x = 3; // block size = 3
		int convertedSize = wavSize / x;

		int maxValue = Int32.MaxValue;

		float[] data = new float[convertedSize];

		byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			Buffer.BlockCopy(source, offset, block, 1, x);
			data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
			++i;
		}

		return data;
	}
	private static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
	{
		int wavSize = BitConverter.ToInt32(source, headerOffset);
		headerOffset += sizeof(int);

		if ((wavSize <= 0) || (wavSize != dataSize))
		{
			Debug.LogWarning("Failed to get valid 32-bit wav size: " + wavSize + " from data bytes: " + dataSize + " at offset: " + headerOffset);
			return null;
		}

		int x = sizeof(float); //  block size = 4
		int convertedSize = wavSize / x;

		uint maxValue = uint.MaxValue;

		float[] data = new float[convertedSize];

		int offset = 0;
		int i = 0;
		while (i < convertedSize)
		{
			offset = i * x + headerOffset;
			data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
			++i;
		}

		return data;
	}
	private static string FormatCode(ushort code)
	{
		switch (code)
		{
			case 1:
				return "PCM";
			case 2:
				return "ADPCM";
			case 3:
				return "IEEE";
			case 7:
				return "μ-law";
			case 65534:
				return "WaveFormatExtensable";
			default:
				Debug.LogWarning("Unknown wav code format:" + code);
				return "";
		}
	}
}

