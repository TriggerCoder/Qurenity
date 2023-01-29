using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	public PlayerCamera Instance;
	public GameObject MainCamera;
	public Camera SkyboxCamera = null;
	public Camera SkyholeCamera = null;
	public Camera UICamera = null;
	public Camera ThirdPerson;

	public PlayerControls playerControls;

	public float yOffset = .35f;
	public float vBob = .002f;
	public float hBob = .002f;

	private float pitch = .002f;
	private float roll = .005f;

	private Transform cTransform;
	private float interp;
	public bool bopActive;
	void Awake()
	{
		Instance = this;

		foreach (Transform child in MainCamera.transform)
		{
			if (child.gameObject.name == "SkyholeCamera")
				SkyholeCamera = child.gameObject.GetComponent<Camera>();
			if (child.gameObject.name == "SkyboxCamera")
				SkyboxCamera = child.gameObject.GetComponent<Camera>();
			if (child.gameObject.name == "UICamera")
				UICamera = child.gameObject.GetComponent<Camera>();
		}

		playerControls = GetComponentInParent<PlayerControls>();

		SkyholeCamera.cullingMask = ((1 << (GameManager.DefaultLayer & 0x1f)) |
										(1 << (GameManager.RagdollLayer & 0x1f)) |
										(1 << (GameManager.CombinesMapMeshesLayer & 0x1f)) |
										(1 << (GameManager.MapMeshesPlayer1Layer & 0x1f)));

		ThirdPerson.cullingMask = ((1 << (GameManager.DefaultLayer & 0x1f)) |
										(1 << (GameManager.ThingsLayer & 0x1f)) |
										(1 << (GameManager.RagdollLayer & 0x1f)) |
										(1 << (GameManager.CombinesMapMeshesLayer & 0x1f)) |
										(1 << (GameManager.MapMeshesPlayer1Layer & 0x1f)));
		cTransform = transform;
	}

	public void UpdateRect(Rect viewRect)
	{
		SkyholeCamera.rect = viewRect;
		SkyboxCamera.rect = viewRect;
		UICamera.rect = viewRect;
		ThirdPerson.rect = viewRect;
	}
	public void ChangeThirdPersonCamera(bool enable)
	{
		ThirdPerson.enabled = enable;
		SkyholeCamera.enabled = !enable;
		UICamera.enabled = !enable;
	}
	void Update()
	{

		if (MainCamera.activeSelf == false)
			return;

		if (GameOptions.HeadBob && bopActive)
			interp = Mathf.Lerp(interp, 1, Time.deltaTime * 5);
		else
			interp = Mathf.Lerp(interp, 0, Time.deltaTime * 6);

		Vector3 position;

		float speed = playerControls.playerVelocity.magnitude;
		float moveSpeed = playerControls.walkSpeed;
		if (playerControls.moveSpeed != playerControls.walkSpeed)
			moveSpeed = playerControls.runSpeed;
		float delta = Mathf.Cos(Time.time * moveSpeed) * hBob * speed * interp;
		if (playerControls.moveSpeed == playerControls.crouchSpeed) //Crouched
			delta *= 5;
		position.x = delta;

		delta = Mathf.Sin(Time.time * moveSpeed) * vBob * speed * interp;
		if (playerControls.moveSpeed == playerControls.crouchSpeed) //Crouched
			delta *= 5;
		position.y = delta;
		//apply bop
		cTransform.localPosition = new Vector3(0, yOffset + position.y, 0);

		//look up and down
		cTransform.localRotation = Quaternion.Euler(playerControls.viewDirection.x, 0, position.x);
	}
}
