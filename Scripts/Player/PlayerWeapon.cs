using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.MultiAudioListener;

public class PlayerWeapon : MonoBehaviour
{
	public Vector3 Offset = new Vector3(.2f, -.2f, .2f);
	public Vector3 MuzzleOffset = new Vector3(-0.5f, 0f, 0);

	[HideInInspector]
	public GameObject muzzleObject = null;

	public string ModelName;
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

	private void FillModelFromProcessedData(MD3 model)
	{
		for (int i = 0; i < model.readyMeshes.Count; i++)
		{
			GameObject modelObject;
			if (i == 0)
				modelObject = gameObject;
			else
			{
				modelObject = new GameObject("Mesh_"+i);
				modelObject.layer = gameObject.layer;
				modelObject.transform.SetParent(transform);
			}

			MeshRenderer mr = modelObject.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
			meshFilter.mesh = model.readyMeshes[i];
			mr.sharedMaterial = model.readyMaterial[i];
		}
	}
	private void GenerateModelFromMeshes(MD3 model)
	{
		var baseGroups = model.meshes.GroupBy(x => new { x.numSkins });
		int groupId = 0;
		foreach (var baseGroup in baseGroups)
		{
			MD3Mesh[] baseGroupMeshes = baseGroup.ToArray();
			if (baseGroupMeshes.Length == 0)
				continue;

			var groupMeshes = baseGroupMeshes.GroupBy(x => new { x.skins[0].name });
			foreach (var groupMesh in groupMeshes)
			{
				MD3Mesh[] meshes = groupMesh.ToArray();
				if (meshes.Length == 0)
					continue;

				string Name = "Mesh_";
				CombineInstance[] combine = new CombineInstance[meshes.Length];
				for (var i = 0; i < combine.Length; i++)
				{
					combine[i].mesh = Mesher.GenerateModelMesh(meshes[i]);
					Name += "_" + meshes[i].name;
				}

				var mesh = new Mesh();
				mesh.name = Name;
				mesh.CombineMeshes(combine, true, false, false);

				GameObject modelObject;
				if (groupId == 0)
					modelObject = gameObject;
				else
				{
					modelObject = new GameObject();
					modelObject.layer = gameObject.layer;
					modelObject.transform.SetParent(transform);
				}

				MeshRenderer mr = modelObject.AddComponent<MeshRenderer>();
				MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
				meshFilter.mesh = mesh;

				Material material = MaterialManager.GetMaterials(meshes[0].skins[0].name, -1);

				mr.sharedMaterial = material;
				model.readyMeshes.Add(mesh);
				model.readyMaterial.Add(material);
				groupId++;
			}
		}
	}

	public void Init(PlayerInfo p)
	{
		playerInfo = p;
		transform.SetParent(playerInfo.WeaponHand);
		playerInfo.WeaponHand.localPosition = Offset;
		MD3 model = ModelsManager.GetModel(ModelName);
		if (model != null)
		{
			if (model.readyMeshes.Count == 0)
				GenerateModelFromMeshes(model);
			else
				FillModelFromProcessedData(model);
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
				Mesher.GenerateModelObject(model, muzzleObject, true);
			muzzleObject.SetActive(false);
//			DisableAfterTime disableObject = muzzleObject.AddComponent<DisableAfterTime>();
//			disableObject._lifeTime = _muzzleTime;
		}

		oldMousePosition.x = Input.GetAxis("Mouse X");
		oldMousePosition.y = Input.GetAxis("Mouse Y");

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
					}
					else if (muzzleLight.intensity <= 0.8f)
						if (muzzleObject != null)
							if (muzzleObject.activeSelf)
								muzzleObject.SetActive(false);

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
