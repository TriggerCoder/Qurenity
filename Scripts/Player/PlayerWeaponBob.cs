using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponBob : MonoBehaviour
{
	public Vector2 Sensitivity = new Vector2(4f, 3f);
	public float rotateSpeed = 4f;
	public float maxTurn = 3f;
	Vector2 MousePosition;
	Vector2 oldMousePosition = Vector2.zero;
	public float currentRotSpeed = 0;
	PlayerControls playerControls;
	void Awake()
	{
		playerControls = transform.root.GetComponent<PlayerControls>();
	}
	void Start()
	{
		oldMousePosition.x = Input.GetAxis("Mouse X");
		oldMousePosition.y = Input.GetAxis("Mouse Y");
	}
	void Update()
	{
		if (GameManager.Paused)
			return;

		MousePosition.x = Input.GetAxis("Mouse X") + playerControls.playerVelocity.x;
		MousePosition.y = Input.GetAxis("Mouse Y") + playerControls.playerVelocity.y;

		ApplyRotation(GetRotation((MousePosition - oldMousePosition) * Sensitivity));
		oldMousePosition = Vector2.Lerp(oldMousePosition,MousePosition, rotateSpeed * Time.deltaTime);

	}
	Quaternion GetRotation(Vector2 mouse)
	{
		mouse = Vector2.ClampMagnitude(mouse, maxTurn);

		Quaternion rotX = Quaternion.AngleAxis(mouse.y, Vector3.forward);
		Quaternion rotY = Quaternion.AngleAxis(mouse.x, Vector3.up);

		if (Input.GetMouseButton(0))
		{
			currentRotSpeed += 100 * rotateSpeed * Time.deltaTime;
			if (currentRotSpeed < -180)
				currentRotSpeed += 360;
			if (currentRotSpeed > 180)
				currentRotSpeed -= 360;
		}

		Quaternion rotZ = Quaternion.AngleAxis(-currentRotSpeed, Vector3.right);

		Quaternion targetRot = rotX * rotY * rotZ;

		return targetRot;
	}

	void ApplyRotation(Quaternion targetRot)
	{
		transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, rotateSpeed * Time.deltaTime);
	}
}
