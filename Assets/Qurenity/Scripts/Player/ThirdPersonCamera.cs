using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
	public Transform MainCamera;
	public PlayerControls playerControls;
	public Vector3 cameraOffset = new(.28f, .25f, -1.7f);
	public float clipDistance = .5f;
	public float distance = .8f;
	public float turnRate = 510f;

	private Vector3 back;
	private Matrix4x4 originMatrix;
	private Vector3 right;
	private Quaternion rotY;
	private Quaternion rotX;
	private Transform cTransform;

	private RaycastHit[] hits = new RaycastHit[10];

	private void Start()
	{
		originMatrix = MainCamera.localToWorldMatrix;
		back = originMatrix.MultiplyVector(Vector3.back);
		right = originMatrix.MultiplyVector(Vector3.right);
		cTransform = transform;
	}

	void Update()
	{
		float yaw = playerControls.viewDirection.y;
		float pitch = playerControls.viewDirection.x;
		rotY = Quaternion.RotateTowards(rotY, Quaternion.AngleAxis(yaw, Vector3.up), Time.deltaTime * turnRate);
		rotX = Quaternion.RotateTowards(rotX, Quaternion.AngleAxis(pitch, right), Time.deltaTime * turnRate);

		Vector3 shoulderOffset = rotY * originMatrix.MultiplyVector(cameraOffset);
		Vector3 armOffset = rotY * (rotX * (distance * back));
		Vector3 shoulderPosition = MainCamera.position + shoulderOffset;
		Vector3 cameraPosition = shoulderPosition + armOffset;
		Vector3 dir = cameraPosition - MainCamera.position;
		float mag = dir.magnitude;
		dir.Normalize();

		RaycastHit hit;
		if (Physics.Raycast(MainCamera.position, dir, out hit, mag, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore))
		{
			float nearest = float.MaxValue;
			int max = Physics.SphereCastNonAlloc(MainCamera.position, .2f, dir, hits, mag, (1 << GameManager.ColliderLayer), QueryTriggerInteraction.Ignore);

			if (max > hits.Length)
				max = hits.Length;

			for (int i = 0; i < max; i++)
			{
				hit = hits[i];
				if (hit.distance < nearest)
				{
					nearest = hit.distance;
					cameraPosition = hit.point + (dir * .5f);
				}
			}
		}

		cTransform.position = cameraPosition;
		cTransform.forward = MainCamera.forward;
	}
}