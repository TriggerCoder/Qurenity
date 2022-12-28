using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.MultiAudioListener;

public class PlayerWeapon : MonoBehaviour
{
	public Vector3 Offset = new Vector3(.2f, -.2f, .2f);
	public Vector3 MuzzleOffset = new Vector3(-0.5f, 0f, 0);

	[HideInInspector]
	public GameObject muzzleObject = null;

	public string UIModelName;
	public string CompleteModelName;
	public string MuzzleModelName;
	public bool useCrosshair = true;
	public virtual float avgDispersion { get { return .02f; } } //tan(2.3º) / 2
	public virtual float maxDispersion { get { return .03f; } } //tan(3.4º) / 2

	public int DamageMin = 5;
	public int DamageMax = 15;

	public float swapSpeed = 6f;

	public Vector2 Sensitivity = new Vector2(4f, 3f);
	public float rotateSpeed = 4f;
	public float maxTurn = 3f;

	Vector2 MousePosition;
	Vector2 oldMousePosition = Vector2.zero;

	public string[] _sounds = new string[0];
	[HideInInspector]
	public AudioClip[] Sounds = new AudioClip[0];

	protected MultiAudioSource audioSource;

	public PlayerWeapon Instance;
	[HideInInspector]
	public PlayerInfo playerInfo;

	public float LowerOffset = -.3f;
	public float LowerAmount = 1f;

	public int Noise = 0;

	public bool bopActive = false;
	float interp;
	float Coef;
	public float vBob = .08f;

	public bool putAway = false;
	public void PutAway() { if (Instance != null) Instance.putAway = true; }


	[HideInInspector]
	public bool cooldown = false;
	public bool useCooldown = false;
	public float muzzleLightTime = 5f;
	public float cooldownTime = 0f;


	public float _fireRate = .4f;
	[HideInInspector]
	public float fireTime = 0f;

	public float _muzzleTime = .1f;
	[HideInInspector]
	public float muzzleTimer = 0f;

	protected Light muzzleLight;

	protected float coolTimer = 0f;

