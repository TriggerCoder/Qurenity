using System;
using System.Collections.Generic;
using UnityEngine;

public class AdaptativeMusicManager : MonoBehaviour
{
	[Serializable]
	public struct AdaptativeTrack
	{
		public int intesityLevel;
		public bool isRepeatable;
		public AudioClip TrackFile;
		public bool hasOutro;
		public AudioClip OutroFile;
	}

	public AdaptativeTrack[] MainTracks = new AdaptativeTrack[0];
	public AdaptativeTrack[] BlendTracks = new AdaptativeTrack[0];

	public static Dictionary<int, List<AdaptativeTrack>> mainTracks = new Dictionary<int, List<AdaptativeTrack>>();
	public static Dictionary<int, List<AdaptativeTrack>> blendTracks = new Dictionary<int, List<AdaptativeTrack>>();

	public static AdaptativeMusicManager Instance;
	[HideInInspector]
	public AudioSource track01, track02;

	public AdaptativeTrack currentTrack;
	public int currentIntensity = 0;
	public int musicIntensity = 0;

	int maxIntensity = 0;
	bool isPlayingTack01 = true;
	bool crossFade = false;
	float targetVol;
	void Awake()
	{
		Instance = this;
		track01 = GetComponent<AudioSource>();
		track02 = gameObject.AddComponent<AudioSource>();
		track02.volume = track01.volume;

		foreach (AdaptativeTrack track in MainTracks)
		{
			if (mainTracks.ContainsKey(track.intesityLevel))
				mainTracks[track.intesityLevel].Add(track);
			else
			{
				List<AdaptativeTrack> list = new List<AdaptativeTrack>();
				list.Add(track);
				mainTracks.Add(track.intesityLevel, list);
				if (track.intesityLevel > maxIntensity)
					maxIntensity = track.intesityLevel;
			}
		}
		foreach (AdaptativeTrack track in BlendTracks)
		{
			if (blendTracks.ContainsKey(track.intesityLevel))
				blendTracks[track.intesityLevel].Add(track);
			else
			{
				List<AdaptativeTrack> list = new List<AdaptativeTrack>();
				list.Add(track);
				blendTracks.Add(track.intesityLevel, list);
			}
		}
	}

	void Start()
	{
		targetVol = GetCurrentVolume();
	}
	public void GetTrackOnCurrentIntensity(int intensity, bool crossFade = false, bool secondary  = false)
	{
		AdaptativeTrack track;
		if (secondary)
			track = blendTracks[intensity][UnityEngine.Random.Range(0, blendTracks[intensity].Count)];
		else
			track = mainTracks[intensity][UnityEngine.Random.Range(0, mainTracks[intensity].Count)];
		ChangeTrack(track.TrackFile, crossFade);
		currentTrack = track;
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (crossFade)
		{
			if (isPlayingTack01)
			{
				track01.volume = Mathf.Lerp(track01.volume, targetVol, 5 * Time.deltaTime);
				track02.volume = Mathf.Lerp(track02.volume, 0, 5 * Time.deltaTime);
				if (track02.volume < 0.001f)
				{
					track01.volume = targetVol;
					track02.volume = 0;
					crossFade = false;
				}
			}
			else
			{
				track02.volume = Mathf.Lerp(track02.volume, targetVol, 5 * Time.deltaTime);
				track01.volume = Mathf.Lerp(track01.volume, 0, 5 * Time.deltaTime);
				if (track01.volume < 0.001f)
				{
					track02.volume = targetVol;
					track01.volume = 0;
					crossFade = false;
				}
			}
		}
		else
		{
			if (isPlayingTack01)
			{
				if (track01.volume < targetVol)
				{
					track01.volume = Mathf.Lerp(track01.volume, targetVol, Time.deltaTime);
					if ((targetVol - track01.volume) < 0.001f)
					{
						track01.volume = targetVol;
						track02.volume = targetVol;
					}
				}
			}
			else
			{
				if (track02.volume < targetVol)
				{
					track02.volume = Mathf.Lerp(track02.volume, targetVol, Time.deltaTime);
					if ((targetVol - track02.volume) < 0.001f)
					{
						track01.volume = targetVol;
						track02.volume = targetVol;
					}
				}
			}
		}

		if ((!track01.isPlaying) && (!track02.isPlaying))
		{
			int newIntensity = currentIntensity + UnityEngine.Random.Range(-1, currentIntensity == maxIntensity? 1 : 2);
			if (newIntensity < 0)
				newIntensity = 0;
			else if (newIntensity > maxIntensity)
				newIntensity = maxIntensity;

			if ((newIntensity < currentIntensity) || ((newIntensity <= currentIntensity) && (currentIntensity < 2)))
			{
				currentIntensity = newIntensity;
				if (currentTrack.hasOutro)
				{
					ChangeTrack(currentTrack.OutroFile);
					GetTrackOnCurrentIntensity(currentIntensity, true, true);
				}
				else
				{
					newIntensity = UnityEngine.Random.Range(0, currentIntensity + 2);
					if (newIntensity <= currentIntensity)
						GetTrackOnCurrentIntensity(currentIntensity);
					else
					{
						newIntensity = UnityEngine.Random.Range(0, currentIntensity + 1);
						GetTrackOnCurrentIntensity(newIntensity, true);
					}
				}
			}
			else
			{
				currentIntensity = newIntensity;
				GetTrackOnCurrentIntensity(currentIntensity);
			}
		}
	}

	public float GetCurrentVolume()
	{
		return (.1f + (currentIntensity / 10f)); 
	}
	public void ChangeTrack(AudioClip newClip, bool fade = false)
	{
		if (isPlayingTack01)
		{
			track02.clip = newClip;
			if (fade)
			{
				crossFade = true;
				track02.volume = 0;
				track02.Play();
			}
			else
			{
				if (track02.volume == 0)
					track02.volume = track01.volume;
				track02.Play();
				track01.Stop();
			}
		}
		else
		{
			track01.clip = newClip;
			if (fade)
			{
				crossFade = true;
				track01.volume = 0;
				track01.Play();
			}
			else
			{
				if (track01.volume == 0)
					track01.volume = track02.volume;
				track01.Play();
				track02.Stop();
			}
		}
		targetVol = GetCurrentVolume();
		isPlayingTack01 = !isPlayingTack01;
	}
}
