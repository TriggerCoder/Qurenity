using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpriteAnimation : MonoBehaviour
{
	public float frameTime = 1f;
	public string[] frames;
	private Texture[] _frames;

	public bool isOmni = true;
	public bool isTransparent;
	public Color color = Color.white;
	public Vector2 pivot = new Vector2(.5f, .5f);
	public float scale = 1;

	public bool oscillates;
	public int direction = 1;
	[HideInInspector]
	private Vector2[] size;

	MeshRenderer mr;
	MeshFilter meshFilter;

	//Cached Transform
	Transform cTransform;
	void Start()
	{
		mr = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
		if ((frames.Length == 0) || (string.IsNullOrEmpty(frames[0])))
		{
			Destroy(gameObject);
			return;
		}
		cTransform = transform;

		_frames = new Texture[frames.Length];
		size = new Vector2[frames.Length];
		for (int i = 0; i < frames.Length; i++)
		{
			if (TextureLoader.HasTexture(frames[i]))
				_frames[i] = TextureLoader.Instance.GetTexture(frames[i]);
			else
			{
				TextureLoader.AddNewTexture(frames[i], true);
				_frames[i] = TextureLoader.Instance.GetTexture(frames[i]);
			}
			size[i].x = _frames[i].width * GameManager.sizeDividor * scale;
			size[i].y = _frames[i].height * GameManager.sizeDividor * scale;
		}
		CreateBillboard();

		if (!isOmni)
			if ((frames.Length == 1) || (frameTime == 0))
				enabled = false;
	}

	bool lastflip = false;
	int index = 0;

	void OnEnable()
	{
		lastflip = false;
		index = 0;
	}
	private void CreateBillboard()
	{
		if (!string.IsNullOrEmpty(frames[0]))
		{
			meshFilter.mesh = Mesher.CreateBillboardMesh(1, 1, pivot.x, pivot.y);
			if (isOmni)
				mr.sharedMaterial = Instantiate(MaterialManager.Instance.billBoardMaterial);
			else
				mr.sharedMaterial = Instantiate(MaterialManager.Instance.spriteMaterial);

			mr.material.SetFloat("_ScaleX", size[0].x);
			mr.material.SetFloat("_ScaleY", size[0].y);
			mr.material.SetColor(MaterialManager.colorPropertyId, color);
			mr.material.SetTexture(MaterialManager.opaqueTexPropertyId, _frames[0]);
		}
	}

	void Update()
	{
		if (isOmni)
			mr.enabled = true;

		if (GameManager.Paused)
			return;

		if ((frames.Length == 1) || (frameTime == 0))
			return;

		bool flip = Time.time % (frameTime + .15f) > frameTime;
		if (flip != lastflip)
		{
			lastflip = flip;
			index += direction;

			if (index >= _frames.Length)
				if (oscillates)
				{
					direction = -1;
					index--;
				}
				else
					index = 0;

			if (index < 0)
				if (oscillates)
				{
					direction = 1;
					index++;
				}
				else
					index = _frames.Length - 1;

			mr.material.SetFloat("_ScaleX", size[index].x);
			mr.material.SetFloat("_ScaleY", size[index].y);
			mr.material.SetTexture(MaterialManager.opaqueTexPropertyId, _frames[index]);
		}
	}
/*
	void OnWillRenderObject()
	{
		Transform camera = Camera.current.transform;
		if (CanLineToCamera(camera))
		{
			//transform.LookAt(Camera.current.transform);
			// This was changed in order to be able to keep the same roll angle but
			// change the yaw and pitch angles to look at the camera position
			// Calculate the roll angle of the object
			float rollAngle = cTransform.rotation.eulerAngles.z;
			Vector3 direction = camera.position - cTransform.position;

			// Set the rotation of the object to look at the camera
			cTransform.rotation = Quaternion.LookRotation(direction);
			// Rotate the object around its new forward axis, while maintaining the original roll angle
			cTransform.RotateAround(cTransform.position, cTransform.forward, rollAngle);
		}
		else
			mr.enabled = false;
	}

	public bool CanLineToCamera(Transform camera)
	{
		Vector3 from = cTransform.position;
		Vector3 toCamera = camera.position;

		if (Physics.Linecast(from, toCamera, ((1 << GameManager.MapMeshesLayer) |
												(1 << GameManager.ColliderLayer) |
												(1 << GameManager.CombinesMapMeshesLayer) |
												(1 << GameManager.MapMeshesPlayer1Layer) |
												(1 << GameManager.MapMeshesPlayer2Layer) |
												(1 << GameManager.MapMeshesPlayer3Layer) |
												(1 << GameManager.MapMeshesPlayer4Layer)), QueryTriggerInteraction.Ignore))
			return false;
		return true;
	}
*/
}
