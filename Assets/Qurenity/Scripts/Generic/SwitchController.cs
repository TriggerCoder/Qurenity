using System.Collections;
using System.Collections.Generic;
using Assets.MultiAudioListener;
using UnityEngine;
public class SwitchController : TriggerController, Damageable
{
	public string activateSound;
	public int hitpoints = 0;
	public float Lip = 4 * GameManager.sizeDividor;
	public TriggerController tc;
	private Bounds bounds;
	private float speed = 40 * GameManager.sizeDividor;

	public float openWaitTime = 0;
	private Vector3 openPosition, closedPosition;
	private Vector3 dirVector = Vector3.forward;
	public float Speed { get { return speed; } set { speed = value * GameManager.sizeDividor; } }
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return false; } }
	public BloodType BloodColor { get { return BloodType.None; } }

	Transform cTransform;
	MultiAudioSource audioSource;
	private float openSqrMagnitude;

	[System.Serializable]
	public enum State
	{
		None,
		Closed,
		Closing,
		Open,
		Opening
	}

	public State currentState = State.Closed;
	public void SetInitialState(State initial)
	{
		if (currentState == State.None)
		{
			switch (initial)
			{
				default:
					Debug.LogWarning("Initial DoorState must be only Open/Closed");
				break;
				case State.Open:
					currentState = initial;
				break;
				case State.Closed:
					currentState = initial;
				break;
			}
		}
	}
	
	public State CurrentState
	{
		get { return currentState; }
		set
		{
			if (value == State.Open)
				openWaitTime = AutoReturnTime;
			if (value == State.Opening)
			{
				audioSource.Play();
				enabled = true;
			}
			else if (value == State.Closing)
				enabled = true;
			else if (value == State.Closed)
			{
				activated = false;
				enabled = false;
			}
			currentState = value;
		}
	}
	public void Init(int angle, Bounds swBounds)
	{
		cTransform = transform;

		if (angle != 0)
			SetAngle(angle);
		SetBounds(swBounds);

		audioSource = GetComponentInChildren<MultiAudioSource>();
		if (audioSource == null)
		{
			GameObject audioPosition = new GameObject("Audio Position");
			audioPosition.transform.position = bounds.center;
			audioPosition.transform.SetParent(transform, true);
			audioSource = audioPosition.AddComponent<MultiAudioSource>();
			audioSource.PlayOnAwake = false;
		}
		audioSource.AudioClip = SoundLoader.LoadSound(activateSound);
	}
	void Update()
	{
		if (GameManager.Paused)
			return;
	}

	void FixedUpdate()
	{
		if (GameManager.Paused)
			return;

		switch (CurrentState)
		{
			default:
				break;

			case State.Open:
				if (openWaitTime > 0)
				{
					openWaitTime -= Time.fixedDeltaTime;
					if (openWaitTime <= 0)
						CurrentState = State.Closing;
				}
				break;

			case State.Closing:
			{
				float newDistance = Time.fixedDeltaTime * speed;
				Vector3 newPosition = cTransform.position - dirVector * newDistance;
				float sqrMagnitude = (openPosition - newPosition).sqrMagnitude;
				if (sqrMagnitude > openSqrMagnitude)
				{
					newPosition = closedPosition;
					CurrentState = State.Closed;
				}
				transform.position = newPosition;
			}
			break;
			case State.Closed:
			break;

			case State.Opening:
			{
				float newDistance = Time.fixedDeltaTime * speed;
				Vector3 newPosition = cTransform.position + dirVector * newDistance;
				float sqrMagnitude = (newPosition - closedPosition).sqrMagnitude;
				if (sqrMagnitude > openSqrMagnitude)
				{
					newPosition = openPosition;
					CurrentState = State.Open;
				}
				transform.position = newPosition;
			}
			break;
		}
	}

	public void SetAngle (int angle)
	{
		Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
		Quaternion forwardRotation = Quaternion.LookRotation(Vector3.left);
		Quaternion finalRotation = rotation * forwardRotation;
		dirVector = finalRotation * Vector3.forward;
	}

	public void SetBounds(Bounds swBounds)
	{
		bounds = swBounds;
		closedPosition = cTransform.position;
		Vector3 extension = new Vector3(dirVector.x * ((2 * bounds.extents.x) - Lip), dirVector.y * ((2 * bounds.extents.y) - Lip), dirVector.z * ((2 * bounds.extents.z) - Lip));
		openPosition = closedPosition + extension;
		openSqrMagnitude = (openPosition - closedPosition).sqrMagnitude;
	}
	public void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
	{
		if (Dead)
			return;
	}
	public void Impulse(Vector3 direction, float force)
	{

	}
	public void JumpPadDest(Vector3 destination)
	{

	}
}
