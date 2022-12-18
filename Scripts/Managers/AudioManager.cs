using UnityEngine;
using Assets.MultiAudioListener;

public static class AudioManager
{
	public static float MainVolume { get { return GameOptions.MainVolume; } set { if ((value >= 0f) && (value <= 1.0f)) if (GameOptions.MainVolume != value) { GameOptions.MainVolume = value; ChangeBGMVolume(); ChangeSFXVolume(); } } }
	public static float BGMVolume { get { return GameOptions.BGMVolume; } set { if ((value >= 0f) && (value <= 1.0f)) if (GameOptions.BGMVolume != value) { GameOptions.BGMVolume = value; ChangeBGMVolume(); } } }
	public static float SFXVolume { get { return GameOptions.SFXVolume; } set { if ((value >= 0f) && (value <= 1.0f)) if (GameOptions.SFXVolume != value) { GameOptions.SFXVolume = value; ChangeSFXVolume(); } } }

	public static void Create2DSound(string soundName)
	{
		Create2DSound(SoundLoader.LoadSound(soundName), 1);
	}
	public static void Create2DSound(AudioClip clip, float minDistance)
	{
		GameObject sound = new GameObject("2dsound");
		sound.transform.position = GameManager.Instance.Player[0].transform.position;
		sound.transform.SetParent(GameManager.Instance.Player[0].transform);

		AudioSource audioSource = sound.AddComponent<AudioSource>();

		audioSource.clip = clip;
		audioSource.loop = false;
		audioSource.spatialBlend = 0;
		audioSource.minDistance = minDistance;
		audioSource.volume = GameOptions.MainVolume * GameOptions.SFXVolume;
		audioSource.Play();

		sound.AddComponent<DestroyAfterSoundPlayed>();
	}

	public static void Create3DSound(Vector3 position, string soundName, float minDistance)
	{
		Create3DSound(position, SoundLoader.LoadSound(soundName), minDistance);
	}

	public static void Create3DSound(Vector3 position, AudioClip clip, float minDistance, float Sound3D = 1f)
	{
		//Check 2D Sounds
		if (Sound3D == 0)
		{
			Create2DSound(clip, minDistance);
			return;
		}

		PoolObject<MultiAudioSource> poolObjectAudio = PoolManager.Get3DSoundFromPool("3DSound");
		GameObject sound = poolObjectAudio.go;
		sound.transform.position = position;
		MultiAudioSource audioSource = (MultiAudioSource)poolObjectAudio.data;

		audioSource.AudioClip = clip;
		audioSource.Loop = false;
		audioSource.MinDistance = minDistance;
		audioSource.Stop();
		audioSource.Play();
	}
	public static void ChangeBGMVolume()
	{
		AudioSource bgmMusic = GameManager.Instance.GetComponent<AudioSource>();
		if (bgmMusic != null)
			bgmMusic.volume = GameOptions.MainVolume * GameOptions.BGMVolume;
	}
	public static void ChangeSFXVolume()
	{
		foreach (PlayerInfo player in GameManager.Instance.Player)
		{
			VirtualMultiAudioListener audioListener = player.gameObject.GetComponentInChildren<VirtualMultiAudioListener>();
			if (audioListener != null)
				audioListener.Volume = GameOptions.MainVolume * GameOptions.SFXVolume;
		}
	}
}
