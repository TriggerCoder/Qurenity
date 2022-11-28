using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	public PlayerCamera Instance;
	public GameObject Camera;
	public Camera SkyboxCamera = null;
	public Camera SkyholeCamera = null;
	public Camera MainCamera = null;
	public Camera UICamera = null;

	public PlayerControls playerControls;

	void Awake()
	{
		Instance = this;
		MainCamera = Camera.GetComponent<Camera>();

		foreach (Transform child in Camera.transform)
		{
			if (child.gameObject.name == "SkyholeCamera")
				SkyholeCamera = child.gameObject.GetComponent<Camera>();
			if (child.gameObject.name == "SkyboxCamera")
				SkyboxCamera = child.gameObject.GetComponent<Camera>();
			if (child.gameObject.name == "UICamera")
				UICamera = child.gameObject.GetComponent<Camera>();
		}

		playerControls = GetComponentInParent<PlayerControls>();

		SkyholeCamera.cullingMask = ((1 << (GameManager.CombinesMapMeshesLayer & 0x1f)) |
											(1 << (GameManager.MapMeshesPlayer1Layer & 0x1f)));
	}

	void Update()
	{

		if (Camera.activeSelf == false)
			return;

		//look up and down
		transform.localRotation = Quaternion.Euler(playerControls.viewDirection.x, 0, 0);
	}
}
