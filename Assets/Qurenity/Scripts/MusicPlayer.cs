using UnityEngine;
using System;

public class MusicPlayer : MonoBehaviour
{
	[Serializable]
	public struct LevelMusic
	{
		public string LevelName;
		public AudioClip MusicFile;
	}

	public LevelMusic[] levelMusics = new LevelMusic[0];

	public static MusicPlayer Instance;
	[HideInInspector]
	public AudioSource audioSource;

	void Awake()
	{
		Instance = this;
		audioSource = GetComponent<AudioSource>();
	}

	public bool Play(string levelName)
	{
		audioSource.Stop();

		foreach (LevelMusic music in levelMusics)
		{
			if (music.LevelName == levelName)
			{
				audioSource.clip = music.MusicFile;
				audioSource.volume = 0.2f;
				audioSource.Play();
				return true;
			}
		}
		return false;
	}
}
