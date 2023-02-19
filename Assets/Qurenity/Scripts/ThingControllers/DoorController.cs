using System.Collections;
using System.Collections.Generic;
using Assets.MultiAudioListener;
using UnityEngine;

public class DoorController : MonoBehaviour, Damageable
{
	public bool doorOn = false;
	public bool playSoundClose = true;
	public string startSound;
	public string endSound;
	public TriggerController tc;
	public int damage = 4;
	public bool crusher = false;
	private int hitpoints = 0;
	private float lip;
	private Bounds bounds;
	private float speed;

	private float wait = 2;
	private float openWaitTime = 0;
	private Vector3 openPosition, closedPosition;
	private Vector3 dirVector = Vector3.right;

	public virtual float waitTime { get { return wait; } set { wait = value; } }
	public virtual bool Activated { get { return doorOn; } set { doorOn = value; } }
	public int Hitpoints { get { return hitpoints; } }
	public bool Dead { get { return hitpoints <= 0; } }
	public bool Bleed { get { return false; } }
	public BloodType BloodColor { get { return BloodType.None; } }

	Transform cTransform;
	public MultiAudioSource audioSource;
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
			{
				if (!string.IsNullOrEmpty(endSound))
				{
					audioSource.AudioClip = SoundLoader.LoadSound(endSound);
					audioSource.Play();
				}
				openWaitTime = waitTime;
			}
			else if (value == State.Opening)
			{
				if (!string.IsNullOrEmpty(startSound))
				{
					audioSource.AudioClip = SoundLoader.LoadSound(startSound);
					audioSource.Play();
				}
				Activated = true;
				enabled = true;
			}
			else if (value == State.Closing)
			{
				if (playSoundClose)
					if (!string.IsNullOrEmpty(startSound))
					{
						audioSource.AudioClip = SoundLoader.LoadSound(startSound);
						audioSource.Play();
					}
				enabled = true;
			}
			else if (value == State.Closed)
			{
				if (playSoundClose)
					if (!string.IsNullOrEmpty(endSound))
					{
						audioSource.AudioClip = SoundLoader.LoadSound(endSound);
						audioSource.Play();
					}
				Activated = false;
				enabled = false;
			}
			currentState = value;
		}
	}
	public void Init(int angle, int hp, int sp, float wait, int openlip, Bounds swBounds, int dmg = 0)
	{
		cTransform = transform;

		if (angle != 0)
			SetAngle(angle);

		hitpoints = hp;
		speed = sp * GameManager.sizeDividor;
		waitTime = wait;
		lip = openlip * GameManager.sizeDividor;
		damage = dmg;
		if (dmg > 100)
			crusher = true;
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
	public void SetAngle(int angle)
	{
		if (angle < 0)
		{
			if (angle == -1)
				dirVector = Vector3.up;
			else
				dirVector = Vector3.down;
			return;
		}
		Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.down);
		Quaternion forwardRotation = Quaternion.LookRotation(Vector3.left);
		Quaternion finalRotation = rotation * forwardRotation;
		dirVector = finalRotation * Vector3.forward;
	}

	public void SetBounds(Bounds swBounds)
	{
		bounds = swBounds;
		closedPosition = cTransform.position;
		Vector3 extension = new Vector3(dirVector.x * ((2 * bounds.extents.x) - lip), dirVector.y * ((2 * bounds.extents.y) - lip), dirVector.z * ((2 * bounds.extents.z) - lip));
		openPosition = closedPosition + extension;
		openSqrMagnitude = (openPosition - closedPosition).sqrMagnitude;
	}
	public virtual void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null)
	{
		if (Dead)
			return;

		if (!Activated)
			CurrentState = State.Opening;
	}
	public void Impulse(Vector3 direction, float force)
	{

	}
}