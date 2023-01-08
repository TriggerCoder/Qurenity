using UnityEngine;
using Assets.MultiAudioListener;

public class PlayAfterRandomTime : MonoBehaviour
{
	public int waitTime;
	public int randomTime;
	private float nextPlayTime;
	private AudioSource audioSource;
	private MultiAudioSource mAudioSource;
	AudioType audioType = AudioType.None;
	enum AudioType
	{
		None,
		Single,
		Multi
	}

	float time = 0f;
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
		if (audioType == AudioType.None)
			enabled = false;
	}

	public void Init(int wait, int random)
	{
		waitTime = wait;
		randomTime = random;
		nextPlayTime = Random.Range(waitTime - randomTime, waitTime + randomTime + 1);
	}

	void ResetTimer()
	{
		time = 0f;
		nextPlayTime = Random.Range(waitTime - randomTime, waitTime + randomTime + 1);
	}
	void Update()
    {
		if (GameManager.Paused)
			return;

		time += Time.deltaTime;

		if (time >= nextPlayTime)
		{
			switch (audioType)
			{
				default:
				break;
				case AudioType.Single:
					if(!audioSource.isPlaying)
					{
						ResetTimer();
						audioSource.Play();
					}
				break;
				case AudioType.Multi:
					if (!mAudioSource.IsPlaying)
					{
						ResetTimer();
						mAudioSource.Play();
					}
				break;
			}
		}
	}
}
