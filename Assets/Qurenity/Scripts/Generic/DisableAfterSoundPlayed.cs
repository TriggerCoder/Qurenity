using UnityEngine;
using Assets.MultiAudioListener;
public class DisableAfterSoundPlayed : MonoBehaviour
{
	public MultiAudioSource audioSource;
	bool soundStarted = false;
	void Awake()
	{
		audioSource = GetComponent<MultiAudioSource>();
		soundStarted = false;
	}
	void Update()
	{
		if (soundStarted)
		{
			if (!audioSource.IsPlaying)
			{
				soundStarted = false;
				gameObject.SetActive(false);
				gameObject.transform.SetParent(GameManager.Instance.BaseThingsHolder);

			}
		}
		else
		{
			if (audioSource.IsPlaying)
				soundStarted = true;
		}
	}
}