	Transform cTransform;
	void Awake()
	{
		if (Instance != null)
			Destroy(Instance.gameObject);

		Instance = this;

		muzzleLight = GetComponentInChildren<Light>();
		audioSource = GetComponent<MultiAudioSource>();

		Sounds = new AudioClip[_sounds.Length];
		for (int i = 0; i < _sounds.Length; i++)
			Sounds[i] = SoundLoader.LoadSound(_sounds[i]);

		if (!GameOptions.UseMuzzleLight)
			if (muzzleLight != null)
			{
				muzzleLight.enabled = false;
			}
	}
	public void Init(PlayerInfo p)
	{
		playerInfo = p;
		transform.SetParent(playerInfo.WeaponHand);
		playerInfo.WeaponHand.localPosition = Offset;
		MD3 model = ModelsManager.GetModel(UIModelName);
		if (model != null)
		{
			if (model.readyMeshes.Count == 0)
				Mesher.GenerateModelFromMeshes(model,gameObject);
			else
				Mesher.FillModelFromProcessedData(model,gameObject);

			if (playerInfo.playerThing.avatar != null)
				playerInfo.playerThing.avatar.LoadWeapon(model, CompleteModelName, MuzzleModelName);
		}

		foreach (MD3Tag tag in model.tags)
		{
			if (string.Equals(tag.name, "tag_flash"))
			{
				MuzzleOffset = tag.origin;
				break;
			}
		}

		cTransform = gameObject.transform;
		for (int d = 0; d < cTransform.childCount; d++)
		{
			cTransform.GetChild(d).gameObject.transform.localPosition = Vector3.zero;
			cTransform.GetChild(d).gameObject.transform.localScale = Vector3.one;
		}

		if (!string.IsNullOrEmpty(MuzzleModelName))
		{
			muzzleObject = new GameObject("Muzzle");
			muzzleObject.layer = GameManager.UILayer;
			muzzleObject.transform.SetParent(transform);
			muzzleObject.transform.localPosition = MuzzleOffset;
			model = ModelsManager.GetModel(MuzzleModelName, true);
			if (model != null)
			{
				if (model.readyMeshes.Count == 0)
					Mesher.GenerateModelFromMeshes(model, muzzleObject, true);
				else
					Mesher.FillModelFromProcessedData(model, muzzleObject);
			}
			muzzleObject.SetActive(false);
		}

		oldMousePosition.x = Input.GetAxis("Mouse X");
		oldMousePosition.y = Input.GetAxis("Mouse Y");

		playerInfo.playerHUD.HUDUpdateAmmoNum();
		OnInit();
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (GameOptions.UseMuzzleLight)
			if (muzzleLight != null)
			{
				if (muzzleLight.enabled)
				{
					muzzleLight.intensity = Mathf.Max(Mathf.Lerp(muzzleLight.intensity, 0, Time.deltaTime * muzzleLightTime), 0);
					if (muzzleLight.intensity <= 0.1f)
					{
						muzzleLight.intensity = 0;
						muzzleLight.enabled = false;
						if (muzzleObject != null)
							if (muzzleObject.activeSelf)
							{
								muzzleObject.SetActive(false);
								playerInfo.playerThing.avatar.MuzzleFlashSetActive(false);
							}
					}
					else if (muzzleLight.intensity <= 0.8f)
						if (muzzleObject != null)
							if (muzzleObject.activeSelf)
							{
								muzzleObject.SetActive(false);
								playerInfo.playerThing.avatar.MuzzleFlashSetActive(false);
							}
				}
			}

		if (fireTime <= 0f)
		{
			if ((useCooldown) && (cooldown))
			{
				coolTimer += Time.deltaTime;
				if (coolTimer >= cooldownTime)
				{
					coolTimer = 0;
					cooldown = false;
				}
			}
		}
		else
		{
			fireTime -= Time.deltaTime;
			bopActive = false;
			if (fireTime <= 0)
			{
				coolTimer = 0;
				if (useCooldown)
					cooldown = true;
				else
				{
				}
			}
			else
			{
				coolTimer += Time.deltaTime;
			}
		}

		MousePosition.x = Input.GetAxis("Mouse X") + playerInfo.playerControls.playerVelocity.x;
		MousePosition.y = Input.GetAxis("Mouse Y") + playerInfo.playerControls.playerVelocity.y;

		ApplyRotation(GetRotation((MousePosition - oldMousePosition) * Sensitivity));
		oldMousePosition = Vector2.Lerp(oldMousePosition,MousePosition, rotateSpeed * Time.deltaTime);

		if (putAway)
		{
			if (playerInfo.playerThing.avatar != null)
				playerInfo.playerThing.avatar.UnloadWeapon();

			LowerAmount = Mathf.Lerp(LowerAmount, 1, Time.deltaTime * swapSpeed);
			if (LowerAmount > .99f)
				DestroyAfterTime.DestroyObject(gameObject);
		}
		else
			LowerAmount = Mathf.Lerp(LowerAmount, 0, Time.deltaTime * swapSpeed);

		if (bopActive)
			interp = Mathf.Lerp(interp, 1, Time.deltaTime * 2);
		else
			interp = Mathf.Lerp(interp, 0, Time.deltaTime * 4);
		Coef = Mathf.Abs(Mathf.Cos(Time.time * 3)) * interp;
		transform.localPosition = new Vector3(0, -Coef * vBob + LowerOffset * LowerAmount, 0);

		OnUpdate();
	}
	Quaternion GetRotation(Vector2 mouse)
	{
		mouse = Vector2.ClampMagnitude(mouse, maxTurn);

		Quaternion rotX = Quaternion.AngleAxis(mouse.y, Vector3.forward);
		Quaternion rotY = Quaternion.AngleAxis(mouse.x, Vector3.up);

		Quaternion rotZ = GetRotate();

		playerInfo.playerThing.avatar.RotateBarrel(rotZ, rotateSpeed * Time.deltaTime);

		Quaternion targetRot = rotX * rotY * rotZ;

		return targetRot;
	}

	void ApplyRotation(Quaternion targetRot)
	{
		transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, rotateSpeed * Time.deltaTime);
	}

	protected virtual Quaternion GetRotate()
	{
		return Quaternion.identity;
	}
	protected virtual void OnUpdate() { }
	protected virtual void OnInit() { }
	public virtual bool Fire()
	{
		return false;
	}
	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
	public Vector2 GetDispersion()
	{
		Vector2 dispersion = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
		float dx = Mathf.Abs(dispersion.x);
		float dy = Mathf.Abs(dispersion.y);

		if (dx == 1)
			return dispersion * maxDispersion;
		if (dy == 1)
			return dispersion * maxDispersion;
		if (dx + dy <= 1)
			return dispersion * avgDispersion;
		if (dx * dx + dy * dy <= 1)
			return dispersion * avgDispersion;
		return dispersion * maxDispersion;
	}
}
