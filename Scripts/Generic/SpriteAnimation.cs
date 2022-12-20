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

	public bool isTransparent;

	public Vector2 pivot = new Vector2(.5f, .5f);
	public float scale = 1;

	public bool oscillates;
	public int direction = 1;
	[HideInInspector]
	private Vector2[] size;

	MeshRenderer mr;
	MeshFilter meshFilter;
	MaterialPropertyBlock materialParameters;

	void Awake()
	{
		mr = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
		materialParameters = new MaterialPropertyBlock();
		if ((frames.Length == 0) || (string.IsNullOrEmpty(frames[0])))
		{
			Destroy(gameObject);
			return;
		}
	}

	void Start()
	{
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
			mr.sharedMaterial = Instantiate(MaterialManager.Instance.billBoardMaterial);
			mr.GetPropertyBlock(materialParameters);
			materialParameters.SetFloat("_ScaleX", size[0].x);
			materialParameters.SetFloat("_ScaleY", size[0].y);
			materialParameters.SetTexture("_MainTex", _frames[0]);
			mr.SetPropertyBlock(materialParameters);
		}
	}

	void Update()
	{
		if (GameManager.Paused)
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

			mr.GetPropertyBlock(materialParameters);
			materialParameters.SetFloat("_ScaleX", size[index].x);
			materialParameters.SetFloat("_ScaleY", size[index].y);
			materialParameters.SetTexture("_MainTex", _frames[index]);
			mr.SetPropertyBlock(materialParameters);
		}
	}
	void OnWillRenderObject()
	{
		transform.LookAt(Camera.current.transform);
	}
}
