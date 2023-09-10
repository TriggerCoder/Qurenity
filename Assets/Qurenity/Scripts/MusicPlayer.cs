using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
	[Serializable]
	public struct LevelMusic
	{
		public string LevelName;
		public AudioClip MusicFile;
	}

	public LevelMusic[] LevelMusics = new LevelMusic[0];
	public static Dictionary<string, AudioClip> levelMusics = new Dictionary<string, AudioClip>();

	public static MusicPlayer Instance;
	[HideInInspector]
	public AudioSource audioSource;

	void Awake()
	{
		Instance = this;
		audioSource = GetComponent<AudioSource>();
		foreach (LevelMusic music in LevelMusics)
			levelMusics.Add(music.LevelName, music.MusicFile);
	}

	public bool Play(string levelName)
	{
		audioSource.Stop();
		if (levelMusics.TryGetValue(levelName, out AudioClip audioClip))
		{
			audioSource.clip = audioClip;
			audioSource.volume = 0.2f;
			audioSource.Play();
			return true;
		}
		return false;
	}
}
