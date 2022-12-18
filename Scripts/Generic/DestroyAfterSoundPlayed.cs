using UnityEngine;
using Assets.MultiAudioListener;
public class DestroyAfterSoundPlayed : MonoBehaviour
{
	public AudioSource audioSource;
	public MultiAudioSource mAudioSource;
	AudioType audioType = AudioType.None;
	enum AudioType
	{
		None,
		Single,
		Multi
	}
	void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		if (audioSource != null)
		{
			audioType = AudioType.Single;
			return;
		}
		mAudioSource = GetComponent<MultiAudioSource>();
		if (mAudioSource != null)
		{
			audioType = AudioType.Multi;
			return;
		}
	}

	void CheckAudioPlay()
	{
		switch (audioType)
		{
			default:
			case AudioType.None:
				Destroy(gameObject);
			break;
			case AudioType.Single:
				if (!audioSource.isPlaying)
					Destroy(gameObject);
			break;
			case AudioType.Multi:
				if (!mAudioSource.IsPlaying)
					Destroy(gameObject);
			break;
		}
	}

	void Update()
	{
		CheckAudioPlay();
	}
}
